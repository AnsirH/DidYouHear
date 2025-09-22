using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using DidYouHear.Manager.Interfaces;
using DidYouHear.Player;
using DidYouHear.Manager.Core;
using DidYouHear.Core;
using DidYouHear.Corridor;
using DidYouHear.Manager.System;
using DidYouHear.Events;

namespace DidYouHear.Manager.Gameplay
{
    /// <summary>
    /// 이벤트 관리자 (싱글톤)
    /// </summary>
    public class EventManager : MonoBehaviour, IManager
    {
        public static EventManager Instance { get; private set; }

        [Header("Event Settings")]
        public float minEventInterval = 10f; // 최소 이벤트 간격
        public float reactionTimeLimit = 2f; // 모든 이벤트의 기본 반응 시간 제한

        [Header("Shoulder Tap Event")]
        public ShoulderTapEvent shoulderTapEvent;
        public float walkShoulderTapMinInterval = 20f;
        public float walkShoulderTapMaxInterval = 30f;
        public float runShoulderTapMinInterval = 10f;
        public float runShoulderTapMaxInterval = 20f;

        [Header("Environmental Event")]
        public EnvironmentalEvent environmentalEvent;
        public float environmentalEventMinInterval = 30f;
        public float environmentalEventMaxInterval = 60f;

        [Header("Ghost Event")]
        public GhostAppearanceEvent ghostAppearanceEvent;

        // 이벤트 큐 및 상태
        private Queue<GameEvent> eventQueue = new Queue<GameEvent>();
        private GameEvent currentActiveEvent;
        private bool isEventActive = false;
        private bool isWaitingForReaction = false;
        private float eventTimer = 0f;
        private float nextShoulderTapTime;
        private float nextEnvironmentalEventTime;

        // 컴포넌트 참조
        public PlayerMovement playerMovement;
        public PlayerLook playerLook;
        private AudioManager audioManager;

        // 이벤트 타입 정의
        public enum EventType
        {
            None,
            ShoulderTap,
            Environmental,
            GhostAppearance
        }

        public Action<EventType> OnEventTriggered;
        public Action<EventType, bool> OnEventCompleted; // EventType, Success
        
        // IManager 구현을 위한 필드
        private bool isInitialized = false;
        private bool isActive = true;
        private DependencyContainer dependencyContainer;
        private EventType currentEventType = EventType.None;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }


        /// <summary>
        /// 이벤트 구독
        /// </summary>
        private void SubscribeToEvents()
        {
            if (playerMovement != null)
            {
                playerMovement.OnMovementStateChanged += HandleMovementStateChanged;
            }
            if (shoulderTapEvent != null)
            {
                shoulderTapEvent.OnEventCompleted += HandleShoulderTapCompleted;
            }
            if (environmentalEvent != null)
            {
                environmentalEvent.OnEventCompleted += HandleEnvironmentalEventCompleted;
            }
            if (ghostAppearanceEvent != null)
            {
                ghostAppearanceEvent.OnEventCompleted += HandleGhostAppearanceCompleted;
            }
        }

        /// <summary>
        /// 이벤트 구독 해제
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (playerMovement != null)
            {
                playerMovement.OnMovementStateChanged -= HandleMovementStateChanged;
            }
            if (shoulderTapEvent != null)
            {
                shoulderTapEvent.OnEventCompleted -= HandleShoulderTapCompleted;
            }
            if (environmentalEvent != null)
            {
                environmentalEvent.OnEventCompleted -= HandleEnvironmentalEventCompleted;
            }
            if (ghostAppearanceEvent != null)
            {
                ghostAppearanceEvent.OnEventCompleted -= HandleGhostAppearanceCompleted;
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentGameState != GameManager.GameState.Playing) return;

