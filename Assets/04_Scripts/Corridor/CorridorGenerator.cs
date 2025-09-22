using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using DidYouHear.Manager.Gameplay;

namespace DidYouHear.Corridor
{
    /// <summary>
    /// 복도 랜덤 생성 및 관리 시스템
    /// </summary>
    public class CorridorGenerator : MonoBehaviour
    {
        [Header("Generation Settings")]
        [SerializeField] private int minCorridorCount = 30;
        [SerializeField] private int maxCorridorCount = 40;
        [SerializeField] private float cornerRatio = 0.4f; // 40% 코너형
        [SerializeField] private float eventProbability = 0.15f; // 15% 이벤트 확률
        
        [Header("Grid Settings")]
        [SerializeField] private int gridSize = 50; // 50x50 그리드
        
        [Header("Corridor Prefabs")]
        [SerializeField] private GameObject straightClassroomPrefab;
        [SerializeField] private GameObject straightClassroomBathroomPrefab;
        [SerializeField] private GameObject startCorridorPrefab;
        [SerializeField] private GameObject endCorridorPrefab;
        [SerializeField] private GameObject leftCornerPrefab;
        [SerializeField] private GameObject rightCornerPrefab;
        
        [Header("Generation Settings")]
        [SerializeField] private int seed = 0;
        
        private List<CorridorPiece> generatedCorridors = new List<CorridorPiece>();
        private int currentCorridorIndex = 0;
        private bool isGenerating = false;
        
        // 이벤트 발생 가능한 복도 조각들
        private List<CorridorPiece> eventEligibleCorridors = new List<CorridorPiece>();
        
        // 그리드 기반 경로 생성
        private bool[,] gridOccupied;
        private List<GridPath> generatedPath = new List<GridPath>();
        
        /// <summary>
        /// 그리드 경로 구조체
        /// </summary>
        [System.Serializable]
        public struct GridPath
        {
            public Vector2Int gridPosition;
            public CorridorType corridorType;
        }
        
        private void Awake()
        {
            // 시드 설정
            if (seed == 0)
            {
                seed = System.DateTime.Now.GetHashCode();
            }
            Random.InitState(seed);
        }
        
        private void Start()
        {
            GenerateCorridors();
        }
        
        private void Update()
        {
        }
        
        /// <summary>
        /// 복도 생성
        /// </summary>
        public void GenerateCorridors()
        {
            if (isGenerating) return;
            
            StartCoroutine(GenerateCorridorsCoroutine());
        }
        
        /// <summary>
        /// 이벤트 발생 가능한 복도들 반환
        /// </summary>
        public List<CorridorPiece> GetEventEligibleCorridors()
        {
            return eventEligibleCorridors;
        }

        /// <summary>
        /// 시드 설정
        /// </summary>
        public void SetSeed(int newSeed)
        {
            seed = newSeed;
            Random.InitState(seed);
        }
        
        /// <summary>
        /// 복도 재생성
        /// </summary>
        public void RegenerateCorridors()
        {
            GenerateCorridors();
        }
        
        /// <summary>
        /// 복도 생성 코루틴
        /// </summary>
        private IEnumerator GenerateCorridorsCoroutine()
        {
            isGenerating = true;
            
            // 기존 복도 정리
            ClearExistingCorridors();
            
            // 그리드 초기화
            InitializeGrid();
            
            // 복도 개수 결정
            int corridorCount = Random.Range(minCorridorCount, maxCorridorCount + 1);
            
            // 그리드 기반 경로 생성
            if (!GenerateGridPath(corridorCount))
            {
                Debug.LogError("Failed to generate grid path");
                yield break;
            }
            
            // 생성된 경로를 바탕으로 복도 생성
            yield return StartCoroutine(CreateCorridorsFromPath());
            
            // 이벤트 배치
            DistributeEvents();
            
            isGenerating = false;
            
            Debug.Log($"Generated {generatedPath.Count} corridors with {eventEligibleCorridors.Count} event-eligible pieces");
        }
        
        /// <summary>
        /// 기존 복도 정리
        /// </summary>
        private void ClearExistingCorridors()
        {
            foreach (CorridorPiece corridor in generatedCorridors)
            {
                if (corridor != null)
                {
                    Destroy(corridor.gameObject);
                }
            }
            
            generatedCorridors.Clear();
            eventEligibleCorridors.Clear();
        }
        
        /// <summary>
        /// 그리드 초기화
        /// </summary>
        private void InitializeGrid()
        {
            gridOccupied = new bool[gridSize, gridSize];
            generatedPath.Clear();
        }
        
