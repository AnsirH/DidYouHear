using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DidYouHear.Manager.Interfaces;

namespace DidYouHear.Manager.System
{
    /// <summary>
    /// 매니저 초기화 시스템
    /// 
    /// 우선순위 기반으로 매니저들을 순차적으로 초기화하는 핵심 시스템입니다.
    /// 이 클래스는 다음과 같은 기능을 제공합니다:
    /// 
    /// 1. 우선순위 기반 초기화: 매니저의 GetPriority() 값을 기준으로 초기화 순서 결정
    /// 2. 의존성 주입: 각 매니저에 DependencyContainer를 주입하여 다른 매니저 참조 가능
    /// 3. 초기화 상태 관리: 매니저의 초기화 상태를 추적하고 관리
    /// 4. 오류 처리: 초기화 실패 시 로그 출력 및 이벤트 발생
    /// 5. 매니저 활성화 제어: 타입별, 범위별로 매니저 활성화/비활성화 가능
    /// 
    /// 초기화 순서 예시:
    /// 1. GameManager (Priority: 0) - 최우선
    /// 2. InputManager (Priority: 1)
    /// 3. AudioManager (Priority: 10)
    /// 4. EventManager (Priority: 11)
    /// 5. GhostManager (Priority: 20)
    /// 6. StonePool (Priority: 25)
    /// 
    /// 사용 방법:
    /// 1. ManagerInitializer 컴포넌트를 GameObject에 추가
    /// 2. RegisterManager()로 매니저들을 등록
    /// 3. InitializeAllManagers()로 초기화 시작
    /// </summary>
    public class ManagerInitializer : MonoBehaviour
    {
        [Header("Initialization Settings")]
        public bool enableDebugLogging = true;
        public bool autoInitializeOnStart = true;
        
        private List<IManager> managers = new List<IManager>();
        private DependencyContainer dependencyContainer;
        private bool isInitialized = false;
        
        // 싱글톤
        public static ManagerInitializer Instance { get; private set; }
        
        // 이벤트
        public static event Action<IManager> OnManagerInitialized;
        public static event Action<IManager> OnManagerInitializationFailed;
        public static event Action OnAllManagersInitialized;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            if (autoInitializeOnStart)
            {
                InitializeAllManagers();
            }
        }
        
        private void Initialize()
        {
            dependencyContainer = new DependencyContainer();
            
            if (enableDebugLogging)
            {
                Debug.Log("ManagerInitializer initialized");
            }
        }
        
        /// <summary>
        /// 매니저 등록
        /// </summary>
        /// <param name="manager">등록할 매니저</param>
        public void RegisterManager(IManager manager)
        {
            if (manager == null) return;
            
            if (!managers.Contains(manager))
            {
                managers.Add(manager);
                
                // 의존성 컨테이너 설정
                manager.SetDependencyContainer(dependencyContainer);
                
                if (enableDebugLogging)
                {
                    Debug.Log($"ManagerInitializer: Registered {manager.GetType().Name} (Priority: {manager.GetPriority()}, Scope: {manager.GetManagerScope()})");
                }
            }
        }
        
        /// <summary>
        /// 매니저 등록 해제
        /// </summary>
        /// <param name="manager">등록 해제할 매니저</param>
        public void UnregisterManager(IManager manager)
        {
            if (manager == null) return;
            
            if (managers.Contains(manager))
            {
                managers.Remove(manager);
                
                if (enableDebugLogging)
                {
                    Debug.Log($"ManagerInitializer: Unregistered {manager.GetType().Name}");
                }
            }
        }
        
        /// <summary>
        /// 모든 매니저 초기화
        /// </summary>
        public void InitializeAllManagers()
        {
            if (isInitialized)
            {
                Debug.LogWarning("ManagerInitializer: Already initialized");
                return;
            }
            
            if (enableDebugLogging)
            {
                Debug.Log("ManagerInitializer: Starting initialization...");
            }
            
            // 우선순위별로 정렬
            var sortedManagers = managers.OrderBy(m => m.GetPriority()).ToList();
            
            int successCount = 0;
            int failCount = 0;
            
            foreach (var manager in sortedManagers)
            {
                try
                {
                    InitializeManager(manager);
                    successCount++;
                }
                catch (Exception e)
                {
                    failCount++;
                    Debug.LogError($"ManagerInitializer: Failed to initialize {manager.GetType().Name}: {e.Message}");
                    OnManagerInitializationFailed?.Invoke(manager);
                }
            }
            
            isInitialized = true;
            
            if (enableDebugLogging)
            {
                Debug.Log($"ManagerInitializer: Initialization complete - Success: {successCount}, Failed: {failCount}");
            }
            
            OnAllManagersInitialized?.Invoke();
        }
        
