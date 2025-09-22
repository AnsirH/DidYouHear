using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using DidYouHear.Manager.Interfaces;
using DidYouHear.Core;
using DidYouHear.Manager.Core;
using DidYouHear.Manager.Gameplay;

namespace DidYouHear.Manager.System
{
    /// <summary>
    /// 매니저 시스템 부트스트랩
    /// 
    /// 새로운 매니저 시스템을 초기화하고 통합하는 메인 컨트롤러입니다.
    /// 이 클래스는 모든 매니저 시스템 컴포넌트들을 조율하고 관리합니다.
    /// 
    /// 주요 기능:
    /// 1. 시스템 초기화: ManagerInitializer, SceneManagerController, DependencyContainer 생성
    /// 2. 매니저 등록: 모든 매니저들을 시스템에 등록하고 의존성 설정
    /// 3. 씬별 관리 설정: 각 씬에서 필요한 매니저들을 미리 등록
    /// 4. 이벤트 통합: 모든 매니저 시스템의 이벤트를 통합 관리
    /// 5. 상태 모니터링: 전체 시스템의 상태를 추적하고 로그 출력
    /// 
    /// 시스템 구성:
    /// - ManagerInitializer: 매니저 초기화 및 우선순위 관리
    /// - SceneManagerController: 씬별 매니저 활성화/비활성화 관리
    /// - DependencyContainer: 매니저 간 의존성 주입 관리
    /// - ManagerEvents: 매니저 간 이벤트 통신 관리
    /// 
    /// 사용 방법:
    /// 1. ManagerSystemBootstrap 컴포넌트를 GameObject에 추가
    /// 2. 매니저 참조들을 Inspector에서 설정 (선택사항)
    /// 3. 자동으로 시스템이 초기화되고 시작됨
    /// 4. LogSystemStatus()로 시스템 상태 확인 가능
    /// 
    /// 초기화 순서:
    /// 1. 부트스트랩 초기화 (Awake)
    /// 2. 매니저 시스템 시작 (Start)
    /// 3. 매니저 등록 및 의존성 설정
    /// 4. 씬별 매니저 관리 설정
    /// 5. 이벤트 구독 및 시스템 활성화
    /// </summary>
    public class ManagerSystemBootstrap : MonoBehaviour
    {
        [Header("Bootstrap Settings")]
        public bool enableDebugLogging = true;
        public bool autoInitializeOnStart = true;
        public bool enableSceneManagerIntegration = true;
        
        [Header("Manager References")]
        public GameManager gameManager;
        public InputManager inputManager;
        public AudioManager audioManager;
        public EventManager eventManager;
        public GhostManager ghostManager;
        public StonePool stonePool;
        
        // 매니저 시스템 컴포넌트들
        private ManagerInitializer managerInitializer;
        private SceneManagerController sceneManagerController;
        private DependencyContainer dependencyContainer;
        
        // 초기화 상태
        private bool isBootstrapComplete = false;
        
        private void Awake()
        {
            // 부트스트랩 초기화
            InitializeBootstrap();
        }
        
        private void Start()
        {
            if (autoInitializeOnStart)
            {
                StartManagerSystem();
            }
        }
        
        /// <summary>
        /// 부트스트랩 초기화
        /// </summary>
        private void InitializeBootstrap()
        {
            // ManagerInitializer 생성 및 설정
            managerInitializer = gameObject.AddComponent<ManagerInitializer>();
            managerInitializer.enableDebugLogging = enableDebugLogging;
            managerInitializer.autoInitializeOnStart = false; // 수동으로 제어
            
            // SceneManagerController 생성 및 설정
            if (enableSceneManagerIntegration)
            {
                sceneManagerController = gameObject.AddComponent<SceneManagerController>();
                sceneManagerController.enableDebugLogging = enableDebugLogging;
            }
            
            // DependencyContainer 생성
            dependencyContainer = new DependencyContainer();
            
            if (enableDebugLogging)
            {
                Debug.Log("ManagerSystemBootstrap: Bootstrap initialized");
            }
        }
        
