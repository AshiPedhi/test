using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Service Locator 패턴 구현
/// FindObjectOfType 대신 사용하여 성능 향상
///
/// [사용법]
/// 1. 서비스 등록: ServiceLocator.Register(this);
/// 2. 서비스 가져오기: var manager = ServiceLocator.Get<ScenarioManager>();
/// </summary>
public static class ServiceLocator
{
    private static Dictionary<Type, object> services = new Dictionary<Type, object>();
    private static bool isQuitting = false;

    /// <summary>
    /// 서비스 등록
    /// </summary>
    public static void Register<T>(T service) where T : class
    {
        if (service == null)
        {
            Debug.LogError($"[ServiceLocator] Cannot register null service of type {typeof(T).Name}");
            return;
        }

        Type type = typeof(T);

        if (services.ContainsKey(type))
        {
            Debug.LogWarning($"[ServiceLocator] Service {type.Name} is already registered. Replacing...");
            services[type] = service;
        }
        else
        {
            services.Add(type, service);
            Debug.Log($"[ServiceLocator] Registered service: {type.Name}");
        }
    }

    /// <summary>
    /// 서비스 가져오기
    /// </summary>
    public static T Get<T>() where T : class
    {
        Type type = typeof(T);

        if (services.TryGetValue(type, out var service))
        {
            return service as T;
        }

        if (!isQuitting)
        {
            Debug.LogError($"[ServiceLocator] Service {type.Name} not found! Make sure it's registered in Awake().");
        }

        return null;
    }

    /// <summary>
    /// 서비스가 등록되어 있는지 확인
    /// </summary>
    public static bool IsRegistered<T>() where T : class
    {
        return services.ContainsKey(typeof(T));
    }

    /// <summary>
    /// 서비스 등록 해제
    /// </summary>
    public static void Unregister<T>() where T : class
    {
        Type type = typeof(T);

        if (services.Remove(type))
        {
            Debug.Log($"[ServiceLocator] Unregistered service: {type.Name}");
        }
    }

    /// <summary>
    /// 모든 서비스 초기화
    /// </summary>
    public static void Clear()
    {
        services.Clear();
        Debug.Log("[ServiceLocator] All services cleared");
    }

    /// <summary>
    /// 등록된 서비스 목록 출력 (디버그용)
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        services.Clear();
        isQuitting = false;
    }

    /// <summary>
    /// 등록된 서비스 목록 출력 (디버그용)
    /// </summary>
    public static void PrintRegisteredServices()
    {
        Debug.Log($"[ServiceLocator] Total registered services: {services.Count}");

        foreach (var kvp in services)
        {
            Debug.Log($"  - {kvp.Key.Name}");
        }
    }

    /// <summary>
    /// 애플리케이션 종료 시 플래그 설정
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        Application.quitting += () => isQuitting = true;
    }
}

/// <summary>
/// ServiceLocator에 자동 등록하는 베이스 클래스
///
/// [사용법]
/// public class MyManager : ServiceBehaviour<MyManager>
/// {
///     // 자동으로 ServiceLocator에 등록됨
/// }
/// </summary>
public abstract class ServiceBehaviour<T> : MonoBehaviour where T : ServiceBehaviour<T>
{
    protected virtual void Awake()
    {
        ServiceLocator.Register(this as T);
    }

    protected virtual void OnDestroy()
    {
        ServiceLocator.Unregister<T>();
    }
}
