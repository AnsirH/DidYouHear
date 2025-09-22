# 매니저 시스템 마이그레이션 가이드

## 📋 개요

기존 매니저 시스템을 새로운 아키텍처로 점진적으로 마이그레이션하는 가이드입니다.

## 🔄 마이그레이션 단계

### Step 1: IManager 인터페이스 구현

#### 1.1 EventManager 마이그레이션
```csharp
// 기존 EventManager.cs
public class EventManager : MonoBehaviour
{
    public void Initialize() { ... }
}

// 개선된 EventManager.cs
public class EventManager : MonoBehaviour, IManager
{
    private bool isInitialized = false;
    private DependencyContainer container;
    
    public void Initialize()
    {
        if (isInitialized) return;
        // 기존 초기화 로직 + 의존성 주입
        isInitialized = true;
    }
    
    public void Reset() { /* 리셋 로직 */ }
    public void SetActive(bool active) { gameObject.SetActive(active); }
    public string GetStatus() { return "EventManager Status"; }
    public bool IsInitialized() { return isInitialized; }
    public int GetPriority() { return 11; }
    public ManagerType GetManagerType() { return ManagerType.System; }
    public void SetDependencyContainer(DependencyContainer container) { this.container = container; }
}
```

#### 1.2 GhostManager 마이그레이션
```csharp
// 기존 GhostManager.cs
public class GhostManager : MonoBehaviour
{
    public void Initialize(PlayerController playerCtrl, ...) { ... }
}

// 개선된 GhostManager.cs
public class GhostManager : MonoBehaviour, IManager
{
    private bool isInitialized = false;
    private DependencyContainer container;
    
    public void Initialize()
    {
        if (isInitialized) return;
        
        // 의존성 주입으로 참조 획득
        var playerController = container.Get<PlayerController>();
        var playerMovement = container.Get<PlayerMovement>();
        var cameraController = container.Get<CameraController>();
        var gameManager = container.Get<GameManager>();
        var eventManager = container.Get<EventManager>();
        
        // 기존 초기화 로직
        InitializeComponents(playerController, playerMovement, cameraController, gameManager, eventManager);
        
        isInitialized = true;
    }
    
    // 기존 Initialize 메서드는 하위 호환성을 위해 유지
    public void Initialize(PlayerController playerCtrl, PlayerMovement playerMove, CameraController cameraCtrl, GameManager gameMgr, EventManager eventMgr)
    {
        // 기존 로직 유지
    }
}
```

### Step 2: GameManager 업데이트

#### 2.1 새로운 초기화 시스템 도입
```csharp
// 기존 GameManager.cs
public class GameManager : MonoBehaviour
{
    private void Start()
    {
        InitializeManagers(); // FindObjectOfType 사용
    }
}

// 개선된 GameManager.cs
public class GameManager : MonoBehaviour, IManager
{
    private ManagerInitializer initializer;
    private DependencyContainer container;
    
    public void Initialize()
    {
        if (isInitialized) return;
        
        // 의존성 컨테이너 초기화
        container = new DependencyContainer();
        container.Register(this);
        
        // 매니저 초기화 시스템 설정
        initializer = gameObject.AddComponent<ManagerInitializer>();
        initializer.SetDependencyContainer(container);
        
        // 매니저들 등록
        RegisterAllManagers();
        
        // 초기화 실행
        initializer.InitializeAll();
        
        isInitialized = true;
    }
    
    private void RegisterAllManagers()
    {
        // 자동으로 찾아서 등록
        var allManagers = FindObjectsOfType<MonoBehaviour>().OfType<IManager>();
        foreach (var manager in allManagers)
        {
            initializer.RegisterManager(manager);
        }
    }
}
```

### Step 3: 의존성 주입으로 전환

#### 3.1 FindObjectOfType 제거
```csharp
// 기존 코드
playerController = FindObjectOfType<PlayerController>();
audioManager = FindObjectOfType<AudioManager>();

// 개선된 코드
playerController = container.Get<PlayerController>();
audioManager = container.Get<AudioManager>();
```

#### 3.2 직접 참조를 이벤트 기반으로 변경
```csharp
// 기존 코드
if (GameManager.Instance != null)
{
    GameManager.Instance.ChangeGameState(GameState.GameOver);
}

// 개선된 코드
ManagerEvents.TriggerGameStateChanged(GameState.GameOver);
```

### Step 4: 이벤트 시스템 통합

#### 4.1 기존 이벤트와 통합
```csharp
// GameManager.cs에서 기존 이벤트와 새 이벤트 연결
public void ChangeGameState(GameState newState)
{
    if (currentState == newState) return;
    
    GameState previousState = currentState;
    currentState = newState;
    
    // 기존 이벤트 (하위 호환성)
    OnGameStateChanged?.Invoke(newState);
    
    // 새로운 이벤트 시스템
    ManagerEvents.TriggerGameStateChanged(newState);
    
    // 기존 로직 유지
    switch (newState)
    {
        // ... 기존 switch 문
    }
}
```

