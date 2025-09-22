using UnityEngine;
using DidYouHear.Player;
using DidYouHear.Core;
using UnityEngine.Animations;

namespace DidYouHear.Core
{
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [Header("Camera Settings")]
        public float fieldOfView = 60f;
        public float nearClipPlane = 0.1f;
        public float farClipPlane = 1000f;
                
        [Header("Camera Shake")]
        public bool enableCameraShake = true;
        public float shakeIntensity = 1f;
        public float shakeDuration = 0.5f;
        
        [Header("Camera Position Settings")]
        public Transform leftShoulderPosition;  // 왼쪽 어깨 위치
        public Transform rightShoulderPosition; // 오른쪽 어깨 위치
        public float positionLerpSpeed = 3f;    // 위치 이동 속도
        
        [Header("Camera FOV Settings")]
        public float normalFOV = 60f;           // 정상 시점 FOV
        public float lookBackFOV = 80f;         // 뒤돌아보기 FOV
        public float fovLerpSpeed = 2f;         // FOV 전환 속도
        
        
        // 컴포넌트 참조
        public Camera playerCamera;
        private PlayerController playerController;

        public ParentConstraint parentConstraint;
        
        // 카메라 상태
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private float currentFOV;
        
        // 카메라 위치 관련
        private Vector3 originalCameraPosition;
        private Vector3 targetCameraPosition;
        private Vector3 currentCameraPosition;
        
        // 카메라 FOV 관련
        private float currentFOVValue;
        private float targetFOVValue;
        
        
        // 카메라 쉐이크 관련
        private float shakeTimer = 0f;
        private Vector3 shakeOffset = Vector3.zero;
        private bool isShaking = false;
        
        
        // 이벤트
        public System.Action OnCameraShakeStarted;
        public System.Action OnCameraShakeEnded;
        
        private void Awake()
        {
            // 컴포넌트 초기화
            playerCamera = GetComponent<Camera>();
            if (playerCamera == null)
            {
                Debug.LogError("CameraController: Camera component not found!");
                return;
            }
            
            // 부모에서 PlayerController 찾기
            playerController = GetComponentInParent<PlayerController>();

            parentConstraint = GetComponent<ParentConstraint>();
            if (parentConstraint == null)
            {
                Debug.LogError("CameraController: ParentConstraint component not found!");
                return;
            }
        }
        
        private void Start()
        {
            // 카메라 초기 설정
            InitializeCamera();
            
            // 이벤트 구독
            SubscribeToEvents();
        }
        
        private void Update()
        {
            // 게임이 일시정지 상태가 아닐 때만 카메라 처리
            if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Playing)
                return;
                
