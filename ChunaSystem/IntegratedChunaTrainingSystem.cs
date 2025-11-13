using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 시나리오 시스템과 추나 트레이닝 시스템 통합 - 완전 보완 버전
/// ScenarioManager의 계층 구조를 활용하면서 HandPosePlayer와 연동
/// </summary>
public class IntegratedChunaTrainingSystem : MonoBehaviour
{
    [Header("=== 핵심 컴포넌트 ===")]
    [Tooltip("CSV 또는 Inspector 프로토타입 데이터 사용")]
    [SerializeField] private ScenarioManager scenarioManager;
    [SerializeField] private HandPosePlayer handPosePlayer;
    [SerializeField] private ChunaEducationGuideSystem guideSystem;

    [Header("=== 데이터 소스 확인 ===")]
    [SerializeField] private bool showDataSourceInfo = true;

    [Header("=== 모드 설정 ===")]
    [SerializeField] private TrainingMode currentMode = TrainingMode.Education;
    [SerializeField] private bool autoDetectMode = true;  // Step 이름으로 자동 모드 전환

    public enum TrainingMode
    {
        Education,  // 교육 모드
        Evaluation  // 평가 모드
    }

    [Header("=== 확장 데이터 매핑 ===")]
    [Tooltip("Step 이름과 CSV 파일 매핑")]
    [SerializeField] private List<StepMotionMapping> stepMotionMappings = new List<StepMotionMapping>();

    [System.Serializable]
    public class StepMotionMapping
    {
        public string stepName;           // ScenarioData의 Step 이름
        public string motionDataFile;     // HandPose CSV 파일명
        public float educationSpeed = 0.5f;
        public float requiredHoldTime = 2.0f;
        public float customThreshold = 0.7f;
        public bool evaluateBothHands = true;
    }

    [Header("=== 평가 설정 ===")]
    [SerializeField] private float defaultHoldTime = 2.0f;
    [SerializeField] private float defaultThreshold = 0.7f;

    // 이벤트 시스템
    private ScenarioEventSystem eventSystem;

    // 상태 관리
    private bool isProcessingStep = false;
    private Coroutine currentCoroutine;
    private float currentStepScore = 0f;
    private int totalSteps = 0;
    private int completedSteps = 0;
    private float totalScore = 0f;

    // 이벤트
    public UnityEvent<TrainingMode> OnModeChanged = new UnityEvent<TrainingMode>();
    public UnityEvent<float> OnStepScoreCalculated = new UnityEvent<float>();
    public UnityEvent<float> OnOverallScoreUpdated = new UnityEvent<float>();
    public UnityEvent OnTrainingCompleted = new UnityEvent();

    // 공개 프로퍼티
    public TrainingMode CurrentMode => currentMode;
    public bool IsProcessing => isProcessingStep;
    public float CurrentScore => currentStepScore;
    public float OverallScore => totalSteps > 0 ? totalScore / totalSteps : 0f;

    private void Awake()
    {
        InitializeComponents();
        SubscribeToEvents();
        SetupDefaultMappings();
    }

    private void InitializeComponents()
    {
        // ServiceLocator를 통한 컴포넌트 가져오기 (FindObjectOfType 제거)
        if (scenarioManager == null)
            scenarioManager = ServiceLocator.Get<ScenarioManager>();

        if (handPosePlayer == null)
            handPosePlayer = ServiceLocator.Get<HandPosePlayer>();

        if (guideSystem == null)
            guideSystem = ServiceLocator.Get<ChunaEducationGuideSystem>();

        eventSystem = ScenarioEventSystem.Instance;

        // HandPosePlayer 설정
        if (handPosePlayer != null)
        {
            ConfigureHandPosePlayer();
        }
    }

    private void ConfigureHandPosePlayer()
    {
        // public 메서드로 설정
        handPosePlayer.SetThresholds(0.05f, 15f, defaultThreshold);
        handPosePlayer.SetReplayHandsVisible(true);
        handPosePlayer.SetReplayHandAlpha(0.5f);
        handPosePlayer.SetDebugGizmos(true);
    }

    private void SubscribeToEvents()
    {
        if (eventSystem != null)
        {
            // ScenarioManager의 이벤트 구독
            eventSystem.OnStepChanged += OnStepChanged;
            eventSystem.OnSubStepStarted += OnSubStepStarted;
            eventSystem.OnSubStepCompleted += OnSubStepCompleted;
            eventSystem.OnScenarioCompleted += OnScenarioCompleted;
            eventSystem.OnPhaseChanged += OnPhaseChanged;
        }
    }

