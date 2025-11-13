using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Oculus.Interaction.Input;
using Oculus.Interaction;
using System.Text;
using System.Globalization;
using System.Linq;

public class HandPoseRecorder_Optimized : MonoBehaviour
{
    [Header("녹화할 손 모델")]
    [SerializeField] private HandVisual leftHandVisual;
    [SerializeField] private HandVisual rightHandVisual;

    [Header("녹화 설정")]
    [SerializeField] private string recordingFileName = "HandPose";
    [SerializeField] private float baseRecordInterval = 0.15f;  // 기본 0.15초
    [SerializeField] private bool recordLeftHand = true;
    [SerializeField] private bool recordRightHand = true;
    [SerializeField] private Transform referencePoint;

    [Header("적응형 샘플링")]
    [SerializeField] private bool useAdaptiveSampling = true;
    [SerializeField] private float fastMovementThreshold = 0.5f;  // m/s
    [SerializeField] private float slowMovementThreshold = 0.1f;  // m/s
    [SerializeField] private float minInterval = 0.05f;  // 최소 간격
    [SerializeField] private float maxInterval = 0.3f;   // 최대 간격

    [Header("키프레임 감지")]
    [SerializeField] private bool useKeyframeDetection = true;
    [SerializeField] private float rotationChangeThreshold = 30f;  // 도
    [SerializeField] private float positionChangeThreshold = 0.1f; // 미터
    [SerializeField] private float contactDistanceThreshold = 0.05f; // 접촉 감지 거리

    [Header("압축 옵션")]
    [SerializeField] private bool useDeltaEncoding = true;
    [SerializeField] private int decimals = 4; // 소수점 자릿수

    [Header("타이머 설정")]
    [SerializeField] private bool useTimer = true;
    [SerializeField] private float timerDuration = 5f;
    [SerializeField] private bool showCountdown = true;

    [Header("환자 모델 (접촉 감지용)")]
    [SerializeField] private Transform patientModel;
    [SerializeField] private Collider[] patientColliders;

    [Header("녹화 상태")]
    [SerializeField] private bool isRecording = false;
    [SerializeField] private bool isWaitingToRecord = false;
    [SerializeField] private int recordedFrames = 0;
    [SerializeField] private float remainingTime = 0f;
    [SerializeField] private float currentRecordInterval;
    [SerializeField] private int totalDataPoints = 0;
    [SerializeField] private float compressionRatio = 0f;

    private List<FrameData> recordedData = new List<FrameData>();
    private float lastRecordTime = 0f;
    private float recordingStartTime = 0f;
    private int currentFrameIndex = 0;
    private StringBuilder csvBuilder = new StringBuilder(1024 * 100);

    // 속도 추적용
    private Vector3 lastLeftWristPosition;
    private Vector3 lastRightWristPosition;
    private Quaternion lastLeftWristRotation;
    private Quaternion lastRightWristRotation;
    private float lastVelocityCheckTime;

    // 이벤트
    public event Action<float> OnTimerTick;
    public event Action OnTimerComplete;
    public event Action OnRecordingStarted;
    public event Action OnRecordingStopped;
    public event Action<float> OnCompressionUpdate; // 압축률 업데이트

    [System.Serializable]
    private class FrameData
    {
        public int frameIndex;
        public string handType;
        public int jointId;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 worldPosition;
        public Quaternion worldRotation;
        public float timestamp;
        public bool isKeyframe;  // 키프레임 여부
        public float recordInterval; // 이 프레임이 기록된 간격
        
        // 델타 인코딩용
        public Vector3 deltaPosition;
        public Vector3 deltaRotation; // Euler angles delta
        public bool useDelta;
    }

    void Start()
    {
        currentRecordInterval = baseRecordInterval;
        
        // 환자 모델의 콜라이더 자동 찾기
        if (patientModel != null && patientColliders == null)
        {
            patientColliders = patientModel.GetComponentsInChildren<Collider>();
        }
    }

