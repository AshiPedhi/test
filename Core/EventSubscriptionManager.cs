using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChunaVR.Core
{
    /// <summary>
    /// 이벤트 구독 자동 관리 헬퍼
    /// using 문과 함께 사용하거나 MonoBehaviour에서 관리
    ///
    /// [사용법 1 - using]
    /// using (var subscription = new EventSubscriptionManager())
    /// {
    ///     subscription.Subscribe(() => SomeEvent += Handler, () => SomeEvent -= Handler);
    /// }
    ///
    /// [사용법 2 - MonoBehaviour]
    /// private EventSubscriptionManager subscriptions = new EventSubscriptionManager();
    ///
    /// void OnEnable()
    /// {
    ///     subscriptions.Subscribe(() => SomeEvent += Handler, () => SomeEvent -= Handler);
    /// }
    ///
    /// void OnDisable()
    /// {
    ///     subscriptions.UnsubscribeAll();
    /// }
    /// </summary>
    public class EventSubscriptionManager : IDisposable
    {
        private readonly List<Action> unsubscribeActions = new List<Action>();
        private bool isDisposed = false;

        /// <summary>
        /// 이벤트 구독 추가
        /// </summary>
        /// <param name="subscribeAction">구독 액션 (예: () => Event += Handler)</param>
        /// <param name="unsubscribeAction">구독 해제 액션 (예: () => Event -= Handler)</param>
        public void Subscribe(Action subscribeAction, Action unsubscribeAction)
        {
            if (isDisposed)
            {
                Debug.LogWarning("[EventSubscriptionManager] 이미 해제된 매니저에 구독을 추가하려고 했습니다.");
                return;
            }

            try
            {
                subscribeAction?.Invoke();
                unsubscribeActions.Add(unsubscribeAction);
            }
            catch (Exception e)
            {
                Debug.LogError($"[EventSubscriptionManager] 구독 중 오류 발생: {e.Message}");
            }
        }

        /// <summary>
        /// 이벤트 구독 추가 (간단 버전 - 이벤트와 핸들러 직접 전달)
        /// </summary>
        public void Subscribe<T>(ref Action<T> eventField, Action<T> handler)
        {
            Subscribe(
                () => eventField += handler,
                () => eventField -= handler
            );
        }

        /// <summary>
        /// 파라미터 없는 이벤트 구독
        /// </summary>
        public void Subscribe(ref Action eventField, Action handler)
        {
            Subscribe(
                () => eventField += handler,
                () => eventField -= handler
            );
        }

        /// <summary>
        /// 모든 구독 해제
        /// </summary>
        public void UnsubscribeAll()
        {
            if (isDisposed)
                return;

            foreach (var unsubscribe in unsubscribeActions)
            {
                try
                {
                    unsubscribe?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EventSubscriptionManager] 구독 해제 중 오류 발생: {e.Message}");
                }
            }

            unsubscribeActions.Clear();
        }

        /// <summary>
        /// Dispose 패턴 구현
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
                return;

            UnsubscribeAll();
            isDisposed = true;
        }

        /// <summary>
        /// 현재 구독 수 반환
        /// </summary>
        public int SubscriptionCount => unsubscribeActions.Count;
    }

    /// <summary>
    /// EventSubscriptionManager를 자동으로 관리하는 MonoBehaviour 베이스 클래스
    ///
    /// [사용법]
    /// public class MyController : EventManagedBehaviour
    /// {
    ///     protected override void SubscribeToEvents()
    ///     {
    ///         AddSubscription(() => SomeEvent += OnSomeEvent, () => SomeEvent -= OnSomeEvent);
    ///     }
    ///
    ///     private void OnSomeEvent()
    ///     {
    ///         // 이벤트 처리
    ///     }
    /// }
    /// </summary>
    public abstract class EventManagedBehaviour : MonoBehaviour
    {
        private EventSubscriptionManager subscriptions;

        protected virtual void OnEnable()
        {
            subscriptions = new EventSubscriptionManager();
            SubscribeToEvents();
        }

        protected virtual void OnDisable()
        {
            subscriptions?.UnsubscribeAll();
        }

        protected virtual void OnDestroy()
        {
            subscriptions?.Dispose();
            subscriptions = null;
        }

        /// <summary>
        /// 이벤트 구독 설정 (파생 클래스에서 오버라이드)
        /// </summary>
        protected abstract void SubscribeToEvents();

        /// <summary>
        /// 이벤트 구독 추가
        /// </summary>
        protected void AddSubscription(Action subscribeAction, Action unsubscribeAction)
        {
            subscriptions?.Subscribe(subscribeAction, unsubscribeAction);
        }

        /// <summary>
        /// 현재 구독 수
        /// </summary>
        protected int SubscriptionCount => subscriptions?.SubscriptionCount ?? 0;
    }
}
