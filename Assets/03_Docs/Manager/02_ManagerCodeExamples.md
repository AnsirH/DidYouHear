# 매니저 시스템 코드 예시

## 🔧 개선된 IManager 인터페이스

```csharp
// 04_Scripts/Core/IManager.cs
using System;

namespace DidYouHear.Core
{
    public interface IManager
    {
        void Initialize();
        void Reset();
        void SetActive(bool active);
        string GetStatus();
        bool IsInitialized();
        int GetPriority();
        ManagerType GetManagerType();
        void SetDependencyContainer(DependencyContainer container);
    }

    public enum ManagerType
    {
        Core = 0,       // GameManager, InputManager
        System = 10,    // AudioManager, EventManager
        Gameplay = 20,  // GhostManager, CorridorManager
        UI = 30         // UIManager
    }
}
```

## 🏗️ ManagerInitializer 구현

```csharp
// 04_Scripts/Core/ManagerInitializer.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DidYouHear.Core
{
    public class ManagerInitializer : MonoBehaviour
    {
        private List<IManager> managers = new List<IManager>();
        private DependencyContainer dependencyContainer;
        private bool isInitialized = false;

        public void SetDependencyContainer(DependencyContainer container)
        {
            dependencyContainer = container;
        }

        public void RegisterManager(IManager manager)
        {
            if (manager == null) return;
            
            managers.Add(manager);
            managers.Sort((a, b) => a.GetPriority().CompareTo(b.GetPriority()));
            
            // 의존성 컨테이너에 등록
            if (dependencyContainer != null)
            {
                dependencyContainer.Register(manager);
            }
            
            Debug.Log($"Manager registered: {manager.GetType().Name} (Priority: {manager.GetPriority()})");
        }

        public void InitializeAll()
        {
            if (isInitialized) return;

            Debug.Log("Starting manager initialization...");
            
            foreach (var manager in managers)
            {
                try
                {
                    if (!manager.IsInitialized())
                    {
                        manager.SetDependencyContainer(dependencyContainer);
                        manager.Initialize();
                        manager.SetActive(true);
                        
                        Debug.Log($"✓ {manager.GetType().Name} initialized");
                        ManagerEvents.OnManagerInitialized?.Invoke(manager);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to initialize {manager.GetType().Name}: {e.Message}");
                }
            }
            
            isInitialized = true;
            Debug.Log("All managers initialized successfully");
        }

        public void ResetAll()
        {
            foreach (var manager in managers)
            {
                try
                {
                    manager.Reset();
                    Debug.Log($"✓ {manager.GetType().Name} reset");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to reset {manager.GetType().Name}: {e.Message}");
                }
            }
        }

        public void SetAllActive(bool active)
        {
            foreach (var manager in managers)
            {
                try
                {
                    manager.SetActive(active);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to set {manager.GetType().Name} active: {e.Message}");
                }
            }
        }

        public List<IManager> GetManagers()
        {
            return new List<IManager>(managers);
        }

        public IManager GetManager<T>() where T : class, IManager
        {
            return managers.FirstOrDefault(m => m is T) as T;
        }
    }
}
```

## 📦 DependencyContainer 구현

```csharp
// 04_Scripts/Core/DependencyContainer.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DidYouHear.Core
{
    public class DependencyContainer
    {
        private Dictionary<Type, object> services = new Dictionary<Type, object>();

        public void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                Debug.LogError($"Cannot register null service of type {typeof(T)}");
                return;
            }

            services[typeof(T)] = service;
            Debug.Log($"Service registered: {typeof(T).Name}");
        }

        public T Get<T>() where T : class
        {
            if (services.TryGetValue(typeof(T), out var service))
            {
                return service as T;
            }

            Debug.LogWarning($"Service not found: {typeof(T).Name}");
            return null;
        }

        public bool IsRegistered<T>() where T : class
        {
            return services.ContainsKey(typeof(T));
        }

        public void Clear()
        {
            services.Clear();
            Debug.Log("Dependency container cleared");
        }

        public int GetServiceCount()
        {
            return services.Count;
        }

        public void LogRegisteredServices()
        {
            Debug.Log($"Registered services ({services.Count}):");
            foreach (var kvp in services)
            {
                Debug.Log($"  - {kvp.Key.Name}: {kvp.Value?.GetType().Name ?? "null"}");
            }
        }
    }
}
```

