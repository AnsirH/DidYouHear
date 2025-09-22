using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using DidYouHear.Player;
using DidYouHear.Corridor;
using DidYouHear.Manager.Gameplay;
using DidYouHear.Manager.Core;
using DidYouHear.Manager.System;
using DidYouHear.Manager.Interfaces;

namespace DidYouHear.Core
{
    /// <summary>
    /// 플레이어 게임 상황 상태 enum (게임 상황)
    /// </summary>
    public enum PlayerGameState
    {
        Normal,         // 정상 상태 (안전)
        GhostBehind,    // 공깃돌 소리 없음 - 귀신이 뒤에 있음 (위험)
        InDanger        // 위험 상황 (귀신 등장 등)
    }
    
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
        private float maxGhostBehindTime = 2f; // 2초 후 게임 오버
        
        [Header("Manager References")]
        public PlayerController playerController;
        public CorridorManager corridorManager;
        public EventManager eventManager;
        public AudioManager audioManager;
        public GhostManager ghostManager;
        public StonePool stonePool;
        public CameraController cameraController;
        
        // IManager 인터페이스를 구현하는 매니저들
        private IManager[] managers;
        
        // IManager 구현을 위한 필드
        private bool isInitialized = false;
        private bool isActive = true;
        private DependencyContainer dependencyContainer;
        
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
        
        // 이벤트 시스템
        public static event Action<GameState> OnGameStateChanged;
        public static event Action OnPlayerDied;
        public static event Action OnGamePaused;
        public static event Action OnGameResumed;
        
        // 플레이어 게임 상태 이벤트
        public static event Action<PlayerGameState> OnPlayerGameStateChanged;
        public static event Action OnGhostDetected;  // 귀신 감지 (공깃돌 소리 없음)
        public static event Action OnGhostGone;      // 귀신 사라짐 (공깃돌 소리 확인)
        
        private void Awake()
        {
            // 싱글톤 패턴 구현
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGame();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // 게임 시작 시 메뉴 상태로 설정
            ChangeGameState(GameState.Menu);
            
            // 모든 매니저 참조 초기화
            InitializeManagers();
        }
        
