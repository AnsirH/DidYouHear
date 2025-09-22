# 매니저 아키텍처 분석 보고서

## 📋 현재 매니저 구조

### 매니저 목록
- **GameManager** (핵심 매니저, 싱글톤)
- **InputManager** (싱글톤)
- **AudioManager** (싱글톤, IManager 구현)
- **EventManager** (싱글톤)
- **GhostManager** (싱글톤)
- **CorridorManager** (일반 MonoBehaviour)
- **UIManager** (일반 MonoBehaviour)

### 현재 초기화 순서
```
1. GameManager.Awake() → InitializeGame()
2. GameManager.Start() → InitializeManagers()
3. 각 매니저의 Awake/Start 실행
```

## 🚨 주요 설계 문제점

### 1. 초기화 순서의 불일치
- **문제**: GameManager가 Start()에서 매니저들을 찾지만, 다른 매니저들은 Awake()에서 이미 초기화됨
- **영향**: 초기화 타이밍 불일치로 인한 NullReferenceException 위험

### 2. IManager 인터페이스 구현의 불일치
- **구현됨**: AudioManager만 IManager 구현
- **미구현**: EventManager, GhostManager, CorridorManager, UIManager
- **영향**: 통일된 매니저 관리 불가능

### 3. 의존성 주입의 혼재
- **직접 참조**: FindObjectOfType 사용
- **수동 주입**: Initialize() 메서드로 참조 전달
- **영향**: 코드 일관성 부족, 유지보수 어려움

### 4. 싱글톤 패턴의 일관성 부족
- **싱글톤**: InputManager, AudioManager, EventManager, GhostManager
- **일반**: CorridorManager, UIManager
- **영향**: 접근 방식의 불일치

### 5. 순환 의존성 위험
```
GameManager → EventManager → GameManager
EventManager → GhostManager → EventManager
```

### 6. 씬별 매니저 관리 부재
- **문제**: 모든 매니저가 DontDestroyOnLoad로 유지되어 메모리 낭비
- **문제**: 씬별로 필요한 매니저만 활성화하는 체계 부재
- **문제**: 씬 전환 시 매니저 초기화 순서 불안정
- **영향**: 성능 저하, 메모리 누수, 씬 전환 시 오류 발생 가능

## ✅ 해결된 설계 문제점

### 1. ✅ 초기화 순서 통일
- **해결**: ManagerInitializer로 우선순위 기반 초기화 시스템 구축
- **결과**: 모든 매니저가 안전한 순서로 초기화됨

### 2. ✅ IManager 인터페이스 완전 구현
- **구현 완료**: GameManager, InputManager, AudioManager, EventManager, GhostManager, StonePool
- **결과**: 통일된 매니저 관리 시스템 구축

### 3. ✅ 의존성 주입 시스템 구축
- **해결**: DependencyContainer로 중앙화된 의존성 관리
- **결과**: 코드 일관성 확보, 유지보수성 향상

### 4. ✅ 싱글톤 패턴 일관성 확보
- **해결**: 모든 매니저가 IManager 인터페이스로 통일된 접근 방식
- **결과**: 일관된 매니저 접근 패턴

### 5. ✅ 순환 의존성 해결
- **해결**: ManagerEvents를 통한 이벤트 기반 통신
- **결과**: 느슨한 결합으로 순환 의존성 제거

### 6. ✅ 씬별 매니저 관리 시스템 구축
- **해결**: SceneManagerController로 씬별 매니저 활성화 관리
- **결과**: 메모리 효율성 향상, 성능 최적화

## 📊 현재 매니저 관계도

```
GameManager (Singleton)
├── PlayerController (참조)
├── CorridorManager (참조) → EventManager (참조)
├── EventManager (Singleton) → GameManager (참조)
├── AudioManager (Singleton, IManager)
├── GhostManager (Singleton) → EventManager (참조)
├── StonePool (참조)
└── CameraController (참조)

InputManager (Singleton) → GameManager (참조)
UIManager → GameManager (참조)
```

## 🔧 개선 방안