    private void SetupDefaultMappings()
    {
        // 기본 매핑이 비어있으면 추가
        if (stepMotionMappings.Count == 0)
        {
            stepMotionMappings.Add(new StepMotionMapping
            {
                stepName = "평가",
                motionDataFile = "UpperTrapezius_Evaluation",
                educationSpeed = 0.5f,
                requiredHoldTime = 2.0f,
                customThreshold = 0.7f
            });

            stepMotionMappings.Add(new StepMotionMapping
            {
                stepName = "세판상박회인",
                motionDataFile = "UpperTrapezius_ROM_Test",
                educationSpeed = 0.3f,
                requiredHoldTime = 3.0f,
                customThreshold = 0.65f
            });

            stepMotionMappings.Add(new StepMotionMapping
            {
                stepName = "등척성운동",
                motionDataFile = "UpperTrapezius_Isometric",
                educationSpeed = 0.5f,
                requiredHoldTime = 5.0f,
                customThreshold = 0.75f
            });
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // 공개 메서드
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 시나리오 시작
    /// </summary>
    public void StartScenario()
    {
        if (scenarioManager == null)
        {
            Debug.LogError("[IntegratedSystem] ScenarioManager가 없습니다!");
            return;
        }

        if (handPosePlayer == null)
        {
            Debug.LogError("[IntegratedSystem] HandPosePlayer가 없습니다!");
            return;
        }

        // 데이터 소스 확인
        if (showDataSourceInfo)
        {
            CheckDataSource();
        }

        // 초기화
        totalSteps = 0;
        completedSteps = 0;
        totalScore = 0f;

        // 시나리오 시작
        scenarioManager.StartScenario();
    }

    /// <summary>
    /// 모드 설정
    /// </summary>
    public void SetMode(TrainingMode mode)
    {
        if (currentMode != mode)
        {
            currentMode = mode;
            OnModeChanged?.Invoke(mode);
            Debug.Log($"[IntegratedSystem] 모드 변경: {mode}");
        }
    }

    /// <summary>
    /// 다음 단계로
    /// </summary>
    public void NextStep()
    {
        if (scenarioManager != null && !isProcessingStep)
        {
            scenarioManager.NextSubStep();
        }
    }

    /// <summary>
    /// 현재 정확도 가져오기
    /// </summary>
    public float GetCurrentAccuracy()
    {
        if (handPosePlayer == null) return 0f;

        float leftSim = handPosePlayer.GetLeftSimilarity();
        float rightSim = handPosePlayer.GetRightSimilarity();

        return (leftSim + rightSim) / 2f;
    }

    /// <summary>
    /// 트레이닝 중지
    /// </summary>
    public void StopTraining()
    {
        isProcessingStep = false;

        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        if (handPosePlayer != null)
        {
            handPosePlayer.StopPlayback();
        }

        if (guideSystem != null)
        {
            guideSystem.StopGuiding();
        }

        Time.timeScale = 1f;
    }

    // ═══════════════════════════════════════════════════════════════
    // 이벤트 핸들러
    // ═══════════════════════════════════════════════════════════════

    private void OnPhaseChanged(PhaseData phase)
    {
        Debug.Log($"[IntegratedSystem] Phase 변경: {phase.phaseName}");

        // Phase 이름으로 모드 자동 감지
        if (autoDetectMode)
        {
            if (phase.phaseName.Contains("평가"))
            {
                SetMode(TrainingMode.Evaluation);
            }
            else
            {
                SetMode(TrainingMode.Education);
            }
        }
    }

    private void OnStepChanged(StepData step)
    {
        Debug.Log($"[IntegratedSystem] Step 변경: {step.stepName}");

        // 이전 동작 정리
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        // Step 이름으로 매핑된 모션 데이터 찾기
        var mapping = FindMotionMapping(step.stepName);

        if (mapping != null)
        {
            // HandPosePlayer로 모션 재생 시작
            StartStepMotion(step, mapping);
        }
        else
        {
            // 모션 데이터 없는 Step (가이드 등)
            HandleNonMotionStep(step);
        }

        totalSteps++;
    }

    private void OnSubStepStarted(SubStepData subStep)
    {
        Debug.Log($"[IntegratedSystem] SubStep {subStep.subStepNo}: {subStep.voiceInstruction}");

        // 음성 안내
        if (!string.IsNullOrEmpty(subStep.voiceInstruction) && !subStep.isSameToPrevious)
        {
            PlayVoiceGuidance(subStep.voiceInstruction);
        }

        // 타이머 설정
        if (subStep.duration > 0)
        {
            StartCoroutine(SubStepTimer(subStep.duration));
        }

        // 커스텀 액션 실행
        if (!string.IsNullOrEmpty(subStep.customAction))
        {
            ExecuteCustomAction(subStep.customAction);
        }
    }

    private void OnSubStepCompleted(SubStepData subStep)
    {
        Debug.Log($"[IntegratedSystem] SubStep {subStep.subStepNo} 완료");
    }

    private void OnScenarioCompleted(ScenarioData scenario)
    {
        Debug.Log($"[IntegratedSystem] 시나리오 '{scenario.scenarioName}' 완료!");

        // 최종 점수 계산
        float finalScore = OverallScore;
        Debug.Log($"[IntegratedSystem] 최종 점수: {finalScore * 100:F1}%");

        // 정리
        StopTraining();

        // 완료 이벤트
        OnTrainingCompleted?.Invoke();
    }

    // ═══════════════════════════════════════════════════════════════
    // 모션 처리
    // ═══════════════════════════════════════════════════════════════

    private void StartStepMotion(StepData step, StepMotionMapping mapping)
    {
        Debug.Log($"[IntegratedSystem] 모션 시작: {mapping.motionDataFile}");

        // CSV 모션 재생
        handPosePlayer.StartPlaybackFromCSV(mapping.motionDataFile);

        // 모드별 처리
        if (currentMode == TrainingMode.Education)
        {
            currentCoroutine = StartCoroutine(EducationModeProcess(step, mapping));
        }
        else
        {
            currentCoroutine = StartCoroutine(EvaluationModeProcess(step, mapping));
        }
    }

    private IEnumerator EducationModeProcess(StepData step, StepMotionMapping mapping)
    {
        isProcessingStep = true;

        // 가이드 시스템 시작
        if (guideSystem != null)
        {
            guideSystem.StartGuiding(mapping.motionDataFile);
        }

        // 재생 속도 조절
        Time.timeScale = mapping.educationSpeed;

        // 재생 완료 대기
        while (handPosePlayer.IsLeftHandPlaying() || handPosePlayer.IsRightHandPlaying())
        {
            // 실시간 정확도 체크 (교육용)
            float accuracy = GetCurrentAccuracy();

            if (Time.frameCount % 30 == 0)  // 0.5초마다
            {
                Debug.Log($"[IntegratedSystem] 교육 중 정확도: {accuracy * 100:F1}%");
            }

            yield return null;
        }

        // 가이드 중지
        if (guideSystem != null)
        {
            guideSystem.StopGuiding();
        }

        Time.timeScale = 1f;
        isProcessingStep = false;
        completedSteps++;

        Debug.Log($"[IntegratedSystem] Step '{step.stepName}' 교육 완료");
    }

    private IEnumerator EvaluationModeProcess(StepData step, StepMotionMapping mapping)
    {
        isProcessingStep = true;

        float holdTime = 0f;
        float bestSimilarity = 0f;

        // 평가 시작
        while (holdTime < mapping.requiredHoldTime &&
               (handPosePlayer.IsLeftHandPlaying() || handPosePlayer.IsRightHandPlaying()))
        {
            // 실제 정확도 계산
            float leftSim = handPosePlayer.GetLeftSimilarity();
            float rightSim = handPosePlayer.GetRightSimilarity();
            float avgSim = (leftSim + rightSim) / 2f;

            if (avgSim > bestSimilarity)
            {
                bestSimilarity = avgSim;
            }

            // 임계값 체크
            bool meetsThreshold = mapping.evaluateBothHands ?
                (leftSim >= mapping.customThreshold && rightSim >= mapping.customThreshold) :
                (avgSim >= mapping.customThreshold);

            if (meetsThreshold)
            {
                holdTime += Time.deltaTime;
                ShowProgressFeedback(holdTime / mapping.requiredHoldTime);
            }
            else
            {
                holdTime = 0f;
                ShowProgressFeedback(0f);
            }

            yield return null;
        }

        // 평가 결과
        bool stepPassed = (holdTime >= mapping.requiredHoldTime);
        currentStepScore = bestSimilarity;
        totalScore += bestSimilarity;

        ShowEvaluationResult(step.stepName, stepPassed, bestSimilarity);

        // 이벤트 발생
        OnStepScoreCalculated?.Invoke(currentStepScore);
        OnOverallScoreUpdated?.Invoke(OverallScore);

        isProcessingStep = false;
        completedSteps++;

        Debug.Log($"[IntegratedSystem] Step '{step.stepName}' 평가 완료 - {(stepPassed ? "통과" : "실패")}");
    }

    private void HandleNonMotionStep(StepData step)
    {
        if (step.IsGuideStep())
        {
            Debug.Log($"[IntegratedSystem] 가이드 Step - UI만 표시");
        }
        else
        {
            Debug.LogWarning($"[IntegratedSystem] '{step.stepName}'에 대한 모션 데이터가 없습니다");
        }

        completedSteps++;
    }

    // ═══════════════════════════════════════════════════════════════
    // 유틸리티 메서드
    // ═══════════════════════════════════════════════════════════════

    private StepMotionMapping FindMotionMapping(string stepName)
    {
        return stepMotionMappings?.Find(m => m.stepName == stepName);
    }

    private IEnumerator SubStepTimer(int duration)
    {
        Debug.Log($"[IntegratedSystem] {duration}초 타이머 시작");

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // 프로그레스 업데이트
            if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"[IntegratedSystem] 타이머: {elapsed:F1}/{duration}초");
            }

            yield return null;
        }

