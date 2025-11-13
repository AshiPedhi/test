using ChunaVR.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Linq;
using System.Globalization;
using Oculus.Interaction;

public class HandPoseDataEditor : MonoBehaviour
{
    [Header("=== 편집기 UI 컴포넌트 ===")]
    public GameObject editorPanel;
    public Slider timelineSlider;
    public Slider startPointSlider;
    public Slider endPointSlider;
    public RectTransform selectedRangeVisual;  // 선택 구간 시각화
    public RectTransform playheadVisual;       // 현재 재생 위치

    [Header("타임라인 마커")]
    public Transform markerContainer;
    public GameObject keyframeMarkerPrefab;
    public Color keyframeColor = Color.yellow;
    public Color normalFrameColor = Color.gray;

    [Header("정보 표시")]
    public TextMeshProUGUI currentTimeText;
    public TextMeshProUGUI totalDurationText;
    public TextMeshProUGUI selectedRangeText;
    public TextMeshProUGUI frameInfoText;
    public TextMeshProUGUI fileSizeText;
    public TextMeshProUGUI compressionInfoText;

    [Header("컨트롤 버튼")]
    public Button playPauseButton;
    public Button stopButton;
    public Button trimButton;
    public Button saveButton;
    public Button loadButton;
    public Button undoButton;
    public Button redoButton;
    public Button exportButton;

    [Header("재생 속도 컨트롤")]
    public Slider playbackSpeedSlider;
    public TextMeshProUGUI playbackSpeedText;
    public Button[] speedPresetButtons;  // 0.25x, 0.5x, 1x, 2x

    [Header("편집 옵션")]
    public Toggle loopToggle;
    public Toggle snapToKeyframeToggle;
    public Toggle previewBothHandsToggle;
    public Toggle showGhostHandToggle;
    public Slider ghostHandOffsetSlider;

    [Header("파일 관리")]
    public TMP_InputField fileNameInput;
    public TMP_Dropdown fileListDropdown;
    public Button refreshFilesButton;
    public Button deleteFileButton;

    [Header("플레이어 연결")]
    public HandPosePlayer handPosePlayer;
    public HandVisual leftHandPreview;
    public HandVisual rightHandPreview;
    public HandVisual leftHandGhost;   // 고스트 핸드 (이전/이후 프레임)
    public HandVisual rightHandGhost;

    [Header("편집 상태")]
    public bool isEditing = false;
    public bool isPlaying = false;
    public float currentTime = 0f;
    public float startTime = 0f;
    public float endTime = 0f;
    public float totalDuration = 0f;
    public int totalFrames = 0;
    public int currentFrame = 0;

    // 로드된 데이터
    private List<HandPoseData> originalData = new List<HandPoseData>();
    private List<HandPoseData> editedData = new List<HandPoseData>();
    private List<int> keyframeIndices = new List<int>();

    // 실행 취소/다시 실행 스택
    private Stack<EditAction> undoStack = new Stack<EditAction>();
    private Stack<EditAction> redoStack = new Stack<EditAction>();

    // 현재 파일 정보
    private string currentFileName = "";
    private bool hasUnsavedChanges = false;

    // 이벤트
    public event Action<string> OnFileLoaded;
    public event Action<string> OnFileSaved;
    public event Action<float, float> OnRangeTrimmed;

    [System.Serializable]
    private class HandPoseData
    {
        public int frameIndex;
        public float timestamp;
        public string handType;
        public int jointId;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 worldPosition;
        public Quaternion worldRotation;
        public bool hasWorldData;
        public bool isKeyframe;
        public float recordInterval;
    }

    [System.Serializable]
    private class EditAction
    {
        public enum ActionType { Trim, Delete, Modify }
        public ActionType type;
        public List<HandPoseData> previousData;
        public List<HandPoseData> newData;
        public float previousStart;
        public float previousEnd;
        public float newStart;
        public float newEnd;
    }

    void Start()
    {
        InitializeUI();
        RefreshFileList();

        // 플레이어 이벤트 연결
        if (handPosePlayer != null)
        {
            // 이벤트가 있는지 확인하고 연결 (리플렉션 사용으로 안전하게)
            var progressEvent = handPosePlayer.GetType().GetEvent("OnPlaybackProgress");
            if (progressEvent != null)
            {
                handPosePlayer.OnPlaybackProgress += OnPlaybackProgress;
            }

            var completedEvent = handPosePlayer.GetType().GetEvent("OnPlaybackCompleted");
            if (completedEvent != null)
            {
                handPosePlayer.OnPlaybackCompleted += OnPlaybackCompleted;
            }
        }
    }

    void Update()
    {
        if (isEditing && isPlaying)
        {
            UpdatePlayback();
        }

        // 키보드 단축키
        HandleKeyboardShortcuts();
    }

