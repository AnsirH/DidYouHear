using UnityEngine;
using DidYouHear.Manager.System;

namespace DidYouHear.Manager.Interfaces
{
    /// <summary>
    /// 모든 매니저가 구현해야 하는 기본 인터페이스
    /// 
    /// 이 인터페이스는 매니저 시스템의 핵심으로, 다음과 같은 기능을 제공합니다:
    /// 1. 통일된 초기화 시스템 - 모든 매니저가 동일한 방식으로 초기화됩니다
    /// 2. 우선순위 기반 관리 - 매니저의 초기화 순서를 제어합니다
    /// 3. 씬별 관리 - 매니저가 어떤 씬에서 활성화될지 결정합니다
    /// 4. 의존성 주입 - 매니저 간의 의존성을 느슨하게 관리합니다
    /// 5. 상태 추적 - 매니저의 현재 상태를 모니터링할 수 있습니다
    /// </summary>
    public interface IManager
    {
        /// <summary>
        /// 매니저 초기화
        /// 
        /// 매니저가 처음 시작될 때 호출됩니다.
        /// 이 메서드에서는 매니저가 작동하는데 필요한 모든 설정을 수행합니다.
        /// 예: 컴포넌트 참조 설정, 이벤트 구독, 초기값 설정 등
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// 매니저 리셋
        /// 
        /// 게임이 재시작되거나 매니저를 초기 상태로 되돌릴 때 호출됩니다.
        /// 이 메서드에서는 매니저의 모든 상태를 초기값으로 되돌립니다.
        /// 예: 타이머 리셋, 카운터 초기화, 이벤트 구독 해제 등
        /// </summary>
        void Reset();
        
        /// <summary>
        /// 매니저 활성화/비활성화
        /// 
        /// 매니저의 활성 상태를 제어합니다.
        /// 비활성화된 매니저는 Update() 메서드가 호출되지 않고,
        /// 게임 오브젝트도 비활성화됩니다.
        /// 
        /// <param name="active">true: 활성화, false: 비활성화</param>
        /// </summary>
        void SetActive(bool active);
        
        /// <summary>
        /// 매니저 상태 정보 반환
        /// 
        /// 매니저의 현재 상태를 문자열로 반환합니다.
        /// 디버깅이나 상태 모니터링에 사용됩니다.
        /// 
        /// <returns>매니저의 현재 상태를 설명하는 문자열</returns>
        /// </summary>
        string GetStatus();
        
        /// <summary>
        /// 매니저 초기화 여부 확인
        /// 
        /// 매니저가 Initialize() 메서드를 통해 초기화되었는지 확인합니다.
        /// 초기화되지 않은 매니저는 사용하면 안 됩니다.
        /// 
        /// <returns>true: 초기화됨, false: 초기화되지 않음</returns>
        /// </summary>
        bool IsInitialized();
        
        /// <summary>
        /// 매니저 초기화 우선순위 반환
        /// 
        /// 매니저들이 초기화되는 순서를 결정합니다.
        /// 낮은 숫자가 먼저 초기화됩니다.
        /// 
        /// 예시:
        /// - GameManager: 0 (최우선)
        /// - InputManager: 1
        /// - AudioManager: 10
        /// - GhostManager: 20
        /// 
        /// <returns>우선순위 값 (낮을수록 먼저 초기화)</returns>
        /// </summary>
        int GetPriority();
        
        /// <summary>
        /// 매니저 타입 반환
        /// 
        /// 매니저의 기능적 분류를 반환합니다.
        /// 같은 타입의 매니저들은 함께 관리됩니다.
        /// 
        /// <returns>매니저 타입 (Core, System, Gameplay, UI)</returns>
        /// </summary>
        ManagerType GetManagerType();
        
        /// <summary>
        /// 매니저 범위 반환
        /// 
        /// 매니저가 어떤 씬에서 활성화될지 결정합니다.
        /// 씬별 매니저 관리 시스템에서 사용됩니다.
        /// 
        /// <returns>매니저 범위 (Global, Scene, Gameplay)</returns>
        /// </summary>
        ManagerScope GetManagerScope();
        
        /// <summary>
        /// 의존성 컨테이너 설정
        /// 
        /// 매니저가 다른 매니저나 서비스를 찾을 때 사용할
        /// 의존성 컨테이너를 설정합니다.
        /// 
        /// <param name="container">의존성 주입 컨테이너</param>
        /// </summary>
        void SetDependencyContainer(DependencyContainer container);
    }

    /// <summary>
    /// 매니저 타입 열거형
    /// 
    /// 매니저의 기능적 분류를 정의합니다.
    /// 각 타입은 고유한 우선순위 값을 가지며, 초기화 순서를 결정합니다.
    /// </summary>
    public enum ManagerType
    {
        /// <summary>
        /// 핵심 매니저 (우선순위: 0-9)
        /// 게임의 기본 기능을 담당하는 가장 중요한 매니저들
        /// 예: GameManager, InputManager
        /// </summary>
        Core = 0,
        
        /// <summary>
        /// 시스템 매니저 (우선순위: 10-19)
        /// 게임의 특정 시스템을 담당하는 매니저들
        /// 예: AudioManager, EventManager
        /// </summary>
        System = 10,
        
        /// <summary>
        /// 게임플레이 매니저 (우선순위: 20-29)
        /// 실제 게임플레이와 관련된 매니저들
        /// 예: GhostManager, CorridorManager, StonePool
        /// </summary>
        Gameplay = 20,
        
        /// <summary>
        /// UI 매니저 (우선순위: 30-39)
        /// 사용자 인터페이스와 관련된 매니저들
        /// 예: UIManager
        /// </summary>
        UI = 30
    }

    /// <summary>
    /// 매니저 범위 열거형 (씬별 관리용)
    /// 
    /// 매니저가 어떤 씬에서 활성화될지 결정합니다.
    /// 이를 통해 메모리 효율성과 성능을 최적화할 수 있습니다.
    /// </summary>
    public enum ManagerScope
    {
        /// <summary>
        /// 전역 매니저
        /// 모든 씬에서 유지되는 매니저들
        /// 예: GameManager, InputManager, AudioManager
        /// 특징: DontDestroyOnLoad로 씬 전환 시에도 유지됨
        /// </summary>
        Global,
        
        /// <summary>
        /// 씬별 매니저
        /// 특정 씬에서만 필요한 매니저들
        /// 예: UIManager, EventManager
        /// 특징: 씬 전환 시 비활성화되고 새 씬에서 재활성화됨
        /// </summary>
        Scene,
        
        /// <summary>
        /// 게임플레이 전용 매니저
        /// 인게임에서만 필요한 매니저들
        /// 예: CorridorManager, GhostManager, StonePool
        /// 특징: Gameplay 씬에서만 활성화됨
        /// </summary>
        Gameplay
    }

    /// <summary>
    /// 씬 타입 열거형
    /// 
    /// 게임의 씬들을 분류하여 매니저 관리에 사용합니다.
    /// 각 씬 타입별로 필요한 매니저들이 다릅니다.
    /// </summary>
    public enum SceneType
    {
        /// <summary>
        /// 메인 메뉴 씬
        /// 활성화되는 매니저: Global + UI
        /// </summary>
        MainMenu,
        
        /// <summary>
        /// 인게임 씬
        /// 활성화되는 매니저: Global + Scene + Gameplay
        /// </summary>
        Gameplay,
        
        /// <summary>
        /// 엔딩 씬
        /// 활성화되는 매니저: Global + UI
        /// </summary>
        Ending
    }
}
