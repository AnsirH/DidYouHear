# 매니저 시스템 흐름도

## 📋 개요

"Did You Hear?" 프로젝트의 매니저 시스템 전체 흐름과 각 컴포넌트 간의 상호작용을 시각화한 문서입니다.

## 🏗️ 시스템 아키텍처 흐름도

### 전체 시스템 구조

```mermaid
graph TB
    subgraph "씬 (Scene)"
        BS[ManagerSystemBootstrap]
        GM[GameManager]
        IM[InputManager]
        AM[AudioManager]
        EM[EventManager]
        GHM[GhostManager]
        SP[StonePool]
    end
    
    subgraph "매니저 시스템 컴포넌트"
        MI[ManagerInitializer]
        SMC[SceneManagerController]
        DC[DependencyContainer]
        ME[ManagerEvents]
    end
    
    subgraph "인터페이스"
        IMI[IManager]
    end
    
    BS --> MI
    BS --> SMC
    BS --> DC
    MI --> DC
    SMC --> DC
    ME --> MI
    ME --> SMC
    
    GM -.-> IMI
    IM -.-> IMI
    AM -.-> IMI
    EM -.-> IMI
    GHM -.-> IMI
    SP -.-> IMI
    
    IMI --> MI
    IMI --> SMC
```

## 🚀 초기화 흐름도

### 1단계: 부트스트랩 초기화

```mermaid
sequenceDiagram
    participant Scene as 씬 시작
    participant BS as ManagerSystemBootstrap
    participant MI as ManagerInitializer
    participant SMC as SceneManagerController
    participant DC as DependencyContainer
    
    Scene->>BS: Awake()
    BS->>BS: ValidateComponents()
    Note over BS: Inspector에서 할당된 컴포넌트 검증
    
    BS->>DC: new DependencyContainer()
    BS->>BS: InitializeBootstrap()
    
    Scene->>BS: Start()
    BS->>BS: StartManagerSystem()
```

### 2단계: 매니저 등록 및 초기화

```mermaid
sequenceDiagram
    participant BS as ManagerSystemBootstrap
    participant MI as ManagerInitializer
    participant DC as DependencyContainer
    participant GM as GameManager
    participant AM as AudioManager
    participant EM as EventManager
    
    BS->>BS: RegisterAllManagers()
    Note over BS: FindObjectOfType으로 매니저들 탐지
    
    BS->>DC: RegisterSingleton(gameManager)
    BS->>DC: RegisterSingleton(audioManager)
    BS->>DC: RegisterSingleton(eventManager)
    
    BS->>MI: RegisterManager(gameManager)
    BS->>MI: RegisterManager(audioManager)
    BS->>MI: RegisterManager(eventManager)
    
    MI->>MI: InitializeAllManagers()
    Note over MI: 우선순위별로 정렬 후 초기화
    
    MI->>GM: Initialize()
    MI->>AM: Initialize()
    MI->>EM: Initialize()
    
    MI-->>BS: OnManagerInitialized 이벤트
    MI-->>BS: OnAllManagersInitialized 이벤트
```

## 🎯 매니저 우선순위 흐름도

### 초기화 순서

```mermaid
graph TD
    Start([시스템 시작]) --> P0{우선순위 0}
    P0 --> GM[GameManager<br/>Priority: 0]
    
    GM --> P1{우선순위 1}
    P1 --> IM[InputManager<br/>Priority: 1]
    
    IM --> P10{우선순위 10}
    P10 --> AM[AudioManager<br/>Priority: 10]
    AM --> EM[EventManager<br/>Priority: 11]
    
    EM --> P20{우선순위 20}
    P20 --> GHM[GhostManager<br/>Priority: 20]
    
    GHM --> P25{우선순위 25}
    P25 --> SP[StonePool<br/>Priority: 25]
    
    SP --> Complete([초기화 완료])
    
    style GM fill:#e1f5fe
    style IM fill:#e8f5e8
    style AM fill:#fff3e0
    style EM fill:#fff3e0
    style GHM fill:#f3e5f5
    style SP fill:#f3e5f5
```

## 🎬 씬별 매니저 관리 흐름도

### 씬 전환 시 매니저 활성화/비활성화

