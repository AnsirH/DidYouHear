using UnityEngine;
using DidYouHear.Manager.Gameplay;

namespace DidYouHear.Corridor
{
    /// <summary>
    /// 복도 조각 타입 열거형
    /// </summary>
    public enum CorridorType
    {
        Start,              // 시작 복도
        End,                // 끝 복도
        Classroom,          // 교실 복도
        ClassroomBathroom,  // 교실&화장실 복도
        LeftCorner,         // 왼쪽 코너
        RightCorner         // 오른쪽 코너
    }
    
    /// <summary>
    /// 개별 복도 조각 컴포넌트
    /// </summary>
    public class CorridorPiece : MonoBehaviour
    {
        [Header("Corridor Settings")]
        [SerializeField] private CorridorType corridorType;
        [SerializeField] private bool isEventEnabled = false;
        [SerializeField] private float corridorLength = 8f;
        [SerializeField] private float corridorWidth = 4f;
        
        [Header("Event Settings")]
        [SerializeField] private Transform eventTriggerZone;
        
        [Header("Connection Points")]
        [SerializeField] private Transform startPoint;  // 시작점 (이전 복도와 연결)
        [SerializeField] private Transform endPoint;    // 끝점 (다음 복도와 연결)
        
        [Header("Rotation Settings")]
        [SerializeField] private Transform rotationTrigger;  // 회전 트리거 지점
        [SerializeField] private float rotationAngle = 0f;   // 회전할 각도
        
        [Header("Visual Settings")]
        [SerializeField] private GameObject[] classroomDoors;
        [SerializeField] private GameObject[] bathroomDoors;
        [SerializeField] private GameObject[] windows;
        [SerializeField] private GameObject exitDoor;
        
        private bool isInitialized = false;
        private bool hasEventTriggered = false;
        private bool hasRotationTriggered = false;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        
        private void Awake()
        {
            // 이벤트 트리거 존 설정
            if (eventTriggerZone == null)
            {
                CreateEventTriggerZone();
            }
        }
        
        private void Start()
        {
            if (isInitialized)
            {
                SetupCorridor();
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && isEventEnabled && !hasEventTriggered)
            {
                TriggerEvent();
            }
        }
        
        /// <summary>
        /// 복도 조각 초기화
        /// </summary>
        public void Initialize(CorridorType type, Vector3 position, Quaternion rotation)
        {
            corridorType = type;
            originalPosition = position;
            originalRotation = rotation;
            
            transform.position = position;
            transform.rotation = rotation;
            
            isInitialized = true;
            SetupCorridor();
        }
        
        /// <summary>
        /// 복도 설정
        /// </summary>
        private void SetupCorridor()
        {
            // 복도 타입에 따른 시각적 요소 설정
            SetupVisualElements();
            
            // 이벤트 설정
            SetupEventSystem();
            
            // 물리적 특성 설정
            SetupPhysics();
        }
        
        /// <summary>
        /// 시각적 요소 설정
        /// </summary>
        private void SetupVisualElements()
        {
            switch (corridorType)
            {
                case CorridorType.Start:
                    SetupStartCorridor();
                    break;
                case CorridorType.End:
                    SetupEndCorridor();
                    break;
                case CorridorType.Classroom:
                    SetupClassroomCorridor();
                    break;
                case CorridorType.ClassroomBathroom:
                    SetupClassroomBathroomCorridor();
                    break;
                case CorridorType.LeftCorner:
                case CorridorType.RightCorner:
                    SetupCornerCorridor();
                    break;
            }
        }
        
        /// <summary>
        /// 시작 복도 설정
        /// </summary>
        private void SetupStartCorridor()
        {
            // 왼쪽에 교실 문 2개, 뒤가 막혀있는 구조
            if (classroomDoors != null)
            {
                foreach (GameObject door in classroomDoors)
                {
                    if (door != null)
                    {
                        door.SetActive(true);
                    }
                }
            }
            
            // 뒤쪽 벽 활성화 (시작 지점이므로)
            // 이는 프리팹에서 미리 설정되어야 함
        }
        
