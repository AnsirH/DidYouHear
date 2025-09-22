# 개발 아키텍처 문서 (Did You Hear?)

## 1. 프로젝트 개요

**"Did You Hear?"** 프로젝트는 다음과 같은 핵심 시스템으로 구성된 1인칭 공포 서바이벌 게임입니다:

### 핵심 게임플레이 메커니즘
1. **후방 공포**: 뒤를 돌아보면 귀신이 보여 게임 오버
2. **공깃돌 시스템**: 이동 중 주기적으로 뒤로 던져 소리로 안전 확인
3. **어깨 두드림 이벤트**: 반대 방향으로 뒤돌아봐야 하는 즉각적 반응 요구
4. **환경 이벤트**: 특정 상황에서 숙이기 필요
5. **랜덤 복도 생성**: 매 플레이마다 다른 복도 구조

---

## 2. Unity 프로젝트 폴더 구조

```
Assets/
├── 01_Scenes/                    # 씬 파일들
│   ├── MainMenu.unity
│   ├── Gameplay.unity
│   └── Ending.unity
│
├── 02_Scripts/                   # C# 스크립트들
│   ├── Core/                     # 핵심 시스템
│   │   ├── GameManager.cs
│   │   ├── PlayerController.cs
│   │   ├── CameraController.cs
│   │   └── InputManager.cs
│   │
│   ├── Player/                   # 플레이어 관련
│   │   ├── PlayerMovement.cs
│   │   ├── PlayerLook.cs
│   │   ├── PlayerCrouch.cs
│   │   └── PlayerState.cs
│   │
│   ├── StoneSystem/              # 공깃돌 시스템
│   │   ├── StoneThrower.cs
│   │   ├── StoneProjectile.cs
│   │   └── StoneAudioManager.cs
│   │
│   ├── Events/                   # 이벤트 시스템
│   │   ├── EventManager.cs
│   │   ├── ShoulderTapEvent.cs
│   │   ├── EnvironmentalEvent.cs
│   │   └── EventTrigger.cs
│   │
│   ├── Ghost/                    # 귀신 시스템
│   │   ├── GhostManager.cs
│   │   ├── GhostAppearance.cs
│   │   ├── GhostChase.cs
│   │   └── GhostAudio.cs
│   │
│   ├── Corridor/                 # 복도 시스템
│   │   ├── CorridorGenerator.cs
│   │   ├── CorridorPiece.cs
│   │   ├── CorridorEvent.cs
│   │   └── CorridorManager.cs
│   │
│   ├── Audio/                    # 오디오 시스템
│   │   ├── AudioManager.cs
│   │   ├── SpatialAudio.cs
│   │   └── AudioEventTrigger.cs
│   │
│   ├── UI/                       # UI 시스템
│   │   ├── UIManager.cs
│   │   ├── GameplayUI.cs
│   │   ├── MenuUI.cs
│   │   └── EndingUI.cs
│   │
│   └── Utilities/                # 유틸리티
│       ├── ObjectPool.cs
│       ├── RandomUtils.cs
│       └── GameConstants.cs
│
├── 03_Prefabs/                   # 프리팹들
│   ├── Player/
│   │   ├── Player.prefab
│   │   └── PlayerCamera.prefab
│   ├── Stone/
│   │   └── StoneProjectile.prefab
│   ├── Corridor/
│   │   ├── StraightCorridor.prefab
│   │   └── CornerCorridor.prefab
│   ├── Ghost/
│   │   └── Ghost.prefab
│   └── Events/
│       ├── ClassroomLight.prefab
│       ├── BathroomSound.prefab
│       └── DoorEvent.prefab
│
├── 04_Materials/                 # 머티리얼
│   ├── Corridor/
│   ├── Ghost/
│   └── Effects/
│
├── 05_Textures/                  # 텍스처
│   ├── Corridor/
│   ├── UI/
│   └── Effects/
│
├── 06_Audio/                     # 오디오 파일들
│   ├── SFX/
│   │   ├── Stone/
│   │   ├── Ghost/
│   │   ├── Environment/
│   │   └── UI/
│   ├── Music/
│   └── Ambient/
│
├── 07_Animations/                # 애니메이션
│   ├── Player/
│   ├── Ghost/
│   └── UI/
│
├── 08_Shaders/                   # 셰이더
│   ├── Ghost/
│   ├── Effects/
│   └── PostProcessing/
│
└── 09_Scenes/                    # 기존 씬들 (유지)
    └── SampleScene.unity
```

