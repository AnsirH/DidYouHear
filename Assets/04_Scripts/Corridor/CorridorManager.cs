using UnityEngine;
using System.Collections.Generic;
using DidYouHear.Manager.Gameplay;

namespace DidYouHear.Corridor
{
    /// <summary>
    /// 복도 시스템 전체 관리자
    /// </summary>
    public class CorridorManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CorridorGenerator corridorGenerator;
        [SerializeField] private EventManager eventManager;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Core.PlayerController playerController;
        
        [Header("Settings")]
        [SerializeField] private float corridorLoadDistance = 20f;
        [SerializeField] private float corridorUnloadDistance = 30f;
        [SerializeField] private int maxLoadedCorridors = 5;
        [SerializeField] private float rotationTriggerRadius = 1.5f;
        
        private List<CorridorPiece> loadedCorridors = new List<CorridorPiece>();
        private CorridorPiece currentCorridor;
        private int currentCorridorIndex = 0;
        private bool isInitialized = false;
        
        // 이벤트 관련
        private List<CorridorPiece> eventEligibleCorridors = new List<CorridorPiece>();
        
        private void Awake()
        {
            // 컴포넌트 초기화
            if (corridorGenerator == null)
            {
                corridorGenerator = GetComponent<CorridorGenerator>();
            }
        }
        
        /// <summary>
        /// CorridorManager 초기화 (GameManager에서 호출)
        /// </summary>
        public void Initialize(EventManager eventMgr, Transform playerTf, Core.PlayerController playerCtrl)
        {
            eventManager = eventMgr;
            playerTransform = playerTf;
            playerController = playerCtrl;
            
            if (eventManager == null) Debug.LogError("CorridorManager: EventManager not provided!");
            if (playerTransform == null) Debug.LogError("CorridorManager: PlayerTransform not provided!");
            if (playerController == null) Debug.LogError("CorridorManager: PlayerController not provided!");
            
            Debug.Log("CorridorManager initialized with provided references");
        }
        
        private void Start()
        {
            Initialize();
        }
        
        private void Update()
        {
            if (isInitialized)
            {
                UpdateCorridorManagement();
                UpdateEventSystem();
            }
        }
        
        /// <summary>
        /// 복도 매니저 초기화
        /// </summary>
        public void Initialize()
        {
            if (corridorGenerator == null)
            {
                Debug.LogError("CorridorGenerator is not assigned!");
                return;
            }
            
            // 복도 생성
            corridorGenerator.GenerateCorridors();
            
            // 이벤트 가능한 복도들 가져오기
            eventEligibleCorridors = corridorGenerator.GetEventEligibleCorridors();
            
            // 이벤트 시스템 설정
            SetupEventSystem();
            
            isInitialized = true;
            
            Debug.Log("CorridorManager initialized successfully");
        }
        
        /// <summary>
        /// 복도 관리 업데이트
        /// </summary>
        private void UpdateCorridorManagement()
        {
            if (playerTransform == null) return;
            
            //// 현재 복도 업데이트
            //UpdateCurrentCorridor();
            
            // 복도 로딩/언로딩 관리
            ManageCorridorLoading();
            
            // 회전 트리거 감지
            CheckRotationTriggers();
        }
        
        ///// <summary>
        ///// 현재 복도 업데이트
        ///// </summary>
        //private void UpdateCurrentCorridor()
        //{
        //    CorridorPiece newCurrentCorridor = corridorGenerator.GetCurrentCorridor();
            
        //    if (newCurrentCorridor != currentCorridor)
        //    {
        //        currentCorridor = newCurrentCorridor;
        //        OnCorridorChanged();
        //    }
        //}
        
        /// <summary>
        /// 복도 변경 시 처리
        /// </summary>
        private void OnCorridorChanged()
        {
            if (currentCorridor == null) return;
            
            Debug.Log($"Entered corridor: {currentCorridor.GetCorridorType()}");
            
            // 코너 타입인 경우 플레이어 자동 회전 처리
            if (currentCorridor.IsCornerType())
            {
                HandleCornerRotation();
            }
        }
        
        /// <summary>
        /// 코너 회전 처리
        /// </summary>
        private void HandleCornerRotation()
        {
            if (playerTransform == null) return;
            
            CorridorType corridorType = currentCorridor.GetCorridorType();
            float rotationAngle = 0f;
            
            if (corridorType == CorridorType.LeftCorner)
            {
                rotationAngle = -90f;
            }
            else if (corridorType == CorridorType.RightCorner)
            {
                rotationAngle = 90f;
            }
            
            // 플레이어 회전
            StartCoroutine(RotatePlayer(rotationAngle));
        }
        
