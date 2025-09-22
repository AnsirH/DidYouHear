using UnityEngine;
using DidYouHear.Player;
using DidYouHear.Manager.Core;
using DidYouHear.Manager.Gameplay;

namespace DidYouHear.Events
{
    /// <summary>
    /// 환경 이벤트
    /// </summary>
    public class EnvironmentalEvent : GameEvent
    {
        [Header("Environmental Event Settings")]
        public float reactionTimeLimit = 2f;
        public float eventDuration = 5f;
        public float crouchRequiredDuration = 3f;
        
        // 환경 이벤트 타입
        public enum EnvironmentalEventType
        {
            ClassroomLight,     // 교실 불 켜짐
            BathroomSound,      // 화장실 물 소리
            DoorOpen,           // 문이 갑자기 열림
            WindowOpen          // 창문이 갑자기 열림
        }
        
        private EnvironmentalEventType eventType;
        private bool isReacted = false;
        private bool isCrouching = false;
        private float eventStartTime;
        private float crouchStartTime;
        private float totalCrouchTime = 0f;
        
        // 사운드 클립
        private AudioClip eventSound;
        private AudioClip chaseSound;
        
        public override void Execute()
        {
            if (!isInitialized) return;
            
            eventStartTime = Time.time;
            isReacted = false;
            isCrouching = false;
            totalCrouchTime = 0f;
            
            // 랜덤하게 이벤트 타입 결정
            eventType = (EnvironmentalEventType)Random.Range(0, System.Enum.GetValues(typeof(EnvironmentalEventType)).Length);
            
            // 이벤트 사운드 재생
            PlayEventSound();
            
            // 시각적 효과 표시
            ShowVisualEffect();
            
            // UI 표시
            ShowEventUI();
            
            // 반응 타이머 시작
            eventManager.StartReactionTimer();
            
            Debug.Log($"Environmental event triggered: {eventType}");
        }
        
        /// <summary>
        /// 이벤트 업데이트 (매 프레임 호출)
        /// </summary>
        public override void UpdateEvent()
        {
            if (isCompleted) return;
            
            // 이벤트 지속 시간 체크
            if (Time.time - eventStartTime >= eventDuration)
            {
                OnEventTimeout();
                return;
            }
            
            // 플레이어가 숙이고 있는지 체크
            CheckCrouchingStatus();
            
            // 숙이기 요구 시간 체크
            if (totalCrouchTime >= crouchRequiredDuration)
            {
                OnEventSuccess();
            }
        }
        
        /// <summary>
        /// 숙이기 상태 체크
        /// </summary>
        private void CheckCrouchingStatus()
        {
            if (eventManager == null) return;
            
            // PlayerMovement에서 숙이기 상태 확인            
            PlayerMovement playerMovement = eventManager.playerMovement;
            if (playerMovement != null)
            {
                bool currentlyCrouching = playerMovement.IsCrouching();
                
                if (currentlyCrouching && !isCrouching)
                {
                    // 숙이기 시작
                    isCrouching = true;
                    crouchStartTime = Time.time;
                    Debug.Log("Player started crouching");
                }
                else if (!currentlyCrouching && isCrouching)
                {
                    // 숙이기 종료
                    isCrouching = false;
                    totalCrouchTime += Time.time - crouchStartTime;
                    Debug.Log($"Player stopped crouching. Total crouch time: {totalCrouchTime:F1}s");
                }
                else if (currentlyCrouching && isCrouching)
                {
                    // 숙이기 중 - 시간 누적
                    totalCrouchTime += Time.deltaTime;
                }
            }
        }
        
        /// <summary>
        /// 이벤트 성공 처리
        /// </summary>
        private void OnEventSuccess()
        {
            isReacted = true;
            
            // UI 숨기기
            HideEventUI();
            
            // 시각적 효과 제거
            HideVisualEffect();
            
            // 이벤트 완료
            Complete(true);
            
            Debug.Log("Environmental event completed successfully!");
        }
        
        /// <summary>
        /// 이벤트 시간 초과 처리
        /// </summary>
        private void OnEventTimeout()
        {
            if (isReacted) return;
            
            isReacted = true;
            
            // UI 숨기기
            HideEventUI();
            
            // 시각적 효과 제거
            HideVisualEffect();
            
            // 추격 이벤트 발생
            TriggerChaseEvent();
            
            // 이벤트 실패
            Fail();
            
            Debug.Log("Environmental event timeout! Chase event triggered.");
        }
        
        /// <summary>
        /// 추격 이벤트 발생
        /// </summary>
        private void TriggerChaseEvent()
        {
            // 추격 사운드 재생
            PlayChaseSound();
            
            // 귀신 등장 이벤트 발생
            eventManager.TriggerGhostAppearance();
            
            Debug.Log("Chase event triggered!");
        }
        
