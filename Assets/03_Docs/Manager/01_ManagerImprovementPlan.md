# 매니저 시스템 개선 계획서

## 🎯 개선 목표

1. **일관성 있는 초기화 시스템** 구축
2. **의존성 주입**을 통한 느슨한 결합 달성
3. **이벤트 기반 통신**으로 순환 의존성 제거
4. **씬별 매니저 관리** 시스템 구축
5. **확장 가능한 아키텍처** 설계
6. **유지보수성** 향상

## ✅ 구현 완료 상태

### Phase 1: IManager 인터페이스 통일 ✅ 완료

#### 1.1 IManager 인터페이스 확장
```csharp
// 04_Scripts/Core/IManager.cs
public interface IManager
{
    void Initialize();
    void Reset();
    void SetActive(bool active);
    string GetStatus();
    bool IsInitialized();
    int GetPriority();
    ManagerType GetManagerType();
    ManagerScope GetManagerScope();
    void SetDependencyContainer(DependencyContainer container);
}

public enum ManagerType
{
    Core = 0,       // GameManager, InputManager
    System = 10,    // AudioManager, EventManager
    Gameplay = 20,  // GhostManager, CorridorManager
    UI = 30         // UIManager
}

public enum ManagerScope
{
    Global,     // 모든 씬에서 유지 (GameManager, InputManager, AudioManager)
    Scene,      // 씬별로 존재 (UIManager, EventManager)
    Gameplay    // 인게임 전용 (CorridorManager, GhostManager, StonePool)
}
```

#### 1.2 모든 매니저에 IManager 구현 ✅ 완료
- ✅ EventManager에 IManager 구현 완료
- ✅ GhostManager에 IManager 구현 완료
- ✅ StonePool에 IManager 구현 완료
- ✅ GameManager에 IManager 구현 완료
- ✅ InputManager에 IManager 구현 완료
- ✅ AudioManager에 IManager 구현 완료

#### 1.3 우선순위 및 범위 정의
```csharp
// 각 매니저별 우선순위 및 범위
GameManager: 0, Global
InputManager: 1, Global
AudioManager: 10, Global
EventManager: 11, Scene
GhostManager: 20, Gameplay
CorridorManager: 21, Gameplay
StonePool: 25, Gameplay
UIManager: 30, Scene
```

### Phase 2: ManagerInitializer 구현 ✅ 완료

#### 2.1 ManagerInitializer 클래스 생성
```csharp
// 04_Scripts/Core/ManagerInitializer.cs
public class ManagerInitializer : MonoBehaviour
{
    private List<IManager> managers = new List<IManager>();
    private DependencyContainer dependencyContainer;
    
    public void RegisterManager(IManager manager)
    public void InitializeAll()
    public void ResetAll()
    public void SetAllActive(bool active)
}
```

#### 2.2 초기화 순서 보장
- 우선순위 기반 정렬
- 의존성 체크
- 초기화 실패 시 롤백

#### 2.3 GameManager 통합
- GameManager에서 ManagerInitializer 사용
- 기존 InitializeManagers() 메서드 대체

### Phase 3: DependencyContainer 도입 ✅ 완료

#### 3.1 DependencyContainer 구현
```csharp
// 04_Scripts/Core/DependencyContainer.cs
public class DependencyContainer
{
    private Dictionary<Type, object> services = new Dictionary<Type, object>();
    
    public void Register<T>(T service) where T : class
    public T Get<T>() where T : class
    public bool IsRegistered<T>() where T : class
    public void Clear()
}
```

#### 3.2 매니저 등록 시스템
- 자동 등록 (Awake에서)
- 수동 등록 (필요시)
- 타입 안전성 보장

#### 3.3 기존 FindObjectOfType 대체
```csharp
// 기존
playerController = FindObjectOfType<PlayerController>();

// 개선
playerController = dependencyContainer.Get<PlayerController>();
```

### Phase 4: 씬별 매니저 관리 시스템 ✅ 완료

#### 4.1 SceneManagerController 구현
```csharp
// 04_Scripts/Core/SceneManagerController.cs
public class SceneManagerController : MonoBehaviour
{
    private Dictionary<string, List<IManager>> sceneManagers = new Dictionary<string, List<IManager>>();
    private Dictionary<ManagerScope, List<IManager>> scopeManagers = new Dictionary<ManagerScope, List<IManager>>();
    
    public void RegisterManagerForScene(string sceneName, IManager manager)
    public void ActivateManagersForScene(string sceneName)
    public void DeactivateManagersForScene(string sceneName)
    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    public void SetManagersForScene(SceneType sceneType)
}

public enum SceneType
{
    MainMenu,    // 메인 메뉴 씬
    Gameplay,    // 인게임 씬
    Ending       // 엔딩 씬
}
```

#### 4.2 씬별 매니저 활성화 로직
```csharp
// 씬별 매니저 활성화 규칙
MainMenu: Global + UI Managers
Gameplay: Global + Scene + Gameplay Managers
Ending: Global + UI Managers
```

### Phase 5: 이벤트 기반 통신 ✅ 완료

