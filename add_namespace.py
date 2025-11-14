#!/usr/bin/env python3
"""
C# 파일에 네임스페이스를 추가하는 스크립트
"""
import sys
import re

def add_namespace(file_path, namespace, additional_usings=None):
    """
    C# 파일에 네임스페이스를 추가하고 들여쓰기를 조정
    """
    # 여러 인코딩 시도
    lines = None
    for encoding in ['utf-8', 'utf-8-sig', 'cp949', 'euc-kr', 'latin-1']:
        try:
            with open(file_path, 'r', encoding=encoding) as f:
                lines = f.readlines()
            break
        except UnicodeDecodeError:
            continue

    if lines is None:
        print(f"❌ {file_path} - 인코딩을 읽을 수 없습니다")
        return

    # using 문 끝 위치 찾기
    using_end_index = 0
    for i, line in enumerate(lines):
        if line.strip().startswith('using '):
            using_end_index = i

    # 주석과 클래스 정의 시작 위치 찾기
    class_start_index = 0
    for i in range(using_end_index + 1, len(lines)):
        if lines[i].strip().startswith('public class') or \
           lines[i].strip().startswith('public enum') or \
           lines[i].strip().startswith('public interface') or \
           lines[i].strip().startswith('public struct') or \
           lines[i].strip().startswith('internal class') or \
           lines[i].strip().startswith('['):
            class_start_index = i
            # 주석을 포함해야 하므로 이전 줄들을 확인
            # 클래스 바로 위의 주석을 찾기
            temp_index = i - 1
            while temp_index > using_end_index:
                stripped = lines[temp_index].strip()
                if stripped.startswith('///') or stripped.startswith('//') or \
                   stripped.startswith('/*') or stripped.startswith('*') or \
                   stripped == '' or stripped.startswith('['):
                    temp_index -= 1
                else:
                    break
            class_start_index = temp_index + 1
            break

    # 새 내용 구성
    new_lines = []

    # 1. using 문들 추가
    for i in range(using_end_index + 1):
        new_lines.append(lines[i])

    # 2. 추가 using 문 추가
    if additional_usings:
        for using in additional_usings:
            new_lines.append(f'using {using};\n')

    # 3. 빈 줄 추가
    new_lines.append('\n')

    # 4. 네임스페이스 선언 추가
    new_lines.append(f'namespace {namespace}\n')
    new_lines.append('{\n')

    # 5. 나머지 내용을 들여쓰기하여 추가 (using 이후부터)
    for i in range(using_end_index + 1, len(lines)):
        line = lines[i]
        # 빈 줄이 아니면 4 스페이스 들여쓰기 추가
        if line.strip():
            new_lines.append('    ' + line)
        else:
            new_lines.append(line)

    # 6. 네임스페이스 닫는 괄호 추가
    new_lines.append('}\n')

    # 파일 쓰기
    with open(file_path, 'w', encoding='utf-8') as f:
        f.writelines(new_lines)

    print(f"✓ {file_path} - namespace {namespace} 추가 완료")

if __name__ == '__main__':
    if len(sys.argv) < 3:
        print("사용법: python add_namespace.py <파일경로> <네임스페이스> [추가using1] [추가using2] ...")
        sys.exit(1)

    file_path = sys.argv[1]
    namespace = sys.argv[2]
    additional_usings = sys.argv[3:] if len(sys.argv) > 3 else None

    add_namespace(file_path, namespace, additional_usings)