        /// <summary>
        /// 그리드 기반 경로 생성
        /// </summary>
        private bool GenerateGridPath(int targetCount)
        {
            // 시작점 설정 (그리드 중앙)
            Vector2Int startPos = new Vector2Int(gridSize / 2, gridSize / 2);
            Vector2Int currentPos = startPos;
            Vector2Int currentDir = Vector2Int.right; // 오른쪽으로 시작
            
            // 시작 복도 추가
            AddGridPath(startPos, CorridorType.Start);
            
            int generatedCount = 1;
            int maxAttempts = targetCount * 3; // 최대 시도 횟수
            int attempts = 0;
            
            while (generatedCount < targetCount - 1 && attempts < maxAttempts)
            {
                attempts++;
                
                // 다음 방향 결정
                Vector2Int nextDir = DetermineNextDirection(currentDir);
                Vector2Int nextPos = currentPos + nextDir;
                
                // 그리드 경계 및 충돌 검사
                if (IsValidGridPosition(nextPos) && !gridOccupied[nextPos.x, nextPos.y])
                {
                    // 방향에 따른 복도 타입 결정
                    CorridorType corridorType = DetermineCorridorTypeFromDirection(currentDir, nextDir);
                    
                    // 경로 추가
                    AddGridPath(nextPos, corridorType);
                    
                    // 추가된 위치의 좌, 우 위치에 그리드 체크
                    Vector2Int leftPos = currentPos + GetRelativeLeftDirection(currentDir);
                    Vector2Int rightPos = currentPos + GetRelativeRightDirection(currentDir);
                    gridOccupied[leftPos.x, leftPos.y] = true;
                    gridOccupied[rightPos.x, rightPos.y] = true;

                    currentPos = nextPos;
                    currentDir = nextDir;
                    generatedCount++;
                }
                else
                {
                    // 다른 방향 시도
                    Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                    bool foundValidDirection = false;
                    
                    foreach (Vector2Int dir in directions)
                    {
                        Vector2Int testPos = currentPos + dir;
                        if (IsValidGridPosition(testPos) && !gridOccupied[testPos.x, testPos.y])
                        {
                            nextDir = dir;
                            nextPos = testPos;
                            foundValidDirection = true;
                            break;
                        }
                    }
                    
                    if (foundValidDirection)
                    {
                        // 방향에 따른 복도 타입 결정
                        CorridorType corridorType = DetermineCorridorTypeFromDirection(currentDir, nextDir);
                        AddGridPath(nextPos, corridorType);
                        
                        // 추가된 위치의 좌, 우 위치에 그리드 체크
                        Vector2Int leftPos = currentPos + GetRelativeLeftDirection(currentDir);
                        Vector2Int rightPos = currentPos + GetRelativeRightDirection(currentDir);
                        gridOccupied[leftPos.x, leftPos.y] = true;
                        gridOccupied[rightPos.x, rightPos.y] = true;
                        
                        currentPos = nextPos;
                        currentDir = nextDir;
                        generatedCount++;
                    }
                    else
                    {
                        // 백트래킹: 이전 위치로 돌아가기
                        if (generatedPath.Count > 1)
                        {
                            generatedPath.RemoveAt(generatedPath.Count - 1);
                            generatedCount--;
                            
                            var prevPath = generatedPath[generatedPath.Count - 1];
                            currentPos = prevPath.gridPosition;
                            // 이전 방향을 계산
                            if (generatedPath.Count > 1)
                            {
                                var prevPrevPath = generatedPath[generatedPath.Count - 2];
                                currentDir = currentPos - prevPrevPath.gridPosition;
                            }
                        }
                        else
                        {
                            break; // 더 이상 백트래킹할 수 없음
                        }
                    }
                }
            }
            
            // 끝 복도 추가
            if (generatedCount > 0)
            {
                AddGridPath(currentPos + currentDir, CorridorType.End);
            }
            
            return generatedCount >= targetCount - 1;
        }
        
        /// <summary>
        /// 그리드 경로 추가
        /// </summary>
        private void AddGridPath(Vector2Int gridPos, CorridorType corridorType)
        {
            var path = new GridPath
            {
                gridPosition = gridPos,
                corridorType = corridorType
            };
            
            generatedPath.Add(path);
            gridOccupied[gridPos.x, gridPos.y] = true;
        }
        
        /// <summary>
        /// 유효한 그리드 위치인지 확인
        /// </summary>
        private bool IsValidGridPosition(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < gridSize && pos.y >= 0 && pos.y < gridSize;
        }
        
        /// <summary>
        /// 다음 방향 결정
        /// </summary>
        private Vector2Int DetermineNextDirection(Vector2Int currentDir)
        {
            // 코너 확률에 따라 회전 결정
            if (Random.value < cornerRatio)
            {
                // 좌회전 또는 우회전
                if (Random.value < 0.5f)
                {
                    return GetRelativeLeftDirection(currentDir);
                }
                else
                {
                    return GetRelativeRightDirection(currentDir);
                }
            }
            else
            {
                // 직진
                return currentDir;
            }
        }
        
