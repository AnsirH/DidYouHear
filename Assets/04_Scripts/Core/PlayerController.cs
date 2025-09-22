using UnityEngine;
using DidYouHear.Player;
using DidYouHear.Core;
using DidYouHear.Manager.Gameplay;
using DidYouHear.Stone;

namespace DidYouHear.Core
{
    [RequireComponent(typeof(PlayerMovement), typeof(PlayerLook), typeof(StoneThrower))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Player Components")]
        public PlayerMovement playerMovement;
        public PlayerLook playerLook;
        public StoneThrower stoneThrower;
        public Animator playerAnimator;
        
        [Header("Player Settings")]
        public bool canMove = true;
        public bool canLook = true;
        public bool canInteract = true;
        
        [Header("Rotation Settings")]
        public float rotationSpeed = 90f; // 회전 속도 (도/초)
        public float rotationThreshold = 5f; // 회전 완료 임계값 (도)
        
        // 이벤트
        public System.Action OnPlayerInitialized;
        public System.Action OnPlayerDied;
        
        // 회전 관련
        private bool isRotating = false;
        private Quaternion targetRotation;
        private System.Action onRotationComplete;
        
        private void Awake()
        {
            // 컴포넌트 초기화
            InitializeComponents();
        }
        
        private void Start()
        {
            // 플레이어 초기화
            InitializePlayer();
            
            // GameManager 이벤트 구독
            if (GameManager.Instance != null)
            {
                GameManager.OnGhostDetected += OnGhostDetected;
                GameManager.OnGhostGone += OnGhostGone;
            }
        }
        
        private void OnDestroy()
        {
            // GameManager 이벤트 구독 해제
            if (GameManager.Instance != null)
            {
                GameManager.OnGhostDetected -= OnGhostDetected;
                GameManager.OnGhostGone -= OnGhostGone;
            }
        }
        
        private void Update()
        {
            // 게임이 일시정지 상태가 아닐 때만 처리
            if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Playing)
                return;
                
            HandleInput();
            UpdatePlayerState();
            UpdateRotation();
        }
        
        /// <summary>
        /// 컴포넌트 초기화
        /// </summary>
        private void InitializeComponents()
        {
            // PlayerMovement 컴포넌트 가져오기
            playerMovement = GetComponent<PlayerMovement>();
            if (playerMovement == null)
            {
                Debug.LogError("PlayerController: PlayerMovement component not found!");
            }
            
            // PlayerLook 컴포넌트 가져오기
            playerLook = GetComponent<PlayerLook>();
            if (playerLook == null)
            {
                Debug.LogError("PlayerController: PlayerLook component not found!");
            }
            
            // StoneThrower 컴포넌트 가져오기
            stoneThrower = GetComponent<StoneThrower>();
            if (stoneThrower == null)
            {
                Debug.LogError("PlayerController: StoneThrower component not found!");
            }
            
            // Animator 컴포넌트 가져오기
            if (playerAnimator == null)
            {
                playerAnimator = GetComponentInChildren<Animator>();
                Debug.LogWarning("PlayerController: Animator component not found! Animation will be disabled.");
            }
        }
        
        /// <summary>
        /// 플레이어 초기화
        /// </summary>
        private void InitializePlayer()
        {
            // 이벤트 구독
            SubscribeToEvents();
            
            // 초기 상태 설정
            canMove = true;
            canLook = true;
            canInteract = true;
            
            OnPlayerInitialized?.Invoke();
            Debug.Log("Player Controller Initialized");
        }
        
        /// <summary>
        /// 이벤트 구독
        /// </summary>
        private void SubscribeToEvents()
        {
            // GameManager 이벤트 구독
            if (GameManager.Instance != null)
            {
                GameManager.OnPlayerDied += HandlePlayerDeath;
                GameManager.OnGameStateChanged += HandleGameStateChanged;
            }
            
            // PlayerMovement 이벤트 구독
            if (playerMovement != null)
            {
                playerMovement.OnMovementStateChanged += HandleMovementStateChanged;
                playerMovement.OnMovementChanged += HandleMovementChanged;
            }
            
            // PlayerLook 이벤트 구독
            if (playerLook != null)
            {
                playerLook.OnLookStateChanged += HandleLookStateChanged;
                playerLook.OnLookingBackChanged += HandleLookingBackChanged;
            }
            
            // StoneThrower 이벤트 구독
            if (stoneThrower != null)
            {
                stoneThrower.OnStoneThrown += HandleStoneThrown;
                stoneThrower.OnAutoThrowTriggered += HandleAutoThrowTriggered;
                stoneThrower.OnManualThrowTriggered += HandleManualThrowTriggered;
            }
        }
        
        /// <summary>
        /// 입력 처리
        /// </summary>
        private void HandleInput()
        {
            // 이동 제어
            if (canMove && playerMovement != null)
            {
                // 이동은 PlayerMovement에서 자동으로 처리됨
            }
            
            // 시점 제어
            if (canLook && playerLook != null)
            {
                // 시점은 PlayerLook에서 자동으로 처리됨
            }
            
            // 상호작용 제어
            if (canInteract)
            {
                HandleInteractionInput();
            }
        }
        
