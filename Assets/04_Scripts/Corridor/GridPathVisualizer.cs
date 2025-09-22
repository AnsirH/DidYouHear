using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

namespace DidYouHear.Corridor
{
    /// <summary>
    /// 그리드 경로 시각화 도구
    /// 
    /// 복도 생성 전에 그리드 경로가 어떻게 생성되었는지 시각적으로 확인할 수 있는 도구입니다.
    /// UI Canvas를 통해 2D 그리드로 경로를 표시하며, 각 복도 타입별로 다른 색상으로 구분합니다.
    /// </summary>
    public class GridPathVisualizer : MonoBehaviour
    {
        [Header("Visualization Settings")]
        [SerializeField] private bool enableVisualization = true;
        [SerializeField] private Canvas visualizationCanvas;
        [SerializeField] private GameObject gridCellPrefab;
        [SerializeField] private Transform gridParent;
        [SerializeField] private float cellSize = 20f;
        [SerializeField] private float gridOffset = 10f;
        
        [Header("Color Settings")]
        [SerializeField] private Color startColor = new Color(0, 1, 0, 0.8f);      // 녹색 - 시작
        [SerializeField] private Color endColor = new Color(1, 0, 0, 0.8f);        // 빨간색 - 끝
        [SerializeField] private Color classroomColor = new Color(0, 0, 1, 0.8f);  // 파란색 - 교실
        [SerializeField] private Color classroomBathroomColor = new Color(0, 1, 1, 0.8f); // 청록색 - 교실&화장실
        [SerializeField] private Color leftCornerColor = new Color(1, 1, 0, 0.8f);  // 노란색 - 좌회전
        [SerializeField] private Color rightCornerColor = new Color(1, 0, 1, 0.8f); // 자홍색 - 우회전
        [SerializeField] private Color occupiedColor = new Color(0.5f, 0.5f, 0.5f, 0.3f); // 회색 - 점유됨
        [SerializeField] private Color emptyColor = new Color(1, 1, 1, 0.1f);       // 흰색 - 빈 공간
        
        [Header("Animation Settings")]
        [SerializeField] private bool animatePathGeneration = true;
        [SerializeField] private float animationDelay = 0.1f;
        [SerializeField] private AnimationCurve scaleAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        // 그리드 셀 관리
        private Dictionary<Vector2Int, GameObject> gridCells = new Dictionary<Vector2Int, GameObject>();
        private Dictionary<Vector2Int, CorridorType> pathData = new Dictionary<Vector2Int, CorridorType>();
        private List<CorridorGenerator.GridPath> currentPath = new List<CorridorGenerator.GridPath>();
        
        // 시각화 상태
        private bool isVisualizing = false;
        private int gridSize = 50;
        
        private void Awake()
        {
            // Canvas 자동 생성
            if (visualizationCanvas == null)
            {
                CreateVisualizationCanvas();
            }
            
            // Grid Parent 자동 생성
            if (gridParent == null)
            {
                CreateGridParent();
            }
        }
        
        private void Start()
        {
            if (enableVisualization)
            {
                InitializeGrid();
            }
        }
        
        /// <summary>
        /// 시각화 Canvas 생성
        /// </summary>
        private void CreateVisualizationCanvas()
        {
            GameObject canvasObj = new GameObject("GridPathVisualizationCanvas");
            canvasObj.transform.SetParent(transform);
            
            visualizationCanvas = canvasObj.AddComponent<Canvas>();
            visualizationCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            visualizationCanvas.sortingOrder = 100; // 다른 UI보다 위에 표시
            
            // Canvas Scaler 추가
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            // GraphicRaycaster 추가
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        /// <summary>
        /// 그리드 부모 오브젝트 생성
        /// </summary>
        private void CreateGridParent()
        {
            GameObject gridParentObj = new GameObject("GridParent");
            gridParentObj.transform.SetParent(visualizationCanvas.transform);
            
            RectTransform rectTransform = gridParentObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            gridParent = gridParentObj.transform;
        }
        
        /// <summary>
        /// 그리드 초기화
        /// </summary>
        private void InitializeGrid()
        {
            ClearGrid();
            
            // 그리드 셀 프리팹이 없으면 자동 생성
            if (gridCellPrefab == null)
            {
                gridCellPrefab = CreateDefaultGridCellPrefab();
            }
            
            // 빈 그리드 생성
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    Vector2Int gridPos = new Vector2Int(x, y);
                    CreateGridCell(gridPos, emptyColor);
                }
            }
        }
        