            if (isEventActive)
            {
                currentActiveEvent?.UpdateEvent();
            }
            else
            {
                eventTimer += Time.deltaTime;
                CheckForEventTriggers();
            }
        }

        /// <summary>
        /// 이벤트 트리거 확인
        /// </summary>
        private void CheckForEventTriggers()
        {
            if (playerMovement == null || playerMovement.CurrentMovementState == PlayerMovementState.Idle || playerMovement.CurrentMovementState == PlayerMovementState.Crouching)
            {
                ResetEventTimers(); // 이동하지 않거나 숙이면 타이머 리셋
                return;
            }

            // 어깨 두드림 이벤트 체크
            if (eventTimer >= nextShoulderTapTime)
            {
                TriggerShoulderTapEvent();
                ResetShoulderTapTimer();
            }

            // 환경 이벤트 체크
            if (eventTimer >= nextEnvironmentalEventTime)
            {
                TriggerEnvironmentalEvent();
                ResetEnvironmentalEventTimer();
            }
        }

        /// <summary>
        /// 어깨 두드림 이벤트 트리거
        /// </summary>
        public void TriggerShoulderTapEvent()
        {
            if (isEventActive) return;

            isEventActive = true;
            currentActiveEvent = shoulderTapEvent;
            shoulderTapEvent.Execute();
            OnEventTriggered?.Invoke(EventType.ShoulderTap);
            Debug.Log("Shoulder Tap Event Triggered!");
        }

        /// <summary>
        /// 환경 이벤트 트리거
        /// </summary>
        public void TriggerEnvironmentalEvent()
        {
            if (isEventActive) return;

            isEventActive = true;
            currentActiveEvent = environmentalEvent;
            environmentalEvent.Execute();
            OnEventTriggered?.Invoke(EventType.Environmental);
            Debug.Log("Environmental Event Triggered!");
        }

        /// <summary>
        /// 귀신 등장 이벤트 트리거
        /// </summary>
        public void TriggerGhostAppearance()
        {
            if (isEventActive) return; // 귀신 이벤트는 다른 이벤트와 동시에 발생하지 않음

            isEventActive = true;
            currentActiveEvent = ghostAppearanceEvent;
            ghostAppearanceEvent.Execute();
            OnEventTriggered?.Invoke(EventType.GhostAppearance);
            Debug.Log("Ghost Appearance Event Triggered!");
        }

        /// <summary>
        /// 반응 타이머 시작
        /// </summary>
        public void StartReactionTimer()
        {
            isWaitingForReaction = true;
            StartCoroutine(ReactionTimerCoroutine(reactionTimeLimit));
        }

        /// <summary>
        /// 반응 타이머 코루틴
        /// </summary>
        private IEnumerator ReactionTimerCoroutine(float limit)
        {
            float timer = 0f;
            while (timer < limit && isWaitingForReaction)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            if (isWaitingForReaction) // 시간 초과
            {
                Debug.Log("Reaction time out!");
                currentActiveEvent?.OnReactionTimeout();
            }
            isWaitingForReaction = false;
        }

        /// <summary>
        /// 반응 완료 (성공 또는 실패)
        /// </summary>
        public void EndReactionTimer()
        {
            isWaitingForReaction = false;
        }

        /// <summary>
        /// 이벤트 타이머 리셋
        /// </summary>
        private void ResetEventTimers()
        {
            eventTimer = 0f;
            ResetShoulderTapTimer();
            ResetEnvironmentalEventTimer();
        }

        private void ResetShoulderTapTimer()
        {
            float min = walkShoulderTapMinInterval;
            float max = walkShoulderTapMaxInterval;

            if (playerMovement != null && playerMovement.IsRunning())
            {
                min = runShoulderTapMinInterval;
                max = runShoulderTapMaxInterval;
            }
            nextShoulderTapTime = eventTimer + UnityEngine.Random.Range(min, max);
        }

        private void ResetEnvironmentalEventTimer()
        {
            nextEnvironmentalEventTime = eventTimer + UnityEngine.Random.Range(environmentalEventMinInterval, environmentalEventMaxInterval);
        }

        // 이벤트 완료 핸들러
        private void HandleShoulderTapCompleted(bool success)
        {
            isEventActive = false;
            currentActiveEvent = null;
            OnEventCompleted?.Invoke(EventType.ShoulderTap, success);
            Debug.Log($"Shoulder Tap Event Completed. Success: {success}");
        }

        private void HandleEnvironmentalEventCompleted(bool success)
        {
            isEventActive = false;
            currentActiveEvent = null;
            OnEventCompleted?.Invoke(EventType.Environmental, success);
            Debug.Log($"Environmental Event Completed. Success: {success}");
        }

        private void HandleGhostAppearanceCompleted(bool success)
        {
            isEventActive = false;
            currentActiveEvent = null;
            OnEventCompleted?.Invoke(EventType.GhostAppearance, success);
            Debug.Log($"Ghost Appearance Event Completed. Success: {success}");
        }

        // 플레이어 이동 상태 변경 핸들러
        private void HandleMovementStateChanged(PlayerMovementState newState)
        {
            // 이동 상태에 따라 이벤트 타이머 간격 조정
            ResetShoulderTapTimer();
        }

        /// <summary>
        /// 현재 활성화된 이벤트 반환
        /// </summary>
        public GameEvent GetCurrentActiveEvent()
        {
            return currentActiveEvent;
        }

        /// <summary>
        /// 이벤트가 활성화 중인지 확인
        /// </summary>
        public bool IsEventActive()
        {
            return isEventActive;
        }

        /// <summary>
        /// 반응 대기 중인지 확인
        /// </summary>
        public bool IsWaitingForReaction()
        {
            return isWaitingForReaction;
        }
        
        /// <summary>
        /// 코루틴 시작 (GameEvent에서 사용)
        /// </summary>
        public void StartEventCoroutine(IEnumerator coroutine)
        {
            StartCoroutine(coroutine);
        }
        
        /// <summary>
        /// 복도 타입에 따른 환경 이벤트 트리거
        /// </summary>
        public void TriggerEnvironmentalEventForCorridor(CorridorType corridorType)
        {
            if (isEventActive) return;
            
            // 복도 타입에 따른 이벤트 설정
            SetEnvironmentalEventForCorridor(corridorType);
            
            // 환경 이벤트 실행
            TriggerEnvironmentalEvent();
        }
        
        /// <summary>
        /// 복도 타입에 따른 환경 이벤트 설정
        /// </summary>
        private void SetEnvironmentalEventForCorridor(CorridorType corridorType)
        {
            if (environmentalEvent == null) return;
            
            // 복도 타입에 따른 이벤트 타입 결정
            EnvironmentalEvent.EnvironmentalEventType eventType = GetEventTypeForCorridor(corridorType);
            environmentalEvent.SetEventType(eventType);
        }
        
        /// <summary>
        /// 복도 타입에 맞는 이벤트 타입 반환
        /// </summary>
        private EnvironmentalEvent.EnvironmentalEventType GetEventTypeForCorridor(CorridorType corridorType)
        {
            switch (corridorType)
            {
                case CorridorType.Classroom:
                    // 교실 복도: 교실 불, 문 열림, 창문 열림
                    return GetRandomEventType(new[] { 
                        EnvironmentalEvent.EnvironmentalEventType.ClassroomLight,
                        EnvironmentalEvent.EnvironmentalEventType.DoorOpen,
                        EnvironmentalEvent.EnvironmentalEventType.WindowOpen
                    });
                    
                case CorridorType.ClassroomBathroom:
                    // 교실&화장실 복도: 모든 이벤트 가능
                    return GetRandomEventType();
                    
                default:
                    return EnvironmentalEvent.EnvironmentalEventType.ClassroomLight;
            }
        }
        
        /// <summary>
        /// 랜덤 이벤트 타입 반환
        /// </summary>
        private EnvironmentalEvent.EnvironmentalEventType GetRandomEventType()
        {
            return (EnvironmentalEvent.EnvironmentalEventType)UnityEngine.Random.Range(0, Enum.GetValues(typeof(EnvironmentalEvent.EnvironmentalEventType)).Length);
        }
        
        /// <summary>
        /// 지정된 이벤트 타입 중에서 랜덤 선택
        /// </summary>
        private EnvironmentalEvent.EnvironmentalEventType GetRandomEventType(EnvironmentalEvent.EnvironmentalEventType[] availableTypes)
        {
            return availableTypes[UnityEngine.Random.Range(0, availableTypes.Length)];
        }
        
        // IManager 인터페이스 구현
        public int GetPriority()
        {
            return (int)ManagerType.System; // 10
        }
        
        public ManagerType GetManagerType()
        {
            return ManagerType.System;
        }
        
        public ManagerScope GetManagerScope()
        {
            return ManagerScope.Scene; // 씬별로 존재
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
            return $"EventManager: Initialized={isInitialized}, Active={isActive}, " +
                   $"CurrentEvent={currentEventType}, EventInterval={minEventInterval}s";
        }
        
        public void Initialize()
        {
            if (!isInitialized)
            {
                InitializeEvents();
                isInitialized = true;
            }
        }
        
        private void InitializeEvents()
        {
            // 이벤트 초기화 로직
            currentEventType = EventType.None;
            ResetEventTimers();
            SubscribeToEvents();
        }
        
        public void Reset()
        {
            isInitialized = false;
            isActive = false;
            currentEventType = EventType.None;
            minEventInterval = 10f;
        }
        
        public bool IsInitialized()
        {
            return isInitialized;
        }
    }
}




