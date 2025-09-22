using UnityEngine;
using DidYouHear.Player;
using DidYouHear.Manager.Core;
using DidYouHear.Manager.Gameplay;

namespace DidYouHear.Events
{
    /// <summary>
    /// 어깨 두드림 이벤트
    /// </summary>
    public class ShoulderTapEvent : GameEvent
    {
        [Header("Shoulder Tap Settings")]
        public float reactionTimeLimit = 2f;
        public float tapSoundVolume = 0.8f;
        public float reliefSoundVolume = 0.6f;
        
        // 어깨 두드림 방향
        public enum ShoulderSide
        {
            Left,
            Right
        }
        
        private ShoulderSide tappedSide;
        private bool isReacted = false;
        private float eventStartTime;
        
        // 사운드 클립
        private AudioClip tapSound;
        private AudioClip reliefSound;
        
        public override void Execute()
        {
            if (!isInitialized) return;
            
            eventStartTime = Time.time;
            isReacted = false;
            
            // 랜덤하게 어깨 방향 결정
            tappedSide = Random.value < 0.5f ? ShoulderSide.Left : ShoulderSide.Right;
            
            // 어깨 두드림 사운드 재생
            PlayTapSound();
            
            // UI 표시 (향후 구현)
            ShowTapUI();
            
            // 반응 타이머 시작
            eventManager.StartReactionTimer();
            
            Debug.Log($"Shoulder tapped on {tappedSide} side!");
        }
        
        /// <summary>
        /// 반응 체크
        /// </summary>
        public void CheckReaction(PlayerLookState lookState)
        {
            if (isReacted || isCompleted) return;
            
            // 올바른 반응인지 체크
            bool correctReaction = IsCorrectReaction(lookState);
            
            if (correctReaction)
            {
                // 올바른 반응
                OnCorrectReaction();
            }
            else if (IsWrongReaction(lookState))
            {
                // 잘못된 반응
                OnWrongReaction();
            }
        }
        
        /// <summary>
        /// 올바른 반응인지 체크
        /// </summary>
        private bool IsCorrectReaction(PlayerLookState lookState)
        {
            // 왼쪽 어깨 두드림 → 오른쪽으로 뒤돌아보기
            if (tappedSide == ShoulderSide.Left && lookState == PlayerLookState.LookingRight)
            {
                return true;
            }
            
            // 오른쪽 어깨 두드림 → 왼쪽으로 뒤돌아보기
            if (tappedSide == ShoulderSide.Right && lookState == PlayerLookState.LookingLeft)
            {
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 잘못된 반응인지 체크
        /// </summary>
        private bool IsWrongReaction(PlayerLookState lookState)
        {
            // 같은 방향을 보거나, 반대 방향이 아닌 다른 방향을 보는 경우
            if (tappedSide == ShoulderSide.Left && lookState == PlayerLookState.LookingLeft)
            {
                return true;
            }
            
            if (tappedSide == ShoulderSide.Right && lookState == PlayerLookState.LookingRight)
            {
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 올바른 반응 처리
        /// </summary>
        private void OnCorrectReaction()
        {
            isReacted = true;
            
            // 안도감 사운드 재생
            PlayReliefSound();
            
            // UI 숨기기
            HideTapUI();
            
            // 이벤트 완료
            Complete(true);
            
            Debug.Log("Correct reaction! Event completed successfully.");
        }
        
        /// <summary>
        /// 잘못된 반응 처리
        /// </summary>
        private void OnWrongReaction()
        {
            isReacted = true;
            
            // UI 숨기기
            HideTapUI();
            
            // 귀신 등장 이벤트 발생
            eventManager.TriggerGhostAppearance();
            
            // 이벤트 실패
            Fail();
            
            Debug.Log("Wrong reaction! Ghost will appear.");
        }
        
        /// <summary>
        /// 어깨 두드림 사운드 재생
        /// </summary>
        private void PlayTapSound()
        {
            if (AudioManager.Instance != null)
            {
                // 3D 방향성 사운드 재생
                Vector3 soundPosition = GetTapSoundPosition();
                AudioManager.Instance.Play3DSound(tapSound, soundPosition, tapSoundVolume);
            }
        }
        
        /// <summary>
        /// 안도감 사운드 재생
        /// </summary>
        private void PlayReliefSound()
        {
            if (AudioManager.Instance != null)
            {
                // 플레이어 위치에서 사운드 재생
                Vector3 playerPosition = Camera.main.transform.position;
                AudioManager.Instance.Play3DSound(reliefSound, playerPosition, reliefSoundVolume);
            }
        }
        
        /// <summary>
        /// 어깨 두드림 사운드 위치 계산
        /// </summary>
        private Vector3 GetTapSoundPosition()
        {
            if (Camera.main == null) return Vector3.zero;
            
            Vector3 playerPosition = Camera.main.transform.position;
            Vector3 playerForward = Camera.main.transform.forward;
            Vector3 playerRight = Camera.main.transform.right;
            
            // 어깨 위치 계산 (플레이어 뒤쪽, 어깨 높이)
            Vector3 shoulderOffset = -playerForward * 0.5f + Vector3.up * 1.2f;
            
            if (tappedSide == ShoulderSide.Left)
            {
                shoulderOffset += -playerRight * 0.3f;
            }
            else
            {
                shoulderOffset += playerRight * 0.3f;
            }
            
            return playerPosition + shoulderOffset;
        }
        
        /// <summary>
        /// 어깨 두드림 UI 표시
        /// </summary>
        private void ShowTapUI()
        {
            // 향후 UI 시스템과 연동
            Debug.Log($"Show tap UI: {tappedSide} side");
        }
        
        /// <summary>
        /// 어깨 두드림 UI 숨기기
        /// </summary>
        private void HideTapUI()
        {
            // 향후 UI 시스템과 연동
            Debug.Log("Hide tap UI");
        }
        
        public override EventManager.EventType GetEventType()
        {
            return EventManager.EventType.ShoulderTap;
        }
        
        /// <summary>
        /// 두드린 어깨 방향 반환
        /// </summary>
        public ShoulderSide GetTappedSide()
        {
            return tappedSide;
        }
        
        /// <summary>
        /// 반응 여부 반환
        /// </summary>
        public bool IsReacted()
        {
            return isReacted;
        }
        
        /// <summary>
        /// 남은 반응 시간 반환
        /// </summary>
        public float GetRemainingReactionTime()
        {
            if (isReacted || isCompleted) return 0f;
            
            return Mathf.Max(0f, reactionTimeLimit - (Time.time - eventStartTime));
        }
    }
}