        /// <summary>
        /// 기본 그리드 셀 프리팹 생성
        /// </summary>
        private GameObject CreateDefaultGridCellPrefab()
        {
            GameObject cellPrefab = new GameObject("GridCell");
            
            // Image 컴포넌트 추가
            Image image = cellPrefab.AddComponent<Image>();
            image.color = Color.white;
            
            // RectTransform 설정
            RectTransform rectTransform = cellPrefab.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(cellSize, cellSize);
            
            return cellPrefab;
        }
        
        /// <summary>
        /// 그리드 셀 생성
        /// </summary>
        private void CreateGridCell(Vector2Int gridPos, Color color)
        {
            GameObject cellObj = Instantiate(gridCellPrefab, gridParent);
            cellObj.name = $"GridCell_{gridPos.x}_{gridPos.y}";
            
            // 위치 설정 (중앙 기준)
            RectTransform rectTransform = cellObj.GetComponent<RectTransform>();
            Vector2 screenPos = GridToScreenPosition(gridPos);
            rectTransform.anchoredPosition = screenPos;
            
            // 색상 설정
            Image image = cellObj.GetComponent<Image>();
            image.color = color;
            
            // 딕셔너리에 저장
            gridCells[gridPos] = cellObj;
        }
        
        /// <summary>
        /// 그리드 좌표를 스크린 좌표로 변환
        /// </summary>
        private Vector2 GridToScreenPosition(Vector2Int gridPos)
        {
            // 그리드 중앙을 화면 중앙에 배치
            float centerX = (gridSize - 1) * 0.5f;
            float centerY = (gridSize - 1) * 0.5f;
            
            float x = (gridPos.x - centerX) * cellSize;
            float y = (gridPos.y - centerY) * cellSize;
            
            return new Vector2(x, y);
        }
        
        /// <summary>
        /// 경로 시각화 시작
        /// </summary>
        public void VisualizePath(List<CorridorGenerator.GridPath> path, int gridSize)
        {
            if (!enableVisualization || isVisualizing) return;
            
            this.gridSize = gridSize;
            currentPath = new List<CorridorGenerator.GridPath>(path);
            
            StartCoroutine(AnimatePathVisualization());
        }
        
        /// <summary>
        /// 경로 시각화 애니메이션
        /// </summary>
        private IEnumerator AnimatePathVisualization()
        {
            isVisualizing = true;
            
            // 기존 그리드 정리
            ClearGrid();
            InitializeGrid();
            
            if (animatePathGeneration)
            {
                // 순차적으로 경로 표시
                for (int i = 0; i < currentPath.Count; i++)
                {
                    var pathPoint = currentPath[i];
                    UpdateGridCell(pathPoint.gridPosition, pathPoint.corridorType, true);
                    
                    yield return new WaitForSeconds(animationDelay);
                }
            }
            else
            {
                // 한 번에 모든 경로 표시
                foreach (var pathPoint in currentPath)
                {
                    UpdateGridCell(pathPoint.gridPosition, pathPoint.corridorType, false);
                }
            }
            
            isVisualizing = false;
            
            // 시각화 완료 후 정보 출력
            LogPathInformation();
        }
        
        /// <summary>
        /// 그리드 셀 업데이트
        /// </summary>
        private void UpdateGridCell(Vector2Int gridPos, CorridorType corridorType, bool animate = false)
        {
            if (!gridCells.ContainsKey(gridPos))
            {
                CreateGridCell(gridPos, GetColorForCorridorType(corridorType));
            }
            else
            {
                GameObject cellObj = gridCells[gridPos];
                Image image = cellObj.GetComponent<Image>();
                
                if (animate)
                {
                    StartCoroutine(AnimateCellUpdate(image, GetColorForCorridorType(corridorType)));
                }
                else
                {
                    image.color = GetColorForCorridorType(corridorType);
                }
            }
            
            // 경로 데이터 저장
            pathData[gridPos] = corridorType;
        }
        