### 1. 통일된 IManager 인터페이스
```csharp
public interface IManager
{
    void Initialize();
    void Reset();
    void SetActive(bool active);
    string GetStatus();
    bool IsInitialized();
    int GetPriority(); // 초기화 우선순위
    ManagerType GetManagerType();
    ManagerScope GetManagerScope(); // 씬별 관리 범위
}

public enum ManagerType
{
    Core,       // GameManager, InputManager
    System,     // AudioManager, EventManager
    Gameplay,   // GhostManager, CorridorManager
    UI          // UIManager
}

public enum ManagerScope
{
    Global,     // 모든 씬에서 유지 (GameManager, InputManager, AudioManager)
    Scene,      // 씬별로 존재 (UIManager, EventManager)
    Gameplay    // 인게임 전용 (CorridorManager, GhostManager, StonePool)
}
```

### 2. 우선순위 기반 초기화 시스템
```csharp
public class ManagerInitializer
{
    private List<IManager> managers = new List<IManager>();
    
    public void RegisterManager(IManager manager)
    {
        managers.Add(manager);
        managers.Sort((a, b) => a.GetPriority().CompareTo(b.GetPriority()));
    }
    
    public void InitializeAll()
    {
        foreach(var manager in managers)
        {
            if (!manager.IsInitialized())
            {
                manager.Initialize();
                Debug.Log($"{manager.GetType().Name} initialized");
            }
        }
    }
}
```

### 3. 의존성 주입 컨테이너
```csharp
public class DependencyContainer
{
    private Dictionary<Type, object> services = new Dictionary<Type, object>();
    
    public void Register<T>(T service) where T : class
    {
        services[typeof(T)] = service;
    }
    
    public T Get<T>() where T : class
    {
        return services[typeof(T)] as T;
    }
    
    public bool IsRegistered<T>() where T : class
    {
        return services.ContainsKey(typeof(T));
    }
}
```

### 4. 이벤트 기반 통신
```csharp
public static class ManagerEvents
{
    public static event Action<IManager> OnManagerInitialized;
    public static event Action<IManager> OnManagerReady;
    public static event Action<GameState> OnGameStateChanged;
    public static event Action<PlayerGameState> OnPlayerGameStateChanged;
}
```

### 5. 씬별 매니저 관리 시스템
```csharp
public class SceneManagerController : MonoBehaviour
{
    private Dictionary<string, List<IManager>> sceneManagers = new Dictionary<string, List<IManager>>();
    
    public void RegisterManagerForScene(string sceneName, IManager manager)
    public void ActivateManagersForScene(string sceneName)
    public void DeactivateManagersForScene(string sceneName)
    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
}

public enum SceneType
{
    MainMenu,    // 메인 메뉴 씬
    Gameplay,    // 인게임 씬
    Ending       // 엔딩 씬
}
```

## 📈 개선 후 예상 구조

```
ManagerInitializer
├── Global Managers (모든 씬에서 유지)
│   ├── GameManager (Priority 0, Global)
│   ├── InputManager (Priority 1, Global)
│   └── AudioManager (Priority 10, Global)
├── Scene Managers (씬별 관리)
│   ├── UIManager (Priority 30, Scene)
│   └── EventManager (Priority 11, Scene)
└── Gameplay Managers (인게임 전용)
    ├── GhostManager (Priority 20, Gameplay)
    ├── CorridorManager (Priority 21, Gameplay)
    └── StonePool (Priority 25, Gameplay)

SceneManagerController
├── MainMenu 씬: Global + UI Managers
├── Gameplay 씬: Global + Scene + Gameplay Managers
└── Ending 씬: Global + UI Managers

DependencyContainer
├── 모든 매니저 등록
└── 의존성 해결

ManagerEvents
├── 매니저 간 통신
├── 씬 전환 이벤트
└── 상태 변경 알림
```

## 🎯 구현 우선순위

1. **Phase 1**: IManager 인터페이스 통일
2. **Phase 2**: ManagerInitializer 구현
3. **Phase 3**: DependencyContainer 도입
4. **Phase 4**: 이벤트 기반 통신으로 전환
5. **Phase 5**: 기존 코드 리팩토링

## 📝 참고사항

- 기존 코드와의 호환성을 위해 점진적 개선 필요
- 테스트 코드 작성으로 안정성 확보
- 문서화를 통한 팀 내 이해도 향상