```mermaid
stateDiagram-v2
    [*] --> SceneLoad: 씬 로드
    
    SceneLoad --> DetermineType: 씬 타입 결정
    
    DetermineType --> MainMenu: MainMenu
    DetermineType --> Gameplay: Gameplay
    DetermineType --> Ending: Ending
    
    state MainMenu {
        [*] --> GlobalActive
        GlobalActive --> UIActive
        UIActive --> [*]
    }
    
    state Gameplay {
        [*] --> GlobalActive
        GlobalActive --> SceneActive
        SceneActive --> GameplayActive
        GameplayActive --> [*]
    }
    
    state Ending {
        [*] --> GlobalActive
        GlobalActive --> UIActive
        UIActive --> [*]
    }
    
    MainMenu --> [*]: 씬 전환
    Gameplay --> [*]: 씬 전환
    Ending --> [*]: 씬 전환
    
    note right of GlobalActive
        Global 매니저들:
        - GameManager
        - InputManager
        - AudioManager
    end note
    
    note right of SceneActive
        Scene 매니저들:
        - EventManager
        - UIManager
    end note
    
    note right of GameplayActive
        Gameplay 매니저들:
        - GhostManager
        - StonePool
        - CorridorManager
    end note
```

## 🔄 의존성 주입 흐름도

### DependencyContainer 동작 방식

```mermaid
graph LR
    subgraph "등록 단계"
        A[매니저 인스턴스] --> B[RegisterSingleton]
        B --> C[DependencyContainer]
    end
    
    subgraph "해결 단계"
        D[매니저 요청] --> E[Get&lt;T&gt;]
        E --> C
        C --> F[인스턴스 반환]
    end
    
    subgraph "사용 예시"
        G[EventManager] --> H[container.Get&lt;AudioManager&gt;]
        H --> I[AudioManager 인스턴스]
        I --> J[Play3DSound 호출]
    end
    
    style C fill:#e1f5fe
    style F fill:#e8f5e8
    style I fill:#e8f5e8
```

## 📡 이벤트 기반 통신 흐름도

### ManagerEvents를 통한 매니저 간 통신

```mermaid
sequenceDiagram
    participant GM as GameManager
    participant ME as ManagerEvents
    participant AM as AudioManager
    participant EM as EventManager
    participant GHM as GhostManager
    
    GM->>ME: TriggerGameStateChanged(GameState.Playing)
    ME-->>AM: OnGameStateChanged 이벤트
    ME-->>EM: OnGameStateChanged 이벤트
    ME-->>GHM: OnGameStateChanged 이벤트
    
    EM->>ME: TriggerEventTriggered(EventType.ShoulderTap)
    ME-->>AM: OnEventTriggered 이벤트
    ME-->>GHM: OnEventTriggered 이벤트
    
    AM->>ME: TriggerSoundPlayed(clip, position)
    ME-->>GM: OnSoundPlayed 이벤트
    
    Note over ME: 순환 의존성 없이 느슨한 결합 달성
```

## 🎮 게임플레이 중 매니저 상호작용 흐름도

### 실제 게임 상황에서의 매니저 동작

```mermaid
graph TD
    Start([게임 시작]) --> PlayerMove[플레이어 이동]
    
    PlayerMove --> InputCheck{입력 감지}
    InputCheck -->|이동 입력| IM[InputManager]
    InputCheck -->|돌 던지기| SP[StonePool]
    
    IM --> PlayerUpdate[플레이어 위치 업데이트]
    PlayerUpdate --> EM[EventManager]
    
    EM --> EventCheck{이벤트 트리거?}
    EventCheck -->|어깨 두드림| ShoulderTap[ShoulderTapEvent]
    EventCheck -->|환경 이벤트| EnvEvent[EnvironmentalEvent]
    EventCheck -->|귀신 등장| GhostEvent[GhostAppearanceEvent]
    
    ShoulderTap --> AM[AudioManager]
    EnvEvent --> AM
    GhostEvent --> GHM[GhostManager]
    
    SP --> StoneImpact[돌 충돌]
    StoneImpact --> AM
    AM --> SoundPlay[3D 사운드 재생]
    
    GHM --> GhostAppear[귀신 등장]
    GhostAppear --> AM
    AM --> GhostSound[귀신 사운드 재생]
    
    style IM fill:#e8f5e8
    style AM fill:#fff3e0
    style EM fill:#fff3e0
    style GHM fill:#f3e5f5
    style SP fill:#f3e5f5
```