## 📡 ManagerEvents 시스템

```csharp
// 04_Scripts/Core/ManagerEvents.cs
using System;
using DidYouHear.Events;

namespace DidYouHear.Core
{
    public static class ManagerEvents
    {
        // 매니저 생명주기 이벤트
        public static event Action<IManager> OnManagerInitialized;
        public static event Action<IManager> OnManagerReady;
        public static event Action<IManager> OnManagerReset;
        public static event Action<IManager> OnManagerDestroyed;

        // 게임 상태 이벤트
        public static event Action<GameState> OnGameStateChanged;
        public static event Action<PlayerGameState> OnPlayerGameStateChanged;
        public static event Action OnPlayerDied;
        public static event Action OnGamePaused;
        public static event Action OnGameResumed;

        // 이벤트 시스템
        public static event Action<EventManager.EventType> OnEventTriggered;
        public static event Action<EventManager.EventType, bool> OnEventCompleted;

        // 귀신 시스템
        public static event Action OnGhostDetected;
        public static event Action OnGhostGone;
        public static event Action OnGhostAppeared;
        public static event Action OnGhostDisappeared;

        // 오디오 시스템
        public static event Action<AudioClip, Vector3> OnSoundPlayed;
        public static event Action<AudioClip> OnSoundFinished;

        // UI 시스템
        public static event Action<string> OnUIPanelChanged;
        public static event Action OnUIShowed;
        public static event Action OnUIHidden;

        // 정적 메서드로 이벤트 발생
        public static void TriggerManagerInitialized(IManager manager)
        {
            OnManagerInitialized?.Invoke(manager);
        }

        public static void TriggerGameStateChanged(GameState newState)
        {
            OnGameStateChanged?.Invoke(newState);
        }

        public static void TriggerPlayerGameStateChanged(PlayerGameState newState)
        {
            OnPlayerGameStateChanged?.Invoke(newState);
        }

        public static void TriggerEventTriggered(EventManager.EventType eventType)
        {
            OnEventTriggered?.Invoke(eventType);
        }

        public static void TriggerEventCompleted(EventManager.EventType eventType, bool success)
        {
            OnEventCompleted?.Invoke(eventType, success);
        }

        // 이벤트 구독 해제 (메모리 누수 방지)
        public static void ClearAllEvents()
        {
            OnManagerInitialized = null;
            OnManagerReady = null;
            OnManagerReset = null;
            OnManagerDestroyed = null;
            OnGameStateChanged = null;
            OnPlayerGameStateChanged = null;
            OnPlayerDied = null;
            OnGamePaused = null;
            OnGameResumed = null;
            OnEventTriggered = null;
            OnEventCompleted = null;
            OnGhostDetected = null;
            OnGhostGone = null;
            OnGhostAppeared = null;
            OnGhostDisappeared = null;
            OnSoundPlayed = null;
            OnSoundFinished = null;
            OnUIPanelChanged = null;
            OnUIShowed = null;
            OnUIHidden = null;
        }
    }
}
```

## 🔄 개선된 GameManager

