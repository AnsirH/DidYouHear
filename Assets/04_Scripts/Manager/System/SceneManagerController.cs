using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using DidYouHear.Manager.Interfaces;

namespace DidYouHear.Manager.System
{
    /// <summary>
    /// 씬별 매니저 관리 시스템
    /// 
    /// 씬별로 필요한 매니저만 활성화하여 메모리 효율성과 성능을 최적화하는 시스템입니다.
    /// 이 클래스는 다음과 같은 기능을 제공합니다:
    /// 
    /// 1. 씬별 매니저 등록: 각 씬에서 필요한 매니저들을 미리 등록
    /// 2. 자동 활성화/비활성화: 씬 전환 시 자동으로 적절한 매니저들만 활성화
    /// 3. 범위 기반 관리: ManagerScope에 따라 매니저들을 그룹화하여 관리
    /// 4. 메모리 최적화: 불필요한 매니저는 비활성화하여 메모리 사용량 절약
    /// 5. 성능 향상: 필요한 매니저만 초기화하여 씬 로딩 시간 단축
    /// 
    /// 씬별 매니저 활성화 규칙:
    /// - MainMenu: Global + UI 매니저들만 활성화
    /// - Gameplay: Global + Scene + Gameplay 매니저들 모두 활성화
    /// - Ending: Global + UI 매니저들만 활성화
    /// 
    /// 매니저 범위별 분류:
    /// - Global: 모든 씬에서 유지 (GameManager, InputManager, AudioManager)
    /// - Scene: 씬별로 존재 (UIManager, EventManager)
    /// - Gameplay: 인게임 전용 (CorridorManager, GhostManager, StonePool)
    /// 
    /// 사용 방법:
    /// 1. SceneManagerController 컴포넌트를 GameObject에 추가
    /// 2. RegisterManagerForScene()로 씬별 매니저 등록
    /// 3. 씬 전환 시 자동으로 적절한 매니저들 활성화/비활성화
    /// </summary>
    public class SceneManagerController : MonoBehaviour
    {
        [Header("Scene Manager Settings")]
        public bool enableDebugLogging = true;
        public bool autoRegisterManagers = true;
        
        // 씬별 매니저 관리
        private Dictionary<string, List<IManager>> sceneManagers = new Dictionary<string, List<IManager>>();
        private Dictionary<ManagerScope, List<IManager>> scopeManagers = new Dictionary<ManagerScope, List<IManager>>();
        
        // 현재 활성화된 매니저들
        private List<IManager> activeManagers = new List<IManager>();
        
        // 싱글톤
        public static SceneManagerController Instance { get; private set; }
        
