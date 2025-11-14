using ChunaVR.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using ChunaVR.PoseData;

namespace ChunaVR.Editor.HandPose
{

    public class HandPoseEditorUIBuilder : MonoBehaviour
    {
        [MenuItem("GameObject/VR Hand Tracking/Hand Pose Editor UI", false, 10)]
        public static void CreateHandPoseEditorUI()
        {
            // Canvas 생성 또는 찾기 (Editor 전용 - 한 번만 실행되므로 성능 영향 없음)
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();

                // VR용 월드 스페이스 설정
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                canvasRect.localScale = Vector3.one * 0.001f;
                canvasRect.sizeDelta = new Vector2(1920, 1080);
                canvasRect.position = new Vector3(0, 1.5f, 2f);
            }

            // 메인 에디터 패널 생성
            GameObject editorPanel = new GameObject("HandPoseEditorPanel");
            editorPanel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = editorPanel.AddComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(1600, 900);
            panelRect.anchoredPosition = Vector2.zero;

            // 배경
            Image bg = editorPanel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            // 에디터 컴포넌트 추가
            HandPoseDataEditor editor = editorPanel.AddComponent<HandPoseDataEditor>();

            // 타이틀 바
            CreateTitleBar(editorPanel.transform);

            // 메인 레이아웃
            GameObject mainLayout = CreateMainLayout(editorPanel.transform);

            // 타임라인 섹션
            GameObject timelineSection = CreateTimelineSection(mainLayout.transform, editor);

            // 컨트롤 버튼 섹션
            GameObject controlSection = CreateControlSection(mainLayout.transform, editor);

            // 정보 패널
            GameObject infoPanel = CreateInfoPanel(mainLayout.transform, editor);

            // 파일 관리 섹션
            GameObject fileSection = CreateFileSection(mainLayout.transform, editor);

            // 편집 옵션 섹션
            GameObject optionsSection = CreateOptionsSection(mainLayout.transform, editor);

            // 에디터에 UI 요소 연결
            editor.editorPanel = editorPanel;

            Debug.Log("Hand Pose Editor UI 생성 완료!");
            Selection.activeGameObject = editorPanel;
        }

        private static void CreateTitleBar(Transform parent)
        {
            GameObject titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(parent, false);
            RectTransform titleRect = titleBar.AddComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(1600, 80);
            titleRect.anchoredPosition = new Vector2(0, 410);

            Image titleBg = titleBar.AddComponent<Image>();
            titleBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // 타이틀 텍스트
            GameObject titleText = new GameObject("Title");
            titleText.transform.SetParent(titleBar.transform, false);
            TextMeshProUGUI title = titleText.AddComponent<TextMeshProUGUI>();
            title.text = "Hand Pose Data Editor";
            title.fontSize = 36;
            title.alignment = TextAlignmentOptions.Center;
            title.color = Color.white;

            RectTransform textRect = titleText.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(800, 80);
            textRect.anchoredPosition = Vector2.zero;
        }

        private static GameObject CreateMainLayout(Transform parent)
        {
            GameObject layout = new GameObject("MainLayout");
            layout.transform.SetParent(parent, false);
            RectTransform layoutRect = layout.AddComponent<RectTransform>();
            layoutRect.sizeDelta = new Vector2(1600, 800);
            layoutRect.anchoredPosition = new Vector2(0, -50);

            return layout;
        }