        /// <summary>
        /// 매니저 시스템 시작
        /// </summary>
        public void StartManagerSystem()
        {
            if (isBootstrapComplete)
            {
                Debug.LogWarning("ManagerSystemBootstrap: System already started");
                return;
            }
            
            if (enableDebugLogging)
            {
                Debug.Log("ManagerSystemBootstrap: Starting manager system...");
            }
            
            // 1. 매니저들 등록
            RegisterAllManagers();
            
            // 2. 의존성 컨테이너에 매니저들 등록
            RegisterManagersInDependencyContainer();
            
            // 3. 매니저 초기화
            InitializeAllManagers();
            
            // 4. 씬별 매니저 관리 설정
            if (enableSceneManagerIntegration)
            {
                SetupSceneManagerIntegration();
            }
            
            // 5. 이벤트 구독
            SubscribeToManagerEvents();
            
            isBootstrapComplete = true;
            
            if (enableDebugLogging)
            {
                Debug.Log("ManagerSystemBootstrap: Manager system started successfully");
                LogSystemStatus();
            }
        }
        
        /// <summary>
        /// 모든 매니저 등록
        /// </summary>
        private void RegisterAllManagers()
        {
            // 자동으로 매니저들 찾기
            if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
            if (inputManager == null) inputManager = FindObjectOfType<InputManager>();
            if (audioManager == null) audioManager = FindObjectOfType<AudioManager>();
            if (eventManager == null) eventManager = FindObjectOfType<EventManager>();
            if (ghostManager == null) ghostManager = FindObjectOfType<GhostManager>();
            if (stonePool == null) stonePool = FindObjectOfType<StonePool>();
            
            // ManagerInitializer에 등록
            var managers = new IManager[] { gameManager, inputManager, audioManager, eventManager, ghostManager, stonePool };
            
            foreach (var manager in managers)
            {
                if (manager != null)
                {
                    managerInitializer.RegisterManager(manager);
                    
                    if (enableDebugLogging)
                    {
                        Debug.Log($"ManagerSystemBootstrap: Registered {manager.GetType().Name} " +
                                $"(Priority: {manager.GetPriority()}, Scope: {manager.GetManagerScope()})");
                    }
                }
            }
        }
        
        /// <summary>
        /// 의존성 컨테이너에 매니저들 등록
        /// </summary>
        private void RegisterManagersInDependencyContainer()
        {
            if (gameManager != null) dependencyContainer.RegisterSingleton(gameManager);
            if (inputManager != null) dependencyContainer.RegisterSingleton(inputManager);
            if (audioManager != null) dependencyContainer.RegisterSingleton(audioManager);
            if (eventManager != null) dependencyContainer.RegisterSingleton(eventManager);
            if (ghostManager != null) dependencyContainer.RegisterSingleton(ghostManager);
            if (stonePool != null) dependencyContainer.RegisterSingleton(stonePool);
            
            if (enableDebugLogging)
            {
                Debug.Log($"ManagerSystemBootstrap: Registered {dependencyContainer.GetRegisteredServices().Count} services in dependency container");
            }
        }
        
        /// <summary>
        /// 모든 매니저 초기화
        /// </summary>
        private void InitializeAllManagers()
        {
            managerInitializer.InitializeAllManagers();
        }
        
        /// <summary>
        /// 씬별 매니저 관리 설정
        /// </summary>
        private void SetupSceneManagerIntegration()
        {
            if (sceneManagerController == null) return;
            
            // Global 매니저들 등록 (모든 씬에서 유지)
            var globalManagers = new IManager[] { gameManager, inputManager, audioManager };
            foreach (var manager in globalManagers)
            {
                if (manager != null)
                {
                    sceneManagerController.RegisterManagerForScene("MainMenu", manager);
                    sceneManagerController.RegisterManagerForScene("Gameplay", manager);
                    sceneManagerController.RegisterManagerForScene("Ending", manager);
                }
            }
            
            // Scene 매니저들 등록 (씬별로 존재)
            var sceneManagers = new IManager[] { eventManager };
            foreach (var manager in sceneManagers)
            {
                if (manager != null)
                {
                    sceneManagerController.RegisterManagerForScene("MainMenu", manager);
                    sceneManagerController.RegisterManagerForScene("Gameplay", manager);
                    sceneManagerController.RegisterManagerForScene("Ending", manager);
                }
            }
            
            // Gameplay 매니저들 등록 (인게임 전용)
            var gameplayManagers = new IManager[] { ghostManager, stonePool };
            foreach (var manager in gameplayManagers)
            {
                if (manager != null)
                {
                    sceneManagerController.RegisterManagerForScene("Gameplay", manager);
                }
            }
            
            if (enableDebugLogging)
            {
                Debug.Log("ManagerSystemBootstrap: Scene manager integration setup complete");
            }
        }
        
