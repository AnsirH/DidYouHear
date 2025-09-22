using UnityEngine;
using System.Collections;
using System;
using DidYouHear.Manager.Interfaces;
using DidYouHear.Player;
using DidYouHear.Manager.System;
using DidYouHear.Manager.Core;
using DidYouHear.Core;
using DidYouHear.Ghost;

namespace DidYouHear.Manager.Gameplay
{
    /// <summary>
    /// 귀신 시스템 관리자
    /// </summary>
    public class GhostManager : MonoBehaviour, IManager
    {
        [Header("Ghost Settings")]
        public bool enableGhost = true;
        public float ghostAppearanceDelay = 0.5f;
        public float chaseDuration = 10f;
        public float chaseDistance = 20f;
        
        [Header("Ghost Appearance Settings")]
        public GameObject ghostPrefab;
        public Transform ghostSpawnPoint;
        public float ghostAppearanceDuration = 2f;
        public float ghostFadeInDuration = 0.5f;
        public float ghostFadeOutDuration = 0.5f;
        
        [Header("Chase Settings")]
        public float chaseSpeed = 5f;
        public float chaseAcceleration = 2f;
        public float chaseDeceleration = 1f;
        public float chaseDetectionRange = 15f;
        
        [Header("Visual Effects")]
        public float screenDistortionIntensity = 0.1f;
        public float screenShakeIntensity = 0.5f;
        public float screenShakeDuration = 1f;
        public Color ghostTintColor = Color.red;
        
        [Header("Audio Settings")]
        public AudioClip ghostAppearanceSound;
        public AudioClip ghostChaseSound;
        public AudioClip ghostScreamSound;
        public float ghostSoundVolume = 1f;
        
        // 컴포넌트 참조
        private PlayerController playerController;
        private PlayerMovement playerMovement;
        private CameraController cameraController;
        private GameManager gameManager;
        private EventManager eventManager;
        
        // 귀신 상태
        private bool isGhostActive = false;
        private bool isChasing = false;
        private bool isAppearing = false;
        
        // 귀신 인스턴스
        private GameObject currentGhost;
        private GhostAppearance ghostAppearance;
        private GhostChase ghostChase;
        
        // 추격 관련
        private float chaseStartTime;
        private float lastChaseCheckTime;
        private Vector3 lastPlayerPosition;
        
        // 이벤트
        public Action OnGhostAppeared;
        public Action OnGhostDisappeared;
        public Action OnChaseStarted;
        public Action OnChaseEnded;
        public Action OnGameOverTriggered;
        
        // IManager 구현을 위한 필드
        private bool isInitialized = false;
        private bool isActive = true;
        private DependencyContainer dependencyContainer;
        
        // 싱글톤
        public static GhostManager Instance { get; private set; }
        