---

## 3. 핵심 컴포넌트 구조

### 3.1 Core System (핵심 시스템)

#### `GameManager.cs`
```csharp
// 게임 전체 상태 관리
- 게임 상태 (메뉴, 플레이, 일시정지, 엔딩)
- 플레이어 생명 상태
- 점수 및 통계 관리
- 씬 전환 관리
```

#### `PlayerController.cs`
```csharp
// 플레이어 메인 컨트롤러
- 이동 상태 관리 (걷기, 달리기, 숙이기, 멈추기)
- 입력 처리 및 상태 전환
- 다른 시스템들과의 연동
```

### 3.2 Player System (플레이어 시스템)

#### `PlayerMovement.cs`
```csharp
// 이동 로직
- 걷기: 1.5m/s, 공깃돌 3-5초 주기
- 달리기: 3.0m/s, 공깃돌 1.5-2.5초 주기  
- 숙이기: 0.8m/s, 귀신 추격 시 생존 불가
- 멈추기: 제자리 정지
```

#### `PlayerLook.cs`
```csharp
// 시점 및 뒤돌아보기
- 마우스 입력 처리
- 좌우 고개 돌리기
- 어깨 두드림 반응 처리
```

### 3.3 Stone System (공깃돌 시스템)

#### `StoneThrower.cs`
```csharp
// 공깃돌 던지기 관리
- 자동 던지기 (이동 중 주기적)
- 수동 던지기 (멈춤 상태에서)
- 던지기 방향 (어깨 뒤쪽)
- 딜레이 관리 (0.5초)
```

#### `StoneProjectile.cs`
```csharp
// 공깃돌 물리 및 충돌
- 물리 시뮬레이션
- 바닥 충돌 감지
- 충돌음 재생
- 오브젝트 풀링
```

### 3.4 Event System (이벤트 시스템)

#### `EventManager.cs`
```csharp
// 이벤트 전체 관리
- 어깨 두드림 이벤트 스케줄링
- 환경 이벤트 트리거
- 이벤트 우선순위 관리
- 반응 시간 제한 관리
```

#### `ShoulderTapEvent.cs`
```csharp
// 어깨 두드림 이벤트
- 랜덤 발생 (걷기: 20-30초, 달리기: 10-20초)
- 좌우 방향 결정
- 반응 시간 제한 (2초)
- 올바른 반응 판정
```

### 3.5 Ghost System (귀신 시스템)

#### `GhostManager.cs`
```csharp
// 귀신 등장 관리
- 등장 조건 체크
- 추격 이벤트 관리
- 게임 오버 처리
- 연출 시퀀스 관리
```

#### `GhostAppearance.cs`
```csharp
// 귀신 등장 연출
- 카메라 강제 뒤돌림
- 시각 효과 (점멸, 왜곡)
- 사운드 연출
- 게임 오버 전환
```

### 3.6 Corridor System (복도 시스템)

#### `CorridorGenerator.cs`
```csharp
// 복도 생성 알고리즘
- 30-40개 복도 조각 랜덤 선택
- 직선형/코너형 조합
- 이벤트 확률 배치 (10-20%)
- 메모리 효율적 로드/언로드
```

#### `CorridorPiece.cs`
```csharp
// 개별 복도 조각
- 복도 타입 (직선/코너)
- 이벤트 트리거 존
- 연결점 관리
- 이벤트 발생 확률
```

### 3.7 Audio System (오디오 시스템)

#### `AudioManager.cs`
```csharp
// 오디오 전체 관리
- 3D 공간 오디오 설정
- 사운드 풀링
- 볼륨 및 방향성 관리
- 이벤트별 사운드 트리거
```

#### `SpatialAudio.cs`
```csharp
// 3D 공간 오디오
- 방향성 사운드 (좌/우/뒤)
- 거리 기반 볼륨 조절
- 플레이어 위치 기반 계산
```

---

## 4. 시스템 간 연동 구조

### 4.1 데이터 흐름
1. **Input** → `PlayerController` → `PlayerMovement` + `PlayerLook`
2. **Movement** → `StoneThrower` → `StoneProjectile` → `AudioManager`
3. **Event** → `EventManager` → `ShoulderTapEvent` → `GhostManager`
4. **Ghost** → `GhostAppearance` → `GameManager` (게임 오버)
5. **Corridor** → `CorridorGenerator` → `CorridorPiece` → `EventTrigger`

