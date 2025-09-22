using UnityEngine;
using System.Collections;
using DidYouHear.Ghost;
using DidYouHear.Manager.Gameplay;

namespace DidYouHear.Ghost
{
    /// <summary>
    /// 귀신 추격 컴포넌트
    /// </summary>
    public class GhostChase : MonoBehaviour
    {
        [Header("Chase Settings")]
        public float chaseSpeed = 5f;
        public float chaseAcceleration = 2f;
        public float chaseDeceleration = 1f;
        public float chaseDetectionRange = 15f;
        public float chaseStopDistance = 2f;
        
        [Header("Movement Settings")]
        public float rotationSpeed = 5f;
        public float heightOffset = 1.5f;
        public float smoothDamping = 0.1f;
        
        [Header("Audio Settings")]
        public AudioClip chaseSound;
        public float chaseSoundVolume = 0.8f;
        public float chaseSoundPitch = 1f;
        
        [Header("Chase Event Settings")]
        public float stoneBlockDuration = 1.5f; // 공깃돌 소리 차단 시간
        public float lookBackLimit = 3f; // 뒤돌아보기 제한 시간
        public float lookBackWarningTime = 2f; // 경고 시작 시간
        
        private GhostManager ghostManager;
        private Transform playerTransform;
        private AudioSource audioSource;
        private bool isInitialized = false;
        private bool isChasing = false;
        private Vector3 targetPosition;
        private Vector3 velocity;
        
        // 추격 중 이벤트 관련
        private bool isStoneBlocked = false;
        private float stoneBlockTimer = 0f;
        private float lookBackTimer = 0f;
        private bool isLookingBack = false;
        private bool hasLookBackWarning = false;
        
        private void Awake()
        {
            // 오디오 소스 설정
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.spatialBlend = 1f; // 3D 사운드
        }
        
        private void Start()
        {
            if (isInitialized)
            {
                StartChase();
            }
        }
        
        private void Update()
        {
            if (isInitialized && isChasing)
            {
                UpdateChase();
            }
        }
        
        /// <summary>
        /// 귀신 추격 초기화
        /// </summary>
        public void Initialize(GhostManager manager)
        {
            ghostManager = manager;
            playerTransform = Camera.main.transform;
            isInitialized = true;
            
            // 추격 시작
            StartChase();
        }
        
        /// <summary>
        /// 추격 시작
        /// </summary>
        private void StartChase()
        {
            isChasing = true;
            
            // 추격 사운드 재생
            PlayChaseSound();
            
            Debug.Log("Ghost chase started!");
        }
        
        /// <summary>
        /// 추격 업데이트
        /// </summary>
        private void UpdateChase()
        {
            if (playerTransform == null) return;
            
            // 추격 중 이벤트 처리
            UpdateChaseEvents();
            
            // 플레이어 위치 계산
            Vector3 playerPosition = playerTransform.position;
            Vector3 direction = (playerPosition - transform.position).normalized;
            
            // 목표 위치 설정 (플레이어 뒤쪽)
            targetPosition = playerPosition - direction * chaseStopDistance;
            targetPosition.y = playerPosition.y + heightOffset;
            
            // 이동 처리
            MoveTowardsTarget();
            
            // 회전 처리
            RotateTowardsPlayer();
            
            // 거리 체크
            CheckDistance();
        }
        
        /// <summary>
        /// 목표 지점으로 이동
        /// </summary>
        private void MoveTowardsTarget()
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, targetPosition);
            
            // 가속도 적용
            if (distance > chaseStopDistance)
            {
                velocity += direction * chaseAcceleration * Time.deltaTime;
            }
            else
            {
                velocity -= velocity * chaseDeceleration * Time.deltaTime;
            }
            
            // 속도 제한
            velocity = Vector3.ClampMagnitude(velocity, chaseSpeed);
            