        private void Awake()
        {
            // 싱글톤 설정
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        
        private void Start()
        {
            InitializeComponents();
            SubscribeToEvents();
        }
        
        private void Update()
        {
            if (!enableGhost) return;
            
            // 추격 중일 때 업데이트
            if (isChasing)
            {
                UpdateChase();
            }
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        /// <summary>
        /// 컴포넌트 초기화 (GameManager에서 호출)
        /// </summary>
        public void Initialize(PlayerController playerCtrl, PlayerMovement playerMove, CameraController cameraCtrl, GameManager gameMgr, EventManager eventMgr)
        {
            playerController = playerCtrl;
            playerMovement = playerMove;
            cameraController = cameraCtrl;
            gameManager = gameMgr;
            eventManager = eventMgr;
            
            if (playerController == null) Debug.LogError("GhostManager: PlayerController not provided!");
            if (playerMovement == null) Debug.LogError("GhostManager: PlayerMovement not provided!");
            if (cameraController == null) Debug.LogError("GhostManager: CameraController not provided!");
            if (gameManager == null) Debug.LogError("GhostManager: GameManager not provided!");
            if (eventManager == null) Debug.LogError("GhostManager: EventManager not provided!");
            
            Debug.Log("GhostManager initialized with provided references");
        }
        
        /// <summary>
        /// 컴포넌트 초기화 (기존 방식 - 호환성 유지)
        /// </summary>
        private void InitializeComponents()
        {
            // GameManager에서 초기화되지 않은 경우에만 FindObjectOfType 사용
            if (playerController == null) playerController = FindObjectOfType<PlayerController>();
            if (playerMovement == null) playerMovement = FindObjectOfType<PlayerMovement>();
            if (cameraController == null) cameraController = FindObjectOfType<CameraController>();
            if (gameManager == null) gameManager = GameManager.Instance;
            if (eventManager == null) eventManager = EventManager.Instance;
            
            if (playerController == null) Debug.LogError("GhostManager: PlayerController not found!");
            if (playerMovement == null) Debug.LogError("GhostManager: PlayerMovement not found!");
            if (cameraController == null) Debug.LogError("GhostManager: CameraController not found!");
            if (gameManager == null) Debug.LogError("GhostManager: GameManager not found!");
            if (eventManager == null) Debug.LogError("GhostManager: EventManager not found!");
        }
        
        /// <summary>
        /// 이벤트 구독
        /// </summary>
        private void SubscribeToEvents()
        {
            if (eventManager != null)
            {
                eventManager.OnEventTriggered += HandleEventTriggered;
                eventManager.OnEventCompleted += HandleEventCompleted;
            }
        }
        
        /// <summary>
        /// 이벤트 구독 해제
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (eventManager != null)
            {
                eventManager.OnEventTriggered -= HandleEventTriggered;
                eventManager.OnEventCompleted -= HandleEventCompleted;
            }
        }
        
        /// <summary>
        /// 이벤트 발생 처리
        /// </summary>
        private void HandleEventTriggered(EventManager.EventType eventType)
        {
            if (eventType == EventManager.EventType.GhostAppearance)
            {
                StartCoroutine(TriggerGhostAppearance());
            }
        }
        
        /// <summary>
        /// 이벤트 완료 처리 (성공/실패)
        /// </summary>
        private void HandleEventCompleted(EventManager.EventType eventType, bool success)
        {
            // 이벤트가 실패했을 때만 귀신 등장
            if (!success && (eventType == EventManager.EventType.ShoulderTap || 
                eventType == EventManager.EventType.Environmental))
            {
                StartCoroutine(TriggerGhostAppearance());
            }
        }
        
        /// <summary>
        /// 귀신 등장 트리거
        /// </summary>
        public IEnumerator TriggerGhostAppearance()
        {
            if (isGhostActive || isAppearing) yield break;
            
            isAppearing = true;
            
            // 지연 시간 대기
            yield return new WaitForSeconds(ghostAppearanceDelay);
            
            // 귀신 등장
            yield return StartCoroutine(ShowGhost());
            
            // 등장 완료 후 추격 시작
            yield return StartCoroutine(StartChase());
        }
        
        /// <summary>
        /// 귀신 등장 연출
        /// </summary>
        private IEnumerator ShowGhost()
        {
            isGhostActive = true;
            
            // 귀신 생성
            CreateGhost();
            
            // 카메라 강제 뒤돌림
            ForceCameraLookBack();
            
            // 시각적 효과 적용
            ApplyVisualEffects();
            
            // 사운드 재생
            PlayGhostSounds();
            
            // 등장 알림
            OnGhostAppeared?.Invoke();
            
            Debug.Log("Ghost appeared!");
            
            // 등장 지속 시간 대기
            yield return new WaitForSeconds(ghostAppearanceDuration);
            
            isAppearing = false;
        }
        
        /// <summary>
        /// 귀신 생성
        /// </summary>
        private void CreateGhost()
        {
            if (ghostPrefab == null) return;
            
            // 귀신 생성 위치 계산
            Vector3 spawnPosition = GetGhostSpawnPosition();
            
            // 귀신 인스턴스 생성
            currentGhost = Instantiate(ghostPrefab, spawnPosition, Quaternion.identity);
            
            // 귀신 컴포넌트 설정
            ghostAppearance = currentGhost.GetComponent<GhostAppearance>();
            if (ghostAppearance == null)
            {
                ghostAppearance = currentGhost.AddComponent<GhostAppearance>();
            }
            
            ghostChase = currentGhost.GetComponent<GhostChase>();
            if (ghostChase == null)
            {
                ghostChase = currentGhost.AddComponent<GhostChase>();
            }
            
            // 귀신 초기화
            ghostAppearance.Initialize(this);
            ghostChase.Initialize(this);
        }
        
        /// <summary>
        /// 귀신 생성 위치 계산
        /// </summary>
        private Vector3 GetGhostSpawnPosition()
        {
            if (ghostSpawnPoint != null)
            {
                return ghostSpawnPoint.position;
            }
            
            // 플레이어 뒤쪽에 생성
            Vector3 playerPosition = Camera.main.transform.position;
            Vector3 playerForward = Camera.main.transform.forward;
            
            return playerPosition - playerForward * 2f + Vector3.up * 1.5f;
        }
        
        /// <summary>
        /// 카메라 강제 뒤돌림
        /// </summary>
        private void ForceCameraLookBack()
        {
            if (cameraController != null)
            {
                // 카메라를 뒤쪽으로 강제 회전
                StartCoroutine(ForceCameraRotation());
            }
        }
        
        /// <summary>
        /// 카메라 강제 회전 코루틴
        /// </summary>
        private IEnumerator ForceCameraRotation()
        {
            float startTime = Time.time;
            float duration = 1f;
            
            Quaternion startRotation = Camera.main.transform.rotation;
            Quaternion targetRotation = Quaternion.Euler(0, 180, 0);
            
            while (Time.time - startTime < duration)
            {
                float progress = (Time.time - startTime) / duration;
                Camera.main.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, progress);
                yield return null;
            }
            
            Camera.main.transform.rotation = targetRotation;
        }
        