        Debug.Log($"[IntegratedSystem] 타이머 완료");
    }

    private void ExecuteCustomAction(string action)
    {
        switch (action)
        {
            case "StartHaptic":
            case "EnableHaptic":
                Debug.Log("[IntegratedSystem] 햅틱 피드백 시작");
                break;

            case "StopHaptic":
            case "DisableHaptic":
                Debug.Log("[IntegratedSystem] 햅틱 피드백 중지");
                break;

            case "ShowArrow":
            case "HighlightArea":
                Debug.Log("[IntegratedSystem] 가이드 화살표 표시");
                break;

            case "CheckPosition":
                float accuracy = GetCurrentAccuracy();
                Debug.Log($"[IntegratedSystem] 손 위치 체크: {accuracy * 100:F1}%");
                break;

            default:
                Debug.LogWarning($"[IntegratedSystem] 알 수 없는 액션: {action}");
                break;
        }
    }

    private void ShowProgressFeedback(float progress)
    {
        // UI 업데이트 또는 이벤트 발생
        Debug.Log($"[IntegratedSystem] 진행도: {progress * 100:F0}%");
    }

    private void ShowEvaluationResult(string stepName, bool passed, float accuracy)
    {
        string result = passed ? "통과" : "재시도 필요";
        Debug.Log($"[IntegratedSystem] {stepName} 평가 결과: {result} (정확도: {accuracy * 100:F1}%)");
    }

    private void PlayVoiceGuidance(string text)
    {
        Debug.Log($"[IntegratedSystem] 음성: {text}");
        // TTS 또는 오디오 재생
    }

    private void CheckDataSource()
    {
        if (scenarioManager == null) return;

        // 리플렉션 제거: public 프로퍼티 사용
        bool useCSV = scenarioManager.UseCSVData;

        if (useCSV)
        {
            Debug.Log("[IntegratedSystem] 📁 CSV 파일에서 시나리오 로드");
            Debug.Log($"[IntegratedSystem] CSV 파일: {scenarioManager.CSVFileName}");
        }
        else
        {
            Debug.Log("[IntegratedSystem] ✏️ Inspector 프로토타입 데이터 사용");

            // 프로토타입 데이터 정보
            var prototypeData = scenarioManager.PrototypeScenario;
            if (prototypeData != null)
            {
                Debug.Log($"[IntegratedSystem] 시나리오: {prototypeData.scenarioName}");
                Debug.Log($"[IntegratedSystem] Phase 수: {prototypeData.phases.Count}");
            }
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (eventSystem != null)
        {
            eventSystem.OnStepChanged -= OnStepChanged;
            eventSystem.OnSubStepStarted -= OnSubStepStarted;
            eventSystem.OnSubStepCompleted -= OnSubStepCompleted;
            eventSystem.OnScenarioCompleted -= OnScenarioCompleted;
            eventSystem.OnPhaseChanged -= OnPhaseChanged;
        }

        StopTraining();
    }

    // ═══════════════════════════════════════════════════════════════
    // 디버그/테스트 메서드
    // ═══════════════════════════════════════════════════════════════

    [ContextMenu("Print Mapping Info")]
    private void PrintMappingInfo()
    {
        Debug.Log($"=== Step-Motion 매핑 정보 ===");
        Debug.Log($"총 {stepMotionMappings.Count}개 매핑");

        foreach (var mapping in stepMotionMappings)
        {
            Debug.Log($"  '{mapping.stepName}' → '{mapping.motionDataFile}.csv'");
        }
    }

    [ContextMenu("Test Current Accuracy")]
    private void TestCurrentAccuracy()
    {
        float accuracy = GetCurrentAccuracy();
        Debug.Log($"[IntegratedSystem] 현재 정확도: {accuracy * 100:F1}%");
    }
}