        private static GameObject CreateTimelineSection(Transform parent, HandPoseDataEditor editor)
        {
            GameObject section = new GameObject("TimelineSection");
            section.transform.SetParent(parent, false);
            RectTransform sectionRect = section.AddComponent<RectTransform>();
            sectionRect.sizeDelta = new Vector2(1400, 300);
            sectionRect.anchoredPosition = new Vector2(0, 200);

            // 배경
            Image bg = section.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // 메인 타임라인 슬라이더
            GameObject timeline = CreateSlider(section.transform, "Timeline", new Vector2(0, 50), new Vector2(1200, 40));
            editor.timelineSlider = timeline.GetComponent<Slider>();

            // 선택 구간 시각화
            GameObject rangeVisual = new GameObject("SelectedRange");
            rangeVisual.transform.SetParent(timeline.transform, false);
            Image rangeImage = rangeVisual.AddComponent<Image>();
            rangeImage.color = new Color(0.3f, 0.5f, 1f, 0.3f);
            editor.selectedRangeVisual = rangeVisual.GetComponent<RectTransform>();

            // 플레이헤드
            GameObject playhead = new GameObject("Playhead");
            playhead.transform.SetParent(timeline.transform, false);
            Image playheadImage = playhead.AddComponent<Image>();
            playheadImage.color = Color.red;
            RectTransform playheadRect = playhead.GetComponent<RectTransform>();
            playheadRect.sizeDelta = new Vector2(4, 60);
            editor.playheadVisual = playheadRect;

            // 시작/끝 포인트 슬라이더
            GameObject startSlider = CreateSlider(section.transform, "Start Point", new Vector2(-400, -50), new Vector2(500, 30));
            editor.startPointSlider = startSlider.GetComponent<Slider>();

            GameObject endSlider = CreateSlider(section.transform, "End Point", new Vector2(400, -50), new Vector2(500, 30));
            editor.endPointSlider = endSlider.GetComponent<Slider>();

            // 시간 표시 텍스트
            GameObject timeDisplay = new GameObject("TimeDisplay");
            timeDisplay.transform.SetParent(section.transform, false);
            TextMeshProUGUI timeText = timeDisplay.AddComponent<TextMeshProUGUI>();
            timeText.fontSize = 24;
            timeText.alignment = TextAlignmentOptions.Center;
            RectTransform timeRect = timeDisplay.GetComponent<RectTransform>();
            timeRect.sizeDelta = new Vector2(300, 40);
            timeRect.anchoredPosition = new Vector2(0, -100);
            editor.currentTimeText = timeText;

            // 구간 정보 텍스트
            GameObject rangeInfo = new GameObject("RangeInfo");
            rangeInfo.transform.SetParent(section.transform, false);
            TextMeshProUGUI rangeText = rangeInfo.AddComponent<TextMeshProUGUI>();
            rangeText.fontSize = 20;
            rangeText.alignment = TextAlignmentOptions.Center;
            RectTransform rangeRect = rangeInfo.GetComponent<RectTransform>();
            rangeRect.sizeDelta = new Vector2(600, 40);
            rangeRect.anchoredPosition = new Vector2(0, -130);
            editor.selectedRangeText = rangeText;

            // 키프레임 마커 컨테이너
            GameObject markerContainer = new GameObject("MarkerContainer");
            markerContainer.transform.SetParent(timeline.transform, false);
            editor.markerContainer = markerContainer.transform;

            return section;
        }

        private static GameObject CreateControlSection(Transform parent, HandPoseDataEditor editor)
        {
            GameObject section = new GameObject("ControlSection");
            section.transform.SetParent(parent, false);
            RectTransform sectionRect = section.AddComponent<RectTransform>();
            sectionRect.sizeDelta = new Vector2(1400, 120);
            sectionRect.anchoredPosition = new Vector2(0, 0);

            // 재생 컨트롤 버튼들
            float buttonSpacing = 120;
            float startX = -400;

            // Play/Pause
            GameObject playPauseBtn = CreateButton(section.transform, "▶", new Vector2(startX, 0), new Vector2(100, 80));
            editor.playPauseButton = playPauseBtn.GetComponent<Button>();

            // Stop
            GameObject stopBtn = CreateButton(section.transform, "■", new Vector2(startX + buttonSpacing, 0), new Vector2(100, 80));
            editor.stopButton = stopBtn.GetComponent<Button>();

            // Trim
            GameObject trimBtn = CreateButton(section.transform, "✂", new Vector2(startX + buttonSpacing * 2, 0), new Vector2(100, 80));
            editor.trimButton = trimBtn.GetComponent<Button>();

            // Save
            GameObject saveBtn = CreateButton(section.transform, "💾", new Vector2(startX + buttonSpacing * 3, 0), new Vector2(100, 80));
            editor.saveButton = saveBtn.GetComponent<Button>();

            // Undo
            GameObject undoBtn = CreateButton(section.transform, "↶", new Vector2(startX + buttonSpacing * 4, 0), new Vector2(100, 80));
            editor.undoButton = undoBtn.GetComponent<Button>();

            // Redo
            GameObject redoBtn = CreateButton(section.transform, "↷", new Vector2(startX + buttonSpacing * 5, 0), new Vector2(100, 80));
            editor.redoButton = redoBtn.GetComponent<Button>();

            // Export
            GameObject exportBtn = CreateButton(section.transform, "📤", new Vector2(startX + buttonSpacing * 6, 0), new Vector2(100, 80));
            editor.exportButton = exportBtn.GetComponent<Button>();

            // 재생 속도 슬라이더
            GameObject speedControl = new GameObject("SpeedControl");
            speedControl.transform.SetParent(section.transform, false);
            RectTransform speedRect = speedControl.AddComponent<RectTransform>();
            speedRect.sizeDelta = new Vector2(300, 80);
            speedRect.anchoredPosition = new Vector2(500, 0);

            GameObject speedSlider = CreateSlider(speedControl.transform, "Speed", Vector2.zero, new Vector2(200, 30));
            Slider speed = speedSlider.GetComponent<Slider>();
            speed.minValue = 0.25f;
            speed.maxValue = 2f;
            speed.value = 1f;
            editor.playbackSpeedSlider = speed;

            GameObject speedText = new GameObject("SpeedText");
            speedText.transform.SetParent(speedControl.transform, false);
            TextMeshProUGUI speedLabel = speedText.AddComponent<TextMeshProUGUI>();
            speedLabel.text = "1.0x";
            speedLabel.fontSize = 20;
            speedLabel.alignment = TextAlignmentOptions.Center;
            RectTransform labelRect = speedText.GetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(80, 30);
            labelRect.anchoredPosition = new Vector2(0, -30);
            editor.playbackSpeedText = speedLabel;

            return section;
        }

