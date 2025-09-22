# 그리드 경로 시각화 가이드

## 📋 개요

복도 랜덤 생성 시스템에서 그리드 경로가 어떻게 생성되었는지 시각적으로 확인할 수 있는 도구입니다. Unity UI를 통해 2D 그리드로 경로를 표시하며, 각 복도 타입별로 다른 색상으로 구분합니다.

## 🎯 주요 기능

### 1. **실시간 경로 시각화**
- 그리드 기반 경로 생성 과정을 실시간으로 확인
- 각 복도 타입별 색상 구분
- 애니메이션을 통한 경로 생성 과정 표시

### 2. **시각적 구분**
- 🟢 **녹색**: 시작 복도 (Start)
- 🔴 **빨간색**: 끝 복도 (End)  
- 🔵 **파란색**: 교실 복도 (Classroom)
- 🔷 **청록색**: 교실&화장실 복도 (ClassroomBathroom)
- 🟡 **노란색**: 좌회전 코너 (LeftCorner)
- 🟣 **자홍색**: 우회전 코너 (RightCorner)
- ⚪ **회색**: 점유된 그리드 셀
- ⚪ **흰색**: 빈 그리드 셀

### 3. **애니메이션 효과**
- 순차적 경로 생성 애니메이션
- 셀 스케일 애니메이션
- 커스터마이징 가능한 애니메이션 속도

## 🚀 사용 방법

### 1. **기본 설정**

```csharp
// CorridorGenerator에 GridPathVisualizer 연결
[Header("Visualization Settings")]
public bool enablePathVisualization = true;
public GridPathVisualizer pathVisualizer;
```

### 2. **시각화 활성화/비활성화**

```csharp
// 런타임에서 시각화 토글
corridorGenerator.SetPathVisualizationEnabled(true/false);

// 또는 직접 접근
pathVisualizer.ToggleVisualization();
```

### 3. **설정 조정**

```csharp
// 그리드 크기 조정
pathVisualizer.SetGridSize(100); // 100x100 그리드

// 셀 크기 조정
pathVisualizer.SetCellSize(30f); // 더 큰 셀

// 애니메이션 속도 조정
pathVisualizer.animationDelay = 0.05f; // 더 빠른 애니메이션
```

## 🔧 Inspector 설정

### **GridPathVisualizer 컴포넌트**

| 설정 항목 | 설명 | 기본값 |
|-----------|------|--------|
| `Enable Visualization` | 시각화 활성화 여부 | true |
| `Cell Size` | 그리드 셀 크기 (픽셀) | 20 |
| `Grid Offset` | 그리드 간격 | 10 |
| `Animate Path Generation` | 경로 생성 애니메이션 | true |
| `Animation Delay` | 애니메이션 지연 시간 | 0.1초 |

### **색상 설정**

각 복도 타입별 색상을 Inspector에서 직접 조정할 수 있습니다:

- `Start Color`: 시작 복도 색상
- `End Color`: 끝 복도 색상  
- `Classroom Color`: 교실 복도 색상
- `Classroom Bathroom Color`: 교실&화장실 복도 색상
- `Left Corner Color`: 좌회전 코너 색상
- `Right Corner Color`: 우회전 코너 색상
- `Occupied Color`: 점유된 셀 색상
- `Empty Color`: 빈 셀 색상

## 📊 시각화 정보

### **콘솔 출력 정보**
```
=== Grid Path Visualization Complete ===
Total Path Length: 35
Grid Size: 50x50
Start: 1 pieces
End: 1 pieces
Classroom: 15 pieces
ClassroomBathroom: 12 pieces
LeftCorner: 3 pieces
RightCorner: 3 pieces
```

### **통계 정보**
- 전체 경로 길이
- 그리드 크기
- 복도 타입별 개수
- 코너 비율
- 이벤트 가능한 복도 수

## 🎮 게임플레이 통합

### **개발 중 사용**
```csharp
// 복도 생성 전에 경로 확인
corridorGenerator.GenerateCorridors();
// → 자동으로 2초간 경로 시각화 표시
// → 그 후 실제 복도 생성
```

### **런타임 디버깅**
```csharp
// 현재 경로 다시 시각화
var currentPath = corridorGenerator.GetGeneratedPath();
pathVisualizer.VisualizePath(currentPath, corridorGenerator.GetGridSize());
```

## 🔍 문제 해결

### **시각화가 나타나지 않는 경우**
1. `enablePathVisualization`이 true인지 확인
2. `pathVisualizer`가 할당되어 있는지 확인
3. Canvas가 올바르게 설정되어 있는지 확인

### **색상이 구분되지 않는 경우**
1. Inspector에서 색상 설정 확인
2. Alpha 값이 너무 낮지 않은지 확인 (0.8 권장)
3. 색상 대비가 충분한지 확인

### **애니메이션이 너무 빠른/느린 경우**
1. `animationDelay` 값 조정
2. `AnimationCurve` 설정 확인
3. 프레임레이트 영향 고려

## 💡 활용 팁

### **1. 개발 단계**
- 복도 생성 알고리즘 디버깅
- 경로 패턴 분석
- 코너 비율 조정 확인

### **2. 테스트 단계**
- 다양한 시드값으로 경로 패턴 확인
- 복도 길이별 분포 분석
- 이벤트 배치 최적화

### **3. 최적화**
- 불필요한 시각화는 비활성화
- 릴리즈 빌드에서는 시각화 비활성화
- 메모리 사용량 모니터링

## 🔮 확장 가능성

### **추가 기능 아이디어**
- 경로 히트맵 표시
- 플레이어 이동 경로 추적
- 이벤트 발생 지점 하이라이트
- 3D 월드 좌표 매핑
- 경로 내보내기/가져오기 기능

### **성능 최적화**
- 오브젝트 풀링 적용
- LOD 시스템 도입
- GPU 기반 렌더링
- 비동기 시각화 처리

## 📝 예제 코드

### **기본 사용법**
```csharp
public class CorridorTestManager : MonoBehaviour
{
    [SerializeField] private CorridorGenerator corridorGenerator;
    [SerializeField] private GridPathVisualizer pathVisualizer;
    
    private void Start()
    {
        // 복도 생성 및 시각화
        corridorGenerator.GenerateCorridors();
    }
    
    private void Update()
    {
        // R키로 시각화 토글
        if (Input.GetKeyDown(KeyCode.R))
        {
            pathVisualizer.ToggleVisualization();
        }
    }
}
```

### **고급 사용법**
```csharp
public class AdvancedCorridorAnalyzer : MonoBehaviour
{
    [SerializeField] private CorridorGenerator corridorGenerator;
    [SerializeField] private GridPathVisualizer pathVisualizer;
    
    public void AnalyzeMultipleSeeds(int[] seeds)
    {
        foreach (int seed in seeds)
        {
            corridorGenerator.SetSeed(seed);
            corridorGenerator.GenerateCorridors();
            
            // 경로 분석
            var path = corridorGenerator.GetGeneratedPath();
            AnalyzePathPattern(path);
            
            // 시각화로 확인
            pathVisualizer.VisualizePath(path, corridorGenerator.GetGridSize());
        }
    }
    
    private void AnalyzePathPattern(List<CorridorGenerator.GridPath> path)
    {
        // 경로 패턴 분석 로직
        Debug.Log($"Analyzing path with {path.Count} segments");
    }
}
```

이 시각화 도구를 통해 복도 생성 알고리즘을 더 쉽게 이해하고 디버깅할 수 있습니다! 🎯
