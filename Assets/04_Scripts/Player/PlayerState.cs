using UnityEngine;

namespace DidYouHear.Player
{
    /// <summary>
    /// 플레이어 이동 상태 enum (이동 방식)
    /// </summary>
    public enum PlayerMovementState
    {
        Idle,       // 정지 상태 (W키를 누르지 않음)
        Walking,    // 걷기 (W키 + 1.5m/s)
        Running,    // 달리기 (W키 + Left Shift + 3.0m/s)
        Crouching   // 숙이기 (W키 + Ctrl + 0.8m/s)
    }
    
    
    /// <summary>
    /// 플레이어 시점 상태 enum
    /// </summary>
    public enum PlayerLookState
    {
        Normal,     // 정상 시점
        LookingLeft,    // 왼쪽 뒤돌아보기
        LookingRight    // 오른쪽 뒤돌아보기
    }
    
    /// <summary>
    /// 플레이어 상태 데이터 클래스
    /// </summary>
    [System.Serializable]
    public class PlayerStateData
    {
        [Header("Movement Settings")]
        public float walkSpeed = 1.5f;
        public float runSpeed = 3.0f;
        public float crouchSpeed = 0.8f;
        
        [Header("Stone Throwing Settings")]
        public float walkStoneIntervalMin = 3f;   // 걷기 시 공깃돌 던지기 최소 간격
        public float walkStoneIntervalMax = 5f;   // 걷기 시 공깃돌 던지기 최대 간격
        public float runStoneIntervalMin = 1.5f;  // 달리기 시 공깃돌 던지기 최소 간격
        public float runStoneIntervalMax = 2.5f;  // 달리기 시 공깃돌 던지기 최대 간격
        public float crouchStoneIntervalMin = 3f; // 숙이기 시 공깃돌 던지기 최소 간격
        public float crouchStoneIntervalMax = 5f; // 숙이기 시 공깃돌 던지기 최대 간격
        public float manualStoneDelay = 0.5f;     // 수동 던지기 딜레이
        
        [Header("Look Settings")]
        public float lookSpeed = 2f;              // 고개 돌리기 속도
        public float maxLookAngle = 90f;          // 최대 고개 돌리기 각도
        
        [Header("Current State")]
        public PlayerMovementState currentMovementState = PlayerMovementState.Idle;
        public PlayerLookState currentLookState = PlayerLookState.Normal;
        public bool isMoving = false;
        public bool isLookingBack = false;
        
        [Header("Input States")]
        public bool isWalkPressed = false;
        public bool isRunPressed = false;
        public bool isCrouchPressed = false;
        public bool isManualStonePressed = false;
        public bool isLookLeftPressed = false;
        public bool isLookRightPressed = false;
    }
}