        /// <summary>
        /// 시각적 효과 적용
        /// </summary>
        private void ApplyVisualEffects()
        {
            // 화면 쉐이크
            if (cameraController != null)
            {
                cameraController.StartCameraShake(screenShakeIntensity, screenShakeDuration);
            }
            
            // 화면 왜곡 효과
            StartCoroutine(ScreenDistortionEffect());
        }
        
        /// <summary>
        /// 화면 왜곡 효과 코루틴
        /// </summary>
        private IEnumerator ScreenDistortionEffect()
        {
            float startTime = Time.time;
            float duration = ghostAppearanceDuration;
            
            while (Time.time - startTime < duration)
            {
                // FOV 왜곡
                float distortion = Mathf.Sin((Time.time - startTime) * 10f) * screenDistortionIntensity;
                Camera.main.fieldOfView = 60f + distortion;
                
                yield return null;
            }
            
            // 원본 FOV 복원
            Camera.main.fieldOfView = 60f;
        }
        
        /// <summary>
        /// 귀신 사운드 재생
        /// </summary>
        private void PlayGhostSounds()
        {
            if (AudioManager.Instance != null)
            {
                Vector3 playerPosition = Camera.main.transform.position;
                
                // 귀신 등장 사운드
                if (ghostAppearanceSound != null)
                {
                    AudioManager.Instance.Play3DSound(ghostAppearanceSound, playerPosition, ghostSoundVolume);
                }
                
                // 비명 사운드
                if (ghostScreamSound != null)
                {
                    AudioManager.Instance.Play3DSound(ghostScreamSound, playerPosition, ghostSoundVolume * 0.8f);
                }
            }
        }
        