```csharp
// 04_Scripts/Core/GameManager.cs (개선된 버전)
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using DidYouHear.Player;
using DidYouHear.Corridor;
using DidYouHear.Events;
using DidYouHear.Audio;
using DidYouHear.Ghost;
using DidYouHear.Stone;

namespace DidYouHear.Core
{
    public class GameManager : MonoBehaviour, IManager
    {
        [Header("Game Settings")]
        public float gameTime = 0f;
        public bool isGamePaused = false;
        
        [Header("Player Status")]
        public bool isPlayerAlive = true;
        public int reactionSuccessCount = 0;
        public int reactionFailCount = 0;
        
        [Header("Player Game State")]
        public PlayerGameState currentPlayerGameState = PlayerGameState.Normal;
        private float ghostBehindTimer = 0f;
        private float maxGhostBehindTime = 2f;
        
        // 매니저 시스템
        private ManagerInitializer initializer;
        private DependencyContainer container;
        private bool isInitialized = false;
        
        // 싱글톤 패턴
        public static GameManager Instance { get; private set; }
        
        // 게임 상태 enum
        public enum GameState
        {
            Menu,
            Playing,
            Paused,
            GameOver,
            Ending
        }
        
        [Header("Current State")]
        public GameState currentState = GameState.Menu;

        // IManager 구현
        public void Initialize()
        {
            if (isInitialized) return;

            // 의존성 컨테이너 초기화
            container = new DependencyContainer();
            container.Register(this);
            
            // 매니저 초기화 시스템 설정
            initializer = gameObject.AddComponent<ManagerInitializer>();
            initializer.SetDependencyContainer(container);
            
            // 매니저들 등록
            RegisterAllManagers();
            
            // 초기화 실행
            initializer.InitializeAll();
            
            isInitialized = true;
            Debug.Log("GameManager initialized with new system");
        }

        public void Reset()
        {
            gameTime = 0f;
            isGamePaused = false;
            isPlayerAlive = true;
            reactionSuccessCount = 0;
            reactionFailCount = 0;
            currentPlayerGameState = PlayerGameState.Normal;
            ghostBehindTimer = 0f;
            currentState = GameState.Menu;
        }

        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }

        public string GetStatus()
        {
            return $"GameManager - State: {currentState}, Time: {gameTime:F1}s, Alive: {isPlayerAlive}";
        }

        public bool IsInitialized()
        {
            return isInitialized;
        }

        public int GetPriority()
        {
            return 0; // 최우선 초기화
        }

        public ManagerType GetManagerType()
        {
            return ManagerType.Core;
        }

        public void SetDependencyContainer(DependencyContainer container)
        {
            this.container = container;
        }

        private void RegisterAllManagers()
        {
            // 자동으로 찾아서 등록
            var allManagers = FindObjectsOfType<MonoBehaviour>().OfType<IManager>();
            foreach (var manager in allManagers)
            {
                initializer.RegisterManager(manager);
            }
        }

        // 기존 메서드들은 그대로 유지하되, 이벤트 기반으로 변경
        public void ChangeGameState(GameState newState)
        {
            if (currentState == newState) return;
            
            GameState previousState = currentState;
            currentState = newState;
            
            // 이벤트 기반으로 상태 변경 알림
            ManagerEvents.TriggerGameStateChanged(newState);
            
            // 기존 로직은 그대로 유지
            switch (newState)
            {
                case GameState.Menu:
                case GameState.Playing:
                    Time.timeScale = 1f;
                    isGamePaused = false;
                    break;
                case GameState.Paused:
                    Time.timeScale = 0f;
                    isGamePaused = true;
                    ManagerEvents.OnGamePaused?.Invoke();
                    break;
                case GameState.GameOver:
                    Time.timeScale = 0f;
                    isPlayerAlive = false;
                    ManagerEvents.OnPlayerDied?.Invoke();
                    break;
                case GameState.Ending:
                    Time.timeScale = 1f;
                    break;
            }
        }

        // 의존성 주입을 통한 매니저 접근
        public T GetManager<T>() where T : class, IManager
        {
            return container?.Get<T>();
        }

        // 기존 public 프로퍼티들은 하위 호환성을 위해 유지
        public PlayerController playerController => GetManager<PlayerController>();
        public CorridorManager corridorManager => GetManager<CorridorManager>();
        public EventManager eventManager => GetManager<EventManager>();
        public AudioManager audioManager => GetManager<AudioManager>();
        public GhostManager ghostManager => GetManager<GhostManager>();
        public StonePool stonePool => GetManager<StonePool>();
        public CameraController cameraController => GetManager<CameraController>();
    }
}
```

## 🎮 개선된 EventManager 예시