### 4.2 상태 관리
- **PlayerState**: 걷기, 달리기, 숙이기, 멈추기, 뒤돌아보기
- **GameState**: 메뉴, 플레이, 일시정지, 엔딩
- **EventState**: 대기, 발생, 반응 대기, 완료, 실패

---

## 5. 개발 우선순위 및 단계별 구현 (4일 일정)

### **Day 1: 기본 시스템 구축**
- **오전 (4시간)**
  - `GameManager` 기본 구조 설계
  - `PlayerController` + `PlayerMovement` 기본 구현
  - `CameraController` 1인칭 시점 구현
  - `InputManager` 키보드/마우스 입력 처리

- **오후 (4시간)**
  - 기본 씬 구성 및 테스트
  - 플레이어 이동 상태 전환 테스트
  - 카메라 회전 및 시점 조정
  - 기본 UI 프레임워크 구축

### **Day 2: 공깃돌 시스템 구현**
- **오전 (4시간)**
  - `StoneThrower` + `StoneProjectile` 구현
  - `AudioManager` 3D 오디오 기본 설정
  - 충돌 감지 및 사운드 재생 시스템
  - 오브젝트 풀링 최적화

- **오후 (4시간)**
  - 공깃돌 자동/수동 던지기 로직 완성
  - 3D 공간 오디오 방향성 구현
  - 공깃돌 시스템과 플레이어 이동 연동
  - 사운드 품질 및 타이밍 최적화

### **Day 3: 이벤트 및 귀신 시스템**
- **오전 (4시간)**
  - `EventManager` 기본 프레임워크
  - `ShoulderTapEvent` 구현
  - `EnvironmentalEvent` 기본 구현
  - 이벤트 반응 시스템 및 타이머

- **오후 (4시간)**
  - `GhostManager` + `GhostAppearance` 구현
  - 게임 오버 시스템
  - 추격 이벤트 구현
  - 공포 연출 및 시각 효과

### **Day 4: 복도 시스템 및 폴리싱**
- **오전 (4시간)**
  - `CorridorGenerator` 알고리즘 구현
  - `CorridorPiece` 프리팹 제작
  - 랜덤 생성 및 이벤트 배치
  - 메모리 최적화 및 성능 튜닝

- **오후 (4시간)**
  - UI 시스템 완성
  - 사운드 및 시각 효과 최적화
  - 전체 시스템 통합 테스트
  - 버그 수정 및 밸런싱

---

## 6. 네이밍 컨벤션

- **클래스**: PascalCase (예: `PlayerController`)
- **메서드**: PascalCase (예: `ThrowStone()`)
- **변수**: camelCase (예: `isRunning`)
- **상수**: UPPER_CASE (예: `MAX_STONE_COUNT`)
- **프리팹**: PascalCase (예: `Player.prefab`)
- **씬**: PascalCase (예: `Gameplay.unity`)

---

## 7. 기술적 고려사항

### 7.1 성능 최적화
- 오브젝트 풀링을 통한 메모리 관리
- 60FPS 유지를 위한 프레임 최적화
- 사운드 및 이벤트 시스템의 효율적 처리

### 7.2 확장성
- 모듈화된 시스템 설계로 기능 추가 용이
- 이벤트 기반 아키텍처로 시스템 간 결합도 최소화
- 설정 가능한 매개변수로 밸런싱 조정 용이

### 7.3 유지보수성
- 명확한 네이밍 컨벤션
- 체계적인 폴더 구조
- 문서화된 코드 주석

---

## 8. 위험 요소 및 대응 방안

### 8.1 기술적 위험
- **3D 오디오 구현 복잡성**: Unity AudioSource 3D 설정으로 해결
- **성능 최적화**: 프로파일링 도구 활용 및 단계적 최적화
- **이벤트 타이밍 정확성**: 코루틴과 Invoke를 활용한 정밀한 타이밍 제어

### 8.2 일정 위험
- **4일 일정의 타이트함**: 핵심 기능 우선 구현, 부가 기능은 후순위
- **통합 테스트 시간 부족**: 각 시스템별 단위 테스트 강화
- **버그 수정 시간**: 코드 리뷰 및 테스트 케이스 작성으로 예방

---

이 구조는 확장성과 유지보수성을 고려하여 설계되었으며, 각 시스템이 독립적으로 개발 가능하도록 모듈화되어 있습니다. 4일이라는 제한된 시간 내에서 핵심 기능을 완성할 수 있도록 우선순위가 설정되어 있습니다.