        /// <summary>
        /// 매니저 이벤트 구독
        /// </summary>
        private void SubscribeToManagerEvents()
        {
            ManagerInitializer.OnManagerInitialized += OnManagerInitialized;
            ManagerInitializer.OnManagerInitializationFailed += OnManagerInitializationFailed;
            ManagerInitializer.OnAllManagersInitialized += OnAllManagersInitialized;
            
            if (sceneManagerController != null)
            {
                SceneManagerController.OnManagerActivated += OnManagerActivated;
                SceneManagerController.OnManagerDeactivated += OnManagerDeactivated;
            }
        }
        
        /// <summary>
        /// 매니저 이벤트 구독 해제
        /// </summary>
        private void UnsubscribeFromManagerEvents()
        {
            ManagerInitializer.OnManagerInitialized -= OnManagerInitialized;
            ManagerInitializer.OnManagerInitializationFailed -= OnManagerInitializationFailed;
            ManagerInitializer.OnAllManagersInitialized -= OnAllManagersInitialized;
            
            if (sceneManagerController != null)
            {
                SceneManagerController.OnManagerActivated -= OnManagerActivated;
                SceneManagerController.OnManagerDeactivated -= OnManagerDeactivated;
            }
        }
        
        /// <summary>
        /// 매니저 초기화 완료 이벤트 핸들러
        /// </summary>
        private void OnManagerInitialized(IManager manager)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"ManagerSystemBootstrap: ✓ {manager.GetType().Name} initialized");
            }
        }
        
        /// <summary>
        /// 매니저 초기화 실패 이벤트 핸들러
        /// </summary>
        private void OnManagerInitializationFailed(IManager manager)
        {
            Debug.LogError($"ManagerSystemBootstrap: ✗ Failed to initialize {manager.GetType().Name}");
        }
        
        /// <summary>
        /// 모든 매니저 초기화 완료 이벤트 핸들러
        /// </summary>
        private void OnAllManagersInitialized()
        {
            if (enableDebugLogging)
            {
                Debug.Log("ManagerSystemBootstrap: All managers initialized successfully");
            }
        }
        
        /// <summary>
        /// 매니저 활성화 이벤트 핸들러
        /// </summary>
        private void OnManagerActivated(IManager manager)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"ManagerSystemBootstrap: {manager.GetType().Name} activated for current scene");
            }
        }
        
        /// <summary>
        /// 매니저 비활성화 이벤트 핸들러
        /// </summary>
        private void OnManagerDeactivated(IManager manager)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"ManagerSystemBootstrap: {manager.GetType().Name} deactivated for current scene");
            }
        }
        
        /// <summary>
        /// 시스템 상태 로그
        /// </summary>
        public void LogSystemStatus()
        {
            Debug.Log("=== Manager System Status ===");
            Debug.Log($"Bootstrap Complete: {isBootstrapComplete}");
            Debug.Log($"Manager Initializer: {managerInitializer?.GetStatus()}");
            Debug.Log($"Scene Manager Controller: {(sceneManagerController != null ? "Active" : "Inactive")}");
            Debug.Log($"Dependency Container: {dependencyContainer?.GetStatus()}");
            
            if (managerInitializer != null)
            {
                var managers = managerInitializer.GetManagers();
                Debug.Log($"Registered Managers: {managers.Count}");
                
                foreach (var manager in managers)
                {
                    Debug.Log($"  - {manager.GetType().Name}: {manager.GetStatus()}");
                }
            }
        }
        
        /// <summary>
        /// 매니저 시스템 재시작
        /// </summary>
        public void RestartManagerSystem()
        {
            if (enableDebugLogging)
            {
                Debug.Log("ManagerSystemBootstrap: Restarting manager system...");
            }
            
            // 이벤트 구독 해제
            UnsubscribeFromManagerEvents();
            
            // 매니저 리셋
            if (managerInitializer != null)
            {
                managerInitializer.ResetAllManagers();
            }
            
            // 부트스트랩 재초기화
            isBootstrapComplete = false;
            InitializeBootstrap();
            
            // 시스템 재시작
            StartManagerSystem();
        }
        
        /// <summary>
        /// 특정 씬으로 전환
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"ManagerSystemBootstrap: Loading scene: {sceneName}");
            }
            
            SceneManager.LoadScene(sceneName);
        }
        
        private void OnDestroy()
        {
            // 이벤트 구독 해제
            UnsubscribeFromManagerEvents();
        }
    }
}