            HandleCameraShake();
            UpdateCameraPosition();
        }
        
        /// <summary>
        /// 카메라 초기화
        /// </summary>
        private void InitializeCamera()
        {
            // 카메라 기본 설정
            playerCamera.fieldOfView = fieldOfView;
            playerCamera.nearClipPlane = nearClipPlane;
            playerCamera.farClipPlane = farClipPlane;
            
            // 원본 위치와 회전 저장
            originalPosition = transform.localPosition;
            originalRotation = transform.localRotation;
            currentFOV = fieldOfView;
            
            // 카메라 원래 위치 저장
            originalCameraPosition = playerCamera.transform.localPosition;
            currentCameraPosition = originalCameraPosition;
            targetCameraPosition = originalCameraPosition;
            
            // 카메라 FOV 초기화
            currentFOVValue = normalFOV;
            targetFOVValue = normalFOV;
            playerCamera.fieldOfView = currentFOVValue;
            
            Debug.Log("Camera Controller Initialized");
        }
        
        /// <summary>
        /// 이벤트 구독
        /// </summary>
        private void SubscribeToEvents()
        {
            // 현재 구독할 이벤트 없음
        }
        
        
        /// <summary>
        /// 카메라 쉐이크 처리
        /// </summary>
        private void HandleCameraShake()
        {
            if (!enableCameraShake) return;
            
            if (isShaking)
            {
                shakeTimer -= Time.deltaTime;
                
                if (shakeTimer <= 0f)
                {
                    // 쉐이크 종료
                    isShaking = false;
                    shakeOffset = Vector3.zero;
                    OnCameraShakeEnded?.Invoke();
                }
                else
                {
                    // 쉐이크 오프셋 계산
                    float intensity = (shakeTimer / shakeDuration) * shakeIntensity;
                    shakeOffset = new Vector3(
                        Random.Range(-1f, 1f) * intensity,
                        Random.Range(-1f, 1f) * intensity,
                        0f
                    );
                }
            }
        }
        
        
        /// <summary>
        /// 카메라 위치 업데이트
        /// </summary>
        private void UpdateCameraPosition()
        {
            // 카메라 위치를 부드럽게 전환
            currentCameraPosition = Vector3.Lerp(currentCameraPosition, targetCameraPosition, positionLerpSpeed * Time.deltaTime);
            
            // 카메라 FOV를 부드럽게 전환
            currentFOVValue = Mathf.Lerp(currentFOVValue, targetFOVValue, fovLerpSpeed * Time.deltaTime);
            playerCamera.fieldOfView = currentFOVValue;
            
            // 최종 카메라 위치 계산 (쉐이크 포함)
            Vector3 finalPosition = currentCameraPosition + shakeOffset;
            transform.localPosition = finalPosition;
        }
        
        
        
        /// <summary>
        /// 카메라 쉐이크 시작
        /// </summary>
        public void StartCameraShake(float intensity = 1f, float duration = 0.5f)
        {
            if (!enableCameraShake) return;
            
            shakeIntensity = intensity;
            shakeDuration = duration;
            shakeTimer = duration;
            isShaking = true;
            
            OnCameraShakeStarted?.Invoke();
            Debug.Log($"Camera Shake Started - Intensity: {intensity}, Duration: {duration}");
        }
        
        /// <summary>
        /// 카메라 쉐이크 중지
        /// </summary>
        public void StopCameraShake()
        {
            isShaking = false;
            shakeTimer = 0f;
            shakeOffset = Vector3.zero;
            
            OnCameraShakeEnded?.Invoke();
            Debug.Log("Camera Shake Stopped");
        }
        
        /// <summary>
        /// 카메라 FOV 설정
        /// </summary>
        public void SetFieldOfView(float fov)
        {
            fieldOfView = fov;
            currentFOV = fov;
            playerCamera.fieldOfView = fov;
        }
        
        /// <summary>
        /// 카메라 효과 활성화/비활성화
        /// </summary>
        public void SetCameraEffects(bool shake)
        {
            enableCameraShake = shake;
        }
        
        /// <summary>
        /// 카메라 회전 설정
        /// </summary>
        public void SetCameraRotation(float yRotation)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
        }
        
        /// <summary>
        /// 목표 카메라 위치 설정
        /// </summary>
        public void SetTargetCameraPosition(PlayerLookState lookDirection)
        {
            switch (lookDirection)
            {
                case PlayerLookState.LookingLeft:
                    if (leftShoulderPosition != null)
                    {
                        targetCameraPosition = leftShoulderPosition.localPosition;
                    }
                    else
                    {
                        // 어깨 위치가 설정되지 않은 경우 기본 오프셋 사용
                        targetCameraPosition = originalCameraPosition + Vector3.left * 0.3f;
                    }
                    break;
                    
                case PlayerLookState.LookingRight:
                    if (rightShoulderPosition != null)
                    {
                        targetCameraPosition = rightShoulderPosition.localPosition;
                    }
                    else
                    {
                        // 어깨 위치가 설정되지 않은 경우 기본 오프셋 사용
                        targetCameraPosition = originalCameraPosition + Vector3.right * 0.3f;
                    }
                    break;
                    
                case PlayerLookState.Normal:
                default:
                    targetCameraPosition = originalCameraPosition;
                    break;
            }
        }
        
        /// <summary>
        /// 카메라 FOV 설정
        /// </summary>
        public void SetTargetFOV(float fov)
        {
            targetFOVValue = fov;
        }
        
        /// <summary>
        /// 카메라를 원래 위치로 복귀
        /// </summary>
        public void ReturnToOriginalPosition()
        {
            targetCameraPosition = originalCameraPosition;
        }
        
        /// <summary>
        /// 카메라 FOV를 정상으로 복귀
        /// </summary>
        public void ReturnToNormalFOV()
        {
            targetFOVValue = normalFOV;
        }
        
        /// <summary>
        /// 카메라를 정면으로 리셋
        /// </summary>
        public void ResetCameraRotation()
        {
            playerCamera.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        }
        
        /// <summary>
        /// 카메라를 원래 위치로 즉시 리셋
        /// </summary>
        public void ResetCameraPosition()
        {
            targetCameraPosition = originalCameraPosition;
            currentCameraPosition = originalCameraPosition;
            playerCamera.transform.localPosition = originalCameraPosition;
        }
        
        /// <summary>
        /// 카메라 FOV를 즉시 정상으로 리셋
        /// </summary>
        public void ResetCameraFOV()
        {
            targetFOVValue = normalFOV;
            currentFOVValue = normalFOV;
            playerCamera.fieldOfView = currentFOVValue;
        }
        
        /// <summary>
        /// 카메라 리셋
        /// </summary>
        public void ResetCamera()
        {
            transform.localPosition = originalPosition;
            transform.localRotation = originalRotation;
            playerCamera.fieldOfView = fieldOfView;
            
            shakeOffset = Vector3.zero;
            isShaking = false;
            
            Debug.Log("Camera Reset");
        }
        
        /// <summary>
        /// 현재 카메라 상태 정보 반환
        /// </summary>
        public string GetCameraStatus()
        {
            return $"Camera Status:\n" +
                   $"FOV: {playerCamera.fieldOfView:F1}\n" +
                   $"Position: {transform.localPosition}\n" +
                   $"Shaking: {isShaking}";
        }
        
        private void OnDestroy()
        {
            // 현재 구독할 이벤트 없음
        }
    }
}