        /// <summary>
        /// 셀 업데이트 애니메이션
        /// </summary>
        private IEnumerator AnimateCellUpdate(Image image, Color targetColor)
        {
            Color startColor = image.color;
            float duration = animationDelay * 0.5f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float curveValue = scaleAnimationCurve.Evaluate(progress);
                
                image.color = Color.Lerp(startColor, targetColor, curveValue);
                
                // 스케일 애니메이션
                RectTransform rectTransform = image.GetComponent<RectTransform>();
                float scale = Mathf.Lerp(1.2f, 1f, curveValue);
                rectTransform.localScale = Vector3.one * scale;
                
                yield return null;
            }
            
            image.color = targetColor;
            image.GetComponent<RectTransform>().localScale = Vector3.one;
        }
        
        /// <summary>
        /// 복도 타입에 따른 색상 반환
        /// </summary>
        private Color GetColorForCorridorType(CorridorType corridorType)
        {
            switch (corridorType)
            {
                case CorridorType.Start:
                    return startColor;
                case CorridorType.End:
                    return endColor;
                case CorridorType.Classroom:
                    return classroomColor;
                case CorridorType.ClassroomBathroom:
                    return classroomBathroomColor;
                case CorridorType.LeftCorner:
                    return leftCornerColor;
                case CorridorType.RightCorner:
                    return rightCornerColor;
                default:
                    return occupiedColor;
            }
        }
        
        /// <summary>
        /// 그리드 정리
        /// </summary>
        private void ClearGrid()
        {
            foreach (var cell in gridCells.Values)
            {
                if (cell != null)
                {
                    DestroyImmediate(cell);
                }
            }
            
            gridCells.Clear();
            pathData.Clear();
        }
        
        /// <summary>
        /// 경로 정보 로그 출력
        /// </summary>
        private void LogPathInformation()
        {
            Debug.Log("=== Grid Path Visualization Complete ===");
            Debug.Log($"Total Path Length: {currentPath.Count}");
            Debug.Log($"Grid Size: {gridSize}x{gridSize}");
            
            // 복도 타입별 통계
            var typeCounts = new Dictionary<CorridorType, int>();
            foreach (var pathPoint in currentPath)
            {
                if (typeCounts.ContainsKey(pathPoint.corridorType))
                {
                    typeCounts[pathPoint.corridorType]++;
                }
                else
                {
                    typeCounts[pathPoint.corridorType] = 1;
                }
            }
            
            foreach (var kvp in typeCounts)
            {
                Debug.Log($"{kvp.Key}: {kvp.Value} pieces");
            }
        }
        
        /// <summary>
        /// 시각화 활성화/비활성화
        /// </summary>
        public void SetVisualizationEnabled(bool enabled)
        {
            enableVisualization = enabled;
            
            if (visualizationCanvas != null)
            {
                visualizationCanvas.gameObject.SetActive(enabled);
            }
        }
        
        /// <summary>
        /// 시각화 토글
        /// </summary>
        [ContextMenu("Toggle Visualization")]
        public void ToggleVisualization()
        {
            SetVisualizationEnabled(!enableVisualization);
        }
        
        /// <summary>
        /// 그리드 크기 설정
        /// </summary>
        public void SetGridSize(int size)
        {
            gridSize = size;
            if (enableVisualization)
            {
                InitializeGrid();
            }
        }
        
        /// <summary>
        /// 셀 크기 설정
        /// </summary>
        public void SetCellSize(float size)
        {
            cellSize = size;
            if (enableVisualization)
            {
                InitializeGrid();
            }
        }
        
        private void OnDestroy()
        {
            ClearGrid();
        }
        
        // 에디터 전용 메서드들
        #if UNITY_EDITOR
        [ContextMenu("Test Visualization")]
        private void TestVisualization()
        {
            // 테스트용 경로 생성
            var testPath = new List<CorridorGenerator.GridPath>();
            testPath.Add(new CorridorGenerator.GridPath { gridPosition = new Vector2Int(25, 25), corridorType = CorridorType.Start });
            testPath.Add(new CorridorGenerator.GridPath { gridPosition = new Vector2Int(26, 25), corridorType = CorridorType.Classroom });
            testPath.Add(new CorridorGenerator.GridPath { gridPosition = new Vector2Int(27, 25), corridorType = CorridorType.RightCorner });
            testPath.Add(new CorridorGenerator.GridPath { gridPosition = new Vector2Int(27, 26), corridorType = CorridorType.ClassroomBathroom });
            testPath.Add(new CorridorGenerator.GridPath { gridPosition = new Vector2Int(27, 27), corridorType = CorridorType.End });
            
            VisualizePath(testPath, 50);
        }
        #endif
    }
}
