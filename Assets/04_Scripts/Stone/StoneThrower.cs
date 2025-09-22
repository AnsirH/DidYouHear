using UnityEngine;
using System.Collections;
using DidYouHear.Player;
using DidYouHear.Manager.Gameplay;
using DidYouHear.Core;

namespace DidYouHear.Stone
{
    /// <summary>
    /// 공깃돌 던지기 시스템 관리자
    /// </summary>
    public class StoneThrower : MonoBehaviour
    {
        [Header("Stone Settings")]
        public Transform throwPoint;
        public float throwForce = 20f;  // 던지기 강도 증가
        public float throwHeight = 2f;  // 던지기 높이 증가
        public float throwRandomness = 0.2f;  // 랜덤성 감소
        
        [Header("Auto Throw Settings")]
        public bool enableAutoThrow = true;
        public float autoThrowTimer = 0f;
        public float currentThrowInterval = 0f;
        
        [Header("Manual Throw Settings")]
        public bool enableManualThrow = true;
        public float manualThrowCooldown = 0.5f;
        public float manualThrowTimer = 0f;
        
        [Header("Pool Settings")]
        public StonePool stonePool;
        public int poolSize = 10;
        public int maxPoolSize = 20;
        
        // 컴포넌트 참조
        private PlayerController playerController;
        private PlayerMovement playerMovement;
        // 던지기 상태
        private bool canThrow = true;
        private bool isThrowing = false;
        
        // 추격 중 이벤트 관련
        private bool isStoneBlocked = false;
        private bool isChaseMode = false;
        
        // 이벤트
        public System.Action OnStoneThrown;
        public System.Action OnAutoThrowTriggered;
        public System.Action OnManualThrowTriggered;
        public System.Action OnStoneBlocked;
        public System.Action OnStoneUnblocked;
        
        private void Awake()
        {
            // 컴포넌트 초기화
            playerController = GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerMovement = playerController.GetComponent<PlayerMovement>();
            }
            
            // 던지기 지점 설정 (기본값)
            if (throwPoint == null)
            {
                throwPoint = transform;
            }
            
            // 오브젝트 풀 초기화
            InitializeStonePool();
        }
        
        private void Start()
        {
            // 이벤트 구독
            SubscribeToEvents();
            
            // 초기 던지기 간격 설정
            UpdateThrowInterval();
        }
        
        private void Update()
        {
            // 게임이 일시정지 상태가 아닐 때만 처리
            if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Playing)
                return;
                
