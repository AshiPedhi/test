using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 시나리오 동작 인터페이스
/// 각 단계별 실행 동작을 정의
/// </summary>
public interface IScenarioAction
{
    void Execute(SubStepData subStep);
    void OnComplete();
}

/// <summary>
/// 시나리오 동작 핸들러
/// 각 단계에서 실행해야 할 동작을 처리
/// </summary>
public class ScenarioActionHandler : MonoBehaviour
{
    [Header("Action Handlers (선택사항)")]
    [Tooltip("추나 실습 컨트롤러")]
    [SerializeField] private MonoBehaviour chunaPracticeController;
    
    [Tooltip("햅틱 피드백 컨트롤러")]
    [SerializeField] private MonoBehaviour hapticFeedbackController;
    
    [Tooltip("오디오 컨트롤러")]
    [SerializeField] private MonoBehaviour audioController;
    
    private ScenarioEventSystem eventSystem;
    private Dictionary<string, IScenarioAction> actionHandlers;
    
    private void Awake()
    {
        eventSystem = ScenarioEventSystem.Instance;

        // ServiceLocator에 등록
        ServiceLocator.Register(this);

        // 동작 핸들러 등록
        RegisterActionHandlers();
    }
    
    private void OnEnable()
    {
        // 이벤트 구독
        eventSystem.OnStepChanged += OnStepChanged;
        eventSystem.OnSubStepStarted += OnSubStepStarted;
    }
    
    private void OnDisable()
    {
        // 이벤트 구독 해제
        eventSystem.OnStepChanged -= OnStepChanged;
        eventSystem.OnSubStepStarted -= OnSubStepStarted;
    }
    
    /// <summary>
    /// 동작 핸들러 등록
    /// </summary>
    private void RegisterActionHandlers()
    {
        actionHandlers = new Dictionary<string, IScenarioAction>();
        
        // 기본 동작들 등록
        actionHandlers.Add("가이드", new GuideAction());
        actionHandlers.Add("평가", new EvaluationAction());
        actionHandlers.Add("세판상박회인", new ROMTestAction());
        actionHandlers.Add("등척성운동", new IsometricAction());
        actionHandlers.Add("스트레칭", new StretchingAction());
        actionHandlers.Add("재평가", new ReEvaluationAction());
        
        Debug.Log($"[ActionHandler] {actionHandlers.Count}개 액션 핸들러 등록 완료");
    }
    
    /// <summary>
    /// Step 변경 시 호출
    /// </summary>
    private void OnStepChanged(StepData step)
    {
        Debug.Log($"[ActionHandler] Step 변경: {step.stepName}");
        
        // Step에 따른 초기화 작업
        if (step.stepName == "가이드")
        {
            SetGuideMode();
        }
        else
        {
            SetPracticeMode();
        }
    }
    
    /// <summary>
    /// SubStep 시작 시 호출
    /// </summary>
    private void OnSubStepStarted(SubStepData subStep)
    {
        Debug.Log($"[ActionHandler] SubStep 시작: {subStep.voiceInstruction}");
        
        // 음성 안내
        if (!string.IsNullOrEmpty(subStep.voiceInstruction) && !subStep.isSameToPrevious)
        {
            PlayVoiceGuidance(subStep.voiceInstruction);
        }
        
        // Step 이름으로 적절한 동작 실행
        string stepName = GetCurrentStepName();
        ExecuteAction(stepName, subStep);
        
        // 커스텀 액션 실행
        if (!string.IsNullOrEmpty(subStep.customAction))
        {
            ExecuteCustomAction(subStep.customAction, subStep);
        }
    }
    
    /// <summary>
    /// 동작 실행
    /// </summary>
    private void ExecuteAction(string actionName, SubStepData subStep)
    {
        if (actionHandlers.ContainsKey(actionName))
        {
            actionHandlers[actionName].Execute(subStep);
        }
        else
        {
            Debug.LogWarning($"[ActionHandler] 등록되지 않은 동작: {actionName}");
        }
    }
    
