using System;

namespace ChunaVR.UI.Events
{
    /// <summary>
    /// UI 이벤트 시스템
    /// 순환 참조를 방지하고 느슨한 결합을 위한 이벤트 허브
    /// </summary>
    public static class UIEvents
    {
        #region Mode Selection Events
        /// <summary>모드가 선택되었을 때</summary>
        public static event Action<string> OnModeSelected;

        /// <summary>난이도가 선택되었을 때</summary>
        public static event Action<string> OnDifficultySelected;

        /// <summary>시뮬레이션 시작 요청</summary>
        public static event Action OnSimulationStartRequested;
        #endregion

        #region Exit/Navigation Events
        /// <summary>종료 확인 버튼 클릭</summary>
        public static event Action OnExitConfirmed;

        /// <summary>종료 취소 버튼 클릭</summary>
        public static event Action OnExitCancelled;

        /// <summary>로비로 돌아가기</summary>
        public static event Action OnReturnToLobby;

        /// <summary>Exit 팝업이 닫혔을 때</summary>
        public static event Action OnExitPopupClosed;
        #endregion

        #region Settings Events
        /// <summary>설정 팝업 열림</summary>
        public static event Action OnSettingsOpened;

        /// <summary>설정 팝업 닫힘</summary>
        public static event Action OnSettingsClosed;

        /// <summary>설정 변경됨</summary>
        public static event Action<string, object> OnSettingChanged;
        #endregion

        #region Popup Events
        /// <summary>모든 팝업 닫기 요청</summary>
        public static event Action OnCloseAllPopupsRequested;
        #endregion

        #region Trigger Methods - Mode Selection
        public static void TriggerModeSelected(string mode)
        {
            OnModeSelected?.Invoke(mode);
            LogEvent($"모드 선택: {mode}");
        }

        public static void TriggerDifficultySelected(string difficulty)
        {
            OnDifficultySelected?.Invoke(difficulty);
            LogEvent($"난이도 선택: {difficulty}");
        }

        public static void TriggerSimulationStart()
        {
            OnSimulationStartRequested?.Invoke();
            LogEvent("시뮬레이션 시작 요청");
        }
        #endregion

        #region Trigger Methods - Exit/Navigation
        public static void TriggerExitConfirmed()
        {
            OnExitConfirmed?.Invoke();
            LogEvent("종료 확인");
        }

        public static void TriggerExitCancelled()
        {
            OnExitCancelled?.Invoke();
            LogEvent("종료 취소");
        }

        public static void TriggerReturnToLobby()
        {
            OnReturnToLobby?.Invoke();
            LogEvent("로비로 돌아가기");
        }

        public static void TriggerExitPopupClosed()
        {
            OnExitPopupClosed?.Invoke();
            LogEvent("Exit 팝업 닫힘");
        }
        #endregion

        #region Trigger Methods - Settings
        public static void TriggerSettingsOpened()
        {
            OnSettingsOpened?.Invoke();
            LogEvent("설정 열림");
        }

        public static void TriggerSettingsClosed()
        {
            OnSettingsClosed?.Invoke();
            LogEvent("설정 닫힘");
        }

        public static void TriggerSettingChanged(string settingName, object value)
        {
            OnSettingChanged?.Invoke(settingName, value);
            LogEvent($"설정 변경: {settingName} = {value}");
        }
        #endregion

        #region Trigger Methods - Popup
        public static void TriggerCloseAllPopups()
        {
            OnCloseAllPopupsRequested?.Invoke();
            LogEvent("모든 팝업 닫기 요청");
        }
        #endregion

        #region Utility
        /// <summary>
        /// 모든 이벤트 구독 해제
        /// </summary>
        public static void ClearAllEvents()
        {
            OnModeSelected = null;
            OnDifficultySelected = null;
            OnSimulationStartRequested = null;

            OnExitConfirmed = null;
            OnExitCancelled = null;
            OnReturnToLobby = null;
            OnExitPopupClosed = null;

            OnSettingsOpened = null;
            OnSettingsClosed = null;
            OnSettingChanged = null;

            OnCloseAllPopupsRequested = null;

            LogEvent("모든 UI 이벤트 구독 해제");
        }

        private static void LogEvent(string message)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log($"[UIEvents] {message}");
#endif
        }

        /// <summary>
        /// 이벤트 구독자 수 로그
        /// </summary>
        public static void LogSubscriberCount()
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log($"[UIEvents] 이벤트 구독자 수:");
            UnityEngine.Debug.Log($"  - OnExitConfirmed: {OnExitConfirmed?.GetInvocationList().Length ?? 0}");
            UnityEngine.Debug.Log($"  - OnModeSelected: {OnModeSelected?.GetInvocationList().Length ?? 0}");
            UnityEngine.Debug.Log($"  - OnSettingsOpened: {OnSettingsOpened?.GetInvocationList().Length ?? 0}");
#endif
        }
        #endregion
    }
}