        /// <summary>
        /// 추격 시작
        /// </summary>
        private IEnumerator StartChase()
        {
            isChasing = true;
            chaseStartTime = Time.time;
            lastPlayerPosition = Camera.main.transform.position;
            
            // 추격 알림
            OnChaseStarted?.Invoke();
            
            Debug.Log("Ghost chase started!");
            
            // 추격 지속 시간 대기
            yield return new WaitForSeconds(chaseDuration);
            
            // 추격 종료
            EndChase();
        }
        
        /// <summary>
        /// 추격 업데이트
        /// </summary>
        private void UpdateChase()
        {
            if (!isChasing) return;
            
            // 플레이어 위치 추적
            Vector3 currentPlayerPosition = Camera.main.transform.position;
            float distance = Vector3.Distance(currentPlayerPosition, lastPlayerPosition);
            
            // 플레이어가 달리지 않으면 게임 오버
            if (distance < 0.1f && playerMovement != null && playerMovement.CurrentMovementState != PlayerMovementState.Running)
            {
                TriggerGameOver();
                return;
            }
            
            // 추격 거리 체크
            if (distance >= chaseDistance)
            {
                EndChase();
                return;
            }
            
            lastPlayerPosition = currentPlayerPosition;
            lastChaseCheckTime = Time.time;
        }
        
        /// <summary>
        /// 추격 종료
        /// </summary>
        private void EndChase()
        {
            isChasing = false;
            
            // 귀신 제거
            if (currentGhost != null)
            {
                Destroy(currentGhost);
                currentGhost = null;
            }
            
            // 귀신 비활성화
            isGhostActive = false;
            
            // 추격 종료 알림
            OnChaseEnded?.Invoke();
            
            Debug.Log("Ghost chase ended!");
        }
        
        /// <summary>
        /// 게임 오버 트리거
        /// </summary>
        public void TriggerGameOver()
        {
            isChasing = false;
            isGhostActive = false;
            
            // 게임 오버 알림
            OnGameOverTriggered?.Invoke();
            
            // 게임 상태 변경
            if (gameManager != null)
            {
                gameManager.ChangeGameState(GameManager.GameState.GameOver);
            }
            
            Debug.Log("Game Over triggered by ghost!");
        }
        
        /// <summary>
        /// 귀신 활성화 여부 반환
        /// </summary>
        public bool IsGhostActive()
        {
            return isGhostActive;
        }
        
        /// <summary>
        /// 추격 중인지 반환
        /// </summary>
        public bool IsChasing()
        {
            return isChasing;
        }
        
        /// <summary>
        /// 등장 중인지 반환
        /// </summary>
        public bool IsAppearing()
        {
            return isAppearing;
        }
        
        /// <summary>
        /// 남은 추격 시간 반환
        /// </summary>
        public float GetRemainingChaseTime()
        {
            if (!isChasing) return 0f;
            
            return Mathf.Max(0f, chaseDuration - (Time.time - chaseStartTime));
        }
        
        // IManager 인터페이스 구현
        public int GetPriority()
        {
            return (int)ManagerType.Gameplay; // 20
        }
        
        public ManagerType GetManagerType()
        {
            return ManagerType.Gameplay;
        }
        
        public ManagerScope GetManagerScope()
        {
            return ManagerScope.Gameplay; // 인게임 전용
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
            return $"GhostManager: Initialized={isInitialized}, Active={isActive}, " +
                   $"GhostActive={isGhostActive}, Chasing={isChasing}, " +
                   $"ChaseTime={GetRemainingChaseTime():F1}s";
        }
        
        public void Initialize()
        {
            if (!isInitialized)
            {
                InitializeGhost();
                isInitialized = true;
            }
        }
        
        private void InitializeGhost()
        {
            // 귀신 시스템 초기화 로직
            isGhostActive = false;
            isChasing = false;
            chaseStartTime = 0f;
            InitializeComponents();
        }
        
        public void Reset()
        {
            isInitialized = false;
            isActive = false;
            isGhostActive = false;
            isChasing = false;
        }
        
        public bool IsInitialized()
        {
            return isInitialized;
        }
    }
}
