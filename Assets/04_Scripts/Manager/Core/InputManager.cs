using UnityEngine;
using System.Collections.Generic;
using System;
using DidYouHear.Manager.Interfaces;
using DidYouHear.Manager.System;
using DidYouHear.Core;

namespace DidYouHear.Manager.Core
{
    public class InputManager : MonoBehaviour, IManager
    {
        [Header("Input Settings")]
        public float inputSensitivity = 1f;
        public bool invertY = false;
        public bool enableInputLogging = false;
        
        [Header("Key Bindings")]
        public KeyCode walkKey = KeyCode.W;
        public KeyCode runKey = KeyCode.LeftShift;
        public KeyCode crouchKey = KeyCode.LeftControl;
        public KeyCode lookLeftKey = KeyCode.Q;
        public KeyCode lookRightKey = KeyCode.E;
        public KeyCode manualStoneKey = KeyCode.Space;
        public KeyCode pauseKey = KeyCode.Escape;
        
        [Header("Mouse Settings")]
        public float mouseSensitivity = 100f;
        public bool mouseInverted = false;
        
        // 입력 상태
        private Dictionary<string, bool> inputStates = new Dictionary<string, bool>();
        private Dictionary<string, bool> previousInputStates = new Dictionary<string, bool>();
        
        // 마우스 입력
        private Vector2 mouseDelta;
        private Vector2 mousePosition;
        
        // 이벤트
        public Action<string, bool> OnInputStateChanged;
        public Action<Vector2> OnMouseDeltaChanged;
        public Action<KeyCode> OnKeyPressed;
        public Action<KeyCode> OnKeyReleased;
        
        // IManager 구현을 위한 필드
        private bool isInitialized = false;
        private bool isActive = true;
        private DependencyContainer dependencyContainer;
        
        // 싱글톤 패턴
        public static InputManager Instance { get; private set; }
        
        private void Awake()
        {
            // 싱글톤 패턴 구현
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeInput();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // 마우스 커서 설정
            SetCursorLock(true);
        }
        
        private void Update()
        {
            // 게임이 일시정지 상태가 아닐 때만 입력 처리
            if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Playing)
                return;
                
            HandleKeyboardInput();
            HandleMouseInput();
            UpdateInputStates();
        }
        
        /// <summary>
        /// 입력 시스템 초기화
        /// </summary>
        private void InitializeInput()
        {
            // 기본 입력 상태 초기화
            InitializeInputStates();
            
            Debug.Log("Input Manager Initialized");
        }
        
        /// <summary>
        /// 입력 상태 초기화
        /// </summary>
        private void InitializeInputStates()
        {
            string[] inputNames = {
                "Walk", "Run", "Crouch", "LookLeft", "LookRight", 
                "ManualStone", "Pause", "Jump", "Interact"
            };
            
            foreach (string inputName in inputNames)
            {
                inputStates[inputName] = false;
                previousInputStates[inputName] = false;
            }
        }
        
        /// <summary>
        /// 키보드 입력 처리 (앞으로만 이동 가능)
        /// </summary>
        private void HandleKeyboardInput()
        {
            // 이동 관련 입력
            UpdateInputState("Walk", Input.GetKey(walkKey));
            UpdateInputState("Run", Input.GetKey(runKey));
            UpdateInputState("Crouch", Input.GetKey(crouchKey));
            
            // 시점 관련 입력 (뒤돌아보기만)
            UpdateInputState("LookLeft", Input.GetKey(lookLeftKey));
            UpdateInputState("LookRight", Input.GetKey(lookRightKey));
            
            // 상호작용 입력
            UpdateInputState("ManualStone", Input.GetKeyDown(manualStoneKey));
            UpdateInputState("Pause", Input.GetKeyDown(pauseKey));
            
            // 기타 입력
            UpdateInputState("Jump", Input.GetKeyDown(KeyCode.Space));
            UpdateInputState("Interact", Input.GetKeyDown(KeyCode.F));
        }
        
        /// <summary>
        /// 마우스 입력 처리
        /// </summary>
        private void HandleMouseInput()
        {
            // 마우스 델타 계산
            mouseDelta = new Vector2(
                Input.GetAxis("Mouse X") * mouseSensitivity * inputSensitivity,
                Input.GetAxis("Mouse Y") * mouseSensitivity * inputSensitivity
            );
            
            // Y축 반전 처리
            if (mouseInverted)
            {
                mouseDelta.y = -mouseDelta.y;
            }
            
            // 마우스 위치 업데이트
            mousePosition = Input.mousePosition;
            
            // 마우스 델타 이벤트 발생
            if (mouseDelta.magnitude > 0.01f)
            {
                OnMouseDeltaChanged?.Invoke(mouseDelta);
            }
        }
        
        /// <summary>
        /// 입력 상태 업데이트
        /// </summary>
        private void UpdateInputState(string inputName, bool isPressed)
        {
            bool previousState = previousInputStates.ContainsKey(inputName) ? previousInputStates[inputName] : false;
            bool currentState = isPressed;
            
            // 상태 변경 감지
            if (previousState != currentState)
            {
                inputStates[inputName] = currentState;
                OnInputStateChanged?.Invoke(inputName, currentState);
                
                if (enableInputLogging)
                {
                    Debug.Log($"Input State Changed: {inputName} = {currentState}");
                }
            }
            
            // 이전 상태 업데이트
            previousInputStates[inputName] = currentState;
        }
        
        /// <summary>
        /// 입력 상태 업데이트 (다음 프레임용)
        /// </summary>
        private void UpdateInputStates()
        {
            // 이전 상태를 현재 상태로 복사
            foreach (var kvp in inputStates)
            {
                previousInputStates[kvp.Key] = kvp.Value;
            }
        }
        