```csharp
// 04_Scripts/Events/EventManager.cs (개선된 버전)
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DidYouHear.Core;
using DidYouHear.Player;
using DidYouHear.Audio;
using DidYouHear.Corridor;

namespace DidYouHear.Events
{
    public class EventManager : MonoBehaviour, IManager
    {
        [Header("Event Settings")]
        public float minEventInterval = 10f;
        public float reactionTimeLimit = 2f;

        // 이벤트들
        public ShoulderTapEvent shoulderTapEvent;
        public EnvironmentalEvent environmentalEvent;
        public GhostAppearanceEvent ghostAppearanceEvent;

        // 상태
        private bool isInitialized = false;
        private DependencyContainer container;
        private PlayerMovement playerMovement;
        private PlayerLook playerLook;
        private AudioManager audioManager;

        // 싱글톤 패턴
        public static EventManager Instance { get; private set; }

        // IManager 구현
        public void Initialize()
        {
            if (isInitialized) return;

            // 의존성 주입으로 참조 획득
            var gameManager = container.Get<GameManager>();
            if (gameManager?.playerController != null)
            {
                playerMovement = gameManager.playerController.playerMovement;
                playerLook = gameManager.playerController.playerLook;
            }
            
            audioManager = container.Get<AudioManager>();

            // 이벤트 초기화
            InitializeEvents();
            
            isInitialized = true;
            Debug.Log("EventManager initialized with dependency injection");
        }

        public void Reset()
        {
            // 이벤트 상태 리셋
            if (shoulderTapEvent != null) shoulderTapEvent = new ShoulderTapEvent();
            if (environmentalEvent != null) environmentalEvent = new EnvironmentalEvent();
            if (ghostAppearanceEvent != null) ghostAppearanceEvent = new GhostAppearanceEvent();
        }

        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }

        public string GetStatus()
        {
            return $"EventManager - Active: {gameObject.activeInHierarchy}, Events: {GetActiveEventCount()}";
        }

        public bool IsInitialized()
        {
            return isInitialized;
        }

        public int GetPriority()
        {
            return 11; // AudioManager 다음
        }

        public ManagerType GetManagerType()
        {
            return ManagerType.System;
        }

        public void SetDependencyContainer(DependencyContainer container)
        {
            this.container = container;
        }

        private void InitializeEvents()
        {
            if (shoulderTapEvent == null) shoulderTapEvent = new ShoulderTapEvent();
            if (environmentalEvent == null) environmentalEvent = new EnvironmentalEvent();
            if (ghostAppearanceEvent == null) ghostAppearanceEvent = new GhostAppearanceEvent();

            shoulderTapEvent.Initialize(this);
            environmentalEvent.Initialize(this);
            ghostAppearanceEvent.Initialize(this);
        }

        private int GetActiveEventCount()
        {
            int count = 0;
            if (shoulderTapEvent != null && !shoulderTapEvent.IsCompleted()) count++;
            if (environmentalEvent != null && !environmentalEvent.IsCompleted()) count++;
            if (ghostAppearanceEvent != null && !ghostAppearanceEvent.IsCompleted()) count++;
            return count;
        }

        // 기존 메서드들은 그대로 유지하되, 이벤트 기반으로 변경
        public void TriggerShoulderTapEvent()
        {
            if (isEventActive) return;

            isEventActive = true;
            currentActiveEvent = shoulderTapEvent;
            shoulderTapEvent.Execute();
            
            // 이벤트 기반 알림
            ManagerEvents.TriggerEventTriggered(EventType.ShoulderTap);
            Debug.Log("Shoulder Tap Event Triggered!");
        }

        // 나머지 메서드들은 기존과 동일...
    }
}
```

## 🧪 테스트 코드 예시

```csharp
// 04_Scripts/Tests/ManagerTests.cs
using NUnit.Framework;
using UnityEngine;
using DidYouHear.Core;

namespace DidYouHear.Tests
{
    public class ManagerTests
    {
        [Test]
        public void TestManagerInitializationOrder()
        {
            // Given
            var gameObject = new GameObject("TestGameManager");
            var gameManager = gameObject.AddComponent<GameManager>();
            var initializer = gameObject.AddComponent<ManagerInitializer>();
            var container = new DependencyContainer();
            
            initializer.SetDependencyContainer(container);
            
            // When
            initializer.RegisterManager(gameManager);
            initializer.InitializeAll();
            
            // Then
            Assert.IsTrue(gameManager.IsInitialized());
            Assert.AreEqual(0, gameManager.GetPriority());
            Assert.AreEqual(ManagerType.Core, gameManager.GetManagerType());
        }

        [Test]
        public void TestDependencyInjection()
        {
            // Given
            var container = new DependencyContainer();
            var testService = new TestService();
            
            // When
            container.Register(testService);
            var retrievedService = container.Get<TestService>();
            
            // Then
            Assert.IsNotNull(retrievedService);
            Assert.AreEqual(testService, retrievedService);
            Assert.IsTrue(container.IsRegistered<TestService>());
        }

        [Test]
        public void TestEventSystem()
        {
            // Given
            bool eventTriggered = false;
            ManagerEvents.OnGameStateChanged += (state) => eventTriggered = true;
            
            // When
            ManagerEvents.TriggerGameStateChanged(GameManager.GameState.Playing);
            
            // Then
            Assert.IsTrue(eventTriggered);
        }
    }

    public class TestService
    {
        public string Name => "TestService";
    }
}
```