    void Update()
    {
        if (isRecording && Time.time - lastRecordTime >= currentRecordInterval)
        {
            RecordFrame();
            lastRecordTime = Time.time;
            
            // 다음 프레임을 위한 간격 업데이트
            if (useAdaptiveSampling)
            {
                UpdateRecordInterval();
            }
        }
    }

    private void UpdateRecordInterval()
    {
        float maxVelocity = 0f;
        
        // 왼손 속도 계산
        if (recordLeftHand && leftHandVisual != null && leftHandVisual.Hand != null)
        {
            Transform wrist = leftHandVisual.Joints[(int)HandJointId.HandWristRoot];
            if (wrist != null)
            {
                float velocity = (wrist.position - lastLeftWristPosition).magnitude / 
                                (Time.time - lastVelocityCheckTime);
                maxVelocity = Mathf.Max(maxVelocity, velocity);
                lastLeftWristPosition = wrist.position;
            }
        }
        
        // 오른손 속도 계산
        if (recordRightHand && rightHandVisual != null && rightHandVisual.Hand != null)
        {
            Transform wrist = rightHandVisual.Joints[(int)HandJointId.HandWristRoot];
            if (wrist != null)
            {
                float velocity = (wrist.position - lastRightWristPosition).magnitude / 
                                (Time.time - lastVelocityCheckTime);
                maxVelocity = Mathf.Max(maxVelocity, velocity);
                lastRightWristPosition = wrist.position;
            }
        }
        
        lastVelocityCheckTime = Time.time;
        
        // 속도에 따른 간격 조정
        if (maxVelocity > fastMovementThreshold)
        {
            currentRecordInterval = minInterval;
        }
        else if (maxVelocity < slowMovementThreshold)
        {
            currentRecordInterval = maxInterval;
        }
        else
        {
            // 선형 보간
            float t = (maxVelocity - slowMovementThreshold) / 
                     (fastMovementThreshold - slowMovementThreshold);
            currentRecordInterval = Mathf.Lerp(maxInterval, minInterval, t);
        }
        
        currentRecordInterval = Mathf.Clamp(currentRecordInterval, minInterval, maxInterval);
    }

    private bool IsKeyframe(HandVisual handVisual, string handType)
    {
        if (!useKeyframeDetection || handVisual == null || handVisual.Hand == null)
            return false;
        
        Transform wrist = handVisual.Joints[(int)HandJointId.HandWristRoot];
        if (wrist == null) return false;
        
        bool isKeyframe = false;
        
        // 1. 큰 회전 변화 감지
        Quaternion lastRotation = handType == "Left" ? lastLeftWristRotation : lastRightWristRotation;
        float rotationChange = Quaternion.Angle(wrist.rotation, lastRotation);
        if (rotationChange > rotationChangeThreshold)
        {
            isKeyframe = true;
            Debug.Log($"키프레임 감지: {handType} 손 회전 변화 {rotationChange:F1}°");
        }
        
        // 2. 큰 위치 변화 감지
        Vector3 lastPosition = handType == "Left" ? lastLeftWristPosition : lastRightWristPosition;
        float positionChange = Vector3.Distance(wrist.position, lastPosition);
        if (positionChange > positionChangeThreshold)
        {
            isKeyframe = true;
            Debug.Log($"키프레임 감지: {handType} 손 위치 변화 {positionChange:F2}m");
        }
        
        // 3. 환자 모델과의 접촉 감지
        if (patientColliders != null && patientColliders.Length > 0)
        {
            foreach (var collider in patientColliders)
            {
                if (collider != null)
                {
                    float distance = Vector3.Distance(wrist.position, 
                                                     collider.ClosestPoint(wrist.position));
                    if (distance < contactDistanceThreshold)
                    {
                        isKeyframe = true;
                        Debug.Log($"키프레임 감지: {handType} 손 접촉");
                        break;
                    }
                }
            }
        }
        
        // 현재 상태 저장
        if (handType == "Left")
        {
            lastLeftWristPosition = wrist.position;
            lastLeftWristRotation = wrist.rotation;
        }
        else
        {
            lastRightWristPosition = wrist.position;
            lastRightWristRotation = wrist.rotation;
        }
        
        return isKeyframe;
    }

