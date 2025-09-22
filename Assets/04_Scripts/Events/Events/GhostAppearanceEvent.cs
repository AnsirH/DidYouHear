using UnityEngine;
using DidYouHear.Core;
using DidYouHear.Manager.Core;
using DidYouHear.Manager.Gameplay;

namespace DidYouHear.Events
{
    /// <summary>
    /// 귀신 등장 이벤트
    /// </summary>
    public class GhostAppearanceEvent : GameEvent
    {
        [Header("Ghost Appearance Settings")]
        public float appearanceDuration = 2f;
        public float cameraForceDuration = 1f;
        public float screenFlashDuration = 0.5f;
        
        // 귀신 등장 타입
        public enum GhostAppearanceType
        {
            Immediate,      // 즉시 게임 오버
            Chase          // 추격 이벤트
        }
        
        private GhostAppearanceType appearanceType;
        private bool isAppearing = false;
        private float eventStartTime;
        
        // 사운드 클립
        private AudioClip ghostSound;
        private AudioClip screamSound;
        
        // 시각적 효과
        private Camera playerCamera;
        private float originalFOV;
        private Color originalBackgroundColor;
        
        public override void Execute()
        {
            if (!isInitialized) return;
            
            eventStartTime = Time.time;
            isAppearing = true;
            
            // 귀신 등장 타입 결정
            appearanceType = DetermineAppearanceType();
            
            // 카메라 강제 뒤돌림
            ForceCameraLookBack();
            
            // 귀신 사운드 재생
            PlayGhostSound();
            
            // 시각적 효과 적용
            ApplyVisualEffects();
            
            // UI 표시
            ShowGhostUI();
            
            Debug.Log($"Ghost appearance event triggered: {appearanceType}");
        }
        
        /// <summary>
        /// 이벤트 업데이트 (매 프레임 호출)
        /// </summary>
        public override void UpdateEvent()
        {
            if (isCompleted) return;
            
            // 등장 지속 시간 체크
            if (Time.time - eventStartTime >= appearanceDuration)
            {
                OnAppearanceComplete();
            }
        }
        
        /// <summary>
        /// 귀신 등장 타입 결정
        /// </summary>
        private GhostAppearanceType DetermineAppearanceType()
        {
            // 현재 게임 상태나 이벤트 상황에 따라 결정
            // 기본적으로는 즉시 게임 오버
            return GhostAppearanceType.Immediate;
        }
        
        /// <summary>
        /// 카메라 강제 뒤돌림
        /// </summary>
        private void ForceCameraLookBack()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
                if (playerCamera == null) return;
            }
            
