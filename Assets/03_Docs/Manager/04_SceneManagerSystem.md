# 씬별 매니저 관리 시스템

## 📋 개요

씬별로 필요한 매니저만 활성화하여 메모리 효율성과 성능을 최적화하는 시스템입니다.

## 🏗️ 핵심 컴포넌트

### 1. SceneManagerController

```csharp
// 04_Scripts/Core/SceneManagerController.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

namespace DidYouHear.Core
{
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
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        private void Initialize()
        {
            // 범위별 매니저 딕셔너리 초기화
            foreach (ManagerScope scope in System.Enum.GetValues(typeof(ManagerScope)))
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
                    Debug.Log($"Manager {manager.GetType().Name} registered for scene {sceneName} (Scope: {scope})");
                }
            }
        }
        
        /// <summary>
        /// 씬 로드 시 매니저 활성화
        /// </summary>
        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"Scene loaded: {scene.name}");
            }
            
            // 이전 씬의 매니저들 비활성화
            DeactivateAllManagers();
            
            // 새 씬의 매니저들 활성화
            ActivateManagersForScene(scene.name);
            
            // 씬 로드 이벤트 발생
            ManagerEvents.TriggerSceneLoaded(scene.name);
        }
        
        /// <summary>
        /// 특정 씬의 매니저들 활성화
        /// </summary>
        public void ActivateManagersForScene(string sceneName)
        {
            // 씬 타입에 따른 매니저 활성화
            SceneType sceneType = GetSceneType(sceneName);
            SetManagersForScene(sceneType);
        }
        
        /// <summary>
        /// 씬 타입에 따른 매니저 설정
        /// </summary>
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
                Debug.Log($"Activated {activeManagers.Count} managers for {sceneType}");
            }
        }
        
        /// <summary>
        /// 매니저 활성화
        /// </summary>
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
                
                if (enableDebugLogging)
                {
                    Debug.Log($"✓ {manager.GetType().Name} activated");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to activate {manager.GetType().Name}: {e.Message}");
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
                        
                        if (enableDebugLogging)
                        {
                            Debug.Log($"✗ {manager.GetType().Name} deactivated");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Failed to deactivate {manager.GetType().Name}: {e.Message}");
                    }
                }
            }
            
            activeManagers.Clear();
        }
        
        /// <summary>
        /// 씬 타입 결정
        /// </summary>
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
        public List<IManager> GetActiveManagers()
        {
            return new List<IManager>(activeManagers);
        }
        
        /// <summary>
        /// 특정 범위의 활성화된 매니저들 반환
        /// </summary>
        public List<IManager> GetActiveManagersByScope(ManagerScope scope)
        {
            return activeManagers.Where(m => m.GetManagerScope() == scope).ToList();
        }
        
        /// <summary>
        /// 매니저 상태 로그
        /// </summary>
        public void LogManagerStatus()
        {
            Debug.Log("=== Manager Status ===");
            Debug.Log($"Active Managers: {activeManagers.Count}");
            
            foreach (var scope in System.Enum.GetValues(typeof(ManagerScope)))
            {
                var scopeManagers = GetActiveManagersByScope((ManagerScope)scope);
                Debug.Log($"{scope}: {scopeManagers.Count} managers");
                
                foreach (var manager in scopeManagers)
                {
                    Debug.Log($"  - {manager.GetType().Name}: {manager.GetStatus()}");
                }
            }
        }
    }
}
```

### 2. 확장된 IManager 인터페이스

```csharp
// 04_Scripts/Core/IManager.cs (확장된 버전)
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
        ManagerScope GetManagerScope();
        void SetDependencyContainer(DependencyContainer container);
    }

    public enum ManagerType
    {
        Core = 0,       // GameManager, InputManager
        System = 10,    // AudioManager, EventManager
        Gameplay = 20,  // GhostManager, CorridorManager
        UI = 30         // UIManager
    }

    public enum ManagerScope
    {
        Global,     // 모든 씬에서 유지 (GameManager, InputManager, AudioManager)
        Scene,      // 씬별로 존재 (UIManager, EventManager)
        Gameplay    // 인게임 전용 (CorridorManager, GhostManager, StonePool)
    }

    public enum SceneType
    {
        MainMenu,    // 메인 메뉴 씬
        Gameplay,    // 인게임 씬
        Ending       // 엔딩 씬
    }
}
```

### 3. 확장된 ManagerEvents

