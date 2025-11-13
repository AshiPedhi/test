using ChunaVR.Core;
using UnityEngine;
using Oculus.Interaction;

namespace ChunaVR.PoseData
{
    /// <summary>
    /// HandPose 디버그 시각화 전용 클래스
    /// Gizmo 그리기 및 디버그 정보 표시
    /// </summary>
    public class HandPoseDebugVisualizer
    {
        private HandPoseConfig config;
        private Transform referencePoint;

        // 디버그용 위치 저장
        private Vector3 leftReplayTargetPosition;
        private Vector3 rightReplayTargetPosition;
        private Vector3 leftPlayerCurrentPosition;
        private Vector3 rightPlayerCurrentPosition;

        private float leftHandPositionError;
        private float rightHandPositionError;

        public HandPoseDebugVisualizer(HandPoseConfig config, Transform referencePoint = null)
        {
            this.config = config;
            this.referencePoint = referencePoint;
        }

        /// <summary>
        /// 디버그 위치 업데이트 (왼손)
        /// </summary>
        public void UpdateLeftHandDebugInfo(
            Vector3 replayTargetPos,
            Vector3 playerCurrentPos,
            float positionError)
        {
            leftReplayTargetPosition = replayTargetPos;
            leftPlayerCurrentPosition = playerCurrentPos;
            leftHandPositionError = positionError;
        }

        /// <summary>
        /// 디버그 위치 업데이트 (오른손)
        /// </summary>
        public void UpdateRightHandDebugInfo(
            Vector3 replayTargetPos,
            Vector3 playerCurrentPos,
            float positionError)
        {
            rightReplayTargetPosition = replayTargetPos;
            rightPlayerCurrentPosition = playerCurrentPos;
            rightHandPositionError = positionError;
        }

        /// <summary>
        /// Gizmo 그리기
        /// </summary>
        public void DrawGizmos(bool isLeftPlaying, bool isRightPlaying)
        {
            if (!config.showDebugGizmos || !Application.isPlaying)
                return;

            // 기준점 표시
            if (referencePoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(referencePoint.position, 0.05f);
                Gizmos.DrawLine(referencePoint.position, referencePoint.position + Vector3.up * 0.1f);
            }

            // 왼손 위치 표시
            if (isLeftPlaying && config.showPositionLines)
            {
                DrawHandGizmo(
                    leftReplayTargetPosition,
                    leftPlayerCurrentPosition,
                    leftHandPositionError,
                    Color.blue,
                    Color.green,
                    "왼손"
                );
            }

            // 오른손 위치 표시
            if (isRightPlaying && config.showPositionLines)
            {
                DrawHandGizmo(
                    rightReplayTargetPosition,
                    rightPlayerCurrentPosition,
                    rightHandPositionError,
                    Color.cyan,
                    Color.green,
                    "오른손"
                );
            }
        }

        /// <summary>
        /// 손 Gizmo 그리기
        /// </summary>
        private void DrawHandGizmo(
            Vector3 targetPos,
            Vector3 currentPos,
            float error,
            Color targetColor,
            Color currentColor,
            string handName)
        {
            // 목표 위치 (리플레이)
            Gizmos.color = targetColor;
            Gizmos.DrawSphere(targetPos, config.gizmoSphereSize);

            // 현재 위치 (플레이어)
            Gizmos.color = currentColor;
            Gizmos.DrawSphere(currentPos, config.gizmoSphereSize);

            // 연결선
            bool isPassed = error <= config.handPositionThreshold;
            Gizmos.color = isPassed ? Color.green : Color.red;
            Gizmos.DrawLine(targetPos, currentPos);

#if UNITY_EDITOR
            // 오차 텍스트 (에디터에서만)
            UnityEditor.Handles.Label(
                (targetPos + currentPos) / 2f + Vector3.up * 0.05f,
                $"{handName} 오차: {error * 100f:F1}cm"
            );
#endif
        }

        /// <summary>
        /// 콘솔 로그 출력
        /// </summary>
        public void LogComparison(
            string handName,
            float similarity,
            float positionError,
            float rotationError,
            bool passed,
            int frameIndex)
        {
            if (!config.showDebugGizmos)
                return;

            if (Time.frameCount % 30 != 0) // 0.5초마다
                return;

            string status = passed ? "<color=green>통과</color>" : "<color=red>실패</color>";

            Debug.Log($"[HandPoseDebug] {handName} 프레임 {frameIndex}\n" +
                     $"상태: {status}\n" +
                     $"조인트 유사도: {similarity * 100:F1}%\n" +
                     $"위치 오차: {positionError * 100:F1}cm\n" +
                     $"회전 오차: {rotationError:F1}°");
        }

        /// <summary>
        /// 진행 상황 로그
        /// </summary>
        public void LogProgress(string handName, int currentFrame, int totalFrames, float progress)
        {
            if (Time.frameCount % 60 != 0) // 1초마다
                return;

            Debug.Log($"[HandPoseDebug] {handName} 진행: {currentFrame}/{totalFrames} ({progress * 100:F0}%)");
        }

        /// <summary>
        /// 재생 시작 로그
        /// </summary>
        public void LogPlaybackStart(string fileName, int totalFrames, bool hasReferencePoint)
        {
            Debug.Log($"<color=cyan>[HandPoseDebug] 재생 시작</color>\n" +
                     $"파일: {fileName}\n" +
                     $"총 프레임: {totalFrames}\n" +
                     $"기준점: {(hasReferencePoint ? "있음" : "없음")}\n" +
                     $"손 위치 비교: {(config.compareHandPosition ? "ON" : "OFF")}\n" +
                     $"손 회전 비교: {(config.compareHandRotation ? "ON" : "OFF")}");
        }

        /// <summary>
        /// 재생 완료 로그
        /// </summary>
        public void LogPlaybackComplete(string handName, float averageSimilarity)
        {
            Debug.Log($"<color=cyan>[HandPoseDebug] {handName} 재생 완료!</color>\n" +
                     $"평균 유사도: {averageSimilarity * 100:F1}%");
        }

        /// <summary>
        /// 상태 텍스트 생성
        /// </summary>
        public string GetStatusText(
            bool isLeftPlaying,
            bool isRightPlaying,
            int leftFrame,
            int rightFrame,
            int totalFrames,
            float leftSimilarity,
            float rightSimilarity,
            float leftPosError,
            float rightPosError,
            float leftRotError,
            float rightRotError)
        {
            string leftStatus = isLeftPlaying ? $"재생중 ({leftFrame + 1}/{totalFrames})" : "중지";
            string rightStatus = isRightPlaying ? $"재생중 ({rightFrame + 1}/{totalFrames})" : "중지";

            return $"왼손: {leftStatus}\n" +
                   $"  조인트 유사도: {leftSimilarity * 100:F1}%\n" +
                   $"  위치 오차: {leftPosError * 100:F1}cm\n" +
                   $"  회전 오차: {leftRotError:F1}°\n" +
                   $"오른손: {rightStatus}\n" +
                   $"  조인트 유사도: {rightSimilarity * 100:F1}%\n" +
                   $"  위치 오차: {rightPosError * 100:F1}cm\n" +
                   $"  회전 오차: {rightRotError:F1}°";
        }

        /// <summary>
        /// 설정 업데이트
        /// </summary>
        public void UpdateConfig(HandPoseConfig newConfig)
        {
            config = newConfig;
        }

        /// <summary>
        /// 기준점 설정
        /// </summary>
        public void SetReferencePoint(Transform reference)
        {
            referencePoint = reference;
        }
    }
}