    public void StartRecording()
    {
        if (isRecording)
        {
            Debug.LogWarning("이미 녹화 중입니다.");
            return;
        }

        if (useTimer)
        {
            StartCoroutine(StartRecordingWithTimer());
        }
        else
        {
            StartRecordingImmediate();
        }
    }

    private IEnumerator StartRecordingWithTimer()
    {
        isWaitingToRecord = true;
        remainingTime = timerDuration;

        Debug.Log($"<color=yellow>녹화 타이머 시작: {timerDuration}초</color>");

        while (remainingTime > 0)
        {
            OnTimerTick?.Invoke(remainingTime);
            
            if (showCountdown && remainingTime <= 3)
            {
                Debug.Log($"<color=orange>{Mathf.Ceil(remainingTime)}...</color>");
            }
            
            yield return new WaitForSeconds(0.1f);
            remainingTime -= 0.1f;
        }

        OnTimerComplete?.Invoke();
        isWaitingToRecord = false;
        StartRecordingImmediate();
    }

    private void StartRecordingImmediate()
    {
        recordedData.Clear();
        currentFrameIndex = 0;
        recordedFrames = 0;
        totalDataPoints = 0;
        
        isRecording = true;
        recordingStartTime = Time.time;
        lastRecordTime = Time.time;
        lastVelocityCheckTime = Time.time;
        currentRecordInterval = baseRecordInterval;
        
        // 초기 위치 저장
        if (leftHandVisual != null && leftHandVisual.Joints.Count > 0)
        {
            Transform wrist = leftHandVisual.Joints[(int)HandJointId.HandWristRoot];
            if (wrist != null)
            {
                lastLeftWristPosition = wrist.position;
                lastLeftWristRotation = wrist.rotation;
            }
        }
        
        if (rightHandVisual != null && rightHandVisual.Joints.Count > 0)
        {
            Transform wrist = rightHandVisual.Joints[(int)HandJointId.HandWristRoot];
            if (wrist != null)
            {
                lastRightWristPosition = wrist.position;
                lastRightWristRotation = wrist.rotation;
            }
        }
        
        Debug.Log($"<color=green>녹화 시작!</color> 적응형 샘플링: {useAdaptiveSampling}, 키프레임: {useKeyframeDetection}");
        OnRecordingStarted?.Invoke();
    }

    private void RecordFrame()
    {
        bool frameRecorded = false;
        bool isKeyframeDetected = false;
        
        if (recordLeftHand && leftHandVisual != null)
        {
            bool leftKeyframe = IsKeyframe(leftHandVisual, "Left");
            if (RecordHandData(leftHandVisual, "Left", leftKeyframe))
            {
                frameRecorded = true;
                if (leftKeyframe) isKeyframeDetected = true;
            }
        }
        
        if (recordRightHand && rightHandVisual != null)
        {
            bool rightKeyframe = IsKeyframe(rightHandVisual, "Right");
            if (RecordHandData(rightHandVisual, "Right", rightKeyframe))
            {
                frameRecorded = true;
                if (rightKeyframe) isKeyframeDetected = true;
            }
        }
        
        if (frameRecorded)
        {
            currentFrameIndex++;
            recordedFrames++;
            
            // 키프레임이 감지되면 즉시 다음 프레임 준비
            if (isKeyframeDetected && useKeyframeDetection)
            {
                currentRecordInterval = minInterval;
            }
            
            if (recordedFrames % 10 == 0)
            {
                float dataReduction = CalculateDataReduction();
                Debug.Log($"녹화 중... 프레임: {recordedFrames}, 간격: {currentRecordInterval:F2}s, 압축률: {dataReduction:F1}%");
                OnCompressionUpdate?.Invoke(dataReduction);
            }
        }
    }