        /// <summary>
        /// 개별 매니저 초기화
        /// </summary>
        /// <param name="manager">초기화할 매니저</param>
        private void InitializeManager(IManager manager)
        {
            if (manager == null) return;
            
            if (manager.IsInitialized())
            {
                if (enableDebugLogging)
                {
                    Debug.Log($"ManagerInitializer: {manager.GetType().Name} already initialized, skipping");
                }
                return;
            }
            
            if (enableDebugLogging)
            {
                Debug.Log($"ManagerInitializer: Initializing {manager.GetType().Name}...");
            }
            
            manager.Initialize();
            OnManagerInitialized?.Invoke(manager);
            
            if (enableDebugLogging)
            {
                Debug.Log($"ManagerInitializer: ✓ {manager.GetType().Name} initialized successfully");
            }
        }
        
        /// <summary>
        /// 모든 매니저 리셋
        /// </summary>
        public void ResetAllManagers()
        {
            if (enableDebugLogging)
            {
                Debug.Log("ManagerInitializer: Resetting all managers...");
            }
            
            foreach (var manager in managers)
            {
                try
                {
                    manager.Reset();
                    
                    if (enableDebugLogging)
                    {
                        Debug.Log($"ManagerInitializer: ✓ {manager.GetType().Name} reset");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"ManagerInitializer: Failed to reset {manager.GetType().Name}: {e.Message}");
                }
            }
            
            isInitialized = false;
        }
        
        /// <summary>
        /// 특정 타입의 매니저들 활성화/비활성화
        /// </summary>
        /// <param name="managerType">매니저 타입</param>
        /// <param name="active">활성화 여부</param>
        public void SetManagersActive(ManagerType managerType, bool active)
        {
            var targetManagers = managers.Where(m => m.GetManagerType() == managerType).ToList();
            
            foreach (var manager in targetManagers)
            {
                try
                {
                    manager.SetActive(active);
                    
                    if (enableDebugLogging)
                    {
                        Debug.Log($"ManagerInitializer: {manager.GetType().Name} {(active ? "activated" : "deactivated")}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"ManagerInitializer: Failed to set {manager.GetType().Name} active: {e.Message}");
                }
            }
        }
        
        /// <summary>
        /// 특정 범위의 매니저들 활성화/비활성화
        /// </summary>
        /// <param name="scope">매니저 범위</param>
        /// <param name="active">활성화 여부</param>
        public void SetManagersActive(ManagerScope scope, bool active)
        {
            var targetManagers = managers.Where(m => m.GetManagerScope() == scope).ToList();
            
            foreach (var manager in targetManagers)
            {
                try
                {
                    manager.SetActive(active);
                    
                    if (enableDebugLogging)
                    {
                        Debug.Log($"ManagerInitializer: {manager.GetType().Name} {(active ? "activated" : "deactivated")}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"ManagerInitializer: Failed to set {manager.GetType().Name} active: {e.Message}");
                }
            }
        }
        
        /// <summary>
        /// 매니저 상태 정보 반환
        /// </summary>
        /// <returns>상태 정보 문자열</returns>
        public string GetStatus()
        {
            var initializedCount = managers.Count(m => m.IsInitialized());
            var totalCount = managers.Count;
            
            return $"ManagerInitializer: {initializedCount}/{totalCount} managers initialized\n" +
                   $"DependencyContainer: {dependencyContainer.GetStatus()}";
        }
        
        /// <summary>
        /// 등록된 매니저 목록 반환
        /// </summary>
        /// <returns>매니저 목록</returns>
        public List<IManager> GetManagers()
        {
            return new List<IManager>(managers);
        }
        
        /// <summary>
        /// 특정 타입의 매니저들 반환
        /// </summary>
        /// <param name="managerType">매니저 타입</param>
        /// <returns>매니저 목록</returns>
        public List<IManager> GetManagersByType(ManagerType managerType)
        {
            return managers.Where(m => m.GetManagerType() == managerType).ToList();
        }
        
        /// <summary>
        /// 특정 범위의 매니저들 반환
        /// </summary>
        /// <param name="scope">매니저 범위</param>
        /// <returns>매니저 목록</returns>
        public List<IManager> GetManagersByScope(ManagerScope scope)
        {
            return managers.Where(m => m.GetManagerScope() == scope).ToList();
        }
        
        /// <summary>
        /// 의존성 컨테이너 반환
        /// </summary>
        /// <returns>의존성 컨테이너</returns>
        public DependencyContainer GetDependencyContainer()
        {
            return dependencyContainer;
        }
    }
}