        private void Update()
        {
            // 게임 시간 업데이트 (플레이 중일 때만)
            if (currentState == GameState.Playing)
            {
                gameTime += Time.deltaTime;
                HandlePlayerGameState();
            }
            
            // ESC 키로 일시정지 토글
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (currentState == GameState.Playing)
                {
                    PauseGame();
                }
                else if (currentState == GameState.Paused)
                {
                    ResumeGame();
                }
            }
        }
        
        /// <summary>
        /// 모든 매니저 참조 초기화
        /// </summary>
        private void InitializeManagers()
        {
            // PlayerController 초기화
            if (playerController == null)
            {
                playerController = FindObjectOfType<PlayerController>();
                if (playerController == null)
                {
                    Debug.LogError("GameManager: PlayerController component not found!");
                }
            }
            
            // CorridorManager 초기화
            if (corridorManager == null)
            {
                corridorManager = FindObjectOfType<CorridorManager>();
                if (corridorManager == null)
                {
                    Debug.LogError("GameManager: CorridorManager component not found!");
                }
            }
            
            // EventManager 초기화
            if (eventManager == null)
            {
                eventManager = FindObjectOfType<EventManager>();
                if (eventManager == null)
                {
                    Debug.LogError("GameManager: EventManager component not found!");
                }
            }
            
            // AudioManager 초기화
            if (audioManager == null)
            {
                audioManager = FindObjectOfType<AudioManager>();
                if (audioManager == null)
                {
                    Debug.LogWarning("GameManager: AudioManager component not found!");
                }
            }
            
            // GhostManager 초기화
            if (ghostManager == null)
            {
                ghostManager = FindObjectOfType<GhostManager>();
                if (ghostManager == null)
                {
                    Debug.LogWarning("GameManager: GhostManager component not found!");
                }
            }
            
            // StonePool 초기화
            if (stonePool == null)
            {
                stonePool = FindObjectOfType<StonePool>();
                if (stonePool == null)
                {
                    Debug.LogWarning("GameManager: StonePool component not found!");
                }
            }
            
            // CameraController 초기화
            if (cameraController == null)
            {
                cameraController = playerController.GetComponent<CameraController>();
                if (cameraController == null)
                {
                    Debug.LogWarning("GameManager: CameraController component not found!");
                }
            }
            
            // IManager 인터페이스를 구현하는 매니저들 수집
            CollectManagers();
            
            Debug.Log("All managers initialized successfully");
        }
        
        /// <summary>
        /// IManager 인터페이스를 구현하는 매니저들 수집
        /// </summary>
        private void CollectManagers()
        {
            var managerList = new System.Collections.Generic.List<IManager>();
            
            // 각 매니저가 IManager를 구현하는지 확인하고 추가
            if (playerController is IManager) managerList.Add(playerController as IManager);
            if (corridorManager is IManager) managerList.Add(corridorManager as IManager);
            if (eventManager is IManager) managerList.Add(eventManager as IManager);
            if (audioManager is IManager) managerList.Add(audioManager as IManager);
            if (ghostManager is IManager) managerList.Add(ghostManager as IManager);
            if (stonePool is IManager) managerList.Add(stonePool as IManager);
            if (cameraController is IManager) managerList.Add(cameraController as IManager);
            
            managers = managerList.ToArray();
            Debug.Log($"Collected {managers.Length} IManager implementations");
        }
        
        /// <summary>
        /// 게임 초기화
        /// </summary>
        private void InitializeGame()
        {
            gameTime = 0f;
            isPlayerAlive = true;
            reactionSuccessCount = 0;
            reactionFailCount = 0;
            isGamePaused = false;
            currentPlayerGameState = PlayerGameState.Normal;
            ghostBehindTimer = 0f;
        }
        
        /// <summary>
        /// 게임 상태 변경
        /// </summary>
        /// <param name="newState">새로운 게임 상태</param>
        public void ChangeGameState(GameState newState)
        {
            if (currentState == newState) return;
            
            GameState previousState = currentState;
            currentState = newState;
            
            // 상태별 처리
            switch (newState)
            {
                case GameState.Menu:
                    Time.timeScale = 1f;
                    isGamePaused = false;
                    NotifyManagersGameStateChanged(newState);
                    break;
                    
                case GameState.Playing:
                    Time.timeScale = 1f;
                    isGamePaused = false;
                    NotifyManagersGameStateChanged(newState);
                    break;
                    
                case GameState.Paused:
                    Time.timeScale = 0f;
                    isGamePaused = true;
                    OnGamePaused?.Invoke();
                    NotifyManagersGameStateChanged(newState);
                    break;
                    
                case GameState.GameOver:
                    Time.timeScale = 0f;
                    isPlayerAlive = false;
                    OnPlayerDied?.Invoke();
                    NotifyManagersGameStateChanged(newState);
                    break;
                    
                case GameState.Ending:
                    Time.timeScale = 1f;
                    NotifyManagersGameStateChanged(newState);
                    break;
            }
            
            Debug.Log($"Game State Changed: {previousState} -> {newState}");
            OnGameStateChanged?.Invoke(newState);
        }
        
        /// <summary>
        /// 게임 시작
        /// </summary>
        public void StartGame()
        {
            InitializeGame();
            InitializeGameSystems();
            ChangeGameState(GameState.Playing);
            Debug.Log("Game Started!");
        }
        
        /// <summary>
        /// 게임 시스템 초기화
        /// </summary>
        private void InitializeGameSystems()
        {
            // IManager 인터페이스를 구현하는 매니저들 일괄 초기화
            InitializeAllManagers();
            
            // 특별한 초기화가 필요한 매니저들 개별 처리
            InitializeSpecialManagers();
            
            Debug.Log("All game systems initialized successfully");
        }
        
        /// <summary>
        /// 모든 IManager 인터페이스 매니저들 초기화
        /// </summary>
        private void InitializeAllManagers()
        {
            if (managers == null) return;
            
            foreach (var manager in managers)
            {
                if (manager != null)
                {
                    manager.Initialize();
                    manager.SetActive(true);
                    Debug.Log($"{manager.GetType().Name} initialized via IManager interface");
                }
            }
        }
        
        /// <summary>
        /// 특별한 초기화가 필요한 매니저들 개별 처리
        /// </summary>
        private void InitializeSpecialManagers()
        {
            // 플레이어 컨트롤러 초기화
            if (playerController != null)
            {
                playerController.SetPlayerControl(true);
                Debug.Log("PlayerController initialized");
            }
            
            // 복도 시스템 초기화
            if (corridorManager != null)
            {
                // CorridorManager에 필요한 참조들 전달
                corridorManager.Initialize(eventManager, playerController?.transform, playerController);
                // 복도 재생성 (새로운 게임 시작)
                corridorManager.RegenerateCorridors();
                Debug.Log("CorridorManager initialized with references");
            }
            
            // 귀신 시스템 초기화
            if (ghostManager != null)
            {
                // GhostManager에 필요한 참조들 전달
                var playerMovement = playerController.playerMovement;
                ghostManager.Initialize(playerController, playerMovement, cameraController, this, eventManager);
                Debug.Log("GhostManager initialized with references");
            }
        }
        
        /// <summary>
        /// 게임 일시정지
        /// </summary>
        public void PauseGame()
        {
            if (currentState == GameState.Playing)
            {
                ChangeGameState(GameState.Paused);
            }
        }
        
        /// <summary>
        /// 게임 재개
        /// </summary>
        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                ChangeGameState(GameState.Playing);
                OnGameResumed?.Invoke();
            }
        }
        
        /// <summary>
        /// 게임 오버 처리
        /// </summary>
        public void GameOver()
        {
            ChangeGameState(GameState.GameOver);
            Debug.Log("Game Over!");
        }
        
        /// <summary>
        /// 엔딩 처리
        /// </summary>
        public void EndGame()
        {
            ChangeGameState(GameState.Ending);
            Debug.Log("Game Ended - Player Escaped!");
        }
        
        /// <summary>
        /// 메인 메뉴로 돌아가기
        /// </summary>
        public void ReturnToMenu()
        {
            ChangeGameState(GameState.Menu);
            SceneManager.LoadScene("MainMenu");
        }
        
        /// <summary>
        /// 게임 재시작
        /// </summary>
        public void RestartGame()
        {
            InitializeGame();
            InitializeGameSystems();
            ChangeGameState(GameState.Playing);
            SceneManager.LoadScene("Gameplay");
        }
        
        /// <summary>
        /// 반응 성공 카운트 증가
        /// </summary>
        public void AddReactionSuccess()
        {
            reactionSuccessCount++;
            Debug.Log($"Reaction Success! Total: {reactionSuccessCount}");
        }
        
        /// <summary>
        /// 반응 실패 카운트 증가
        /// </summary>
        public void AddReactionFail()
        {
            reactionFailCount++;
            Debug.Log($"Reaction Failed! Total: {reactionFailCount}");
        }
        
        /// <summary>
        /// 반응 성공률 계산
        /// </summary>
        /// <returns>성공률 (0-1)</returns>
        public float GetReactionSuccessRate()
        {
            int totalReactions = reactionSuccessCount + reactionFailCount;
            if (totalReactions == 0) return 0f;
            return (float)reactionSuccessCount / totalReactions;
        }
        
        /// <summary>
        /// 게임 통계 정보 가져오기
        /// </summary>
        /// <returns>게임 통계 문자열</returns>
        public string GetGameStats()
        {
            return $"Play Time: {gameTime:F1}s\n" +
                   $"Success Rate: {GetReactionSuccessRate():P1}\n" +
                   $"Success: {reactionSuccessCount}\n" +
                   $"Failed: {reactionFailCount}";
        }
        
        // ===== PlayerGameState 관리 =====
        
        /// <summary>
        /// 플레이어 게임 상태 처리
        /// </summary>
        private void HandlePlayerGameState()
        {
            if (currentPlayerGameState == PlayerGameState.GhostBehind)
            {
                // 귀신이 뒤에 있는 상태에서 플레이어가 이동 중이면 타이머 증가
                // 귀신이 뒤에 있으면 정지해도 타이머는 유지 (이동 불가)
                // 귀신이 사라질 때까지 이동할 수 없음
                if (IsPlayerMoving())
                {
                    ghostBehindTimer += Time.deltaTime;
                    
                    // 2초 동안 계속 이동하면 게임 오버
                    if (ghostBehindTimer >= maxGhostBehindTime)
                    {
                        Debug.Log("Player continued moving with ghost behind - Game Over!");
                        GameOver();
                    }
                }
            }
        }
        
        /// <summary>
        /// 플레이어가 이동 중인지 확인
        /// </summary>
        private bool IsPlayerMoving()
        {
            if (playerController != null)
            {
                return playerController.IsPlayerMoving();
            }
            return false;
        }
        
        /// <summary>
        /// 플레이어 게임 상태 변경
        /// </summary>
        /// <param name="newState">새로운 플레이어 게임 상태</param>
        public void ChangePlayerGameState(PlayerGameState newState)
        {
            if (currentPlayerGameState == newState) return;
            
            currentPlayerGameState = newState;
            OnPlayerGameStateChanged?.Invoke(newState);
            Debug.Log($"Player Game State changed to: {newState}");
            
            // 상태별 처리
            switch (newState)
            {
                case PlayerGameState.GhostBehind:
                    ghostBehindTimer = 0f;
                    OnGhostDetected?.Invoke();
                    break;
                case PlayerGameState.Normal:
                    ghostBehindTimer = 0f;
                    OnGhostGone?.Invoke();
                    break;
            }
        }
        
        /// <summary>
        /// 현재 게임 상태 반환
        /// </summary>
        public GameState CurrentGameState
        {
            get { return currentState; }
        }
        
        /// <summary>
        /// 매니저들에게 게임 상태 변경 알림
        /// </summary>
        private void NotifyManagersGameStateChanged(GameState newState)
        {
            // IManager 인터페이스를 구현하는 매니저들에 상태 변경 알림
            if (managers != null)
            {
                foreach (var manager in managers)
                {
                    if (manager != null)
                    {
                        // 게임 상태에 따른 활성화/비활성화 처리
                        bool shouldBeActive = newState == GameState.Playing;
                        manager.SetActive(shouldBeActive);
                    }
                }
            }
            
            // 특별한 처리가 필요한 매니저들 개별 처리
            NotifySpecialManagers(newState);
            
            Debug.Log($"Notified all managers of game state change: {newState}");
        }
        
        /// <summary>
        /// 특별한 처리가 필요한 매니저들 개별 알림
        /// </summary>
        private void NotifySpecialManagers(GameState newState)
        {
            // 플레이어 컨트롤러는 이미 이벤트를 통해 상태 변경을 감지함
            // 필요시 특별한 처리가 있는 매니저들을 여기에 추가
        }
        
        /// <summary>
        /// 게임 상태 설정 (외부에서 호출 가능)
        /// </summary>
        public void SetGameState(GameState newState)
        {
            ChangeGameState(newState);
        }
        
        /// <summary>
        /// 특정 매니저 참조 가져오기
        /// </summary>
        public T GetManager<T>() where T : MonoBehaviour
        {
            if (typeof(T) == typeof(PlayerController)) return playerController as T;
            if (typeof(T) == typeof(CorridorManager)) return corridorManager as T;
            if (typeof(T) == typeof(EventManager)) return eventManager as T;
            if (typeof(T) == typeof(AudioManager)) return audioManager as T;
            if (typeof(T) == typeof(GhostManager)) return ghostManager as T;
            if (typeof(T) == typeof(StonePool)) return stonePool as T;
            if (typeof(T) == typeof(CameraController)) return cameraController as T;
            
            return null;
        }
        
        // IManager 인터페이스 구현
        public int GetPriority()
        {
            return (int)ManagerType.Core; // 0 (최우선)
        }
        
        public ManagerType GetManagerType()
        {
            return ManagerType.Core;
        }
        
        public ManagerScope GetManagerScope()
        {
            return ManagerScope.Global; // 모든 씬에서 유지
        }
        
        public void SetDependencyContainer(DependencyContainer container)
        {
            dependencyContainer = container;
        }
        
        public void SetActive(bool active)
        {
            isActive = active;
            gameObject.SetActive(active);
        }
        
        public string GetStatus()
        {
            return $"GameManager: Initialized={isInitialized}, Active={isActive}, " +
                   $"GameState={currentState}, PlayerAlive={isPlayerAlive}, " +
                   $"GameTime={gameTime:F1}s";
        }
        
        public void Initialize()
        {
            if (!isInitialized)
            {
                InitializeGame();
                isInitialized = true;
            }
        }
        
        public void Reset()
        {
            isInitialized = false;
            isActive = false;
            gameTime = 0f;
            isPlayerAlive = true;
            currentState = GameState.Menu;
        }
        
        public bool IsInitialized()
        {
            return isInitialized;
        }
    }
}