    private void InitializeUI()
    {
        // 슬라이더 이벤트
        timelineSlider.onValueChanged.AddListener(OnTimelineChanged);
        startPointSlider.onValueChanged.AddListener(OnStartPointChanged);
        endPointSlider.onValueChanged.AddListener(OnEndPointChanged);
        playbackSpeedSlider.onValueChanged.AddListener(OnPlaybackSpeedChanged);
        ghostHandOffsetSlider.onValueChanged.AddListener(OnGhostOffsetChanged);

        // 버튼 이벤트
        playPauseButton.onClick.AddListener(TogglePlayPause);
        stopButton.onClick.AddListener(StopPlayback);
        trimButton.onClick.AddListener(TrimToSelection);
        saveButton.onClick.AddListener(SaveEditedData);
        loadButton.onClick.AddListener(LoadSelectedFile);
        undoButton.onClick.AddListener(Undo);
        redoButton.onClick.AddListener(Redo);
        exportButton.onClick.AddListener(ExportSelection);
        refreshFilesButton.onClick.AddListener(RefreshFileList);
        deleteFileButton.onClick.AddListener(DeleteSelectedFile);

        // 속도 프리셋 버튼
        if (speedPresetButtons != null && speedPresetButtons.Length >= 4)
        {
            speedPresetButtons[0].onClick.AddListener(() => SetPlaybackSpeed(0.25f));
            speedPresetButtons[1].onClick.AddListener(() => SetPlaybackSpeed(0.5f));
            speedPresetButtons[2].onClick.AddListener(() => SetPlaybackSpeed(1.0f));
            speedPresetButtons[3].onClick.AddListener(() => SetPlaybackSpeed(2.0f));
        }

        // 토글 이벤트
        loopToggle.onValueChanged.AddListener(OnLoopToggleChanged);
        snapToKeyframeToggle.onValueChanged.AddListener(OnSnapToggleChanged);
        showGhostHandToggle.onValueChanged.AddListener(OnGhostHandToggleChanged);

        // 초기 상태
        UpdateButtonStates();
    }

    private void HandleKeyboardShortcuts()
    {
        // 스페이스바: 재생/일시정지
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TogglePlayPause();
        }

        // S: 정지
        if (Input.GetKeyDown(KeyCode.S))
        {
            StopPlayback();
        }