        /// <summary>
        /// 좌회전 방향 반환
        /// </summary>
        private Vector2Int GetRelativeLeftDirection(Vector2Int currentDirection)
        {
            // 좌회전: (1,0) -> (0,1), (0,1) -> (-1,0), (-1,0) -> (0,-1), (0,-1) -> (1,0)
            return new Vector2Int(currentDirection.y, -currentDirection.x);
        }
        
        /// <summary>
        /// 우회전 방향 반환
        /// </summary>
        private Vector2Int GetRelativeRightDirection(Vector2Int currentDirection)
        {
            // 우회전: (1,0) -> (0,-1), (0,-1) -> (-1,0), (-1,0) -> (0,1), (0,1) -> (1,0)
            return new Vector2Int(-currentDirection.y, currentDirection.x);
        }

        /// <summary>
        /// 방향에 따른 복도 타입 결정
        /// </summary>
        private CorridorType DetermineCorridorTypeFromDirection(Vector2Int currentDirection, Vector2Int nextDirection)
        {
            if (currentDirection == nextDirection)
            {
                // 직진 방향 - 직진 복도 선택
                return Random.Range(0f, 1f) < 0.5f ? CorridorType.Classroom : CorridorType.ClassroomBathroom;
            }
            else if (new Vector2Int(currentDirection.y, -currentDirection.x) == nextDirection) 
            {
                // 우회전 방향
                return CorridorType.RightCorner;
            }
            else if (new Vector2Int(-currentDirection.y, currentDirection.x) == nextDirection)
            {
                // 좌회전 방향
                return CorridorType.LeftCorner;
            }
            else
            {
                return CorridorType.End;
            }
        }
                
        /// <summary>
        /// 좌회전인지 판단
        /// </summary>
        private bool IsLeftTurn(Vector2Int from, Vector2Int to)
        {
            // 2D 벡터의 외적을 사용하여 좌회전 판단
            // 외적이 양수이면 좌회전, 음수이면 우회전
            int crossProduct = from.x * to.y - from.y * to.x;
            return crossProduct > 0;
        }
        
        /// <summary>
        /// 경로를 바탕으로 복도 생성
        /// </summary>
        private IEnumerator CreateCorridorsFromPath()
        {            
            // 시작 복도 생성
            CorridorPiece currentCorridorPiece = CreateCorridorPiece(CorridorType.Start, Vector3.zero, Quaternion.identity);
            generatedCorridors.Add(currentCorridorPiece);

            for (int i = 1; i < generatedPath.Count; i++)
            {
                var path = generatedPath[i];
                
                // 복도 생성
                Vector3 newPosition = CalculateNextPosition(currentCorridorPiece, path.corridorType);
                Quaternion newRotation = CalculateNextRotation(currentCorridorPiece, path.corridorType);
                
                CorridorPiece newCorridorPiece = CreateCorridorPiece(path.corridorType, newPosition, newRotation);
                
                if (newCorridorPiece != null)
                {
                    generatedCorridors.Add(newCorridorPiece);
                    
                    // 이벤트 발생 가능한 복도인지 확인
                    if (IsEventEligible(path.corridorType))
                    {
                        eventEligibleCorridors.Add(newCorridorPiece);
                    }
                }

                currentCorridorPiece = newCorridorPiece;

                // 프레임 분할
                if (i % 5 == 0)
                {
                    yield return null;
                }
            }
        }
        
        /// <summary>
        /// 복도 조각 생성
        /// </summary>
        private CorridorPiece CreateCorridorPiece(CorridorType type, Vector3 position, Quaternion rotation)
        {
            GameObject prefab = GetCorridorPrefab(type);
            if (prefab == null)
            {
                Debug.LogError($"No prefab found for corridor type: {type}");
                return null;
            }
            
            CorridorPiece newCorridor = Instantiate(prefab, position, rotation).GetComponent<CorridorPiece>();
            newCorridor.name = $"{type}_Corridor_{generatedCorridors.Count}";            
            
            position += newCorridor.transform.position - newCorridor.GetStartPosition();

            newCorridor.Initialize(type, position, rotation);
            
            // 코너 복도의 경우 회전 각도 설정
            if (type == CorridorType.LeftCorner || type == CorridorType.RightCorner)
            {
                SetCorridorRotationAngle(newCorridor, type, rotation);
            }
            
            return newCorridor;
        }
        
