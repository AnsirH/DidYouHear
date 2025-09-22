using UnityEngine;
using DidYouHear.Core;

namespace DidYouHear.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Player State")]
        public PlayerStateData stateData = new PlayerStateData();
        
        [Header("Movement Settings")]
        public float acceleration = 10f;
        public float deceleration = 10f;
        
        // 컴포넌트 참조
        private CharacterController controller;
        private Transform playerTransform;
        
        // 이동 관련 변수
        private float currentSpeed;
        private Vector3 lastMoveDirection;
        
        
        // 이벤트
        public System.Action<PlayerMovementState> OnMovementStateChanged;
        public System.Action<bool> OnMovementChanged;
        
        private void Awake()
        {
            // 컴포넌트 초기화
            controller = GetComponent<CharacterController>();
            playerTransform = transform;
        }
        
        private void Start()
        {
            // 초기 상태 설정
            stateData.currentMovementState = PlayerMovementState.Idle;
            currentSpeed = 0f;
        }
        
        private void Update()
        {
            // 게임이 일시정지 상태가 아닐 때만 이동 처리
            if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Playing)
                return;
                
            HandleMovement();
        }
        
        
        /// <summary>
        /// 이동 처리 (앞으로만 이동 가능)
        /// </summary>
        private void HandleMovement()
        {
            // W키 입력만 받기 (앞으로만 이동)
            float forwardInput = Input.GetAxis("Vertical");
            
            // 앞으로만 이동 (뒤로 이동 제한)
            Vector3 moveDirection = Vector3.zero;
            if (forwardInput > 0.1f) // W키만 허용
            {
                moveDirection = transform.forward;
            }
            
            // 이동 상태 결정
            DetermineMovementState(moveDirection);
            
            // 속도 계산
            CalculateSpeed(moveDirection);
            
            // 실제 이동 적용
            if (moveDirection.magnitude > 0.1f)
            {
                lastMoveDirection = moveDirection;
                controller.Move(moveDirection * currentSpeed * Time.deltaTime);
                stateData.isMoving = true;
            }
            else
            {
                stateData.isMoving = false;
            }
            
            // 이동 상태 변경 이벤트 발생
            OnMovementChanged?.Invoke(stateData.isMoving);
        }
        
        /// <summary>
        /// 이동 상태 결정
        /// </summary>
        private void DetermineMovementState(Vector3 moveDirection)
        {
            PlayerMovementState newState = PlayerMovementState.Idle;
            
            if (moveDirection.magnitude > 0.1f)
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    newState = PlayerMovementState.Running;
                }
                else if (Input.GetKey(KeyCode.LeftControl))
                {
                    newState = PlayerMovementState.Crouching;
                }
                else
                {
                    newState = PlayerMovementState.Walking;
                }
            }
            else
            {
                newState = PlayerMovementState.Idle;
            }
            
            // 상태 변경 시 이벤트 발생
            if (newState != stateData.currentMovementState)
            {
                stateData.currentMovementState = newState;
                OnMovementStateChanged?.Invoke(newState);
                Debug.Log($"Movement State Changed: {newState}");
            }
        }
        
        /// <summary>
        /// 속도 계산
        /// </summary>
        private void CalculateSpeed(Vector3 moveDirection)
        {
            float targetSpeed = 0f;
            
            // 상태에 따른 목표 속도 설정
            switch (stateData.currentMovementState)
            {
                case PlayerMovementState.Walking:
                    targetSpeed = stateData.walkSpeed;
                    break;
                case PlayerMovementState.Running:
                    targetSpeed = stateData.runSpeed;
                    break;
                case PlayerMovementState.Crouching:
                    targetSpeed = stateData.crouchSpeed;
                    break;
                case PlayerMovementState.Idle:
                    targetSpeed = 0f;
                    break;
            }
            
            // 부드러운 속도 전환
            if (moveDirection.magnitude > 0.1f)
            {
                currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
            }
            else
            {
                currentSpeed = Mathf.Lerp(currentSpeed, 0f, deceleration * Time.deltaTime);
            }
        }
        
        
        /// <summary>
        /// 현재 이동 상태 반환
        /// </summary>
        public PlayerMovementState GetCurrentMovementState()
        {
            return stateData.currentMovementState;
        }
        
        /// <summary>
        /// 현재 속도 반환
        /// </summary>
        public float GetCurrentSpeed()
        {
            return currentSpeed;
        }
        
        /// <summary>
        /// 이동 중인지 확인
        /// </summary>
        public bool IsMoving()
        {
            return stateData.isMoving;
        }
        
        /// <summary>
        /// 특정 상태인지 확인
        /// </summary>
        public bool IsInState(PlayerMovementState state)
        {
            return stateData.currentMovementState == state;
        }
        
        /// <summary>
        /// 공깃돌 던지기 간격 반환 (랜덤)
        /// </summary>
        public float GetStoneThrowInterval()
        {
            switch (stateData.currentMovementState)
            {
                case PlayerMovementState.Walking:
                    return Random.Range(stateData.walkStoneIntervalMin, stateData.walkStoneIntervalMax);
                case PlayerMovementState.Running:
                    return Random.Range(stateData.runStoneIntervalMin, stateData.runStoneIntervalMax);
                case PlayerMovementState.Crouching:
                    return Random.Range(stateData.crouchStoneIntervalMin, stateData.crouchStoneIntervalMax);
                case PlayerMovementState.Idle:
                    return 0f; // 정지 상태에서는 자동 던지지 않음
                default:
                    return 0f;
            }
        }
        
        /// <summary>
        /// 수동 공깃돌 던지기 가능 여부 확인
        /// </summary>
        public bool CanThrowStoneManually()
        {
            // Idle 상태에서만 수동 던지기 가능 (이동하지 않을 때)
            return stateData.currentMovementState == PlayerMovementState.Idle;
        }
        
        /// <summary>
        /// 플레이어가 현재 이동 중인지 확인
        /// </summary>
        public bool IsPlayerMoving()
        {
            return stateData.isMoving;
        }
        
        /// <summary>
        /// 현재 이동 상태 반환 (프로퍼티)
        /// </summary>
        public PlayerMovementState CurrentMovementState
        {
            get { return stateData.currentMovementState; }
        }
        
        /// <summary>
        /// 숙이고 있는지 확인
        /// </summary>
        public bool IsCrouching()
        {
            return stateData.currentMovementState == PlayerMovementState.Crouching;
        }
        
        /// <summary>
        /// 달리고 있는지 확인
        /// </summary>
        public bool IsRunning()
        {
            return stateData.currentMovementState == PlayerMovementState.Running;
        }
    }
}