    /// <summary>
    /// 커스텀 액션 실행
    /// </summary>
    private void ExecuteCustomAction(string customAction, SubStepData subStep)
    {
        Debug.Log($"[ActionHandler] 커스텀 액션 실행: {customAction}");
        
        // 커스텀 액션 처리
        switch (customAction)
        {
            case "EnableHaptic":
                // 햅틱 활성화
                break;
            case "DisableHaptic":
                // 햅틱 비활성화
                break;
            case "HighlightArea":
                // 영역 하이라이트
                break;
            default:
                Debug.LogWarning($"알 수 없는 커스텀 액션: {customAction}");
                break;
        }
    }
    
    /// <summary>
    /// 가이드 모드 설정
    /// </summary>
    private void SetGuideMode()
    {
        Debug.Log("[ActionHandler] 가이드 모드 활성화");
        // VR 상호작용 비활성화
        // UI만 표시
    }
    
    /// <summary>
    /// 실습 모드 설정
    /// </summary>
    private void SetPracticeMode()
    {
        Debug.Log("[ActionHandler] 실습 모드 활성화");
        // VR 상호작용 활성화
        // 촉진, 스트레칭 등 가능
    }
    
    /// <summary>
    /// 음성 안내 재생
    /// </summary>
    private void PlayVoiceGuidance(string text)
    {
        Debug.Log($"[ActionHandler] 음성 안내: {text}");
        
        // 실제 TTS 또는 오디오 재생
        if (audioController != null)
        {
            // audioController.PlayTTS(text);
        }
    }
    
    /// <summary>
    /// 현재 Step 이름 가져오기
    /// </summary>
    private string GetCurrentStepName()
    {
        ScenarioManager manager = ServiceLocator.Get<ScenarioManager>();
        return manager?.CurrentStep?.stepName ?? "";
    }
}

// ===== 구체적인 동작 클래스들 =====

/// <summary>
/// 가이드 동작
/// </summary>
public class GuideAction : IScenarioAction
{
    public void Execute(SubStepData subStep)
    {
        Debug.Log("[GuideAction] 가이드 표시");
        // 가이드 UI 표시
        // 텍스트 인스트럭션 하이라이트
    }
    
    public void OnComplete()
    {
        Debug.Log("[GuideAction] 가이드 완료");
    }
}

/// <summary>
/// 평가 동작
/// </summary>
public class EvaluationAction : IScenarioAction
{
    public void Execute(SubStepData subStep)
    {
        Debug.Log("[EvaluationAction] 평가 시작");
        // 주동수/보조수 위치 감지
        // 촉진 위치 하이라이트
    }
    
    public void OnComplete()
    {
        Debug.Log("[EvaluationAction] 평가 완료");
    }
}

/// <summary>
/// ROM 테스트 동작
/// </summary>
public class ROMTestAction : IScenarioAction
{
    public void Execute(SubStepData subStep)
    {
        Debug.Log("[ROMTestAction] ROM 테스트 시작");
        // 관절 가동 범위 측정 활성화
    }
    
    public void OnComplete()
    {
        Debug.Log("[ROMTestAction] ROM 테스트 완료");
    }
}

/// <summary>
/// 등척성 운동 동작
/// </summary>
public class IsometricAction : IScenarioAction
{
    public void Execute(SubStepData subStep)
    {
        Debug.Log("[IsometricAction] 등척성 운동 시작");
        // 저항 압력 감지
        // 햅틱 피드백 활성화
        
        if (subStep.duration > 0)
        {
            Debug.Log($"[IsometricAction] 타이머 시작: {subStep.duration}초");
        }
    }
    
    public void OnComplete()
    {
        Debug.Log("[IsometricAction] 등척성 운동 완료");
    }
}

/// <summary>
/// 스트레칭 동작
/// </summary>
public class StretchingAction : IScenarioAction
{
    public void Execute(SubStepData subStep)
    {
        Debug.Log("[StretchingAction] 스트레칭 시작");
        // 스트레칭 강도 측정
        // 제한 장벽 감지
    }
    
    public void OnComplete()
    {
        Debug.Log("[StretchingAction] 스트레칭 완료");
    }
}

/// <summary>
/// 재평가 동작
/// </summary>
public class ReEvaluationAction : IScenarioAction
{
    public void Execute(SubStepData subStep)
    {
        Debug.Log("[ReEvaluationAction] 재평가 시작");
        // 개선도 측정
        // 결과 비교
    }
    
    public void OnComplete()
    {
        Debug.Log("[ReEvaluationAction] 재평가 완료");
    }
}