    private bool RecordHandData(HandVisual handVisual, string handType, bool isKeyframe)
    {
        if (handVisual == null || handVisual.Hand == null || !handVisual.Hand.IsTrackedDataValid)
            return false;
        
        float timestamp = Time.time - recordingStartTime;
        
        Transform wrist = handVisual.Joints[(int)HandJointId.HandWristRoot];
        Vector3 wristWorldPos = wrist.position;
        Quaternion wristWorldRot = wrist.rotation;
        
        if (referencePoint != null)
        {
            wristWorldPos = wrist.position - referencePoint.position;
        }
        
        // 이전 프레임 찾기 (델타 인코딩용)
        FrameData lastFrame = null;
        if (useDeltaEncoding && recordedData.Count > 0)
        {
            lastFrame = recordedData.LastOrDefault(f => 
                f.handType == handType && f.jointId == (int)HandJointId.HandWristRoot);
        }
        
        for (int i = 0; i < handVisual.Joints.Count; i++)
        {
            Transform joint = handVisual.Joints[i];
            if (joint == null) continue;
            
            FrameData frameData = new FrameData
            {
                frameIndex = currentFrameIndex,
                handType = handType,
                jointId = i,
                localPosition = joint.localPosition,
                localRotation = joint.localRotation,
                timestamp = timestamp,
                isKeyframe = isKeyframe,
                recordInterval = currentRecordInterval,
                useDelta = false
            };
            
            // Wrist에만 월드 좌표 저장
            if (i == (int)HandJointId.HandWristRoot)
            {
                frameData.worldPosition = wristWorldPos;
                frameData.worldRotation = wristWorldRot;
                
                // 델타 인코딩 적용
                if (useDeltaEncoding && lastFrame != null && !isKeyframe)
                {
                    frameData.deltaPosition = wristWorldPos - lastFrame.worldPosition;
                    frameData.deltaRotation = (wristWorldRot.eulerAngles - lastFrame.worldRotation.eulerAngles);
                    frameData.useDelta = true;
                }
            }
            
            recordedData.Add(frameData);
            totalDataPoints++;
        }
        
        return true;
    }

    private float CalculateDataReduction()
    {
        if (recordedFrames == 0) return 0f;
        
        // 60 FPS로 녹화했을 때의 예상 데이터 포인트
        float expectedDataPoints = (Time.time - recordingStartTime) * 60f * 48f; // 48 joints
        float actualDataPoints = totalDataPoints;
        
        compressionRatio = (1f - (actualDataPoints / expectedDataPoints)) * 100f;
        return compressionRatio;
    }

