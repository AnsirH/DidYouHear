using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DidYouHear.Manager.Core;
using DidYouHear.Core;

namespace DidYouHear.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("UI Panels")]
        public GameObject mainMenuPanel;
        public GameObject gameplayPanel;
        public GameObject pausePanel;
        public GameObject gameOverPanel;
        public GameObject endingPanel;
        
        [Header("Gameplay UI Elements")]
        public TextMeshProUGUI statusText;
        public TextMeshProUGUI instructionText;
        public Slider healthBar;
        public Image crosshair;
        
        [Header("Game Over UI Elements")]
        public TextMeshProUGUI gameOverText;
        public TextMeshProUGUI statsText;
        public Button restartButton;
        public Button menuButton;
        
        [Header("Pause UI Elements")]
        public Button resumeButton;
        public Button settingsButton;
        public Button quitButton;
        
        [Header("Settings")]
        public bool showDebugInfo = true;
        public float uiUpdateInterval = 0.1f;
        
        // 현재 활성화된 패널
        private GameObject currentActivePanel;
        
        // UI 업데이트 타이머
        private float uiUpdateTimer = 0f;
        
        // 이벤트
        public System.Action OnUIPanelChanged;
        
        private void Awake()
        {
            // 싱글톤 패턴 (필요시)
            InitializeUI();
        }
        
        private void Start()
        {
            // 이벤트 구독
            SubscribeToEvents();
            
            // 초기 UI 상태 설정
            SetInitialUIState();
        }
        
        private void Update()
        {
            // UI 업데이트
            UpdateUI();
        }
        
        /// <summary>
        /// UI 초기화
        /// </summary>
        private void InitializeUI()
        {
            // 모든 패널 비활성화
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (gameplayPanel != null) gameplayPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (endingPanel != null) endingPanel.SetActive(false);
            
            // 버튼 이벤트 설정
            SetupButtonEvents();
            
            Debug.Log("UI Manager Initialized");
        }
        
        /// <summary>
        /// 이벤트 구독
        /// </summary>
        private void SubscribeToEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.OnGameStateChanged += HandleGameStateChanged;
                GameManager.OnPlayerDied += HandlePlayerDied;
            }
        }
        
        /// <summary>
        /// 초기 UI 상태 설정
        /// </summary>
        private void SetInitialUIState()
        {
            if (GameManager.Instance != null)
            {
                HandleGameStateChanged(GameManager.Instance.currentState);
            }
        }
        
        /// <summary>
        /// 버튼 이벤트 설정
        /// </summary>
        private void SetupButtonEvents()
        {
            // 게임 오버 패널 버튼
            if (restartButton != null)
                restartButton.onClick.AddListener(RestartGame);
            
            if (menuButton != null)
                menuButton.onClick.AddListener(ReturnToMenu);
            
            // 일시정지 패널 버튼
            if (resumeButton != null)
                resumeButton.onClick.AddListener(ResumeGame);
            
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OpenSettings);
            
            if (quitButton != null)
                quitButton.onClick.AddListener(QuitGame);
        }
        
        /// <summary>
        /// UI 업데이트
        /// </summary>
        private void UpdateUI()
        {
            uiUpdateTimer += Time.deltaTime;
            
            if (uiUpdateTimer >= uiUpdateInterval)
            {
                UpdateGameplayUI();
                uiUpdateTimer = 0f;
            }
        }
        
        /// <summary>
        /// 게임플레이 UI 업데이트
        /// </summary>
        private void UpdateGameplayUI()
        {
            if (gameplayPanel == null || !gameplayPanel.activeInHierarchy) return;
            
            // 상태 텍스트 업데이트
            if (statusText != null)
            {
                UpdateStatusText();
            }
            
            // 지시사항 텍스트 업데이트
            if (instructionText != null)
            {
                UpdateInstructionText();
            }
            
            // 크로스헤어 업데이트
            if (crosshair != null)
            {
                UpdateCrosshair();
            }
        }
        
        /// <summary>
        /// 상태 텍스트 업데이트
        /// </summary>
        private void UpdateStatusText()
        {
            if (GameManager.Instance == null) return;
            
            string status = "";
            
            if (showDebugInfo)
            {
                status += $"Game Time: {GameManager.Instance.gameTime:F1}s\n";
                status += $"Success Rate: {GameManager.Instance.GetReactionSuccessRate():P1}\n";
                status += $"State: {GameManager.Instance.currentState}";
            }
            else
            {
                status = "Find the exit...";
            }
            
            statusText.text = status;
        }
        
        /// <summary>
        /// 지시사항 텍스트 업데이트
        /// </summary>
        private void UpdateInstructionText()
        {
            if (GameManager.Instance == null) return;
            
            string instruction = "";
            
            switch (GameManager.Instance.currentState)
            {
                case GameManager.GameState.Playing:
                    instruction = "W: Move Forward | Shift: Run | Ctrl: Crouch | Q/E: Look Back | Space: Throw Stone";
                    break;
                case GameManager.GameState.Paused:
                    instruction = "Press ESC to resume";
                    break;
                case GameManager.GameState.GameOver:
                    instruction = "Press R to restart or M for menu";
                    break;
            }
            
            instructionText.text = instruction;
        }
        
        /// <summary>
        /// 크로스헤어 업데이트
        /// </summary>
        private void UpdateCrosshair()
        {
            if (crosshair == null) return;
            
            // 크로스헤어 표시/숨김 로직
            bool shouldShow = GameManager.Instance != null && 
                             GameManager.Instance.currentState == GameManager.GameState.Playing;
            
            crosshair.gameObject.SetActive(shouldShow);
        }
        
        /// <summary>
        /// 게임 상태 변경 처리
        /// </summary>
        private void HandleGameStateChanged(GameManager.GameState newState)
        {
            switch (newState)
            {
                case GameManager.GameState.Menu:
                    ShowPanel(mainMenuPanel);
                    break;
                case GameManager.GameState.Playing:
                    ShowPanel(gameplayPanel);
                    break;
                case GameManager.GameState.Paused:
                    ShowPanel(pausePanel);
                    break;
                case GameManager.GameState.GameOver:
                    ShowPanel(gameOverPanel);
                    UpdateGameOverUI();
                    break;
                case GameManager.GameState.Ending:
                    ShowPanel(endingPanel);
                    break;
            }
        }
        
        /// <summary>
        /// 플레이어 사망 처리
        /// </summary>
        private void HandlePlayerDied()
        {
            ShowPanel(gameOverPanel);
            UpdateGameOverUI();
        }
        
        /// <summary>
        /// 게임 오버 UI 업데이트
        /// </summary>
        private void UpdateGameOverUI()
        {
            if (GameManager.Instance == null) return;
            
            if (gameOverText != null)
            {
                gameOverText.text = "GAME OVER";
            }
            
            if (statsText != null)
            {
                statsText.text = GameManager.Instance.GetGameStats();
            }
        }
        
        /// <summary>
        /// 패널 표시
        /// </summary>
        private void ShowPanel(GameObject panel)
        {
            if (panel == null) return;
            
            // 현재 활성화된 패널 숨기기
            if (currentActivePanel != null && currentActivePanel != panel)
            {
                currentActivePanel.SetActive(false);
            }
            
            // 새 패널 표시
            panel.SetActive(true);
            currentActivePanel = panel;
            
            OnUIPanelChanged?.Invoke();
            Debug.Log($"UI Panel Changed: {panel.name}");
        }
        
        /// <summary>
        /// 게임 재시작
        /// </summary>
        public void RestartGame()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
        }
        
        /// <summary>
        /// 메뉴로 돌아가기
        /// </summary>
        public void ReturnToMenu()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMenu();
            }
        }
        
        /// <summary>
        /// 게임 재개
        /// </summary>
        public void ResumeGame()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResumeGame();
            }
        }
        
        /// <summary>
        /// 설정 열기
        /// </summary>
        public void OpenSettings()
        {
            Debug.Log("Settings opened");
            // TODO: 설정 UI 구현
        }
        
        /// <summary>
        /// 게임 종료
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("Quit game");
            Application.Quit();
        }
        
        /// <summary>
        /// 디버그 정보 표시 설정
        /// </summary>
        public void SetDebugInfo(bool show)
        {
            showDebugInfo = show;
        }
        
        /// <summary>
        /// UI 업데이트 간격 설정
        /// </summary>
        public void SetUIUpdateInterval(float interval)
        {
            uiUpdateInterval = interval;
        }
        
        /// <summary>
        /// 현재 활성화된 패널 반환
        /// </summary>
        public GameObject GetCurrentActivePanel()
        {
            return currentActivePanel;
        }
        
        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (GameManager.Instance != null)
            {
                GameManager.OnGameStateChanged -= HandleGameStateChanged;
                GameManager.OnPlayerDied -= HandlePlayerDied;
            }
        }
    }
}