        /// <summary>
        /// 이벤트 사운드 재생
        /// </summary>
        private void PlayEventSound()
        {
            if (AudioManager.Instance != null)
            {
                // 이벤트 타입에 따른 사운드 재생
                Vector3 soundPosition = GetEventSoundPosition();
                AudioManager.Instance.Play3DSound(eventSound, soundPosition, 0.8f);
            }
        }
        
        /// <summary>
        /// 추격 사운드 재생
        /// </summary>
        private void PlayChaseSound()
        {
            if (AudioManager.Instance != null)
            {
                // 플레이어 위치에서 사운드 재생
                Vector3 playerPosition = Camera.main.transform.position;
                AudioManager.Instance.Play3DSound(chaseSound, playerPosition, 1.0f);
            }
        }
        
        /// <summary>
        /// 이벤트 사운드 위치 계산
        /// </summary>
        private Vector3 GetEventSoundPosition()
        {
            if (Camera.main == null) return Vector3.zero;
            
            Vector3 playerPosition = Camera.main.transform.position;
            Vector3 playerForward = Camera.main.transform.forward;
            
            // 이벤트 타입에 따른 위치 설정
            Vector3 eventPosition = playerPosition;
            
            switch (eventType)
            {
                case EnvironmentalEventType.ClassroomLight:
                    // 교실 위치 (플레이어 앞쪽)
                    eventPosition += playerForward * 5f + Vector3.up * 2f;
                    break;
                    
                case EnvironmentalEventType.BathroomSound:
                    // 화장실 위치 (플레이어 옆쪽)
                    eventPosition += playerForward * 3f + Vector3.right * 2f;
                    break;
                    
                case EnvironmentalEventType.DoorOpen:
                    // 문 위치 (플레이어 앞쪽)
                    eventPosition += playerForward * 3f;
                    break;
                    
                case EnvironmentalEventType.WindowOpen:
                    // 창문 위치 (플레이어 옆쪽)
                    eventPosition += playerForward * 2f + Vector3.right * 3f + Vector3.up * 1.5f;
                    break;
            }
            
            return eventPosition;
        }
        
        /// <summary>
        /// 시각적 효과 표시
        /// </summary>
        private void ShowVisualEffect()
        {
            // 향후 시각적 효과 시스템과 연동
            Debug.Log($"Show visual effect for {eventType}");
        }
        
        /// <summary>
        /// 시각적 효과 제거
        /// </summary>
        private void HideVisualEffect()
        {
            // 향후 시각적 효과 시스템과 연동
            Debug.Log($"Hide visual effect for {eventType}");
        }
        
        /// <summary>
        /// 이벤트 UI 표시
        /// </summary>
        private void ShowEventUI()
        {
            // 향후 UI 시스템과 연동
            Debug.Log($"Show event UI: {eventType}");
        }
        
        /// <summary>
        /// 이벤트 UI 숨기기
        /// </summary>
        private void HideEventUI()
        {
            // 향후 UI 시스템과 연동
            Debug.Log($"Hide event UI: {eventType}");
        }
        
        public override EventManager.EventType GetEventType()
        {
            return EventManager.EventType.Environmental;
        }
        
        /// <summary>
        /// 이벤트 타입 반환
        /// </summary>
        public EnvironmentalEventType GetEnvironmentalEventType()
        {
            return eventType;
        }
        
        /// <summary>
        /// 반응 여부 반환
        /// </summary>
        public bool IsReacted()
        {
            return isReacted;
        }
        
        /// <summary>
        /// 숙이기 여부 반환
        /// </summary>
        public bool IsCrouching()
        {
            return isCrouching;
        }
        
        /// <summary>
        /// 총 숙이기 시간 반환
        /// </summary>
        public float GetTotalCrouchTime()
        {
            return totalCrouchTime;
        }
        
        /// <summary>
        /// 남은 반응 시간 반환
        /// </summary>
        public float GetRemainingReactionTime()
        {
            if (isReacted || isCompleted) return 0f;
            
            return Mathf.Max(0f, reactionTimeLimit - (Time.time - eventStartTime));
        }
        
        /// <summary>
        /// 남은 이벤트 시간 반환
        /// </summary>
        public float GetRemainingEventTime()
        {
            if (isCompleted) return 0f;
            
            return Mathf.Max(0f, eventDuration - (Time.time - eventStartTime));
        }
        
        /// <summary>
        /// 이벤트 타입 설정
        /// </summary>
        public void SetEventType(EnvironmentalEventType eventType)
        {
            this.eventType = eventType;
        }
    }
}