        private static GameObject CreateInfoPanel(Transform parent, HandPoseDataEditor editor)
        {
            GameObject panel = new GameObject("InfoPanel");
            panel.transform.SetParent(parent, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(400, 300);
            panelRect.anchoredPosition = new Vector2(-500, -200);

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // 정보 텍스트들
            string[] infoLabels = { "Total Duration", "Frame Info", "File Size", "Compression" };
            TextMeshProUGUI[] infoTexts = new TextMeshProUGUI[4];

            for (int i = 0; i < infoLabels.Length; i++)
            {
                GameObject infoItem = new GameObject(infoLabels[i]);
                infoItem.transform.SetParent(panel.transform, false);
                TextMeshProUGUI text = infoItem.AddComponent<TextMeshProUGUI>();
                text.fontSize = 18;
                text.color = Color.white;

                RectTransform textRect = infoItem.GetComponent<RectTransform>();
                textRect.sizeDelta = new Vector2(380, 60);
                textRect.anchoredPosition = new Vector2(0, 100 - i * 70);

                infoTexts[i] = text;
            }

            editor.totalDurationText = infoTexts[0];
            editor.frameInfoText = infoTexts[1];
            editor.fileSizeText = infoTexts[2];
            editor.compressionInfoText = infoTexts[3];

            return panel;
        }

        private static GameObject CreateFileSection(Transform parent, HandPoseDataEditor editor)
        {
            GameObject section = new GameObject("FileSection");
            section.transform.SetParent(parent, false);
            RectTransform sectionRect = section.AddComponent<RectTransform>();
            sectionRect.sizeDelta = new Vector2(400, 300);
            sectionRect.anchoredPosition = new Vector2(0, -200);

            Image bg = section.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // 파일명 입력
            GameObject fileNameField = CreateInputField(section.transform, "File Name", new Vector2(0, 100), new Vector2(350, 40));
            editor.fileNameInput = fileNameField.GetComponent<TMP_InputField>();

            // 파일 리스트 드롭다운
            GameObject dropdown = new GameObject("FileDropdown");
            dropdown.transform.SetParent(section.transform, false);
            RectTransform dropRect = dropdown.AddComponent<RectTransform>();
            dropRect.sizeDelta = new Vector2(350, 40);
            dropRect.anchoredPosition = new Vector2(0, 30);

            TMP_Dropdown fileDropdown = dropdown.AddComponent<TMP_Dropdown>();
            Image dropBg = dropdown.AddComponent<Image>();
            dropBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            editor.fileListDropdown = fileDropdown;

            // 파일 관리 버튼들
            GameObject loadBtn = CreateButton(section.transform, "Load", new Vector2(-90, -40), new Vector2(80, 40));
            editor.loadButton = loadBtn.GetComponent<Button>();

            GameObject refreshBtn = CreateButton(section.transform, "↻", new Vector2(0, -40), new Vector2(80, 40));
            editor.refreshFilesButton = refreshBtn.GetComponent<Button>();

            GameObject deleteBtn = CreateButton(section.transform, "🗑", new Vector2(90, -40), new Vector2(80, 40));
            editor.deleteFileButton = deleteBtn.GetComponent<Button>();

            return section;
        }

        private static GameObject CreateOptionsSection(Transform parent, HandPoseDataEditor editor)
        {
            GameObject section = new GameObject("OptionsSection");
            section.transform.SetParent(parent, false);
            RectTransform sectionRect = section.AddComponent<RectTransform>();
            sectionRect.sizeDelta = new Vector2(400, 300);
            sectionRect.anchoredPosition = new Vector2(500, -200);

            Image bg = section.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // 옵션 토글들
            GameObject loopToggle = CreateToggle(section.transform, "Loop Playback", new Vector2(0, 100));
            editor.loopToggle = loopToggle.GetComponent<Toggle>();

            GameObject snapToggle = CreateToggle(section.transform, "Snap to Keyframes", new Vector2(0, 50));
            editor.snapToKeyframeToggle = snapToggle.GetComponent<Toggle>();

            GameObject previewToggle = CreateToggle(section.transform, "Preview Both Hands", new Vector2(0, 0));
            editor.previewBothHandsToggle = previewToggle.GetComponent<Toggle>();

            GameObject ghostToggle = CreateToggle(section.transform, "Show Ghost Hand", new Vector2(0, -50));
            editor.showGhostHandToggle = ghostToggle.GetComponent<Toggle>();

            // Ghost 오프셋 슬라이더
            GameObject ghostOffset = CreateSlider(section.transform, "Ghost Offset", new Vector2(0, -100), new Vector2(300, 30));
            Slider offsetSlider = ghostOffset.GetComponent<Slider>();
            offsetSlider.minValue = -1f;
            offsetSlider.maxValue = 1f;
            offsetSlider.value = 0.1f;
            editor.ghostHandOffsetSlider = offsetSlider;

            return section;
        }

        private static GameObject CreateButton(Transform parent, string text, Vector2 position, Vector2 size)
        {
            GameObject button = new GameObject("Button_" + text);
            button.transform.SetParent(parent, false);

            RectTransform rect = button.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Image image = button.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            Button btn = button.AddComponent<Button>();

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(button.transform, false);
            TextMeshProUGUI btnText = textObj.AddComponent<TextMeshProUGUI>();
            btnText.text = text;
            btnText.fontSize = 24;
            btnText.alignment = TextAlignmentOptions.Center;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = size;
            textRect.anchoredPosition = Vector2.zero;

            return button;
        }

        private static GameObject CreateSlider(Transform parent, string label, Vector2 position, Vector2 size)
        {
            GameObject sliderObj = new GameObject("Slider_" + label);
            sliderObj.transform.SetParent(parent, false);

            RectTransform rect = sliderObj.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Slider slider = sliderObj.AddComponent<Slider>();

            // Background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(sliderObj.transform, false);
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.sizeDelta = size;

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillRect = fillArea.AddComponent<RectTransform>();
            fillRect.sizeDelta = size;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.5f, 1f, 1f);
            RectTransform fillImgRect = fill.GetComponent<RectTransform>();
            fillImgRect.sizeDelta = size;

            // Handle
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.sizeDelta = size;

            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, size.y);