#### 5.1 ManagerEvents 시스템
```csharp
// 04_Scripts/Core/ManagerEvents.cs
public static class ManagerEvents
{
    // 매니저 생명주기
    public static event Action<IManager> OnManagerInitialized;
    public static event Action<IManager> OnManagerReady;
    public static event Action<IManager> OnManagerReset;
    
    // 씬 전환
    public static event Action<string> OnSceneLoaded;
    public static event Action<string> OnSceneUnloaded;
    
    // 게임 상태
    public static event Action<GameState> OnGameStateChanged;
    public static event Action<PlayerGameState> OnPlayerGameStateChanged;
    
    // 이벤트 시스템
    public static event Action<EventType> OnEventTriggered;
    public static event Action<EventType, bool> OnEventCompleted;
}
```

#### 4.2 순환 의존성 제거
- 직접 참조 → 이벤트 기반 통신
- GameManager ↔ EventManager 분리
- 매니저 간 느슨한 결합

#### 4.3 기존 이벤트 시스템 통합
- GameManager의 기존 이벤트와 통합
- 하위 호환성 유지

### Phase 6: 기존 코드 리팩토링 ✅ 완료

#### 6.1 매니저별 리팩토링 ✅ 완료
- ✅ GameManager: ManagerInitializer + SceneManagerController 사용 완료
- ✅ EventManager: DependencyContainer + Scene 범위 설정 완료
- ✅ GhostManager: 이벤트 기반 통신 + Gameplay 범위 설정 완료
- ✅ StonePool: IManager 구현 + Gameplay 범위 설정 완료
- ✅ InputManager: IManager 구현 + Global 범위 설정 완료
- ✅ AudioManager: IManager 구현 + Global 범위 설정 완료

#### 6.2 테스트 코드 작성
```csharp
// 04_Scripts/Tests/ManagerTests.cs
[Test]
public void TestManagerInitializationOrder()
{
    // 초기화 순서 테스트
}

[Test]
public void TestDependencyInjection()
{
    // 의존성 주입 테스트
}
```

#### 6.3 문서화
- API 문서 작성
- 사용법 가이드
- 마이그레이션 가이드

## 🔧 구현 세부사항

### 1. IManager 구현 예시
```csharp
public class EventManager : MonoBehaviour, IManager
{
    private bool isInitialized = false;
    private DependencyContainer container;
    
    public void Initialize()
    {
        if (isInitialized) return;
        
        // 의존성 주입으로 참조 획득
        var gameManager = container.Get<GameManager>();
        var audioManager = container.Get<AudioManager>();
        
        // 초기화 로직
        isInitialized = true;
    }
    
    public int GetPriority() => 11;
    public ManagerType GetManagerType() => ManagerType.System;
}
```

### 2. ManagerInitializer 사용법
```csharp
public class GameManager : MonoBehaviour
{
    private ManagerInitializer initializer;
    private DependencyContainer container;
    
    private void Awake()
    {
        container = new DependencyContainer();
        initializer = new ManagerInitializer();
        initializer.SetDependencyContainer(container);
    }
    
    private void Start()
    {
        // 매니저 등록
        initializer.RegisterManager(this);
        initializer.RegisterManager(FindObjectOfType<InputManager>());
        // ... 기타 매니저들
        
        // 초기화 실행
        initializer.InitializeAll();
    }
}
```

### 3. 이벤트 기반 통신 예시
```csharp
// 기존: 직접 참조
if (GameManager.Instance != null)
{
    GameManager.Instance.ChangeGameState(GameState.GameOver);
}

// 개선: 이벤트 기반
ManagerEvents.OnGameStateChanged?.Invoke(GameState.GameOver);
```

## 📊 예상 효과

### 개선 전
- ❌ 초기화 순서 불일치
- ❌ 순환 의존성 위험
- ❌ 코드 일관성 부족
- ❌ 유지보수 어려움

### 개선 후
- ✅ 명확한 초기화 순서
- ✅ 의존성 주입으로 느슨한 결합
- ✅ 이벤트 기반 통신
- ✅ 확장 가능한 아키텍처
- ✅ 테스트 가능한 코드

## ⚠️ 주의사항

1. **점진적 마이그레이션**: 기존 코드와의 호환성 유지
2. **테스트 우선**: 각 단계별 테스트 코드 작성
3. **문서화**: 변경사항에 대한 명확한 문서 작성
4. **백업**: 중요한 변경 전 코드 백업
5. **성능**: 이벤트 시스템의 성능 영향 모니터링

## 📅 일정표

| Phase | 기간 | 주요 작업 | 완료 기준 |
|-------|------|-----------|-----------|
| 1 | 1-2일 | IManager 통일 | 모든 매니저가 IManager 구현 |
| 2 | 2-3일 | ManagerInitializer | 우선순위 기반 초기화 |
| 3 | 2-3일 | DependencyContainer | 의존성 주입 시스템 |
| 4 | 3-4일 | 씬별 매니저 관리 | SceneManagerController 구현 |
| 5 | 2-3일 | 이벤트 통신 | 순환 의존성 제거 |
| 6 | 4-5일 | 리팩토링 | 기존 코드 통합 완료 |

**총 예상 기간: 14-20일**