        /// <summary>
        /// 플레이어 회전 코루틴
        /// </summary>
        private System.Collections.IEnumerator RotatePlayer(float angle)
        {
            Quaternion startRotation = playerTransform.rotation;
            Quaternion targetRotation = startRotation * Quaternion.Euler(0, angle, 0);
            float duration = 1f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                playerTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, progress);
                yield return null;
            }
            
            playerTransform.rotation = targetRotation;
        }
        
        /// <summary>
        /// 복도 로딩 관리
        /// </summary>
        private void ManageCorridorLoading()
        {
            List<CorridorPiece> allCorridors = corridorGenerator.GetEventEligibleCorridors();
            
            foreach (CorridorPiece corridor in allCorridors)
            {
                if (corridor == null) continue;
                
                float distance = Vector3.Distance(playerTransform.position, corridor.transform.position);
                bool shouldBeLoaded = distance <= corridorLoadDistance;
                bool isCurrentlyLoaded = loadedCorridors.Contains(corridor);
                
                if (shouldBeLoaded && !isCurrentlyLoaded)
                {
                    LoadCorridor(corridor);
                }
                else if (!shouldBeLoaded && isCurrentlyLoaded)
                {
                    UnloadCorridor(corridor);
                }
            }
        }
        
        /// <summary>
        /// 복도 로드
        /// </summary>
        private void LoadCorridor(CorridorPiece corridor)
        {
            if (loadedCorridors.Count >= maxLoadedCorridors)
            {
                // 가장 먼 복도 언로드
                UnloadFarthestCorridor();
            }
            
            corridor.gameObject.SetActive(true);
            loadedCorridors.Add(corridor);
            
            Debug.Log($"Loaded corridor: {corridor.GetCorridorType()}");
        }
        
        /// <summary>
        /// 복도 언로드
        /// </summary>
        private void UnloadCorridor(CorridorPiece corridor)
        {
            corridor.gameObject.SetActive(false);
            loadedCorridors.Remove(corridor);
            
            Debug.Log($"Unloaded corridor: {corridor.GetCorridorType()}");
        }
        
        /// <summary>
        /// 가장 먼 복도 언로드
        /// </summary>
        private void UnloadFarthestCorridor()
        {
            if (loadedCorridors.Count == 0) return;
            
            CorridorPiece farthestCorridor = null;
            float maxDistance = 0f;
            
            foreach (CorridorPiece corridor in loadedCorridors)
            {
                float distance = Vector3.Distance(playerTransform.position, corridor.transform.position);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    farthestCorridor = corridor;
                }
            }
            
            if (farthestCorridor != null)
            {
                UnloadCorridor(farthestCorridor);
            }
        }
        
        /// <summary>
        /// 이벤트 시스템 설정
        /// </summary>
        private void SetupEventSystem()
        {
            // 이벤트 가능한 복도들의 이벤트 활성화
            foreach (CorridorPiece corridor in eventEligibleCorridors)
            {
                if (corridor != null)
                {
                    corridor.SetEventEnabled(true);
                }
            }
        }
        
        /// <summary>
        /// 이벤트 시스템 업데이트
        /// </summary>
        private void UpdateEventSystem()
        {
            // 이벤트 처리는 CorridorPiece의 OnTriggerEnter에서 자동으로 처리됨
            // 여기서는 추가적인 이벤트 로직이 필요할 때만 구현
        }
        
        /// <summary>
        /// 현재 복도 반환
        /// </summary>
        public CorridorPiece GetCurrentCorridor()
        {
            return currentCorridor;
        }
        
        /// <summary>
        /// 이벤트 가능한 복도들 반환
        /// </summary>
        public List<CorridorPiece> GetEventEligibleCorridors()
        {
            return eventEligibleCorridors;
        }
        
        /// <summary>
        /// 복도 재생성
        /// </summary>
        public void RegenerateCorridors()
        {
            // 기존 복도 정리
            loadedCorridors.Clear();
            
            // 복도 재생성
            corridorGenerator.RegenerateCorridors();
            
            // 이벤트 가능한 복도들 다시 가져오기
            eventEligibleCorridors = corridorGenerator.GetEventEligibleCorridors();
            
            // 이벤트 시스템 재설정
            SetupEventSystem();
            
            Debug.Log("Corridors regenerated successfully");
        }
        
        /// <summary>
        /// 복도 시드 설정
        /// </summary>
        public void SetSeed(int seed)
        {
            corridorGenerator.SetSeed(seed);
        }
        
        /// <summary>
        /// 회전 트리거 감지
        /// </summary>
        private void CheckRotationTriggers()
        {
            if (playerController == null || playerController.IsRotating()) return;
            
            Vector3 playerPosition = playerTransform.position;
            
            // 로드된 복도들 중에서 회전 트리거 확인
            foreach (var corridor in loadedCorridors)
            {
                if (corridor == null || !corridor.HasRotationTrigger()) continue;
                
                // 플레이어가 회전 트리거 지점에 도달했는지 확인
                if (corridor.IsPlayerAtRotationTrigger(playerPosition, rotationTriggerRadius))
                {
                    // 회전 트리거 활성화
                    corridor.TriggerRotation();
                    
                    // 플레이어 회전 시작
                    float rotationAngle = corridor.GetRotationAngle();
                    playerController.StartRotation(rotationAngle, () => {
                        Debug.Log($"Player completed rotation for {corridor.GetCorridorType()}");
                    });
                    
                    Debug.Log($"Player rotation triggered at {corridor.GetCorridorType()} corridor");
                }
            }
        }
        
        /// <summary>
        /// 복도 통계 반환
        /// </summary>
        public CorridorStats GetCorridorStats()
        {
            return new CorridorStats
            {
                totalCorridors = corridorGenerator.GetEventEligibleCorridors().Count,
                loadedCorridors = loadedCorridors.Count,
                eventEligibleCorridors = eventEligibleCorridors.Count,
                currentCorridorType = currentCorridor?.GetCorridorType() ?? CorridorType.Start
            };
        }
    }
    
    /// <summary>
    /// 복도 통계 구조체
    /// </summary>
    [System.Serializable]
    public struct CorridorStats
    {
        public int totalCorridors;
        public int loadedCorridors;
        public int eventEligibleCorridors;
        public CorridorType currentCorridorType;
    }
}