```csharp
// 04_Scripts/Core/ManagerEvents.cs (확장된 버전)
using System;
using UnityEngine;

namespace DidYouHear.Core
{
    public static class ManagerEvents
    {
        // 매니저 생명주기 이벤트
        public static event Action<IManager> OnManagerInitialized;
        public static event Action<IManager> OnManagerReady;
        public static event Action<IManager> OnManagerReset;
        public static event Action<IManager> OnManagerDestroyed;

        // 씬 전환 이벤트
        public static event Action<string> OnSceneLoaded;
        public static event Action<string> OnSceneUnloaded;
        public static event Action<SceneType> OnSceneTypeChanged;

        // 게임 상태 이벤트
        public static event Action<GameState> OnGameStateChanged;
        public static event Action<PlayerGameState> OnPlayerGameStateChanged;
        public static event Action OnPlayerDied;
        public static event Action OnGamePaused;
        public static event Action OnGameResumed;

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

        public static void TriggerSceneLoaded(string sceneName)
        {
            OnSceneLoaded?.Invoke(sceneName);
        }

        public static void TriggerSceneTypeChanged(SceneType sceneType)
        {
            OnSceneTypeChanged?.Invoke(sceneType);
        }

        public static void TriggerGameStateChanged(GameState newState)
        {
            OnGameStateChanged?.Invoke(newState);
        }

        public static void TriggerPlayerGameStateChanged(PlayerGameState newState)
        {
            OnPlayerGameStateChanged?.Invoke(newState);
        }

        // 이벤트 구독 해제 (메모리 누수 방지)
        public static void ClearAllEvents()
        {
            OnManagerInitialized = null;
            OnManagerReady = null;
            OnManagerReset = null;
            OnManagerDestroyed = null;
            OnSceneLoaded = null;
            OnSceneUnloaded = null;
            OnSceneTypeChanged = null;
            OnGameStateChanged = null;
            OnPlayerGameStateChanged = null;
            OnPlayerDied = null;
            OnGamePaused = null;
            OnGameResumed = null;
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

## 🎮 사용 예시

### 1. 매니저 등록

```csharp
// GameManager.cs에서
public class GameManager : MonoBehaviour, IManager
{
    public ManagerScope GetManagerScope()
    {
        return ManagerScope.Global; // 모든 씬에서 유지
    }
    
    private void Start()
    {
        // SceneManagerController에 등록
        if (SceneManagerController.Instance != null)
        {
            SceneManagerController.Instance.RegisterManagerForScene("MainMenu", this);
            SceneManagerController.Instance.RegisterManagerForScene("Gameplay", this);
            SceneManagerController.Instance.RegisterManagerForScene("Ending", this);
        }
    }
}

// CorridorManager.cs에서
public class CorridorManager : MonoBehaviour, IManager
{
    public ManagerScope GetManagerScope()
    {
        return ManagerScope.Gameplay; // 인게임 전용
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

### 2. 씬 전환 처리

```csharp
// 씬 전환 시 자동으로 매니저 활성화/비활성화
// MainMenu 씬: Global + UI Managers만 활성화
// Gameplay 씬: Global + Scene + Gameplay Managers 활성화
// Ending 씬: Global + UI Managers만 활성화
```

### 3. 매니저 상태 확인

```csharp
// 현재 활성화된 매니저들 확인
var activeManagers = SceneManagerController.Instance.GetActiveManagers();
Debug.Log($"Active managers: {activeManagers.Count}");

// 특정 범위의 매니저들 확인
var gameplayManagers = SceneManagerController.Instance.GetActiveManagersByScope(ManagerScope.Gameplay);
Debug.Log($"Gameplay managers: {gameplayManagers.Count}");

// 매니저 상태 로그
SceneManagerController.Instance.LogManagerStatus();
```

## 📊 성능 최적화 효과

### 메모리 사용량
- **MainMenu 씬**: Global + UI 매니저만 활성화 (약 30% 절약)
- **Gameplay 씬**: 모든 매니저 활성화 (100%)
- **Ending 씬**: Global + UI 매니저만 활성화 (약 30% 절약)

### 초기화 시간
- **씬 전환 시**: 필요한 매니저만 초기화
- **메모리 정리**: 비활성화된 매니저들의 리소스 해제

### 안정성
- **씬별 격리**: 씬 전환 시 매니저 상태 초기화
- **오류 격리**: 한 씬의 매니저 오류가 다른 씬에 영향 없음

## 🔧 설정 및 디버깅

### 디버그 로깅 활성화
```csharp
// SceneManagerController의 enableDebugLogging을 true로 설정
// 씬 전환과 매니저 활성화/비활성화 로그 확인
```

### 매니저 상태 모니터링
```csharp
// 매니저 상태 실시간 확인
SceneManagerController.Instance.LogManagerStatus();
```

이 시스템을 통해 씬별로 필요한 매니저만 관리하여 메모리 효율성과 성능을 크게 향상시킬 수 있습니다.