        /// <summary>
        /// 상호작용 입력 처리
        /// </summary>
        private void HandleInteractionInput()
        {
            // 수동 공깃돌 던지기는 StoneThrower에서 직접 처리
            // 중복 호출 방지를 위해 여기서는 제거
        }
        
        /// <summary>
        /// 귀신 감지 시 처리 (PRD REQ-STONE-04)
        /// </summary>
        public void OnGhostDetected()
        {
            Debug.Log("Ghost detected behind player - Player must stop (release W key)!");
            // 플레이어에게 정지하라는 UI 표시
            // TODO: UI 시스템과 연동하여 정지 알림 표시
        }
        
        /// <summary>
        /// 귀신 사라짐 시 처리
        /// </summary>
        public void OnGhostGone()
        {
            Debug.Log("Ghost gone - Safe to continue!");
            // TODO: UI 시스템과 연동하여 안전 알림 표시
        }
        
        /// <summary>
        /// 플레이어가 이동 중인지 확인 (GameManager에서 호출)
        /// </summary>
        public bool IsPlayerMoving()
        {
            if (playerMovement != null)
            {
                return playerMovement.IsPlayerMoving();
            }
            return false;
        }
        
        /// <summary>
        /// 공깃돌 소리 없음 상태에서 계속 이동 시 게임 오버 (PRD REQ-STONE-05)
        /// </summary>
        public void OnContinuedMovementWithoutStoneSound()
        {
            Debug.Log("Player continued moving without stone sound - Game Over!");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }
        }
        
        /// <summary>
        /// 플레이어 상태 업데이트
        /// </summary>
        private void UpdatePlayerState()
        {
            // 플레이어 상태에 따른 제어 업데이트
            if (playerMovement != null)
            {
                // 이동 상태에 따른 추가 처리
                var movementState = playerMovement.GetCurrentMovementState();
                switch (movementState)
                {
                    case PlayerMovementState.Idle:
                        // 정지 상태에서는 상호작용 가능
                        canInteract = true;
                        break;
                    default:
                        canInteract = true;
                        break;
                }
            }
        }
        
        /// <summary>
        /// 이동 상태 변경 처리
        /// </summary>
        private void HandleMovementStateChanged(PlayerMovementState newState)
        {
            Debug.Log($"Player Movement State Changed: {newState}");
            
            // 애니메이션 파라미터 업데이트
            UpdateAnimationParameters();
            
            // PRD에 정의된 상태별 처리
            switch (newState)
            {
                case PlayerMovementState.Running:
                    // 달리기 상태에서는 더 자주 공깃돌 던지기 (PRD REQ-PLAYER-02)
                    Debug.Log("Player is running - increased stone throw frequency");
                    break;
                case PlayerMovementState.Crouching:
                    // 숙이기 상태에서는 조용히 이동 (PRD REQ-PLAYER-03)
                    Debug.Log("Player is crouching - silent movement");
                    break;
                case PlayerMovementState.Idle:
                    // 정지 상태에서는 수동 던지기만 가능 (PRD REQ-PLAYER-04)
                    Debug.Log("Player stopped - manual stone throw only");
                    break;
            }
        }
        
        /// <summary>
        /// 이동 변경 처리
        /// </summary>
        private void HandleMovementChanged(bool isMoving)
        {
            Debug.Log($"Player Movement Changed: {isMoving}");
            
            // 애니메이션 파라미터 업데이트
            UpdateAnimationParameters();
        }
        
        /// <summary>
        /// 시점 상태 변경 처리
        /// </summary>
        private void HandleLookStateChanged(PlayerLookState newState)
        {
            Debug.Log($"Player Look State Changed: {newState}");
            
            // PRD에 정의된 뒤돌아보기 상태 처리 (REQ-PLAYER-05)
            switch (newState)
            {
                case PlayerLookState.LookingLeft:
                case PlayerLookState.LookingRight:
                    Debug.Log("Player is looking back - potential ghost encounter");
                    break;
                case PlayerLookState.Normal:
                    Debug.Log("Player returned to normal view");
                    break;
            }
        }
        
        /// <summary>
        /// 뒤돌아보기 변경 처리
        /// </summary>
        private void HandleLookingBackChanged(bool isLookingBack)
        {
            Debug.Log($"Player Looking Back Changed: {isLookingBack}");
        }
        
        /// <summary>
        /// 공깃돌 던지기 처리
        /// </summary>
        private void HandleStoneThrown()
        {
            Debug.Log("Stone thrown by player");
        }
        
        /// <summary>
        /// 자동 던지기 트리거 처리
        /// </summary>
        private void HandleAutoThrowTriggered()
        {
            Debug.Log("Auto stone throw triggered");
        }
        
        /// <summary>
        /// 수동 던지기 트리거 처리
        /// </summary>
        private void HandleManualThrowTriggered()
        {
            Debug.Log("Manual stone throw triggered");
        }
        
        /// <summary>
        /// 플레이어 사망 처리
        /// </summary>
        private void HandlePlayerDeath()
        {
            canMove = false;
            canLook = false;
            canInteract = false;
            
            OnPlayerDied?.Invoke();
            Debug.Log("Player Died - Controls Disabled");
        }
        
        /// <summary>
        /// 게임 상태 변경 처리
        /// </summary>
        private void HandleGameStateChanged(GameManager.GameState newState)
        {
            switch (newState)
            {
                case GameManager.GameState.Playing:
                    canMove = true;
                    canLook = true;
                    canInteract = true;
                    break;
                case GameManager.GameState.Paused:
                case GameManager.GameState.GameOver:
                case GameManager.GameState.Ending:
                    canMove = false;
                    canLook = false;
                    canInteract = false;
                    break;
            }
        }
        
        /// <summary>
        /// 플레이어 제어 활성화/비활성화
        /// </summary>
        public void SetPlayerControl(bool enabled)
        {
            canMove = enabled;
            canLook = enabled;
            canInteract = enabled;
            
            Debug.Log($"Player Control {(enabled ? "Enabled" : "Disabled")}");
        }
        
        /// <summary>
        /// 특정 제어만 활성화/비활성화
        /// </summary>
        public void SetControlState(bool move, bool look, bool interact)
        {
            canMove = move;
            canLook = look;
            canInteract = interact;
            
            Debug.Log($"Player Control - Move: {move}, Look: {look}, Interact: {interact}");
        }
        
        /// <summary>
        /// 회전 업데이트
        /// </summary>
        private void UpdateRotation()
        {
            if (!isRotating) return;
            
            // 부드러운 회전 처리
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            
            // 회전 완료 확인
            float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);
            if (angleDifference <= rotationThreshold)
            {
                // 정확한 회전값으로 설정
                transform.rotation = targetRotation;
                isRotating = false;
                
                // 완료 콜백 호출
                onRotationComplete?.Invoke();
                onRotationComplete = null;
                
                Debug.Log("Player rotation completed");
            }
        }
        
        /// <summary>
        /// 플레이어 회전 시작
        /// </summary>
        public void StartRotation(Quaternion targetRot, System.Action onComplete = null)
        {
            if (isRotating)
            {
                Debug.LogWarning("Player is already rotating, ignoring new rotation request");
                return;
            }
            
            targetRotation = targetRot;
            onRotationComplete = onComplete;
            isRotating = true;
            
            Debug.Log($"Player rotation started - Target: {targetRot.eulerAngles}");
        }
        
        /// <summary>
        /// 플레이어 회전 시작 (각도로)
        /// </summary>
        public void StartRotation(float angle, System.Action onComplete = null)
        {
            Quaternion targetRot = Quaternion.Euler(0, angle, 0);
            StartRotation(targetRot, onComplete);
        }
        
        /// <summary>
        /// 회전 중인지 확인
        /// </summary>
        public bool IsRotating()
        {
            return isRotating;
        }
        
        /// <summary>
        /// 회전 중단
        /// </summary>
        public void StopRotation()
        {
            if (isRotating)
            {
                isRotating = false;
                onRotationComplete = null;
                Debug.Log("Player rotation stopped");
            }
        }
        
        /// <summary>
        /// 애니메이션 파라미터 업데이트
        /// </summary>
        private void UpdateAnimationParameters()
        {
            if (playerAnimator == null || playerMovement == null) return;
            
            // 이동 상태에 따른 애니메이션 파라미터 설정
            var movementState = playerMovement.GetCurrentMovementState();
            bool isMoving = playerMovement.IsMoving();
            
            // isMoving 파라미터 (이동 중일 때 true)
            playerAnimator.SetBool("IsMoving", isMoving);
            
            // isRunning 파라미터 (달리기 상태일 때 true)
            bool isRunning = movementState == PlayerMovementState.Running;
            playerAnimator.SetBool("IsRunning", isRunning);
            
            // isCrouching 파라미터 (숙이기 상태일 때 true)
            bool isCrouching = movementState == PlayerMovementState.Crouching;
            playerAnimator.SetBool("IsCrouching", isCrouching);
            
            Debug.Log($"Animation Parameters - Moving: {isMoving}, Running: {isRunning}, Crouching: {isCrouching}");
        }
        
        /// <summary>
        /// 현재 플레이어 상태 정보 반환
        /// </summary>
        public string GetPlayerStatus()
        {
            string status = "Player Status:\n";
            
            if (playerMovement != null)
            {
                status += $"Movement: {playerMovement.GetCurrentMovementState()}\n";
                status += $"Speed: {playerMovement.GetCurrentSpeed():F1}\n";
                status += $"Moving: {playerMovement.IsMoving()}\n";
            }
            
            if (playerLook != null)
            {
                status += $"Look State: {playerLook.GetCurrentLookState()}\n";
                status += $"Looking Back: {playerLook.IsLookingBack()}\n";
            }
            
            status += $"Can Move: {canMove}\n";
            status += $"Can Look: {canLook}\n";
            status += $"Can Interact: {canInteract}";
            
            return status;
        }
    }
}