        /// <summary>
        /// 끝 복도 설정
        /// </summary>
        private void SetupEndCorridor()
        {
            // 왼쪽에 밝은 빛이 나오는 탈출구
            if (exitDoor != null)
            {
                exitDoor.SetActive(true);
            }
        }
        
        /// <summary>
        /// 교실 복도 설정
        /// </summary>
        private void SetupClassroomCorridor()
        {
            // 왼쪽에 교실 문 2개
            if (classroomDoors != null)
            {
                foreach (GameObject door in classroomDoors)
                {
                    if (door != null)
                    {
                        door.SetActive(true);
                    }
                }
            }
        }
        
        /// <summary>
        /// 교실&화장실 복도 설정
        /// </summary>
        private void SetupClassroomBathroomCorridor()
        {
            // 왼쪽은 교실 문 2개, 오른쪽은 화장실 문 2개
            if (classroomDoors != null)
            {
                foreach (GameObject door in classroomDoors)
                {
                    if (door != null)
                    {
                        door.SetActive(true);
                    }
                }
            }
            
            if (bathroomDoors != null)
            {
                foreach (GameObject door in bathroomDoors)
                {
                    if (door != null)
                    {
                        door.SetActive(true);
                    }
                }
            }
        }
        
        /// <summary>
        /// 코너 복도 설정
        /// </summary>
        private void SetupCornerCorridor()
        {
            // 회전 지점이 명확히 보이는 구조
            // 이는 프리팹에서 미리 설정되어야 함
        }
        
        /// <summary>
        /// 이벤트 시스템 설정
        /// </summary>
        private void SetupEventSystem()
        {
            // 이벤트 트리거 존 활성화/비활성화
            if (eventTriggerZone != null)
            {
                bool canHaveEvent = CanHaveEvent();
                eventTriggerZone.gameObject.SetActive(canHaveEvent && isEventEnabled);
            }
        }
        
        /// <summary>
        /// 물리적 특성 설정
        /// </summary>
        private void SetupPhysics()
        {
            // 콜라이더 설정
            BoxCollider collider = GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<BoxCollider>();
            }
            
            collider.size = new Vector3(corridorWidth, 3f, corridorLength);
            collider.isTrigger = true;
        }
        
        /// <summary>
        /// 이벤트 트리거 존 생성
        /// </summary>
        private void CreateEventTriggerZone()
        {
            GameObject triggerObj = new GameObject("EventTriggerZone");
            triggerObj.transform.SetParent(transform);
            triggerObj.transform.localPosition = Vector3.zero;
            triggerObj.transform.localRotation = Quaternion.identity;
            
            BoxCollider triggerCollider = triggerObj.AddComponent<BoxCollider>();
            triggerCollider.size = new Vector3(corridorWidth, 3f, corridorLength);
            triggerCollider.isTrigger = true;
            
            eventTriggerZone = triggerObj.transform;
        }
        
        /// <summary>
        /// 이벤트 트리거
        /// </summary>
        private void TriggerEvent()
        {
            if (hasEventTriggered || !isEventEnabled) return;
            
            hasEventTriggered = true;
            
            // EventManager에 복도 타입과 함께 이벤트 트리거 요청
            if (EventManager.Instance != null)
            {
                EventManager.Instance.TriggerEnvironmentalEventForCorridor(corridorType);
            }
            
            Debug.Log($"Event triggered in {corridorType} corridor");
        }
        
        /// <summary>
        /// 이벤트 활성화 설정
        /// </summary>
        public void SetEventEnabled(bool enabled)
        {
            isEventEnabled = enabled;
            
            // 이벤트 트리거 존 활성화/비활성화
            if (eventTriggerZone != null)
            {
                bool canHaveEvent = CanHaveEvent();
                eventTriggerZone.gameObject.SetActive(canHaveEvent && enabled);
            }
        }
        
        /// <summary>
        /// 복도 타입 반환
        /// </summary>
        public CorridorType GetCorridorType()
        {
            return corridorType;
        }
        
        /// <summary>
        /// 이벤트 활성화 여부 반환
        /// </summary>
        public bool IsEventEnabled()
        {
            return isEventEnabled;
        }
        
        /// <summary>
        /// 이벤트 트리거 여부 반환
        /// </summary>
        public bool HasEventTriggered()
        {
            return hasEventTriggered;
        }
        