    private void SaveToCSV()
    {
        if (recordedData.Count == 0)
        {
            Debug.LogError("저장할 데이터가 없습니다.");
            return;
        }
        
        string path = Path.Combine(Application.persistentDataPath, recordingFileName + ".csv");
        
        try
        {
            csvBuilder.Clear();
            
            // 헤더에 새로운 필드 추가
            csvBuilder.AppendLine("FrameIndex,HandType,JointID,LocalPosX,LocalPosY,LocalPosZ," +
                                 "LocalRotX,LocalRotY,LocalRotZ,LocalRotW,Timestamp," +
                                 "WorldPosX,WorldPosY,WorldPosZ,WorldRotX,WorldRotY,WorldRotZ,WorldRotW," +
                                 "IsKeyframe,Interval,UseDelta,DeltaPosX,DeltaPosY,DeltaPosZ," +
                                 "DeltaRotX,DeltaRotY,DeltaRotZ");
            
            CultureInfo invariantCulture = CultureInfo.InvariantCulture;
            
            foreach (FrameData data in recordedData)
            {
                string line = string.Format(invariantCulture,
                    "{0},{1},{2}," +
                    "{3:F" + decimals + "},{4:F" + decimals + "},{5:F" + decimals + "}," +
                    "{6:F" + decimals + "},{7:F" + decimals + "},{8:F" + decimals + "},{9:F" + decimals + "}," +
                    "{10:F3}," +
                    "{11:F" + decimals + "},{12:F" + decimals + "},{13:F" + decimals + "}," +
                    "{14:F" + decimals + "},{15:F" + decimals + "},{16:F" + decimals + "},{17:F" + decimals + "}," +
                    "{18},{19:F3},{20}," +
                    "{21:F" + decimals + "},{22:F" + decimals + "},{23:F" + decimals + "}," +
                    "{24:F" + decimals + "},{25:F" + decimals + "},{26:F" + decimals + "}",
                    data.frameIndex,
                    data.handType,
                    data.jointId,
                    data.localPosition.x, data.localPosition.y, data.localPosition.z,
                    data.localRotation.x, data.localRotation.y, data.localRotation.z, data.localRotation.w,
                    data.timestamp,
                    data.worldPosition.x, data.worldPosition.y, data.worldPosition.z,
                    data.worldRotation.x, data.worldRotation.y, data.worldRotation.z, data.worldRotation.w,
                    data.isKeyframe ? 1 : 0,
                    data.recordInterval,
                    data.useDelta ? 1 : 0,
                    data.deltaPosition.x, data.deltaPosition.y, data.deltaPosition.z,
                    data.deltaRotation.x, data.deltaRotation.y, data.deltaRotation.z
                );
                
                csvBuilder.AppendLine(line);
            }
            
            File.WriteAllText(path, csvBuilder.ToString());
            
            float fileSizeKB = new FileInfo(path).Length / 1024f;
            float originalSizeKB = (recordedData.Count * 100) / 1024f; // 대략적인 원본 크기
            float compressionRatio = (1f - (fileSizeKB / originalSizeKB)) * 100f;
            
            Debug.Log($"<color=green>저장 완료!</color>\n" +
                     $"경로: {path}\n" +
                     $"총 데이터: {recordedData.Count}개\n" +
                     $"프레임: {recordedFrames}개\n" +
                     $"파일 크기: {fileSizeKB:F2}KB\n" +
                     $"압축률: {compressionRatio:F1}%\n" +
                     $"평균 간격: {(recordedFrames > 0 ? (Time.time - recordingStartTime) / recordedFrames : 0):F2}초");
            
            recordedData.Clear();
        }
        catch (Exception e)
        {
            Debug.LogError($"CSV 저장 실패: {e.Message}");
        }
    }

    public void StopRecording()
    {
        if (!isRecording)
        {
            Debug.LogWarning("녹화 중이 아닙니다.");
            return;
        }
        
        isRecording = false;
        
        float recordingDuration = Time.time - recordingStartTime;
        Debug.Log($"<color=yellow>녹화 중지</color>\n" +
                 $"녹화 시간: {recordingDuration:F1}초\n" +
                 $"프레임 수: {recordedFrames}\n" +
                 $"데이터 압축률: {compressionRatio:F1}%\n" +
                 $"저장 중...");
        
        SaveToCSV();
        OnRecordingStopped?.Invoke();
    }

    public void CancelTimer()
    {
        if (isWaitingToRecord)
        {
            StopAllCoroutines();
            isWaitingToRecord = false;
            remainingTime = 0f;
            Debug.Log("<color=red>타이머 취소됨</color>");
        }
    }

    public void PauseRecording()
    {
        if (!isRecording) return;
        isRecording = false;
        Debug.Log("녹화 일시정지");
    }

    public void ResumeRecording()
    {
        if (isRecording) return;
        isRecording = true;
        lastRecordTime = Time.time;
        Debug.Log("녹화 재개");
    }