## 🎮 씬별 매니저 관리 사용 예시

### 1. 매니저 등록 및 활성화

```csharp
// Global 매니저 (모든 씬에서 유지)
public class GameManager : MonoBehaviour, IManager
{
    public ManagerScope GetManagerScope()
    {
        return ManagerScope.Global;
    }
    
    private void Start()
    {
        // 모든 씬에 등록
        if (SceneManagerController.Instance != null)
        {
            SceneManagerController.Instance.RegisterManagerForScene("MainMenu", this);
            SceneManagerController.Instance.RegisterManagerForScene("Gameplay", this);
            SceneManagerController.Instance.RegisterManagerForScene("Ending", this);
        }
    }
}

// Scene 매니저 (씬별로 존재)
public class UIManager : MonoBehaviour, IManager
{
    public ManagerScope GetManagerScope()
    {
        return ManagerScope.Scene;
    }
    
    private void Start()
    {
        // 필요한 씬에만 등록
        if (SceneManagerController.Instance != null)
        {
            SceneManagerController.Instance.RegisterManagerForScene("MainMenu", this);
            SceneManagerController.Instance.RegisterManagerForScene("Ending", this);
        }
    }
}

// Gameplay 매니저 (인게임 전용)
public class CorridorManager : MonoBehaviour, IManager
{
    public ManagerScope GetManagerScope()
    {
        return ManagerScope.Gameplay;
    }
    
    private void Start()
    {
        // Gameplay 씬에만 등록
        if (SceneManagerController.Instance != null)
        {
            SceneManagerController.Instance.RegisterManagerForScene("Gameplay", this);
        }
    }
}
```

### 2. 씬 전환 시 매니저 상태 확인

```csharp
// 씬 전환 시 자동으로 매니저 활성화/비활성화
// MainMenu 씬: Global + UI Managers만 활성화
// Gameplay 씬: Global + Scene + Gameplay Managers 활성화
// Ending 씬: Global + UI Managers만 활성화

// 현재 활성화된 매니저들 확인
var activeManagers = SceneManagerController.Instance.GetActiveManagers();
Debug.Log($"Active managers: {activeManagers.Count}");

// 특정 범위의 매니저들 확인
var gameplayManagers = SceneManagerController.Instance.GetActiveManagersByScope(ManagerScope.Gameplay);
Debug.Log($"Gameplay managers: {gameplayManagers.Count}");

// 매니저 상태 로그
SceneManagerController.Instance.LogManagerStatus();
```

### 3. 이벤트 기반 씬 전환

```csharp
// 씬 전환 이벤트 구독
private void Start()
{
    ManagerEvents.OnSceneLoaded += OnSceneLoaded;
    ManagerEvents.OnSceneTypeChanged += OnSceneTypeChanged;
}

private void OnSceneLoaded(string sceneName)
{
    Debug.Log($"Scene loaded: {sceneName}");
    
    // 씬별 특별한 처리
    switch (sceneName)
    {
        case "MainMenu":
            // 메뉴 씬 특별 처리
            break;
        case "Gameplay":
            // 게임플레이 씬 특별 처리
            break;
        case "Ending":
            // 엔딩 씬 특별 처리
            break;
    }
}

private void OnSceneTypeChanged(SceneType sceneType)
{
    Debug.Log($"Scene type changed to: {sceneType}");
}
```

이러한 개선을 통해 더욱 안정적이고 확장 가능한 매니저 시스템을 구축할 수 있습니다.