            HandleAutoThrow();
            HandleManualThrow();
            UpdateTimers();
        }
        
        /// <summary>
        /// 오브젝트 풀 초기화
        /// </summary>
        private void InitializeStonePool()
        {
            // 풀 부모 오브젝트 생성
            if (stonePool == null)
            {
                Debug.LogError("StoneThrower: StonePoolParent is null!");
                return;
            }
            
            // Unity ObjectPool 설정
            stonePool.SetPoolSize(poolSize, maxPoolSize);
        }
        
        /// <summary>
        /// 이벤트 구독
        /// </summary>
        private void SubscribeToEvents()
        {
            // PlayerMovement 이벤트 구독
            if (playerMovement != null)
            {
                playerMovement.OnMovementStateChanged += HandleMovementStateChanged;
            }
            
            // StonePool 이벤트 구독
            if (stonePool != null)
            {
                stonePool.OnStoneSpawned += HandleStoneSpawned;
                stonePool.OnStoneReturned += HandleStoneReturned;
            }
        }
        
        /// <summary>
        /// 자동 던지기 처리
        /// </summary>
        private void HandleAutoThrow()
        {
            if (!enableAutoThrow || !canThrow || isThrowing) return;
            
            // 이동 중일 때만 자동 던지기
            if (playerMovement != null && playerMovement.IsMoving())
            {
                autoThrowTimer += Time.deltaTime;
                
                if (autoThrowTimer >= currentThrowInterval)
                {
                    ThrowStone(true);
                    autoThrowTimer = 0f;
                    UpdateThrowInterval();
                }
            }
            else
            {
                // 정지 시 타이머 리셋
                autoThrowTimer = 0f;
            }
        }
        
        /// <summary>
        /// 수동 던지기 처리
        /// </summary>
        private void HandleManualThrow()
        {
            if (!enableManualThrow || !canThrow || isThrowing) return;
            
            // 수동 던지기 쿨다운 체크
            if (manualThrowTimer > 0f) return;
            
            // 스페이스바 입력 확인
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // Idle 상태에서만 수동 던지기 가능
                if (playerMovement != null && playerMovement.CanThrowStoneManually())
                {
                    ThrowStone(false);
                    manualThrowTimer = manualThrowCooldown;
                }
            }
        }
        
        /// <summary>
        /// 타이머 업데이트
        /// </summary>
        private void UpdateTimers()
        {
            if (manualThrowTimer > 0f)
            {
                manualThrowTimer -= Time.deltaTime;
            }
        }
        
        /// <summary>
        /// 공깃돌 던지기
        /// </summary>
        /// <param name="isAuto">자동 던지기 여부</param>
        public void ThrowStone(bool isAuto)
        {
            if (!canThrow || isThrowing) return;
            
            // 던지기 상태 설정
            isThrowing = true;
            
            // 풀에서 공깃돌 가져오기
            GameObject stone = stonePool.GetStone();
            if (stone == null)
            {
                Debug.LogWarning("StoneThrower: No available stone in pool!");
                isThrowing = false; // 상태 리셋
                return;
            }
            
            // 던지기 위치 설정
            stone.transform.position = throwPoint.position;
            stone.transform.rotation = throwPoint.rotation;
            
            // 던지기 방향 계산
            Vector3 throwDirection = CalculateThrowDirection();
            
            // 던지기 힘 적용 (OnEnable 완료 후 실행)
            Rigidbody stoneRb = stone.GetComponent<Rigidbody>();
            if (stoneRb != null)
            {
                // 한 프레임 지연 후 velocity 설정
                StartCoroutine(ApplyThrowForce(stoneRb, throwDirection));
            }
            
            // 이벤트 발생
            if (isAuto)
            {
                OnAutoThrowTriggered?.Invoke();
                Debug.Log("Auto stone thrown");
            }
            else
            {
                OnManualThrowTriggered?.Invoke();
                Debug.Log("Manual stone thrown");
            }
            
            OnStoneThrown?.Invoke();
        }
        
        /// <summary>
        /// 던지기 방향 계산
        /// </summary>
        private Vector3 CalculateThrowDirection()
        {
            // 기본 던지기 방향 (뒤쪽)
            Vector3 baseDirection = -transform.forward;
            
            // 높이 추가
            baseDirection.y += throwHeight;
            
            // 랜덤성 추가
            Vector3 randomOffset = new Vector3(
                Random.Range(-throwRandomness, throwRandomness),
                Random.Range(-throwRandomness * 0.5f, throwRandomness * 0.5f),
                Random.Range(-throwRandomness, throwRandomness)
            );
            
            return (baseDirection + randomOffset).normalized;
        }
        
        /// <summary>
        /// 이동 상태 변경 처리
        /// </summary>
        private void HandleMovementStateChanged(PlayerMovementState newState)
        {
            UpdateThrowInterval();
            Debug.Log($"StoneThrower: Movement state changed to {newState}");
        }
        
        /// <summary>
        /// 던지기 간격 업데이트
        /// </summary>
        private void UpdateThrowInterval()
        {
            if (playerMovement != null)
            {
                currentThrowInterval = playerMovement.GetStoneThrowInterval();
            }
            else
            {
                currentThrowInterval = 3f; // 기본값
            }
        }
        
        /// <summary>
        /// 던지기 활성화/비활성화
        /// </summary>
        public void SetCanThrow(bool canThrow)
        {
            this.canThrow = canThrow;
            Debug.Log($"StoneThrower: Can throw = {canThrow}");
        }
        
        /// <summary>
        /// 자동 던지기 활성화/비활성화
        /// </summary>
        public void SetAutoThrow(bool enabled)
        {
            enableAutoThrow = enabled;
            if (!enabled)
            {
                autoThrowTimer = 0f;
            }
        }
        
        /// <summary>
        /// 수동 던지기 활성화/비활성화
        /// </summary>
        public void SetManualThrow(bool enabled)
        {
            enableManualThrow = enabled;
        }
        
        /// <summary>
        /// 던지기 설정 업데이트
        /// </summary>
        public void UpdateThrowSettings(float force, float height, float randomness)
        {
            throwForce = force;
            throwHeight = height;
            throwRandomness = randomness;
        }
        
        /// <summary>
        /// 현재 던지기 상태 정보 반환
        /// </summary>
        public string GetThrowerStatus()
        {
            string status = $"StoneThrower Status:\n" +
                          $"Can Throw: {canThrow}\n" +
                          $"Auto Throw: {enableAutoThrow}\n" +
                          $"Manual Throw: {enableManualThrow}\n" +
                          $"Current Interval: {currentThrowInterval:F2}s\n" +
                          $"Auto Timer: {autoThrowTimer:F2}s\n" +
                          $"Manual Cooldown: {manualThrowTimer:F2}s\n" +
                          $"Pool Size: {poolSize} / {maxPoolSize}";
            
            if (stonePool != null)
            {
                status += $"\n\n{stonePool.GetPoolStatistics()}";
            }
            
            return status;
        }
        
        /// <summary>
        /// 공깃돌 스폰 처리
        /// </summary>
        private void HandleStoneSpawned(GameObject stone)
        {
            Debug.Log("Stone spawned from pool");
        }
        
        /// <summary>
        /// 공깃돌 반환 처리
        /// </summary>
        private void HandleStoneReturned(GameObject stone)
        {
            Debug.Log("Stone returned to pool");
        }
        
        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (playerMovement != null)
            {
                playerMovement.OnMovementStateChanged -= HandleMovementStateChanged;
            }
            
            if (stonePool != null)
            {
                stonePool.OnStoneSpawned -= HandleStoneSpawned;
                stonePool.OnStoneReturned -= HandleStoneReturned;
            }
        }
        
        /// <summary>
        /// 던지기 힘 적용 (지연)
        /// </summary>
        private IEnumerator ApplyThrowForce(Rigidbody stoneRb, Vector3 throwDirection)
        {
            // 한 프레임 대기 (OnEnable 완료 후)
            yield return null;
            
            // velocity 설정 전 상태 확인
            Debug.Log($"Before velocity: {stoneRb.velocity}, Direction: {throwDirection}, Force: {throwForce}");
            
            // velocity 설정
            stoneRb.velocity = throwDirection * throwForce;
            
            // velocity 설정 후 상태 확인
            Debug.Log($"After velocity: {stoneRb.velocity}");
            
            // 던지기 완료
            isThrowing = false;
        }
        
        /// <summary>
        /// 추격 모드 설정
        /// </summary>
        public void SetChaseMode(bool isChasing)
        {
            isChaseMode = isChasing;
            
            if (!isChasing)
            {
                // 추격 종료 시 공깃돌 차단 해제
                UnblockStone();
            }
        }
        
        /// <summary>
        /// 공깃돌 소리 차단
        /// </summary>
        public void BlockStone()
        {
            if (isStoneBlocked) return;
            
            isStoneBlocked = true;
            OnStoneBlocked?.Invoke();
            
            Debug.Log("Stone sound blocked!");
        }
        
        /// <summary>
        /// 공깃돌 소리 차단 해제
        /// </summary>
        public void UnblockStone()
        {
            if (!isStoneBlocked) return;
            
            isStoneBlocked = false;
            OnStoneUnblocked?.Invoke();
            
            Debug.Log("Stone sound unblocked!");
        }
        
        /// <summary>
        /// 공깃돌 소리가 차단되었는지 확인
        /// </summary>
        public bool IsStoneBlocked()
        {
            return isStoneBlocked;
        }
        
        /// <summary>
        /// 추격 중인지 확인
        /// </summary>
        public bool IsInChaseMode()
        {
            return isChaseMode;
        }
    }
}
