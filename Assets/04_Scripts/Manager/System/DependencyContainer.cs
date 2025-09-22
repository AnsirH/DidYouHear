using System;
using System.Collections.Generic;
using UnityEngine;

namespace DidYouHear.Manager.System
{
    /// <summary>
    /// 의존성 주입 컨테이너
    /// 
    /// 매니저 간의 의존성을 관리하고 해결하는 시스템입니다.
    /// 이 클래스는 다음과 같은 기능을 제공합니다:
    /// 
    /// 1. 서비스 등록: 매니저나 서비스를 컨테이너에 등록
    /// 2. 서비스 해결: 등록된 서비스를 타입으로 찾아서 반환
    /// 3. 싱글톤 관리: 싱글톤 서비스의 인스턴스 생명주기 관리
    /// 4. 팩토리 패턴: 서비스 생성 로직을 팩토리 메서드로 등록
    /// 
    /// 사용 예시:
    /// ```csharp
    /// // 싱글톤 등록
    /// container.RegisterSingleton<AudioManager>(audioManager);
    /// 
    /// // 팩토리 등록
    /// container.RegisterFactory<EventManager>(() => new EventManager(), true);
    /// 
    /// // 서비스 해결
    /// var audioManager = container.Get<AudioManager>();
    /// ```
    /// </summary>
    public class DependencyContainer
    {
        // 서비스 저장소들
        private Dictionary<Type, object> services = new Dictionary<Type, object>();        // 싱글톤 인스턴스 저장
        private Dictionary<Type, Func<object>> factories = new Dictionary<Type, Func<object>>();  // 팩토리 메서드 저장
        private Dictionary<Type, bool> singletons = new Dictionary<Type, bool>();          // 싱글톤 여부 저장

        /// <summary>
        /// 싱글톤 서비스 등록
        /// 
        /// 이미 생성된 인스턴스를 싱글톤으로 등록합니다.
        /// 같은 타입으로 다시 등록하면 이전 인스턴스가 교체됩니다.
        /// 
        /// <typeparam name="T">서비스 타입 (클래스여야 함)</typeparam>
        /// <param name="instance">등록할 서비스 인스턴스</param>
        /// 
        /// 예시:
        /// ```csharp
        /// var audioManager = FindObjectOfType<AudioManager>();
        /// container.RegisterSingleton(audioManager);
        /// ```
        /// </summary>
        public void RegisterSingleton<T>(T instance) where T : class
        {
            var type = typeof(T);
            services[type] = instance;
            singletons[type] = true;
            
            Debug.Log($"DependencyContainer: Registered singleton {type.Name}");
        }

        /// <summary>
        /// 팩토리 메서드로 서비스 등록
        /// </summary>
        /// <typeparam name="T">서비스 타입</typeparam>
        /// <param name="factory">팩토리 메서드</param>
        /// <param name="isSingleton">싱글톤 여부</param>
        public void RegisterFactory<T>(Func<T> factory, bool isSingleton = false) where T : class
        {
            var type = typeof(T);
            factories[type] = () => factory();
            singletons[type] = isSingleton;
            
            Debug.Log($"DependencyContainer: Registered factory for {type.Name} (Singleton: {isSingleton})");
        }

        /// <summary>
        /// 서비스 해결 (의존성 주입)
        /// </summary>
        /// <typeparam name="T">서비스 타입</typeparam>
        /// <returns>서비스 인스턴스</returns>
        public T Get<T>() where T : class
        {
            var type = typeof(T);
            
            // 이미 등록된 싱글톤 인스턴스가 있는지 확인
            if (services.ContainsKey(type))
            {
                return services[type] as T;
            }
            
            // 팩토리가 등록되어 있는지 확인
            if (factories.ContainsKey(type))
            {
                var instance = factories[type]() as T;
                
                // 싱글톤인 경우 인스턴스 저장
                if (singletons.ContainsKey(type) && singletons[type])
                {
                    services[type] = instance;
                }
                
                return instance;
            }
            
            Debug.LogWarning($"DependencyContainer: Service {type.Name} not found");
            return null;
        }

        /// <summary>
        /// 서비스가 등록되어 있는지 확인
        /// </summary>
        /// <typeparam name="T">서비스 타입</typeparam>
        /// <returns>등록 여부</returns>
        public bool Contains<T>() where T : class
        {
            var type = typeof(T);
            return services.ContainsKey(type) || factories.ContainsKey(type);
        }

        /// <summary>
        /// 서비스 등록 해제
        /// </summary>
        /// <typeparam name="T">서비스 타입</typeparam>
        public void Unregister<T>() where T : class
        {
            var type = typeof(T);
            services.Remove(type);
            factories.Remove(type);
            singletons.Remove(type);
            
            Debug.Log($"DependencyContainer: Unregistered {type.Name}");
        }

        /// <summary>
        /// 모든 서비스 등록 해제
        /// </summary>
        public void Clear()
        {
            services.Clear();
            factories.Clear();
            singletons.Clear();
            
            Debug.Log("DependencyContainer: Cleared all services");
        }

        /// <summary>
        /// 등록된 서비스 목록 반환
        /// </summary>
        /// <returns>서비스 타입 목록</returns>
        public List<Type> GetRegisteredServices()
        {
            var types = new List<Type>();
            types.AddRange(services.Keys);
            types.AddRange(factories.Keys);
            return types;
        }

        /// <summary>
        /// 컨테이너 상태 정보 반환
        /// </summary>
        /// <returns>상태 정보 문자열</returns>
        public string GetStatus()
        {
            return $"DependencyContainer: {services.Count} singletons, {factories.Count} factories";
        }
    }
}
