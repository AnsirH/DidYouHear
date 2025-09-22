using UnityEngine;
using DidYouHear.Manager.Core;
using DidYouHear.Core;

namespace DidYouHear.Player
{
    public class PlayerLook : MonoBehaviour
    {
        [Header("Look Settings")]
        public float mouseSensitivity = 100f;
        public float lookSpeed = 2f;
        public float maxLookAngle = 90f;
        
        
        [Header("Look States")]
        public PlayerLookState currentLookState = PlayerLookState.Normal;
        public bool isLookingBack = false;
        
        // 컴포넌트 참조
        private Transform playerTransform;
        private CameraController cameraController;
        private Camera playerCamera;

        // 뒤돌아보기 관련
        private float targetLookAngle = 0f;
        private float currentLookAngle = 0f;
        
        
        // 이벤트
        public System.Action<PlayerLookState> OnLookStateChanged;
        public System.Action<bool> OnLookingBackChanged;
        
        private void Awake()
        {
            playerTransform = transform;
            cameraController = GetComponentInChildren<CameraController>();
            if (cameraController == null)
            {
                Debug.LogError("PlayerLook: CameraController component not found!");
            }
            playerCamera = cameraController.playerCamera;
            
            if (playerCamera == null)
            {
                Debug.LogError("PlayerLook: Camera component not found!");
            }
        }
        
        private void Start()
        {
            // 마우스 커서 잠금
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            // 초기 상태 설정
            currentLookState = PlayerLookState.Normal;
        }
        
        private void Update()
        {
            // 게임이 일시정지 상태가 아닐 때만 시점 처리
            if (DidYouHear.Core.GameManager.Instance != null && 
                DidYouHear.Core.GameManager.Instance.currentState != DidYouHear.Core.GameManager.GameState.Playing)
                return;
                
            HandleMouseInput();
            HandleLookBack();
        }
        
        /// <summary>
        /// 마우스 입력 처리
        /// </summary>
        private void HandleMouseInput()
        {
        }
        
        /// <summary>
        /// 뒤돌아보기 처리
        /// </summary>
        private void HandleLookBack()
        {
            // Q키로 왼쪽 뒤돌아보기
            if (Input.GetKey(KeyCode.Q))
            {
                StartLookBack(PlayerLookState.LookingLeft);
            }
            // E키로 오른쪽 뒤돌아보기
            else if (Input.GetKey(KeyCode.E))
            {
                StartLookBack(PlayerLookState.LookingRight);
            }
            // 아무 키도 누르지 않으면 즉시 정상 상태로 설정
            else
            {
                ReturnToNormalLook();
            }
            
            // 뒤돌아보기 각도 적용
            ApplyLookBackAngle();
        }
        
        /// <summary>
        /// 뒤돌아보기 시작
        /// </summary>
        private void StartLookBack(PlayerLookState lookDirection)
        {
            // 이미 같은 방향을 보고 있으면 무시
            if (currentLookState == lookDirection) return;
            
            // 현재 뒤를 보고 있는 상태에서 다른 방향으로 전환하려면
            // 먼저 정면으로 돌아가야 함
            if (isLookingBack && currentLookState != PlayerLookState.Normal)
            {
                // 정면으로 먼저 돌아가기
                ReturnToNormalLook();
                return;
            }
            
            // 정면에서 뒤돌아보기 시작
            currentLookState = lookDirection;
            isLookingBack = true;
            
            // ParentConstraint 비활성화
            cameraController.parentConstraint.constraintActive = false;
            cameraController.parentConstraint.weight = 0f;

            // 목표 각도 설정
            targetLookAngle = (lookDirection == PlayerLookState.LookingLeft) ? -maxLookAngle : maxLookAngle;
            
            // 카메라 위치 설정
            cameraController.SetTargetCameraPosition(lookDirection);
            
            // 카메라 FOV 설정 (뒤돌아보기 FOV)
            cameraController.SetTargetFOV(cameraController.lookBackFOV);
            
            OnLookStateChanged?.Invoke(currentLookState);
            OnLookingBackChanged?.Invoke(true);
            
            Debug.Log($"Started looking back: {lookDirection}");
        }
        