            slider.fillRect = fillImgRect;
            slider.handleRect = handleRect;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(sliderObj.transform, false);
            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 16;
            labelText.alignment = TextAlignmentOptions.Center;
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(200, 30);
            labelRect.anchoredPosition = new Vector2(0, size.y * 0.5f + 15);

            return sliderObj;
        }

        private static GameObject CreateToggle(Transform parent, string label, Vector2 position)
        {
            GameObject toggleObj = new GameObject("Toggle_" + label);
            toggleObj.transform.SetParent(parent, false);

            RectTransform rect = toggleObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 30);
            rect.anchoredPosition = position;

            Toggle toggle = toggleObj.AddComponent<Toggle>();

            // Background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(toggleObj.transform, false);
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(30, 30);
            bgRect.anchoredPosition = new Vector2(-135, 0);

            // Checkmark
            GameObject checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(bg.transform, false);
            Image checkImage = checkmark.AddComponent<Image>();
            checkImage.color = new Color(0.3f, 1f, 0.3f, 1f);
            RectTransform checkRect = checkmark.GetComponent<RectTransform>();
            checkRect.sizeDelta = new Vector2(20, 20);

            toggle.graphic = checkImage;
            toggle.targetGraphic = bgImage;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(toggleObj.transform, false);
            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 18;
            labelText.alignment = TextAlignmentOptions.Left;
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(250, 30);
            labelRect.anchoredPosition = new Vector2(25, 0);

            return toggleObj;
        }

        private static GameObject CreateInputField(Transform parent, string placeholder, Vector2 position, Vector2 size)
        {
            GameObject inputObj = new GameObject("InputField_" + placeholder);
            inputObj.transform.SetParent(parent, false);

            RectTransform rect = inputObj.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Image bg = inputObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();

            // Text Area
            GameObject textArea = new GameObject("Text Area");
            textArea.transform.SetParent(inputObj.transform, false);
            RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.sizeDelta = size;
            RectMask2D mask = textArea.AddComponent<RectMask2D>();

            // Placeholder
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(textArea.transform, false);
            TextMeshProUGUI placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholderText.text = placeholder;
            placeholderText.fontSize = 18;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            RectTransform placeholderRect = placeholderObj.GetComponent<RectTransform>();
            placeholderRect.sizeDelta = size;
            placeholderRect.anchoredPosition = new Vector2(10, 0);

            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(textArea.transform, false);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.fontSize = 18;
            text.color = Color.white;
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = size;
            textRect.anchoredPosition = new Vector2(10, 0);

            inputField.textViewport = textAreaRect;
            inputField.textComponent = text;
            inputField.placeholder = placeholderText;

            return inputObj;
        }
    }}
}