        /// <summary>
        /// 특정 입력이 눌렸는지 확인
        /// </summary>
        public bool GetInput(string inputName)
        {
            return inputStates.ContainsKey(inputName) ? inputStates[inputName] : false;
        }
        
        /// <summary>
        /// 특정 입력이 이번 프레임에 눌렸는지 확인
        /// </summary>
        public bool GetInputDown(string inputName)
        {
            bool current = GetInput(inputName);
            bool previous = previousInputStates.ContainsKey(inputName) ? previousInputStates[inputName] : false;
            return current && !previous;
        }
        
        /// <summary>
        /// 특정 입력이 이번 프레임에 떼어졌는지 확인
        /// </summary>
        public bool GetInputUp(string inputName)
        {
            bool current = GetInput(inputName);
            bool previous = previousInputStates.ContainsKey(inputName) ? previousInputStates[inputName] : false;
            return !current && previous;
        }
        
        /// <summary>
        /// 마우스 델타 반환
        /// </summary>
        public Vector2 GetMouseDelta()
        {
            return mouseDelta;
        }
        
        /// <summary>
        /// 마우스 위치 반환
        /// </summary>
        public Vector2 GetMousePosition()
        {
            return mousePosition;
        }
        
        /// <summary>
        /// 마우스 감도 설정
        /// </summary>
        public void SetMouseSensitivity(float sensitivity)
        {
            mouseSensitivity = sensitivity;
        }
        
        /// <summary>
        /// 입력 감도 설정
        /// </summary>
        public void SetInputSensitivity(float sensitivity)
        {
            inputSensitivity = sensitivity;
        }
        
        /// <summary>
        /// 마우스 Y축 반전 설정
        /// </summary>
        public void SetMouseInverted(bool inverted)
        {
            mouseInverted = inverted;
        }
        
        /// <summary>
        /// 마우스 커서 잠금 설정
        /// </summary>
        public void SetCursorLock(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
        
        /// <summary>
        /// 키 바인딩 변경
        /// </summary>
        public void SetKeyBinding(string action, KeyCode newKey)
        {
            switch (action.ToLower())
            {
                case "walk":
                    walkKey = newKey;
                    break;
                case "run":
                    runKey = newKey;
                    break;
                case "crouch":
                    crouchKey = newKey;
                    break;
                case "lookleft":
                    lookLeftKey = newKey;
                    break;
                case "lookright":
                    lookRightKey = newKey;
                    break;
                case "manualstone":
                    manualStoneKey = newKey;
                    break;
                case "pause":
                    pauseKey = newKey;
                    break;
                default:
                    Debug.LogWarning($"Unknown action: {action}");
                    break;
            }
        }
        
        /// <summary>
        /// 입력 로깅 활성화/비활성화
        /// </summary>
        public void SetInputLogging(bool enabled)
        {
            enableInputLogging = enabled;
        }
        
        /// <summary>
        /// 모든 입력 상태 리셋
        /// </summary>
        public void ResetAllInputs()
        {
            foreach (string key in inputStates.Keys)
            {
                inputStates[key] = false;
                previousInputStates[key] = false;
            }
            
            mouseDelta = Vector2.zero;
            Debug.Log("All inputs reset");
        }
        
        /// <summary>
        /// 현재 입력 상태 정보 반환
        /// </summary>
        public string GetInputStatus()
        {
            string status = "Input Status:\n";
            
            foreach (var kvp in inputStates)
            {
                status += $"{kvp.Key}: {kvp.Value}\n";
            }
            
            status += $"Mouse Delta: {mouseDelta}\n";
            status += $"Mouse Position: {mousePosition}\n";
            status += $"Mouse Sensitivity: {mouseSensitivity}\n";
            status += $"Input Sensitivity: {inputSensitivity}";
            
            return status;
        }
        
        /// <summary>
        /// 키 바인딩 정보 반환
        /// </summary>
        public string GetKeyBindings()
        {
            return $"Key Bindings:\n" +
                   $"Walk: {walkKey}\n" +
                   $"Run: {runKey}\n" +
                   $"Crouch: {crouchKey}\n" +
                   $"Look Left: {lookLeftKey}\n" +
                   $"Look Right: {lookRightKey}\n" +
                   $"Manual Stone: {manualStoneKey}\n" +
                   $"Pause: {pauseKey}";
        }
        
        // IManager 인터페이스 구현
        public int GetPriority()
        {
            return (int)ManagerType.Core; // 0
        }
        
        public ManagerType GetManagerType()
        {
            return ManagerType.Core;
        }
        
        public ManagerScope GetManagerScope()
        {
            return ManagerScope.Global; // 모든 씬에서 유지
        }
        
        public void SetDependencyContainer(DependencyContainer container)
        {
            dependencyContainer = container;
        }
        
        public void SetActive(bool active)
        {
            isActive = active;
            gameObject.SetActive(active);
        }
        
        public string GetStatus()
        {
            return $"InputManager: Initialized={isInitialized}, Active={isActive}, " +
                   $"Sensitivity={inputSensitivity}, MouseSensitivity={mouseSensitivity}";
        }
        
        public void Initialize()
        {
            if (!isInitialized)
            {
                InitializeInput();
                isInitialized = true;
            }
        }
        
        public void Reset()
        {
            isInitialized = false;
            isActive = false;
            inputSensitivity = 1f;
            mouseSensitivity = 1f;
        }
        
        public bool IsInitialized()
        {
            return isInitialized;
        }
    }
}