        // 이벤트
        public static event Action<string> OnSceneLoaded;
        public static event Action<string> OnSceneUnloaded;
        public static event Action<SceneType> OnSceneTypeChanged;
        public static event Action<IManager> OnManagerActivated;
        public static event Action<IManager> OnManagerDeactivated;
        
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
            // 씬 로드 이벤트 구독
            SceneManager.sceneLoaded += OnSceneLoadedHandler;
        }
        
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoadedHandler;
        }
        
        private void Initialize()
        {
            // 범위별 매니저 딕셔너리 초기화
            foreach (ManagerScope scope in Enum.GetValues(typeof(ManagerScope)))
            {
                scopeManagers[scope] = new List<IManager>();
            }
            
            if (enableDebugLogging)
            {
                Debug.Log("SceneManagerController initialized");
            }
        }
        
        /// <summary>
        /// 매니저를 특정 씬에 등록
        /// </summary>
        /// <param name="sceneName">씬 이름</param>
        /// <param name="manager">등록할 매니저</param>
        public void RegisterManagerForScene(string sceneName, IManager manager)
        {
            if (manager == null) return;
            
            if (!sceneManagers.ContainsKey(sceneName))
            {
                sceneManagers[sceneName] = new List<IManager>();
            }
            
            if (!sceneManagers[sceneName].Contains(manager))
            {
                sceneManagers[sceneName].Add(manager);
                
                // 범위별 매니저에도 등록
                var scope = manager.GetManagerScope();
                if (!scopeManagers[scope].Contains(manager))
                {
                    scopeManagers[scope].Add(manager);
                }
                
                if (enableDebugLogging)
                {
                    Debug.Log($"SceneManagerController: Registered {manager.GetType().Name} for scene {sceneName} (Scope: {scope})");
                }
            }
        }
        
        /// <summary>
        /// 씬 로드 이벤트 핸들러
        /// </summary>
        /// <param name="scene">로드된 씬</param>
        /// <param name="mode">로드 모드</param>
        private void OnSceneLoadedHandler(Scene scene, LoadSceneMode mode)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"SceneManagerController: Scene loaded: {scene.name}");
            }
            
            // 매니저 참조 업데이트 (씬 전환 후 새로 생성된 매니저들 찾기)
            RefreshManagerReferences();
            
            // 이전 씬의 매니저들 비활성화
            DeactivateAllManagers();
            
            // 새 씬의 매니저들 활성화
            ActivateManagersForScene(scene.name);
            
            // 씬 로드 이벤트 발생
            OnSceneLoaded?.Invoke(scene.name);
            
            // 씬 타입 변경 이벤트 발생
            var sceneType = GetSceneType(scene.name);
            OnSceneTypeChanged?.Invoke(sceneType);
        }
        
        /// <summary>
        /// 씬 전환 후 매니저 참조 업데이트
        /// </summary>
        private void RefreshManagerReferences()
        {
            if (enableDebugLogging)
            {
                Debug.Log("SceneManagerController: Refreshing manager references after scene change...");
            }
            
            // ManagerSystemBootstrap 찾기
            var bootstrap = FindObjectOfType<ManagerSystemBootstrap>();
            if (bootstrap != null)
            {
                bootstrap.RefreshManagerReferences();
                
                if (enableDebugLogging)
                {
                    Debug.Log("SceneManagerController: Manager references refreshed via bootstrap");
                }
            }
            else
            {
                Debug.LogWarning("SceneManagerController: ManagerSystemBootstrap not found! Cannot refresh manager references.");
            }
        }
        
        /// <summary>
        /// 특정 씬의 매니저들 활성화
        /// </summary>
        /// <param name="sceneName">씬 이름</param>
        public void ActivateManagersForScene(string sceneName)
        {
            // 씬 타입에 따른 매니저 활성화
            SceneType sceneType = GetSceneType(sceneName);
            SetManagersForScene(sceneType);
        }
        
        /// <summary>
        /// 씬 타입에 따른 매니저 설정
        /// </summary>
        /// <param name="sceneType">씬 타입</param>
        public void SetManagersForScene(SceneType sceneType)
        {
            List<ManagerScope> requiredScopes = GetRequiredScopes(sceneType);
            
            foreach (var scope in requiredScopes)
            {
                foreach (var manager in scopeManagers[scope])
                {
                    if (manager != null && !activeManagers.Contains(manager))
                    {
                        ActivateManager(manager);
                    }
                }
            }
            
            if (enableDebugLogging)
            {
                Debug.Log($"SceneManagerController: Activated {activeManagers.Count} managers for {sceneType}");
            }
        }
        
        /// <summary>
        /// 매니저 활성화
        /// </summary>
        /// <param name="manager">활성화할 매니저</param>
        private void ActivateManager(IManager manager)
        {
            if (manager == null) return;
            
            try
            {
                if (!manager.IsInitialized())
                {
                    manager.Initialize();
                }
                
                manager.SetActive(true);
                activeManagers.Add(manager);
                OnManagerActivated?.Invoke(manager);
                
                if (enableDebugLogging)
                {
                    Debug.Log($"SceneManagerController: ✓ {manager.GetType().Name} activated");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"SceneManagerController: Failed to activate {manager.GetType().Name}: {e.Message}");
            }
        }
        
        /// <summary>
        /// 모든 매니저 비활성화
        /// </summary>
        public void DeactivateAllManagers()
        {
            foreach (var manager in activeManagers.ToList())
            {
                if (manager != null)
                {
                    try
                    {
                        manager.SetActive(false);
                        OnManagerDeactivated?.Invoke(manager);
                        
                        if (enableDebugLogging)
                        {
                            Debug.Log($"SceneManagerController: ✗ {manager.GetType().Name} deactivated");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"SceneManagerController: Failed to deactivate {manager.GetType().Name}: {e.Message}");
                    }
                }
            }
            
            activeManagers.Clear();
        }
        
        /// <summary>
        /// 씬 타입 결정
        /// </summary>
        /// <param name="sceneName">씬 이름</param>
        /// <returns>씬 타입</returns>
        private SceneType GetSceneType(string sceneName)
        {
            switch (sceneName.ToLower())
            {
                case "mainmenu":
                case "menu":
                    return SceneType.MainMenu;
                case "gameplay":
                case "game":
                case "ingame":
                    return SceneType.Gameplay;
                case "ending":
                case "end":
                    return SceneType.Ending;
                default:
                    return SceneType.Gameplay; // 기본값
            }
        }
        
        /// <summary>
        /// 씬 타입별 필요한 범위 반환
        /// </summary>
        /// <param name="sceneType">씬 타입</param>
        /// <returns>필요한 매니저 범위 목록</returns>
        private List<ManagerScope> GetRequiredScopes(SceneType sceneType)
        {
            switch (sceneType)
            {
                case SceneType.MainMenu:
                    return new List<ManagerScope> { ManagerScope.Global, ManagerScope.Scene };
                case SceneType.Gameplay:
                    return new List<ManagerScope> { ManagerScope.Global, ManagerScope.Scene, ManagerScope.Gameplay };
                case SceneType.Ending:
                    return new List<ManagerScope> { ManagerScope.Global, ManagerScope.Scene };
                default:
                    return new List<ManagerScope> { ManagerScope.Global };
            }
        }
        
        /// <summary>
        /// 현재 활성화된 매니저들 반환
        /// </summary>
        /// <returns>활성화된 매니저 목록</returns>
        public List<IManager> GetActiveManagers()
        {
            return new List<IManager>(activeManagers);
        }
        
        /// <summary>
        /// 특정 범위의 활성화된 매니저들 반환
        /// </summary>
        /// <param name="scope">매니저 범위</param>
        /// <returns>활성화된 매니저 목록</returns>
        public List<IManager> GetActiveManagersByScope(ManagerScope scope)
        {
            return activeManagers.Where(m => m.GetManagerScope() == scope).ToList();
        }
        
        /// <summary>
        /// 매니저 상태 로그
        /// </summary>
        public void LogManagerStatus()
        {
            Debug.Log("=== SceneManagerController Status ===");
            Debug.Log($"Active Managers: {activeManagers.Count}");
            
            foreach (var scope in Enum.GetValues(typeof(ManagerScope)))
            {
                var scopeManagers = GetActiveManagersByScope((ManagerScope)scope);
                Debug.Log($"{scope}: {scopeManagers.Count} managers");
                
                foreach (var manager in scopeManagers)
                {
                    Debug.Log($"  - {manager.GetType().Name}: {manager.GetStatus()}");
                }
            }
        }
        
        /// <summary>
        /// 씬별 등록된 매니저 수 반환
        /// </summary>
        /// <param name="sceneName">씬 이름</param>
        /// <returns>등록된 매니저 수</returns>
        public int GetRegisteredManagerCount(string sceneName)
        {
            return sceneManagers.ContainsKey(sceneName) ? sceneManagers[sceneName].Count : 0;
        }
        
        /// <summary>
        /// 모든 씬의 매니저 등록 상태 반환
        /// </summary>
        /// <returns>씬별 매니저 등록 상태</returns>
        public Dictionary<string, int> GetAllSceneManagerCounts()
        {
            var result = new Dictionary<string, int>();
            foreach (var kvp in sceneManagers)
            {
                result[kvp.Key] = kvp.Value.Count;
            }
            return result;
        }
    }
}
