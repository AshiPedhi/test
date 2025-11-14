using ChunaVR.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Oculus.Interaction.Input;
using Oculus.Interaction;
using System.Text;
using System.Globalization;

namespace ChunaVR.PoseData
{

    public class HandPoseRecorder : MonoBehaviour
    {
        [Header("녹화할 손 모델")]
        [SerializeField]
        private HandVisual leftHandVisual;

        [SerializeField]
        private HandVisual rightHandVisual;

        [Header("녹화 설정")]
        [SerializeField]
        private string recordingFileName = "HandPose";

        [SerializeField]
        private float recordInterval = 0.1f;

        [SerializeField]
        private bool recordLeftHand = true;

        [SerializeField]
        private bool recordRightHand = true;

        [SerializeField]
        private Transform referencePoint;

        [Header("타이머 설정")]
        [SerializeField]
        private bool useTimer = true;  // 타이머 사용 여부

        [SerializeField]
        private float timerDuration = 5f;  // 타이머 시간 (초)

        [SerializeField]
        private bool showCountdown = true;  // 카운트다운 표시

        [Header("녹화 상태")]
        [SerializeField]
        private bool isRecording = false;

        [SerializeField]
        private bool isWaitingToRecord = false;  // 타이머 대기 중

        [SerializeField]
        private int recordedFrames = 0;

        [SerializeField]
        private float remainingTime = 0f;  // 남은 타이머 시간

        private List<FrameData> recordedData = new List<FrameData>();
        private float lastRecordTime = 0f;
        private float recordingStartTime = 0f;
        private int currentFrameIndex = 0;

        private StringBuilder csvBuilder = new StringBuilder(1024 * 100);

        // 타이머 이벤트
        public event Action<float> OnTimerTick;  // 매 초마다 호출 (남은 시간)
        public event Action OnTimerComplete;  // 타이머 완료 시 호출
        public event Action OnRecordingStarted;  // 녹화 시작 시 호출
        public event Action OnRecordingStopped;  // 녹화 중지 시 호출

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
        }

        void Start()
        {
            if (referencePoint == null)
            {
                Debug.LogWarning("기준점이 설정되지 않아 월드 좌표를 사용합니다.");
            }
        }