            // EventManager에서 코루틴 처리 요청
            if (eventManager != null)
            {
                eventManager.StartEventCoroutine(ForceCameraRotation());
            }
        }
        
        /// <summary>
        /// 카메라 강제 회전 코루틴
        /// </summary>
        private System.Collections.IEnumerator ForceCameraRotation()
        {
            float startTime = Time.time;
            Quaternion startRotation = playerCamera.transform.rotation;
            Quaternion targetRotation = Quaternion.Euler(0, 180, 0); // 뒤쪽으로 180도 회전
            
            while (Time.time - startTime < cameraForceDuration)
            {
                float progress = (Time.time - startTime) / cameraForceDuration;
                playerCamera.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, progress);
                yield return null;
            }
            
            // 최종 회전 설정
            playerCamera.transform.rotation = targetRotation;
        }
        
        /// <summary>
        /// 귀신 사운드 재생
        /// </summary>
        private void PlayGhostSound()
        {
            if (AudioManager.Instance != null)
            {
                // 플레이어 위치에서 귀신 사운드 재생
                Vector3 playerPosition = Camera.main.transform.position;
                AudioManager.Instance.Play3DSound(ghostSound, playerPosition, 1.0f);
                
                // 비명 사운드도 재생
                AudioManager.Instance.Play3DSound(screamSound, playerPosition, 0.8f);
            }
        }
        
        /// <summary>
        /// 시각적 효과 적용
        /// </summary>
        private void ApplyVisualEffects()
        {
            if (playerCamera == null) return;
            
            // 원본 설정 저장
            originalFOV = playerCamera.fieldOfView;
            originalBackgroundColor = playerCamera.backgroundColor;
            
            // 화면 점멸 효과
            if (eventManager != null)
            {
                eventManager.StartEventCoroutine(ScreenFlashEffect());
            }
            
            // FOV 왜곡 효과
            if (eventManager != null)
            {
                eventManager.StartEventCoroutine(FOVDistortionEffect());
            }
        }
        
        /// <summary>
        /// 화면 점멸 효과 코루틴
        /// </summary>
        private System.Collections.IEnumerator ScreenFlashEffect()
        {
            float startTime = Time.time;
            
            while (Time.time - startTime < screenFlashDuration)
            {
                // 화면을 흰색으로 점멸
                playerCamera.backgroundColor = Color.white;
                yield return new WaitForSeconds(0.1f);
                
                playerCamera.backgroundColor = Color.black;
                yield return new WaitForSeconds(0.1f);
            }
            
            // 원본 색상 복원
            playerCamera.backgroundColor = originalBackgroundColor;
        }
        
        /// <summary>
        /// FOV 왜곡 효과 코루틴
        /// </summary>
        private System.Collections.IEnumerator FOVDistortionEffect()
        {
            float startTime = Time.time;
            float duration = appearanceDuration;
            
            while (Time.time - startTime < duration)
            {
                // FOV를 랜덤하게 변화시켜 왜곡 효과
                float distortion = Mathf.Sin((Time.time - startTime) * 10f) * 10f;
                playerCamera.fieldOfView = originalFOV + distortion;
                yield return null;
            }
            
            // 원본 FOV 복원
            playerCamera.fieldOfView = originalFOV;
        }
        
        /// <summary>
        /// 귀신 등장 완료 처리
        /// </summary>
        private void OnAppearanceComplete()
        {
            isAppearing = false;
            
            // UI 숨기기
            HideGhostUI();
            
            // 시각적 효과 제거
            RemoveVisualEffects();
            
            // 게임 오버 또는 추격 이벤트 처리
            if (appearanceType == GhostAppearanceType.Immediate)
            {
                TriggerGameOver();
            }
            else
            {
                TriggerChaseEvent();
            }
            
            // 이벤트 완료
            Complete(false); // 실패로 처리
            
            Debug.Log("Ghost appearance completed!");
        }
        
        /// <summary>
        /// 게임 오버 처리
        /// </summary>
        private void TriggerGameOver()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameState(GameManager.GameState.GameOver);
            }
            
            Debug.Log("Game Over triggered!");
        }
        
        /// <summary>
        /// 추격 이벤트 처리
        /// </summary>
        private void TriggerChaseEvent()
        {
            // 향후 추격 이벤트 시스템과 연동
            Debug.Log("Chase event triggered!");
        }
        
        /// <summary>
        /// 시각적 효과 제거
        /// </summary>
        private void RemoveVisualEffects()
        {
            if (playerCamera == null) return;
            
            // 원본 설정 복원
            playerCamera.fieldOfView = originalFOV;
            playerCamera.backgroundColor = originalBackgroundColor;
        }
        
        /// <summary>
        /// 귀신 UI 표시
        /// </summary>
        private void ShowGhostUI()
        {
            // 향후 UI 시스템과 연동
            Debug.Log("Show ghost UI");
        }
        
        /// <summary>
        /// 귀신 UI 숨기기
        /// </summary>
        private void HideGhostUI()
        {
            // 향후 UI 시스템과 연동
            Debug.Log("Hide ghost UI");
        }
        
        public override EventManager.EventType GetEventType()
        {
            return EventManager.EventType.GhostAppearance;
        }
        
        /// <summary>
        /// 귀신 등장 타입 반환
        /// </summary>
        public GhostAppearanceType GetAppearanceType()
        {
            return appearanceType;
        }
        
        /// <summary>
        /// 등장 중인지 반환
        /// </summary>
        public bool IsAppearing()
        {
            return isAppearing;
        }
        
        /// <summary>
        /// 남은 등장 시간 반환
        /// </summary>
        public float GetRemainingAppearanceTime()
        {
            if (isCompleted) return 0f;
            
            return Mathf.Max(0f, appearanceDuration - (Time.time - eventStartTime));
        }
    }
}
