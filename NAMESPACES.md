# ChunaVR 프로젝트 네임스페이스 구조

## 네임스페이스 설계

```
ChunaVR
├── Core                    # 핵심 인프라
│   ├── ServiceLocator
│   └── EventSystem
│
├── Scenario               # 시나리오 시스템
│   ├── Manager
│   ├── Events
│   ├── Data
│   ├── CSV
│   └── Actions
│
├── PoseData              # 포즈 데이터 관리
│   ├── Player
│   ├── Recorder
│   ├── Comparer
│   ├── Config
│   └── Visualizer
│
├── Training              # 트레이닝 시스템
│   ├── Controller
│   ├── Integration
│   ├── Education
│   └── Data
│
├── Auth                  # 인증 시스템
│   ├── Service
│   ├── Events
│   ├── Data
│   └── UI
│
├── UI                    # UI 컴포넌트
│   ├── Controllers
│   ├── Popups
│   └── Timeline
│
└── Editor                # 에디터 확장
    ├── HandPose
    └── Scenario
```

## 파일별 네임스페이스 매핑

### Core
- `ServiceLocator.cs` → `ChunaVR.Core`

### Scenario
- `ScenarioManager.cs` → `ChunaVR.Scenario`
- `ScenarioEventSystem.cs` → `ChunaVR.Scenario.Events`
- `ScenarioDataClasses.cs` → `ChunaVR.Scenario.Data`
- `ScenarioCSVLoader.cs` → `ChunaVR.Scenario.CSV`
- `ScenarioActionHandler.cs` → `ChunaVR.Scenario.Actions`
- `ScenarioUIController.cs` → `ChunaVR.UI.Controllers`
- `ScenarioSystemInitializer.cs` → `ChunaVR.Scenario`
- `ScenarioPrototypeBuilder.cs` → `ChunaVR.Scenario`
- `ScenarioDataSO.cs` → `ChunaVR.Scenario.Data`
- `ScenarioDataSOEditor.cs` → `ChunaVR.Editor.Scenario`

### PoseData
- `HandPosePlayer.cs` → `ChunaVR.PoseData`
- `HandPoseRecorder.cs` → `ChunaVR.PoseData`
- `HandPoseComparer.cs` → `ChunaVR.PoseData`
- `HandPoseConfig.cs` → `ChunaVR.PoseData`
- `HandPoseDebugVisualizer.cs` → `ChunaVR.PoseData`
- `PosePlayer.cs` → `ChunaVR.PoseData`
- `PoseRecorder.cs` → `ChunaVR.PoseData`
- `ObjectController.cs` → `ChunaVR.PoseData`

### Training
- `IntegratedChunaTrainingSystem.cs` → `ChunaVR.Training`
- `ChunaTrainingController.cs` → `ChunaVR.Training`
- `ChunaEducationGuideSystem.cs` → `ChunaVR.Training.Education`
- `ChunaMotionDataManager.cs` → `ChunaVR.Training.Data`

### Auth
- `AuthenticationService.cs` → `ChunaVR.Auth`
- `IAuthenticationInterfaces.cs` → `ChunaVR.Auth`
- `AuthEvents.cs` → `ChunaVR.Auth.Events`
- `AuthDataClasses.cs` → `ChunaVR.Auth.Data`
- `LobbyAuthUI_Complete.cs` → `ChunaVR.Auth.UI`

### UI
- `ModeSelectionManagerV2.cs` → `ChunaVR.UI.Controllers`
- `QuickMenuController.cs` → `ChunaVR.UI.Controllers`
- `DotTimeline.cs` → `ChunaVR.UI.Timeline`
- `DotTimelineController.cs` → `ChunaVR.UI.Timeline`
- `ExitPopupController.cs` → `ChunaVR.UI.Popups`
- `SettingsPopupController.cs` → `ChunaVR.UI.Popups`
- `SimulationStartController.cs` → `ChunaVR.UI.Controllers`
- `ScenarioPrefabCreator.cs` → `ChunaVR.UI.Controllers`
- `ScenarioCardButton.cs` → `ChunaVR.UI.Controllers`

### Editor
- `HandPoseDataEditor.cs` → `ChunaVR.Editor.HandPose`
- `HandPoseEditorUIBuilder.cs` → `ChunaVR.Editor.HandPose`
- `HandPoseRecorder_Optimized.cs` → `ChunaVR.Editor.HandPose`

## 마이그레이션 우선순위

### Phase 1: Core & Infrastructure (즉시)
1. ✅ `ServiceLocator.cs` → `ChunaVR.Core`
2. ✅ `HandPoseConfig.cs` → `ChunaVR.PoseData`
3. ✅ `HandPoseComparer.cs` → `ChunaVR.PoseData`
4. ✅ `HandPoseDebugVisualizer.cs` → `ChunaVR.PoseData`

### Phase 2: Scenario System (1주)
5. `ScenarioEventSystem.cs` → `ChunaVR.Scenario.Events`
6. `ScenarioDataClasses.cs` → `ChunaVR.Scenario.Data`
7. `ScenarioManager.cs` → `ChunaVR.Scenario`
8. 나머지 Scenario 관련 파일

### Phase 3: Training & Auth (2주)
9. Training 시스템 파일들
10. Auth 시스템 파일들

### Phase 4: UI & Editor (3주)
11. UI 컨트롤러들
12. Editor 확장들

## Using 구문 가이드

```csharp
// Core 사용
using ChunaVR.Core;

// Scenario 사용
using ChunaVR.Scenario;
using ChunaVR.Scenario.Events;
using ChunaVR.Scenario.Data;

// PoseData 사용
using ChunaVR.PoseData;

// Training 사용
using ChunaVR.Training;
using ChunaVR.Training.Education;

// Auth 사용
using ChunaVR.Auth;
using ChunaVR.Auth.Events;

// UI 사용
using ChunaVR.UI.Controllers;
using ChunaVR.UI.Popups;
```

## 주의사항

1. **Unity 네임스페이스 유지**: UnityEngine, UnityEditor 등은 그대로 유지
2. **외부 라이브러리**: Oculus SDK, UniTask 등은 변경하지 않음
3. **점진적 적용**: 한 번에 모든 파일을 변경하지 않고 단계별로 적용
4. **테스트**: 각 단계마다 컴파일 확인

## 혜택

1. ✅ **이름 충돌 방지**: 다른 에셋과의 충돌 방지
2. ✅ **코드 구조 명확화**: 파일의 역할과 위치가 명확해짐
3. ✅ **유지보수 용이**: 관련 코드를 쉽게 찾을 수 있음
4. ✅ **프로젝트 스케일링**: 대규모 프로젝트로 확장 가능