        /// <summary>
        /// 뒤돌아보기 각도 적용
        /// </summary>
        private void ApplyLookBackAngle()
        {
            if (currentLookState == PlayerLookState.Normal) 
            {
                if (cameraController.parentConstraint.weight < 1f)
                    cameraController.parentConstraint.weight += Time.deltaTime;
                else
                    cameraController.parentConstraint.weight = 1f;
                return;
            }
            
            // 뒤돌아보기 각도 적용 (마우스 회전 없이 수평 회전만)
            currentLookAngle = Mathf.Lerp(currentLookAngle, targetLookAngle, lookSpeed * Time.deltaTime);
            cameraController.SetCameraRotation(currentLookAngle);
        }
        
        /// <summary>
        /// 정상 시점으로 돌아가기
        /// </summary>
        private void ReturnToNormalLook()
        {
            if (currentLookState == PlayerLookState.Normal) return;
            
            targetLookAngle = 0f;
            
            // 카메라를 원래 위치로 복귀
            cameraController.ReturnToOriginalPosition();
            
            // 카메라 FOV를 정상으로 복귀
            cameraController.ReturnToNormalFOV();

            // 회전 시작 시 ParentConstraint 비활성화
            if (cameraController != null && cameraController.parentConstraint != null)
            {
                cameraController.parentConstraint.constraintActive = false;
            }

            // 부드럽게 정상 각도로 돌아가기 (마우스 회전 없이)
            currentLookAngle = Mathf.Lerp(currentLookAngle, 0f, lookSpeed * Time.deltaTime);
            cameraController.SetCameraRotation(currentLookAngle);
            
            if (Mathf.Abs(currentLookAngle) < 0.1f)
            {
                currentLookAngle = 0f;
                currentLookState = PlayerLookState.Normal;
                OnLookStateChanged?.Invoke(currentLookState);
                isLookingBack = false;
                
                // 회전 완료 시 ParentConstraint 활성화
                if (cameraController != null && cameraController.parentConstraint != null)
                {
                    cameraController.parentConstraint.constraintActive = true;
                }

                OnLookingBackChanged?.Invoke(false);
                
                Debug.Log("Returned to normal look");
            }
        }
        
        /// <summary>
        /// 현재 시점 상태 반환
        /// </summary>
        public PlayerLookState GetCurrentLookState()
        {
            return currentLookState;
        }
        
        /// <summary>
        /// 뒤를 보고 있는 상태인지 확인 (왼쪽이든 오른쪽이든)
        /// </summary>
        public bool IsLookingBack()
        {
            return isLookingBack;
        }
        
        /// <summary>
        /// 특정 방향으로 뒤돌아보기 중인지 확인
        /// </summary>
        public bool IsLookingInDirection(PlayerLookState direction)
        {
            return currentLookState == direction;
        }
        
        
        /// <summary>
        /// 마우스 감도 설정
        /// </summary>
        public void SetMouseSensitivity(float sensitivity)
        {
            mouseSensitivity = sensitivity;
        }
        
        /// <summary>
        /// 마우스 커서 상태 설정
        /// </summary>
        public void SetCursorLock(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
        
        /// <summary>
        /// 강제로 정상 시점으로 돌아가기 (귀신 등장 시 등)
        /// </summary>
        public void ForceReturnToNormal()
        {
            currentLookState = PlayerLookState.Normal;
            isLookingBack = false;
            currentLookAngle = 0f;
            targetLookAngle = 0f;
            
            // 카메라를 원래 위치로 즉시 리셋
            cameraController.ResetCameraPosition();
            
            // 카메라 FOV를 즉시 정상으로 리셋
            cameraController.ResetCameraFOV();
            
            // 카메라를 정면으로 리셋 (마우스 회전 없이)
            cameraController.ResetCameraRotation();
            
            // 강제 리셋 시 ParentConstraint 활성화
            if (cameraController != null && cameraController.parentConstraint != null)
            {
                cameraController.parentConstraint.constraintActive = true;
            }
            
            OnLookStateChanged?.Invoke(currentLookState);
            OnLookingBackChanged?.Invoke(false);
            
            Debug.Log("Force returned to normal look");
        }
    }
}