        /// <summary>
        /// 코너 복도의 회전 각도 설정
        /// </summary>
        private void SetCorridorRotationAngle(CorridorPiece corridor, CorridorType type, Quaternion rotation)
        {
            // 현재 복도의 방향에서 다음 방향으로의 회전 각도 계산
            float rotationAngle = 0f;
            
            if (type == CorridorType.LeftCorner)
            {
                rotationAngle = 90f; // 좌회전
            }
            else if (type == CorridorType.RightCorner)
            {
                rotationAngle = -90f; // 우회전
            }
            
            // CorridorPiece의 회전 각도 설정 (리플렉션 사용)
            var rotationAngleField = typeof(CorridorPiece).GetField("rotationAngle", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            rotationAngleField?.SetValue(corridor, rotationAngle);
            
            Debug.Log($"Set rotation angle for {type}: {rotationAngle}°");
        }
        
        /// <summary>
        /// 복도 타입에 따른 프리팹 반환
        /// </summary>
        private GameObject GetCorridorPrefab(CorridorType type)
        {
            switch (type)
            {
                case CorridorType.Start:
                    return startCorridorPrefab;
                case CorridorType.End:
                    return endCorridorPrefab;
                case CorridorType.Classroom:
                    return straightClassroomPrefab;
                case CorridorType.ClassroomBathroom:
                    return straightClassroomBathroomPrefab;
                case CorridorType.LeftCorner:
                    return leftCornerPrefab;
                case CorridorType.RightCorner:
                    return rightCornerPrefab;
                default:
                    return straightClassroomPrefab;
            }
        }
        
        /// <summary>
        /// 다음 위치 계산 (연결점 기반)
        /// </summary>
        private Vector3 CalculateNextPosition(CorridorPiece previousCorridor, CorridorType type)
        {
            if (previousCorridor == null || !previousCorridor.HasEndPoint())
            {
                // 이전 복도가 없거나 끝점이 없는 경우 (시작 복도)
                return Vector3.zero;
            }
            
            // 이전 복도의 끝점 위치와 방향을 기준으로 계산
            Vector3 endPosition = previousCorridor.GetEndPosition();
            return endPosition;
        }
        
        /// <summary>
        /// 다음 회전 계산 (연결점 기반)
        /// </summary>
        private Quaternion CalculateNextRotation(CorridorPiece previousCorridor, CorridorType type)
        {
            if (previousCorridor == null || !previousCorridor.HasEndPoint())
            {
                // 이전 복도가 없거나 끝점이 없는 경우
                return Quaternion.identity;
            }
            
            Vector3 endDirection = previousCorridor.GetEndDirection();
            Quaternion baseRotation = Quaternion.LookRotation(endDirection);
            
            return baseRotation;
        }
        
        /// <summary>
        /// 이벤트 발생 가능한 복도인지 확인
        /// </summary>
        private bool IsEventEligible(CorridorType type)
        {
            return type == CorridorType.Classroom || type == CorridorType.ClassroomBathroom;
        }
        
        /// <summary>
        /// 이벤트 배치
        /// </summary>
        private void DistributeEvents()
        {
            foreach (CorridorPiece corridor in eventEligibleCorridors)
            {
                if (Random.value < eventProbability)
                {
                    corridor.SetEventEnabled(true);
                }
            }
        }
        
        /// <summary>
        /// 복도 분포 정보 출력 (디버그용)
        /// </summary>
        public void LogCorridorDistribution()
        {
            if (generatedPath.Count < 3) 
            {
                Debug.Log("Not enough corridors to analyze distribution.");
                return;
            }
            
            int totalCorridors = generatedPath.Count - 2; // 시작과 끝 제외
            int cornerCount = 0;
            int straightCount = 0;
            
            for (int i = 1; i < generatedPath.Count - 1; i++) // 시작과 끝 제외
            {
                var corridorType = generatedPath[i].corridorType;
                if (corridorType == CorridorType.LeftCorner || corridorType == CorridorType.RightCorner)
                {
                    cornerCount++;
                }
                else if (corridorType == CorridorType.Classroom || corridorType == CorridorType.ClassroomBathroom)
                {
                    straightCount++;
                }
            }
            
            float cornerRatio = totalCorridors > 0 ? (float)cornerCount / totalCorridors : 0f;
            float straightRatio = totalCorridors > 0 ? (float)straightCount / totalCorridors : 0f;
            
            Debug.Log($"=== Corridor Distribution ===");
            Debug.Log($"Total Corridors: {totalCorridors}");
            Debug.Log($"Corner Count: {cornerCount} ({cornerRatio:P1})");
            Debug.Log($"Straight Count: {straightCount} ({straightRatio:P1})");
            Debug.Log($"Target Corner Ratio: {cornerRatio:P1}");
            Debug.Log($"Event-Eligible Corridors: {eventEligibleCorridors.Count}");
        }
    }
}