        void Update()
        {
            if (isRecording)
            {
                if (Time.time - lastRecordTime >= recordInterval)
                {
                    RecordFrame();
                    lastRecordTime = Time.time;
                }
            }

    #if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (isRecording)
                    StopRecording();
                else
                    StartRecording();
            }
    #endif
        }

        // 녹화 시작 (타이머 포함)
        public void StartRecording()
        {
            if (isRecording)
            {
                Debug.LogWarning("이미 녹화 중입니다.");
                return;
            }

            if (isWaitingToRecord)
            {
                Debug.LogWarning("타이머가 이미 실행 중입니다.");
                return;
            }

            if (useTimer && timerDuration > 0)
            {
                // 타이머 시작
                StartCoroutine(StartRecordingWithTimer());
            }
            else
            {
                // 즉시 녹화 시작
                StartRecordingImmediately();
            }
        }

        // 타이머와 함께 녹화 시작
        private IEnumerator StartRecordingWithTimer()
        {
            isWaitingToRecord = true;
            remainingTime = timerDuration;

            Debug.Log($"<color=yellow>{timerDuration}초 후 녹화 시작...</color>");

            // 카운트다운
            while (remainingTime > 0)
            {
                if (showCountdown)
                {
                    Debug.Log($"<color=yellow>녹화 시작까지: {Mathf.Ceil(remainingTime)}초</color>");
                }

                // 이벤트 발생
                OnTimerTick?.Invoke(remainingTime);

                yield return new WaitForSeconds(1f);
                remainingTime -= 1f;
            }

            isWaitingToRecord = false;
            remainingTime = 0f;

            // 타이머 완료 이벤트
            OnTimerComplete?.Invoke();

            // 녹화 시작
            StartRecordingImmediately();
        }

        // 즉시 녹화 시작
        private void StartRecordingImmediately()
        {
            recordedData.Clear();
            currentFrameIndex = 0;
            recordedFrames = 0;
            recordingStartTime = Time.time;
            lastRecordTime = Time.time;
            isRecording = true;

            Debug.Log($"<color=cyan>녹화 시작!</color>\n" +
                     $"파일명: {recordingFileName}.csv\n" +
                     $"녹화 간격: {recordInterval}초\n" +
                     $"왼손: {(recordLeftHand ? "ON" : "OFF")}, 오른손: {(recordRightHand ? "ON" : "OFF")}");

            // 녹화 시작 이벤트
            OnRecordingStarted?.Invoke();
        }

        // 타이머 취소
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

        // 녹화 중지
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
                     $"저장 중...");

            SaveToCSV();

            // 녹화 중지 이벤트
            OnRecordingStopped?.Invoke();
        }

        private void RecordFrame()
        {
            bool frameRecorded = false;

            if (recordLeftHand && leftHandVisual != null)
            {
                if (RecordHandData(leftHandVisual, "Left"))
                {
                    frameRecorded = true;
                }
            }

            if (recordRightHand && rightHandVisual != null)
            {
                if (RecordHandData(rightHandVisual, "Right"))
                {
                    frameRecorded = true;
                }
            }

            if (frameRecorded)
            {
                currentFrameIndex++;
                recordedFrames++;

                if (recordedFrames % 10 == 0)
                {
                    Debug.Log($"녹화 중... 프레임: {recordedFrames}");
                }
            }
        }

        private bool RecordHandData(HandVisual handVisual, string handType)
        {
            if (handVisual == null || handVisual.Hand == null)
                return false;

            if (!handVisual.Hand.IsTrackedDataValid)
            {
                Debug.LogWarning($"{handType} 핸드 트래킹 데이터가 유효하지 않습니다.");
                return false;
            }

            float timestamp = Time.time - recordingStartTime;

            Transform wrist = handVisual.Joints[(int)HandJointId.HandWristRoot];
            Vector3 wristWorldPos = wrist.position;
            Quaternion wristWorldRot = wrist.rotation;

            if (referencePoint != null)
            {
                wristWorldPos = wrist.position - referencePoint.position;
            }

            for (int i = 0; i < handVisual.Joints.Count; i++)
            {
                Transform joint = handVisual.Joints[i];
                if (joint == null)
                    continue;

                FrameData frameData = new FrameData
                {
                    frameIndex = currentFrameIndex,
                    handType = handType,
                    jointId = i,
                    localPosition = joint.localPosition,
                    localRotation = joint.localRotation,
                    timestamp = timestamp
                };

                if (i == (int)HandJointId.HandWristRoot)
                {
                    frameData.worldPosition = wristWorldPos;
                    frameData.worldRotation = wristWorldRot;
                }
                else
                {
                    frameData.worldPosition = Vector3.zero;
                    frameData.worldRotation = Quaternion.identity;
                }

                recordedData.Add(frameData);
            }

            return true;
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

                csvBuilder.AppendLine("FrameIndex,HandType,JointID,LocalPosX,LocalPosY,LocalPosZ," +
                                     "LocalRotX,LocalRotY,LocalRotZ,LocalRotW,Timestamp," +
                                     "WorldPosX,WorldPosY,WorldPosZ,WorldRotX,WorldRotY,WorldRotZ,WorldRotW");

                CultureInfo invariantCulture = CultureInfo.InvariantCulture;

                foreach (FrameData data in recordedData)
                {
                    if (data.jointId == (int)HandJointId.HandWristRoot)
                    {
                        csvBuilder.AppendFormat(invariantCulture,
                            "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17}\n",
                            data.frameIndex,
                            data.handType,
                            data.jointId,
                            data.localPosition.x, data.localPosition.y, data.localPosition.z,
                            data.localRotation.x, data.localRotation.y, data.localRotation.z, data.localRotation.w,
                            data.timestamp,
                            data.worldPosition.x, data.worldPosition.y, data.worldPosition.z,
                            data.worldRotation.x, data.worldRotation.y, data.worldRotation.z, data.worldRotation.w
                        );
                    }
                    else
                    {
                        csvBuilder.AppendFormat(invariantCulture,
                            "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},,,,,,,\n",
                            data.frameIndex,
                            data.handType,
                            data.jointId,
                            data.localPosition.x, data.localPosition.y, data.localPosition.z,
                            data.localRotation.x, data.localRotation.y, data.localRotation.z, data.localRotation.w,
                            data.timestamp
                        );
                    }
                }

                File.WriteAllText(path, csvBuilder.ToString());

                float fileSizeKB = new FileInfo(path).Length / 1024f;
                Debug.Log($"<color=green>저장 완료!</color>\n" +
                         $"경로: {path}\n" +
                         $"총 데이터: {recordedData.Count}개\n" +
                         $"프레임: {recordedFrames}개\n" +
                         $"파일 크기: {fileSizeKB:F2}KB");

                recordedData.Clear();
            }
            catch (Exception e)
            {
                Debug.LogError($"CSV 저장 실패: {e.Message}");
            }
        }

        public void PauseRecording()
        {
            if (!isRecording)
                return;

            isRecording = false;
            Debug.Log("녹화 일시정지");
        }

        public void ResumeRecording()
        {
            if (isRecording)
                return;

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
            Debug.Log("녹화 취소됨");
        }

        // Getter 메서드들
        public bool IsRecording()
        {
            return isRecording;
        }

        public bool IsWaitingToRecord()
        {
            return isWaitingToRecord;
        }

        public int GetRecordedFrames()
        {
            return recordedFrames;
        }

        public float GetRecordingDuration()
        {
            if (isRecording)
                return Time.time - recordingStartTime;
            return 0f;
        }

        public float GetRemainingTime()
        {
            return remainingTime;
        }

        // Setter 메서드들
        public void SetRecordingSettings(bool left, bool right, float interval)
        {
            recordLeftHand = left;
            recordRightHand = right;
            recordInterval = Mathf.Max(0.01f, interval);
            Debug.Log($"녹화 설정 변경 - 왼손: {left}, 오른손: {right}, 간격: {interval}초");
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

        public void SetTimerDuration(float duration)
        {
            timerDuration = Mathf.Max(0f, duration);
            Debug.Log($"타이머 시간 설정: {timerDuration}초");
        }

        public void SetUseTimer(bool use)
        {
            useTimer = use;
            Debug.Log($"타이머 사용: {(use ? "ON" : "OFF")}");
        }

        public void SetShowCountdown(bool show)
        {
            showCountdown = show;
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
                return $"녹화 중...\n시간: {duration:F1}초\n프레임: {recordedFrames}";
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
    }}
}