        // Ctrl+Z: 실행 취소
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z))
        {
            Undo();
        }

        // Ctrl+Y: 다시 실행
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Y))
        {
            Redo();
        }

        // Ctrl+S: 저장
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S))
        {
            SaveEditedData();
        }

        // [ ]: 구간 시작/끝 설정
        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            SetStartPointToCurrent();
        }
        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            SetEndPointToCurrent();
        }

        // 화살표: 프레임 단위 이동
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            SeekToPreviousFrame();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            SeekToNextFrame();
        }

        // Shift + 화살표: 키프레임 이동
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                SeekToPreviousKeyframe();
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                SeekToNextKeyframe();
            }
        }
    }

    public void LoadCSVFile(string fileName)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName + ".csv");

        if (!File.Exists(path))
        {
            Debug.LogError($"파일을 찾을 수 없습니다: {path}");
            return;
        }

        try
        {
            originalData.Clear();
            editedData.Clear();
            keyframeIndices.Clear();

            string[] lines = File.ReadAllLines(path);
            if (lines.Length < 2)
            {
                Debug.LogError("CSV 데이터가 부족합니다.");
                return;
            }

            // 헤더 파싱
            string[] headers = lines[0].Split(',');
            int isKeyframeCol = Array.IndexOf(headers, "IsKeyframe");
            int intervalCol = Array.IndexOf(headers, "Interval");

            CultureInfo invariantCulture = CultureInfo.InvariantCulture;

            // 데이터 파싱
            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = lines[i].Split(',');

                if (values.Length < 11) continue;

                HandPoseData data = new HandPoseData
                {
                    frameIndex = int.Parse(values[0]),
                    timestamp = float.Parse(values[10], invariantCulture),
                    handType = values[1],
                    jointId = int.Parse(values[2]),
                    localPosition = new Vector3(
                        float.Parse(values[3], invariantCulture),
                        float.Parse(values[4], invariantCulture),
                        float.Parse(values[5], invariantCulture)
                    ),
                    localRotation = new Quaternion(
                        float.Parse(values[6], invariantCulture),
                        float.Parse(values[7], invariantCulture),
                        float.Parse(values[8], invariantCulture),
                        float.Parse(values[9], invariantCulture)
                    )
                };

                // 키프레임 정보
                if (isKeyframeCol >= 0 && values.Length > isKeyframeCol)
                {
                    data.isKeyframe = values[isKeyframeCol] == "1";
                    if (data.isKeyframe && !keyframeIndices.Contains(data.frameIndex))
                    {
                        keyframeIndices.Add(data.frameIndex);
                    }
                }

                // 녹화 간격
                if (intervalCol >= 0 && values.Length > intervalCol)
                {
                    data.recordInterval = float.Parse(values[intervalCol], invariantCulture);
                }

                // 월드 좌표 (Wrist만)
                if (data.jointId == 0 && values.Length > 17 && !string.IsNullOrEmpty(values[11]))
                {
                    data.worldPosition = new Vector3(
                        float.Parse(values[11], invariantCulture),
                        float.Parse(values[12], invariantCulture),
                        float.Parse(values[13], invariantCulture)
                    );
                    data.worldRotation = new Quaternion(
                        float.Parse(values[14], invariantCulture),
                        float.Parse(values[15], invariantCulture),
                        float.Parse(values[16], invariantCulture),
                        float.Parse(values[17], invariantCulture)
                    );
                    data.hasWorldData = true;
                }

                originalData.Add(data);
            }

            // 편집용 데이터 복사
            editedData = new List<HandPoseData>(originalData);

            // 타임라인 설정
            if (editedData.Count > 0)
            {
                totalDuration = editedData.Max(d => d.timestamp);
                totalFrames = editedData.Select(d => d.frameIndex).Distinct().Count();

                startTime = 0f;
                endTime = totalDuration;

                UpdateTimeline();
                CreateKeyframeMarkers();
            }

            // 파일 정보 업데이트
            currentFileName = fileName;
            hasUnsavedChanges = false;

            // UI 업데이트
            UpdateFileInfo();
            UpdateButtonStates();

            // 플레이어에 로드
            if (handPosePlayer != null)
            {
                // LoadFromCSV 메서드가 있는지 확인
                var loadMethod = handPosePlayer.GetType().GetMethod("LoadFromCSV");
                if (loadMethod != null)
                {
                    loadMethod.Invoke(handPosePlayer, new object[] { fileName });
                }
                else
                {
                    // 대체 메서드 사용
                    handPosePlayer.StartPlaybackFromCSV(fileName);
                }
            }

            Debug.Log($"파일 로드 완료: {fileName}\n" +
                     $"총 데이터: {originalData.Count}\n" +
                     $"프레임: {totalFrames}\n" +
                     $"시간: {totalDuration:F2}초\n" +
                     $"키프레임: {keyframeIndices.Count}개");

            OnFileLoaded?.Invoke(fileName);

            // 편집 모드 활성화
            isEditing = true;
            editorPanel.SetActive(true);
        }
        catch (Exception e)
        {
            Debug.LogError($"파일 로드 실패: {e.Message}");
        }
    }

    private void UpdateTimeline()
    {
        // 슬라이더 범위 설정
        timelineSlider.minValue = 0f;
        timelineSlider.maxValue = totalDuration;
        timelineSlider.value = currentTime;

        startPointSlider.minValue = 0f;
        startPointSlider.maxValue = totalDuration;
        startPointSlider.value = startTime;

        endPointSlider.minValue = 0f;
        endPointSlider.maxValue = totalDuration;
        endPointSlider.value = endTime;

        // 선택 구간 시각화
        UpdateSelectedRangeVisual();
    }

    private void UpdateSelectedRangeVisual()
    {
        if (selectedRangeVisual == null) return;

        RectTransform timelineRect = timelineSlider.GetComponent<RectTransform>();
        float timelineWidth = timelineRect.rect.width;

        float startRatio = startTime / totalDuration;
        float endRatio = endTime / totalDuration;

        float startX = timelineWidth * startRatio - timelineWidth * 0.5f;
        float width = timelineWidth * (endRatio - startRatio);

        selectedRangeVisual.anchoredPosition = new Vector2(startX + width * 0.5f, 0);
        selectedRangeVisual.sizeDelta = new Vector2(width, selectedRangeVisual.sizeDelta.y);
    }

    private void CreateKeyframeMarkers()
    {
        // 기존 마커 삭제
        if (markerContainer != null)
        {
            foreach (Transform child in markerContainer)
            {
                Destroy(child.gameObject);
            }
        }

        if (keyframeMarkerPrefab == null || markerContainer == null) return;

        RectTransform timelineRect = timelineSlider.GetComponent<RectTransform>();
        float timelineWidth = timelineRect.rect.width;

        // 키프레임 마커 생성
        foreach (int frameIndex in keyframeIndices)
        {
            var frameData = editedData.FirstOrDefault(d => d.frameIndex == frameIndex);
            if (frameData != null)
            {
                float ratio = frameData.timestamp / totalDuration;
                float xPos = timelineWidth * ratio - timelineWidth * 0.5f;

                GameObject marker = Instantiate(keyframeMarkerPrefab, markerContainer);
                RectTransform markerRect = marker.GetComponent<RectTransform>();
                markerRect.anchoredPosition = new Vector2(xPos, 0);

                // 색상 설정
                Image markerImage = marker.GetComponent<Image>();
                if (markerImage != null)
                {
                    markerImage.color = keyframeColor;
                }

                // 툴팁 추가
                if (marker.TryGetComponent<Tooltip>(out var tooltip))
                {
                    tooltip.text = $"키프레임 {frameIndex}\n시간: {frameData.timestamp:F2}초";
                }
            }
        }
    }

    private void UpdatePlayback()
    {
        // null 체크 및 기본값 처리
        float speed = 1.0f;
        if (handPosePlayer != null)
        {
            // GetPlaybackSpeed 메서드가 있는지 확인
            var method = handPosePlayer.GetType().GetMethod("GetPlaybackSpeed");
            if (method != null)
            {
                speed = (float)method.Invoke(handPosePlayer, null);
            }
        }

        currentTime += Time.deltaTime * speed;

        // 루프 처리
        if (currentTime > endTime)
        {
            if (loopToggle.isOn)
            {
                currentTime = startTime;
            }
            else
            {
                currentTime = endTime;
                StopPlayback();
            }
        }

        // 타임라인 업데이트
        timelineSlider.value = currentTime;

        // 플레이어 시간 동기화
        if (handPosePlayer != null)
        {
            var seekMethod = handPosePlayer.GetType().GetMethod("SeekToTime");
            if (seekMethod != null)
            {
                seekMethod.Invoke(handPosePlayer, new object[] { currentTime });
            }
        }

        UpdateTimeDisplay();
    }

    private void OnTimelineChanged(float value)
    {
        currentTime = value;

        // 키프레임 스냅
        if (snapToKeyframeToggle.isOn)
        {
            float nearestKeyframe = FindNearestKeyframeTime(currentTime);
            if (Mathf.Abs(nearestKeyframe - currentTime) < 0.1f) // 0.1초 이내면 스냅
            {
                currentTime = nearestKeyframe;
                timelineSlider.value = currentTime;
            }
        }

        // 플레이어 동기화
        if (handPosePlayer != null && !isPlaying)
        {
            var seekMethod = handPosePlayer.GetType().GetMethod("SeekToTime");
            if (seekMethod != null)
            {
                seekMethod.Invoke(handPosePlayer, new object[] { currentTime });
            }
        }

        // 현재 프레임 찾기
        var currentFrameData = editedData.Where(d => d.timestamp <= currentTime)
                                        .OrderByDescending(d => d.timestamp)
                                        .FirstOrDefault();
        if (currentFrameData != null)
        {
            currentFrame = currentFrameData.frameIndex;
        }

        UpdateTimeDisplay();
        UpdateGhostHands();
    }

    private void OnStartPointChanged(float value)
    {
        startTime = Mathf.Min(value, endTime - 0.1f); // 최소 0.1초 구간 유지
        startPointSlider.value = startTime;

        if (snapToKeyframeToggle.isOn)
        {
            startTime = FindNearestKeyframeTime(startTime);
            startPointSlider.value = startTime;
        }

        UpdateSelectedRangeVisual();
        UpdateRangeDisplay();
    }

    private void OnEndPointChanged(float value)
    {
        endTime = Mathf.Max(value, startTime + 0.1f); // 최소 0.1초 구간 유지
        endPointSlider.value = endTime;

        if (snapToKeyframeToggle.isOn)
        {
            endTime = FindNearestKeyframeTime(endTime);
            endPointSlider.value = endTime;
        }

        UpdateSelectedRangeVisual();
        UpdateRangeDisplay();
    }

    private void OnPlaybackSpeedChanged(float value)
    {
        if (handPosePlayer != null)
        {
            var setSpeedMethod = handPosePlayer.GetType().GetMethod("SetPlaybackSpeed");
            if (setSpeedMethod != null)
            {
                setSpeedMethod.Invoke(handPosePlayer, new object[] { value });
            }
        }

        if (playbackSpeedText != null)
        {
            playbackSpeedText.text = $"{value:F1}x";
        }
    }

    private void OnGhostOffsetChanged(float value)
    {
        UpdateGhostHands();
    }

    private void TogglePlayPause()
    {
        if (editedData.Count == 0) return;

        isPlaying = !isPlaying;

        if (isPlaying)
        {
            if (handPosePlayer != null)
            {
                // StartPlayback 메서드가 있는지 확인
                var startMethod = handPosePlayer.GetType().GetMethod("StartPlayback");
                if (startMethod != null)
                {
                    startMethod.Invoke(handPosePlayer, null);
                }
                else
                {
                    // 대체 메서드 사용
                    handPosePlayer.StartPlaybackFromCSV(currentFileName);
                }
            }

            // 끝에 도달했으면 처음부터
            if (currentTime >= endTime)
            {
                currentTime = startTime;
            }
        }
        else
        {
            if (handPosePlayer != null)
            {
                // PausePlayback 메서드가 있는지 확인
                var pauseMethod = handPosePlayer.GetType().GetMethod("PausePlayback");
                if (pauseMethod != null)
                {
                    pauseMethod.Invoke(handPosePlayer, null);
                }
                else
                {
                    // 대체: StopPlayback 사용
                    handPosePlayer.StopPlayback();
                }
            }
        }

        UpdateButtonStates();
    }

    private void StopPlayback()
    {
        isPlaying = false;
        currentTime = startTime;
        timelineSlider.value = currentTime;

        if (handPosePlayer != null)
        {
            handPosePlayer.StopPlayback();

            var seekMethod = handPosePlayer.GetType().GetMethod("SeekToTime");
            if (seekMethod != null)
            {
                seekMethod.Invoke(handPosePlayer, new object[] { startTime });
            }
        }

        UpdateButtonStates();
        UpdateTimeDisplay();
    }

    private void TrimToSelection()
    {
        if (editedData.Count == 0) return;

        // 실행 취소를 위한 액션 저장
        EditAction action = new EditAction
        {
            type = EditAction.ActionType.Trim,
            previousData = new List<HandPoseData>(editedData),
            previousStart = 0,
            previousEnd = totalDuration,
            newStart = startTime,
            newEnd = endTime
        };

        // 선택 구간의 데이터만 추출
        var trimmedData = editedData.Where(d => d.timestamp >= startTime && d.timestamp <= endTime).ToList();

        // 타임스탬프 조정 (시작점을 0으로)
        float timeOffset = startTime;
        foreach (var data in trimmedData)
        {
            data.timestamp -= timeOffset;
        }

        // 프레임 인덱스 재정렬
        int newFrameIndex = 0;
        int lastOriginalIndex = -1;
        foreach (var data in trimmedData.OrderBy(d => d.timestamp))
        {
            if (data.frameIndex != lastOriginalIndex)
            {
                newFrameIndex++;
                lastOriginalIndex = data.frameIndex;
            }
            data.frameIndex = newFrameIndex;
        }

        editedData = trimmedData;
        action.newData = new List<HandPoseData>(editedData);

        undoStack.Push(action);
        redoStack.Clear();

        // 타임라인 업데이트
        totalDuration = endTime - startTime;
        totalFrames = editedData.Select(d => d.frameIndex).Distinct().Count();
        startTime = 0;
        endTime = totalDuration;
        currentTime = 0;

        UpdateTimeline();
        CreateKeyframeMarkers();
        UpdateFileInfo();
        UpdateButtonStates();

        hasUnsavedChanges = true;

        Debug.Log($"구간 트림 완료: {action.newStart:F2}초 ~ {action.previousEnd:F2}초\n" +
                 $"새 길이: {totalDuration:F2}초");

        OnRangeTrimmed?.Invoke(action.newStart, action.previousEnd);
    }

    private void SaveEditedData()
    {
        if (editedData.Count == 0)
        {
            Debug.LogError("저장할 데이터가 없습니다.");
            return;
        }

        string fileName = string.IsNullOrEmpty(fileNameInput.text) ?
                         $"{currentFileName}_edited" : fileNameInput.text;

        string path = Path.Combine(Application.persistentDataPath, fileName + ".csv");

        try
        {
            using (StreamWriter writer = new StreamWriter(path))
            {
                // 헤더
                writer.WriteLine("FrameIndex,HandType,JointID,LocalPosX,LocalPosY,LocalPosZ," +
                               "LocalRotX,LocalRotY,LocalRotZ,LocalRotW,Timestamp," +
                               "WorldPosX,WorldPosY,WorldPosZ,WorldRotX,WorldRotY,WorldRotZ,WorldRotW," +
                               "IsKeyframe,Interval");

                CultureInfo invariantCulture = CultureInfo.InvariantCulture;

                // 데이터
                foreach (var data in editedData.OrderBy(d => d.timestamp).ThenBy(d => d.handType).ThenBy(d => d.jointId))
                {
                    string line = string.Format(invariantCulture,
                        "{0},{1},{2},{3:F4},{4:F4},{5:F4},{6:F4},{7:F4},{8:F4},{9:F4},{10:F3}," +
                        "{11:F4},{12:F4},{13:F4},{14:F4},{15:F4},{16:F4},{17:F4},{18},{19:F3}",
                        data.frameIndex,
                        data.handType,
                        data.jointId,
                        data.localPosition.x, data.localPosition.y, data.localPosition.z,
                        data.localRotation.x, data.localRotation.y, data.localRotation.z, data.localRotation.w,
                        data.timestamp,
                        data.hasWorldData ? data.worldPosition.x : 0,
                        data.hasWorldData ? data.worldPosition.y : 0,
                        data.hasWorldData ? data.worldPosition.z : 0,
                        data.hasWorldData ? data.worldRotation.x : 0,
                        data.hasWorldData ? data.worldRotation.y : 0,
                        data.hasWorldData ? data.worldRotation.z : 0,
                        data.hasWorldData ? data.worldRotation.w : 0,
                        data.isKeyframe ? 1 : 0,
                        data.recordInterval
                    );

                    writer.WriteLine(line);
                }
            }

            float fileSizeKB = new FileInfo(path).Length / 1024f;

            Debug.Log($"<color=green>파일 저장 완료!</color>\n" +
                     $"파일명: {fileName}.csv\n" +
                     $"크기: {fileSizeKB:F2}KB\n" +
                     $"데이터: {editedData.Count}개\n" +
                     $"프레임: {totalFrames}개");

            currentFileName = fileName;
            hasUnsavedChanges = false;

            RefreshFileList();
            UpdateButtonStates();

            OnFileSaved?.Invoke(fileName);
        }
        catch (Exception e)
        {
            Debug.LogError($"파일 저장 실패: {e.Message}");
        }
    }

    private void ExportSelection()
    {
        // 현재 선택 구간만 별도 파일로 저장
        if (editedData.Count == 0) return;

        var exportData = editedData.Where(d => d.timestamp >= startTime && d.timestamp <= endTime).ToList();

        // 임시로 editedData 교체
        var tempData = editedData;
        editedData = exportData;

        // 타임스탬프 조정
        float offset = startTime;
        foreach (var data in editedData)
        {
            data.timestamp -= offset;
        }

        // 파일명 설정
        string originalName = fileNameInput.text;
        fileNameInput.text = $"{currentFileName}_selection_{startTime:F1}-{endTime:F1}";

        SaveEditedData();

        // 원래 데이터로 복구
        editedData = tempData;
        fileNameInput.text = originalName;
    }

    private void Undo()
    {
        if (undoStack.Count == 0) return;

        EditAction action = undoStack.Pop();

        // 현재 상태를 redo 스택에 저장
        EditAction redoAction = new EditAction
        {
            type = action.type,
            previousData = new List<HandPoseData>(editedData),
            newData = new List<HandPoseData>(action.previousData),
            previousStart = startTime,
            previousEnd = endTime,
            newStart = action.previousStart,
            newEnd = action.previousEnd
        };
        redoStack.Push(redoAction);

        // 이전 상태로 복원
        editedData = new List<HandPoseData>(action.previousData);

        if (action.type == EditAction.ActionType.Trim)
        {
            totalDuration = action.previousEnd - action.previousStart;
            startTime = action.previousStart;
            endTime = action.previousEnd;
        }

        // UI 업데이트
        totalFrames = editedData.Select(d => d.frameIndex).Distinct().Count();
        UpdateTimeline();
        CreateKeyframeMarkers();
        UpdateFileInfo();
        UpdateButtonStates();

        hasUnsavedChanges = true;

        Debug.Log("실행 취소 완료");
    }

    private void Redo()
    {
        if (redoStack.Count == 0) return;

        EditAction action = redoStack.Pop();

        // 현재 상태를 undo 스택에 저장
        EditAction undoAction = new EditAction
        {
            type = action.type,
            previousData = new List<HandPoseData>(editedData),
            newData = new List<HandPoseData>(action.newData),
            previousStart = startTime,
            previousEnd = endTime,
            newStart = action.newStart,
            newEnd = action.newEnd
        };
        undoStack.Push(undoAction);

        // 다시 실행
        editedData = new List<HandPoseData>(action.newData);

        if (action.type == EditAction.ActionType.Trim)
        {
            totalDuration = action.newEnd - action.newStart;
            startTime = action.newStart;
            endTime = action.newEnd;
        }

        // UI 업데이트
        totalFrames = editedData.Select(d => d.frameIndex).Distinct().Count();
        UpdateTimeline();
        CreateKeyframeMarkers();
        UpdateFileInfo();
        UpdateButtonStates();

        hasUnsavedChanges = true;

        Debug.Log("다시 실행 완료");
    }

    private void SeekToNextFrame()
    {
        if (editedData.Count == 0) return;

        var nextFrame = editedData.Where(d => d.timestamp > currentTime)
                                  .OrderBy(d => d.timestamp)
                                  .FirstOrDefault();

        if (nextFrame != null)
        {
            currentTime = nextFrame.timestamp;
            timelineSlider.value = currentTime;
        }
    }

    private void SeekToPreviousFrame()
    {
        if (editedData.Count == 0) return;

        var prevFrame = editedData.Where(d => d.timestamp < currentTime)
                                  .OrderByDescending(d => d.timestamp)
                                  .FirstOrDefault();

        if (prevFrame != null)
        {
            currentTime = prevFrame.timestamp;
            timelineSlider.value = currentTime;
        }
    }

    private void SeekToNextKeyframe()
    {
        if (keyframeIndices.Count == 0) return;

        foreach (int index in keyframeIndices.OrderBy(i => i))
        {
            var frame = editedData.FirstOrDefault(d => d.frameIndex == index && d.timestamp > currentTime);
            if (frame != null)
            {
                currentTime = frame.timestamp;
                timelineSlider.value = currentTime;
                break;
            }
        }
    }

    private void SeekToPreviousKeyframe()
    {
        if (keyframeIndices.Count == 0) return;

        foreach (int index in keyframeIndices.OrderByDescending(i => i))
        {
            var frame = editedData.FirstOrDefault(d => d.frameIndex == index && d.timestamp < currentTime);
            if (frame != null)
            {
                currentTime = frame.timestamp;
                timelineSlider.value = currentTime;
                break;
            }
        }
    }

    private void SetStartPointToCurrent()
    {
        startTime = currentTime;
        startPointSlider.value = startTime;
        UpdateSelectedRangeVisual();
        UpdateRangeDisplay();
    }

    private void SetEndPointToCurrent()
    {
        endTime = currentTime;
        endPointSlider.value = endTime;
        UpdateSelectedRangeVisual();
        UpdateRangeDisplay();
    }

    private void SetPlaybackSpeed(float speed)
    {
        playbackSpeedSlider.value = speed;
        OnPlaybackSpeedChanged(speed);
    }

    private float FindNearestKeyframeTime(float time)
    {
        if (keyframeIndices.Count == 0) return time;

        float nearestTime = time;
        float minDistance = float.MaxValue;

        foreach (int index in keyframeIndices)
        {
            var frame = editedData.FirstOrDefault(d => d.frameIndex == index);
            if (frame != null)
            {
                float distance = Mathf.Abs(frame.timestamp - time);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestTime = frame.timestamp;
                }
            }
        }

        return nearestTime;
    }

    private void UpdateGhostHands()
    {
        if (!showGhostHandToggle.isOn || editedData.Count == 0) return;

        float ghostTime = currentTime + ghostHandOffsetSlider.value;

        // Ghost 시간의 데이터 찾기
        var ghostFrames = editedData.Where(d => Mathf.Abs(d.timestamp - ghostTime) < 0.05f).ToList();

        // Ghost 핸드에 적용 (구현 필요)
        // TODO: Ghost 핸드 포즈 적용
    }

    private void UpdateTimeDisplay()
    {
        if (currentTimeText != null)
        {
            currentTimeText.text = $"{currentTime:F2}s / F{currentFrame}";
        }

        if (totalDurationText != null)
        {
            totalDurationText.text = $"총 {totalDuration:F2}초";
        }
    }

    private void UpdateRangeDisplay()
    {
        if (selectedRangeText != null)
        {
            float duration = endTime - startTime;
            int startFrame = editedData.Where(d => d.timestamp <= startTime)
                                      .Select(d => d.frameIndex)
                                      .DefaultIfEmpty(0)
                                      .Max();
            int endFrame = editedData.Where(d => d.timestamp <= endTime)
                                    .Select(d => d.frameIndex)
                                    .DefaultIfEmpty(totalFrames)
                                    .Max();

            selectedRangeText.text = $"구간: {startTime:F2}s ~ {endTime:F2}s ({duration:F2}s)\n" +
                                    $"프레임: {startFrame} ~ {endFrame}";
        }
    }

    private void UpdateFileInfo()
    {
        if (frameInfoText != null)
        {
            frameInfoText.text = $"프레임: {totalFrames}\n" +
                               $"키프레임: {keyframeIndices.Count}";
        }

        if (fileSizeText != null)
        {
            // 예상 파일 크기 계산
            float estimatedSize = editedData.Count * 100f / 1024f; // 대략적인 계산
            fileSizeText.text = $"예상 크기: {estimatedSize:F1}KB";
        }

        if (compressionInfoText != null)
        {
            float avgInterval = totalDuration / totalFrames;
            float compressionRatio = (1f - (totalFrames / (totalDuration * 60f))) * 100f;
            compressionInfoText.text = $"평균 간격: {avgInterval:F3}s\n" +
                                      $"압축률: {compressionRatio:F1}%";
        }
    }

    private void UpdateButtonStates()
    {
        // 재생/일시정지 버튼
        if (playPauseButton != null)
        {
            var buttonText = playPauseButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = isPlaying ? "⏸" : "▶";
            }
        }

        // 실행 취소/다시 실행
        if (undoButton != null)
        {
            undoButton.interactable = undoStack.Count > 0;
        }

        if (redoButton != null)
        {
            redoButton.interactable = redoStack.Count > 0;
        }

        // 저장 버튼
        if (saveButton != null)
        {
            var buttonText = saveButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null && hasUnsavedChanges)
            {
                buttonText.text = "저장*";
            }
        }

        // 편집 버튼들
        bool hasData = editedData.Count > 0;
        if (trimButton != null) trimButton.interactable = hasData;
        if (exportButton != null) exportButton.interactable = hasData;
    }

    private void RefreshFileList()
    {
        if (fileListDropdown == null) return;

        fileListDropdown.ClearOptions();

        string[] files = Directory.GetFiles(Application.persistentDataPath, "*.csv");
        List<string> fileNames = new List<string>();

        foreach (string file in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            fileNames.Add(fileName);
        }

        if (fileNames.Count > 0)
        {
            fileListDropdown.AddOptions(fileNames);
        }
        else
        {
            fileListDropdown.AddOptions(new List<string> { "파일 없음" });
        }
    }

    private void LoadSelectedFile()
    {
        if (fileListDropdown == null || fileListDropdown.options.Count == 0) return;

        string selectedFile = fileListDropdown.options[fileListDropdown.value].text;

        if (selectedFile != "파일 없음")
        {
            LoadCSVFile(selectedFile);
        }
    }

    private void DeleteSelectedFile()
    {
        if (fileListDropdown == null || fileListDropdown.options.Count == 0) return;

        string selectedFile = fileListDropdown.options[fileListDropdown.value].text;

        if (selectedFile != "파일 없음")
        {
            string path = Path.Combine(Application.persistentDataPath, selectedFile + ".csv");

            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"파일 삭제됨: {selectedFile}");
                RefreshFileList();
            }
        }
    }

    private void OnLoopToggleChanged(bool value)
    {
        if (handPosePlayer != null)
        {
            // Player에 루프 설정 전달
        }
    }

    private void OnSnapToggleChanged(bool value)
    {
        // 스냅 설정 변경 시 현재 위치 재조정
        if (value)
        {
            currentTime = FindNearestKeyframeTime(currentTime);
            timelineSlider.value = currentTime;
        }
    }

    private void OnGhostHandToggleChanged(bool value)
    {
        if (leftHandGhost != null)
        {
            leftHandGhost.gameObject.SetActive(value);
        }

        if (rightHandGhost != null)
        {
            rightHandGhost.gameObject.SetActive(value);
        }

        if (value)
        {
            UpdateGhostHands();
        }
    }

    private void OnPlaybackProgress(float progress)
    {
        // 플레이어로부터 진행률 업데이트 받기
        if (!isPlaying) return;

        currentTime = progress * totalDuration;
        UpdateTimeDisplay();
    }

    private void OnPlaybackCompleted()
    {
        if (!loopToggle.isOn)
        {
            StopPlayback();
        }
    }

    void OnDestroy()
    {
        // 이벤트 해제
        if (handPosePlayer != null)
        {
            // 이벤트가 있는지 확인하고 해제
            var progressEvent = handPosePlayer.GetType().GetEvent("OnPlaybackProgress");
            if (progressEvent != null)
            {
                var field = handPosePlayer.GetType().GetField("OnPlaybackProgress",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                if (field != null)
                {
                    var eventDelegate = field.GetValue(handPosePlayer) as System.Delegate;
                    if (eventDelegate != null)
                    {
                        foreach (var d in eventDelegate.GetInvocationList())
                        {
                            if (d.Target == this)
                            {
                                progressEvent.RemoveEventHandler(handPosePlayer, d);
                            }
                        }
                    }
                }
            }

            var completedEvent = handPosePlayer.GetType().GetEvent("OnPlaybackCompleted");
            if (completedEvent != null)
            {
                var field = handPosePlayer.GetType().GetField("OnPlaybackCompleted",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                if (field != null)
                {
                    var eventDelegate = field.GetValue(handPosePlayer) as System.Delegate;
                    if (eventDelegate != null)
                    {
                        foreach (var d in eventDelegate.GetInvocationList())
                        {
                            if (d.Target == this)
                            {
                                completedEvent.RemoveEventHandler(handPosePlayer, d);
                            }
                        }
                    }
                }
            }
        }
    }

    // 툴팁 컴포넌트 (선택사항)
    [System.Serializable]
    public class Tooltip : MonoBehaviour
    {
        public string text;
    }
}