        /// <summary>
        /// 복도 길이 반환
        /// </summary>
        public float GetCorridorLength()
        {
            return corridorLength;
        }
        
        /// <summary>
        /// 복도 폭 반환
        /// </summary>
        public float GetCorridorWidth()
        {
            return corridorWidth;
        }
        
        /// <summary>
        /// 코너 타입인지 확인
        /// </summary>
        public bool IsCornerType()
        {
            return corridorType == CorridorType.LeftCorner || corridorType == CorridorType.RightCorner;
        }
        
        /// <summary>
        /// 이벤트 발생 가능한 타입인지 확인
        /// </summary>
        public bool CanHaveEvent()
        {
            return corridorType == CorridorType.Classroom || corridorType == CorridorType.ClassroomBathroom;
        }
        
        /// <summary>
        /// 복도 리셋
        /// </summary>
        public void ResetCorridor()
        {
            hasEventTriggered = false;
            transform.position = originalPosition;
            transform.rotation = originalRotation;
            
            // 이벤트 트리거 존 재활성화
            if (eventTriggerZone != null)
            {
                bool canHaveEvent = CanHaveEvent();
                eventTriggerZone.gameObject.SetActive(canHaveEvent && isEventEnabled);
            }
        }
        
        /// <summary>
        /// 시작점 반환
        /// </summary>
        public Transform GetStartPoint()
        {
            return startPoint;
        }
        
        /// <summary>
        /// 끝점 반환
        /// </summary>
        public Transform GetEndPoint()
        {
            return endPoint;
        }
        
        /// <summary>
        /// 시작점이 있는지 확인
        /// </summary>
        public bool HasStartPoint()
        {
            return startPoint != null;
        }
        
        /// <summary>
        /// 끝점이 있는지 확인
        /// </summary>
        public bool HasEndPoint()
        {
            return endPoint != null;
        }
        
        /// <summary>
        /// 시작점 위치 반환
        /// </summary>
        public Vector3 GetStartPosition()
        {
            return startPoint != null ? startPoint.position : transform.position;
        }
        
        /// <summary>
        /// 끝점 위치 반환
        /// </summary>
        public Vector3 GetEndPosition()
        {
            return endPoint != null ? endPoint.position : transform.position;
        }
        
        /// <summary>
        /// 시작점 방향 반환
        /// </summary>
        public Vector3 GetStartDirection()
        {
            return startPoint != null ? startPoint.forward : transform.forward;
        }
        
        /// <summary>
        /// 끝점 방향 반환
        /// </summary>
        public Vector3 GetEndDirection()
        {
            return endPoint != null ? endPoint.forward : transform.forward;
        }
        
        /// <summary>
        /// 회전 트리거가 있는지 확인
        /// </summary>
        public bool HasRotationTrigger()
        {
            return rotationTrigger != null && IsCornerType();
        }
        
        /// <summary>
        /// 회전 각도 반환
        /// </summary>
        public float GetRotationAngle()
        {
            return rotationAngle;
        }
        
        /// <summary>
        /// 회전 트리거 위치 반환
        /// </summary>
        public Vector3 GetRotationTriggerPosition()
        {
            return rotationTrigger != null ? rotationTrigger.position : transform.position;
        }
        
        /// <summary>
        /// 회전 트리거 반경 확인
        /// </summary>
        public bool IsPlayerAtRotationTrigger(Vector3 playerPosition, float triggerRadius = 1f)
        {
            if (!HasRotationTrigger()) return false;
            
            float distance = Vector3.Distance(playerPosition, GetRotationTriggerPosition());
            return distance <= triggerRadius;
        }
        
        /// <summary>
        /// 회전 트리거 활성화
        /// </summary>
        public void TriggerRotation()
        {
            if (!HasRotationTrigger() || hasRotationTriggered) return;
            
            hasRotationTriggered = true;
            Debug.Log($"Rotation triggered for {corridorType} corridor - Angle: {rotationAngle}°");
        }
        
        /// <summary>
        /// 회전 트리거 리셋
        /// </summary>
        public void ResetRotationTrigger()
        {
            hasRotationTriggered = false;
        }
    }
}