## 🛠️ 에러 처리 및 복구 흐름도

### 초기화 실패 시 처리

```mermaid
graph TD
    Start([매니저 초기화 시작]) --> Init[InitializeAllManagers]
    
    Init --> Check{매니저 초기화}
    Check -->|성공| Success[초기화 성공]
    Check -->|실패| Error[초기화 실패]
    
    Success --> Complete([시스템 준비 완료])
    
    Error --> LogError[에러 로그 출력]
    LogError --> Event[OnManagerInitializationFailed 이벤트]
    Event --> Continue{계속 진행?}
    
    Continue -->|예| NextManager[다음 매니저 초기화]
    Continue -->|아니오| Stop[시스템 중단]
    
    NextManager --> Check
    
    Stop --> Restart{시스템 재시작?}
    Restart -->|예| Start
    Restart -->|아니오| End([시스템 종료])
    
    style Error fill:#ffebee
    style LogError fill:#ffebee
    style Stop fill:#ffebee
    style Success fill:#e8f5e8
    style Complete fill:#e8f5e8
```

## 📊 시스템 상태 모니터링 흐름도

### LogSystemStatus 동작

```mermaid
graph TD
    StatusRequest[LogSystemStatus 호출] --> BootstrapCheck[부트스트랩 상태 확인]
    
    BootstrapCheck --> ManagerInitStatus[ManagerInitializer 상태]
    BootstrapCheck --> SceneManagerStatus[SceneManagerController 상태]
    BootstrapCheck --> DependencyStatus[DependencyContainer 상태]
    
    ManagerInitStatus --> ManagerList[등록된 매니저 목록]
    ManagerList --> ManagerDetails[각 매니저 상세 정보]
    
    SceneManagerStatus --> ActiveManagers[활성화된 매니저 목록]
    ActiveManagers --> SceneBreakdown[씬별 매니저 분류]
    
    DependencyStatus --> ServiceCount[등록된 서비스 수]
    ServiceCount --> ServiceList[서비스 목록]
    
    ManagerDetails --> LogOutput[로그 출력]
    SceneBreakdown --> LogOutput
    ServiceList --> LogOutput
    
    LogOutput --> Complete([상태 확인 완료])
    
    style StatusRequest fill:#e1f5fe
    style LogOutput fill:#fff3e0
    style Complete fill:#e8f5e8
```

## 🔧 시스템 재시작 흐름도

### RestartManagerSystem 동작

```mermaid
sequenceDiagram
    participant User as 사용자
    participant BS as ManagerSystemBootstrap
    participant MI as ManagerInitializer
    participant SMC as SceneManagerController
    
    User->>BS: RestartManagerSystem()
    
    BS->>BS: UnsubscribeFromManagerEvents()
    Note over BS: 모든 이벤트 구독 해제
    
    BS->>MI: ResetAllManagers()
    Note over MI: 모든 매니저 리셋
    
    BS->>BS: isBootstrapComplete = false
    BS->>BS: InitializeBootstrap()
    Note over BS: 부트스트랩 재초기화
    
    BS->>BS: StartManagerSystem()
    Note over BS: 시스템 재시작
    
    BS-->>User: 시스템 재시작 완료
```

## 📝 주요 특징 요약

### ✅ 장점
- **명확한 초기화 순서**: 우선순위 기반 초기화
- **씬별 매니저 관리**: 메모리 효율성 향상
- **의존성 주입**: 느슨한 결합 달성
- **이벤트 기반 통신**: 순환 의존성 제거
- **에러 처리**: 견고한 시스템 설계

### 🎯 핵심 컴포넌트
1. **ManagerSystemBootstrap**: 시스템 진입점 및 조율자
2. **ManagerInitializer**: 우선순위 기반 초기화 관리
3. **SceneManagerController**: 씬별 매니저 활성화 관리
4. **DependencyContainer**: 의존성 주입 컨테이너
5. **ManagerEvents**: 이벤트 기반 통신 시스템

이 흐름도를 통해 매니저 시스템의 전체적인 동작 방식과 각 컴포넌트 간의 상호작용을 이해할 수 있습니다.