## 🔧 단계별 마이그레이션 체크리스트

### Phase 1: IManager 인터페이스 구현
- [ ] EventManager에 IManager 구현
- [ ] GhostManager에 IManager 구현
- [ ] CorridorManager에 IManager 구현
- [ ] UIManager에 IManager 구현
- [ ] 각 매니저의 우선순위 정의
- [ ] 테스트 코드 작성

### Phase 2: ManagerInitializer 구현
- [ ] ManagerInitializer 클래스 생성
- [ ] DependencyContainer 클래스 생성
- [ ] GameManager에 새로운 초기화 시스템 통합
- [ ] 기존 InitializeManagers() 메서드 대체
- [ ] 초기화 순서 테스트

### Phase 3: 의존성 주입 도입
- [ ] FindObjectOfType 사용 제거
- [ ] container.Get<T>() 패턴으로 변경
- [ ] 매니저 간 직접 참조 제거
- [ ] 의존성 주입 테스트

### Phase 4: 이벤트 기반 통신
- [ ] ManagerEvents 시스템 구현
- [ ] 기존 이벤트와 통합
- [ ] 순환 의존성 제거
- [ ] 이벤트 기반 통신 테스트

### Phase 5: 리팩토링 및 최적화
- [ ] 불필요한 코드 제거
- [ ] 성능 최적화
- [ ] 문서화 업데이트
- [ ] 최종 통합 테스트

## ⚠️ 주의사항

### 1. 하위 호환성 유지
```csharp
// 기존 API는 유지하되, 내부적으로는 새로운 시스템 사용
public PlayerController playerController => GetManager<PlayerController>();

// 기존 메서드도 유지
public void Initialize(EventManager eventMgr, Transform playerTf, Core.PlayerController playerCtrl)
{
    // 기존 로직 유지
}
```

### 2. 점진적 마이그레이션
- 한 번에 모든 매니저를 변경하지 말고 단계별로 진행
- 각 단계마다 테스트 실행
- 문제 발생 시 이전 단계로 롤백 가능하도록 준비

### 3. 테스트 코드 작성
```csharp
[Test]
public void TestManagerInitialization()
{
    // 각 매니저가 올바르게 초기화되는지 테스트
}

[Test]
public void TestDependencyInjection()
{
    // 의존성 주입이 올바르게 작동하는지 테스트
}
```

### 4. 성능 모니터링
- 이벤트 시스템의 성능 영향 측정
- 메모리 사용량 모니터링
- 초기화 시간 측정

## 🐛 문제 해결

### 1. 초기화 순서 문제
```csharp
// 문제: 매니저가 아직 초기화되지 않음
var audioManager = container.Get<AudioManager>(); // null 반환

// 해결: 초기화 완료 후 접근
ManagerEvents.OnManagerInitialized += (manager) =>
{
    if (manager is AudioManager)
    {
        // AudioManager 초기화 완료 후 작업
    }
};
```

### 2. 순환 의존성 문제
```csharp
// 문제: GameManager ↔ EventManager 순환 참조
// GameManager가 EventManager를 참조하고, EventManager가 GameManager를 참조

// 해결: 이벤트 기반 통신 사용
// GameManager에서 직접 EventManager 호출하지 않고 이벤트 발생
ManagerEvents.TriggerEventTriggered(EventType.ShoulderTap);
```

### 3. NullReferenceException
```csharp
// 문제: 매니저가 아직 등록되지 않음
var manager = container.Get<SomeManager>(); // null

// 해결: null 체크 및 안전한 접근
var manager = container?.Get<SomeManager>();
if (manager != null)
{
    // 안전하게 사용
}
```

## 📊 마이그레이션 후 예상 효과

### 개선 전
- ❌ 초기화 순서 불일치
- ❌ FindObjectOfType 남발
- ❌ 순환 의존성 위험
- ❌ 테스트 어려움

### 개선 후
- ✅ 명확한 초기화 순서
- ✅ 의존성 주입으로 느슨한 결합
- ✅ 이벤트 기반 통신
- ✅ 테스트 가능한 코드
- ✅ 확장 가능한 아키텍처

## 📝 마이그레이션 완료 체크리스트

- [ ] 모든 매니저가 IManager 구현
- [ ] ManagerInitializer로 통일된 초기화
- [ ] DependencyContainer로 의존성 관리
- [ ] ManagerEvents로 통신
- [ ] FindObjectOfType 사용 제거
- [ ] 순환 의존성 제거
- [ ] 테스트 코드 작성
- [ ] 문서 업데이트
- [ ] 성능 테스트 통과
- [ ] 기존 기능 정상 동작 확인

이 가이드를 따라 단계별로 마이그레이션하면 안전하고 효율적인 매니저 시스템을 구축할 수 있습니다.