            // 이동 적용
            transform.position += velocity * Time.deltaTime;
        }
        
        /// <summary>
        /// 플레이어를 향해 회전
        /// </summary>
        private void RotateTowardsPlayer()
        {
            if (playerTransform == null) return;
            
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            direction.y = 0; // Y축 회전 제거
            
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        
        /// <summary>
        /// 거리 체크
        /// </summary>
        private void CheckDistance()
        {
            if (playerTransform == null) return;
            
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            
            // 플레이어와 너무 가까워지면 게임 오버
            if (distance < chaseStopDistance)
            {
                TriggerGameOver();
            }
        }
        
        /// <summary>
        /// 추격 사운드 재생
        /// </summary>
        private void PlayChaseSound()
        {
            if (audioSource != null && chaseSound != null)
            {
                audioSource.clip = chaseSound;
                audioSource.volume = chaseSoundVolume;
                audioSource.pitch = chaseSoundPitch;
                audioSource.Play();
            }
        }
        
        /// <summary>
        /// 추격 사운드 정지
        /// </summary>
        private void StopChaseSound()
        {
            if (audioSource != null)
            {
                audioSource.Stop();
            }
        }
        
        /// <summary>
        /// 게임 오버 트리거
        /// </summary>
        private void TriggerGameOver()
        {
            if (ghostManager != null)
            {
                ghostManager.TriggerGameOver();
            }
        }
        
        /// <summary>
        /// 추격 중 이벤트 업데이트
        /// </summary>
        private void UpdateChaseEvents()
        {
            // 공깃돌 소리 차단 이벤트 처리
            UpdateStoneBlockEvent();
            
            // 뒤돌아보기 제한 처리
            UpdateLookBackLimit();
        }
        
        /// <summary>
        /// 공깃돌 소리 차단 이벤트 업데이트
        /// </summary>
        private void UpdateStoneBlockEvent()
        {
            if (isStoneBlocked)
            {
                stoneBlockTimer += Time.deltaTime;
                
                // 차단 시간 종료
                if (stoneBlockTimer >= stoneBlockDuration)
                {
                    EndStoneBlock();
                }
            }
            else
            {
                // 랜덤하게 공깃돌 소리 차단 발생
                if (Random.value < 0.1f) // 10% 확률
                {
                    StartStoneBlock();
                }
            }
        }
        
        /// <summary>
        /// 공깃돌 소리 차단 시작
        /// </summary>
        private void StartStoneBlock()
        {
            isStoneBlocked = true;
            stoneBlockTimer = 0f;
            
            // 플레이어에게 공깃돌 소리 차단 알림
            NotifyStoneBlock();
            
            Debug.Log("Stone sound blocked during chase!");
        }
        
        /// <summary>
        /// 공깃돌 소리 차단 종료
        /// </summary>
        private void EndStoneBlock()
        {
            isStoneBlocked = false;
            stoneBlockTimer = 0f;
            
            // 플레이어에게 정상 상태 복구 알림
            NotifyStoneUnblock();
            
            Debug.Log("Stone sound unblocked during chase!");
        }
        
        /// <summary>
        /// 뒤돌아보기 제한 업데이트
        /// </summary>
        private void UpdateLookBackLimit()
        {
            // 플레이어가 뒤를 보고 있는지 확인
            bool isCurrentlyLookingBack = IsPlayerLookingBack();
            
            if (isCurrentlyLookingBack && !isLookingBack)
            {
                // 뒤돌아보기 시작
                StartLookBackTimer();
            }
            else if (!isCurrentlyLookingBack && isLookingBack)
            {
                // 뒤돌아보기 중단
                StopLookBackTimer();
            }
            
            if (isLookingBack)
            {
                lookBackTimer += Time.deltaTime;
                
                // 경고 시간 체크
                if (lookBackTimer >= lookBackWarningTime && !hasLookBackWarning)
                {
                    ShowLookBackWarning();
                }
                
                // 제한 시간 체크
                if (lookBackTimer >= lookBackLimit)
                {
                    TriggerLookBackGameOver();
                }
            }
        }
        
        /// <summary>
        /// 뒤돌아보기 타이머 시작
        /// </summary>
        private void StartLookBackTimer()
        {
            isLookingBack = true;
            lookBackTimer = 0f;
            hasLookBackWarning = false;
            
            Debug.Log("Look back timer started!");
        }
        
        /// <summary>
        /// 뒤돌아보기 타이머 중단
        /// </summary>
        private void StopLookBackTimer()
        {
            isLookingBack = false;
            lookBackTimer = 0f;
            hasLookBackWarning = false;
            
            Debug.Log("Look back timer stopped!");
        }
        
        /// <summary>
        /// 뒤돌아보기 경고 표시
        /// </summary>
        private void ShowLookBackWarning()
        {
            hasLookBackWarning = true;
            
            // 화면 가장자리 빨간색 깜빡임 효과
            StartCoroutine(ShowLookBackWarningEffect());
            
            Debug.Log("Look back warning!");
        }
        
        /// <summary>
        /// 뒤돌아보기 경고 효과 코루틴
        /// </summary>
        private System.Collections.IEnumerator ShowLookBackWarningEffect()
        {
            // 화면 가장자리 빨간색 깜빡임 효과 구현
            // 이는 UI 시스템과 연동되어야 함
            yield return null;
        }
        
        /// <summary>
        /// 뒤돌아보기 제한 시간 초과 시 게임 오버
        /// </summary>
        private void TriggerLookBackGameOver()
        {
            Debug.Log("Look back limit exceeded! Game Over!");
            
            // 게임 오버 처리
            if (ghostManager != null)
            {
                ghostManager.TriggerGameOver();
            }
        }
        
        /// <summary>
        /// 플레이어가 뒤를 보고 있는지 확인
        /// </summary>
        private bool IsPlayerLookingBack()
        {
            if (playerTransform == null) return false;
            
            // 플레이어의 시선 방향과 추격 귀신 방향의 각도 계산
            Vector3 playerForward = playerTransform.forward;
            Vector3 toGhost = (transform.position - playerTransform.position).normalized;
            
            float angle = Vector3.Angle(playerForward, toGhost);
            
            // 90도 이상이면 뒤를 보고 있는 것으로 판단
            return angle > 90f;
        }
        
        /// <summary>
        /// 공깃돌 소리 차단 알림
        /// </summary>
        private void NotifyStoneBlock()
        {
            // 플레이어에게 공깃돌 소리 차단 알림
            // 이는 StoneSystem과 연동되어야 함
            Debug.Log("Notify: Stone sound blocked!");
        }
        
        /// <summary>
        /// 공깃돌 소리 정상화 알림
        /// </summary>
        private void NotifyStoneUnblock()
        {
            // 플레이어에게 공깃돌 소리 정상화 알림
            // 이는 StoneSystem과 연동되어야 함
            Debug.Log("Notify: Stone sound unblocked!");
        }
        
        /// <summary>
        /// 추격 종료
        /// </summary>
        public void EndChase()
        {
            isChasing = false;
            StopChaseSound();
            
            // 추격 중 이벤트 리셋
            ResetChaseEvents();
            
            Debug.Log("Ghost chase ended!");
        }
        
        /// <summary>
        /// 추격 중 이벤트 리셋
        /// </summary>
        private void ResetChaseEvents()
        {
            isStoneBlocked = false;
            stoneBlockTimer = 0f;
            lookBackTimer = 0f;
            isLookingBack = false;
            hasLookBackWarning = false;
        }
        
        /// <summary>
        /// 추격 중인지 반환
        /// </summary>
        public bool IsChasing()
        {
            return isChasing;
        }
        
        /// <summary>
        /// 플레이어와의 거리 반환
        /// </summary>
        public float GetDistanceToPlayer()
        {
            if (playerTransform == null) return float.MaxValue;
            
            return Vector3.Distance(transform.position, playerTransform.position);
        }
    }
}