    public void CancelRecording()
    {
        isRecording = false;
        recordedData.Clear();
        currentFrameIndex = 0;
        recordedFrames = 0;
        totalDataPoints = 0;
        Debug.Log("녹화 취소됨");
    }

    // Getter 메서드들
    public bool IsRecording() => isRecording;
    public bool IsWaitingToRecord() => isWaitingToRecord;
    public int GetRecordedFrames() => recordedFrames;
    public float GetRecordingDuration() => isRecording ? Time.time - recordingStartTime : 0f;
    public float GetRemainingTime() => remainingTime;
    public float GetCurrentInterval() => currentRecordInterval;
    public float GetCompressionRatio() => compressionRatio;

    // Setter 메서드들
    public void SetRecordingSettings(bool left, bool right, float interval)
    {
        recordLeftHand = left;
        recordRightHand = right;
        baseRecordInterval = Mathf.Max(0.01f, interval);
        Debug.Log($"녹화 설정 변경 - 왼손: {left}, 오른손: {right}, 간격: {interval}초");
    }

    public void SetAdaptiveSampling(bool enabled, float minInt = 0.05f, float maxInt = 0.3f)
    {
        useAdaptiveSampling = enabled;
        minInterval = minInt;
        maxInterval = maxInt;
        Debug.Log($"적응형 샘플링: {enabled}, 범위: {minInt}~{maxInt}초");
    }

    public void SetKeyframeDetection(bool enabled)
    {
        useKeyframeDetection = enabled;
        Debug.Log($"키프레임 감지: {enabled}");
    }

    public void SetDeltaEncoding(bool enabled)
    {
        useDeltaEncoding = enabled;
        Debug.Log($"델타 인코딩: {enabled}");
    }

    public void SetFileName(string fileName)
    {
        recordingFileName = fileName;
        Debug.Log($"파일명 설정: {fileName}.csv");
    }

    public void SetReferencePoint(Transform reference)
    {
        referencePoint = reference;
        Debug.Log($"기준점 설정: {(reference != null ? reference.name : "없음")}");
    }

    public void SetPatientModel(Transform patient)
    {
        patientModel = patient;
        if (patient != null)
        {
            patientColliders = patient.GetComponentsInChildren<Collider>();
            Debug.Log($"환자 모델 설정: {patient.name}, 콜라이더: {patientColliders.Length}개");
        }
    }

    public string GetStatusText()
    {
        if (isWaitingToRecord)
        {
            return $"녹화 대기 중...\n{Mathf.Ceil(remainingTime)}초 후 시작";
        }
        else if (isRecording)
        {
            float duration = Time.time - recordingStartTime;
            return $"녹화 중...\n시간: {duration:F1}초\n프레임: {recordedFrames}\n간격: {currentRecordInterval:F2}초\n압축률: {compressionRatio:F1}%";
        }
        else
        {
            return "대기 중";
        }
    }

    void OnApplicationQuit()
    {
        if (isRecording && recordedData.Count > 0)
        {
            Debug.Log("종료 감지, 자동 저장 중...");
            isRecording = false;
            SaveToCSV();
        }
    }

    void OnDrawGizmos()
    {
        if (!isRecording) return;
        
        // 현재 녹화 중인 손 위치 표시
        if (recordLeftHand && leftHandVisual != null && leftHandVisual.Joints.Count > 0)
        {
            Transform wrist = leftHandVisual.Joints[(int)HandJointId.HandWristRoot];
            if (wrist != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(wrist.position, 0.02f);
            }
        }
        
        if (recordRightHand && rightHandVisual != null && rightHandVisual.Joints.Count > 0)
        {
            Transform wrist = rightHandVisual.Joints[(int)HandJointId.HandWristRoot];
            if (wrist != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(wrist.position, 0.02f);
            }
        }
    }
}
