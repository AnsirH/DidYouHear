using System;
using UnityEngine;
using DidYouHear.Manager.Interfaces;
using DidYouHear.Core;

namespace DidYouHear.Manager.System
{
    /// <summary>
    /// 매니저 간 통신을 위한 이벤트 시스템
    /// 
    /// 매니저들 간의 직접적인 참조를 제거하고 이벤트 기반 통신을 통해
    /// 순환 의존성을 제거하고 느슨한 결합을 달성하는 시스템입니다.
    /// 
    /// 이 클래스는 다음과 같은 기능을 제공합니다:
    /// 
    /// 1. 매니저 생명주기 이벤트: 초기화, 리셋, 파괴 등의 생명주기 추적
    /// 2. 씬 전환 이벤트: 씬 로드/언로드, 씬 타입 변경 등 씬 관련 이벤트
    /// 3. 게임 상태 이벤트: 게임 상태 변경, 플레이어 상태 변경 등 게임 관련 이벤트
    /// 4. 시스템별 이벤트: 귀신, 오디오, UI 등 각 시스템별 특화 이벤트
    /// 5. 메모리 관리: 이벤트 구독 해제를 통한 메모리 누수 방지
    /// 
    /// 사용 방법:
    /// ```csharp
    /// // 이벤트 구독
    /// ManagerEvents.OnGameStateChanged += OnGameStateChanged;
    /// 
    /// // 이벤트 발생
    /// ManagerEvents.TriggerGameStateChanged(GameState.Playing);
    /// 
    /// // 이벤트 구독 해제 (메모리 누수 방지)
    /// ManagerEvents.OnGameStateChanged -= OnGameStateChanged;
    /// ```
    /// 
    /// 장점:
    /// - 순환 의존성 제거: 매니저들이 서로 직접 참조하지 않음
    /// - 느슨한 결합: 이벤트를 통한 간접적 통신
    /// - 확장성: 새로운 이벤트 타입 쉽게 추가 가능
    /// - 디버깅 용이: 이벤트 흐름 추적 가능
    /// </summary>
    public static class ManagerEvents
    {
        // 매니저 생명주기 이벤트
        public static event Action<IManager> OnManagerInitialized;
        public static event Action<IManager> OnManagerReady;
        public static event Action<IManager> OnManagerReset;
        public static event Action<IManager> OnManagerDestroyed;

        // 씬 전환 이벤트
        public static event Action<string> OnSceneLoaded;
        public static event Action<string> OnSceneUnloaded;
        public static event Action<SceneType> OnSceneTypeChanged;

        // 게임 상태 이벤트
        public static event Action<GameManager.GameState> OnGameStateChanged;
        public static event Action<PlayerGameState> OnPlayerGameStateChanged;
        public static event Action OnPlayerDied;
        public static event Action OnGamePaused;
        public static event Action OnGameResumed;

        // 귀신 시스템 이벤트
        public static event Action OnGhostDetected;
        public static event Action OnGhostGone;
        public static event Action OnGhostAppeared;
        public static event Action OnGhostDisappeared;

        // 오디오 시스템 이벤트
        public static event Action<AudioClip, Vector3> OnSoundPlayed;
        public static event Action<AudioClip> OnSoundFinished;

        // UI 시스템 이벤트
        public static event Action<string> OnUIPanelChanged;
        public static event Action OnUIShowed;
        public static event Action OnUIHidden;

        // 이벤트 발생 메서드들
        public static void TriggerManagerInitialized(IManager manager)
        {
            OnManagerInitialized?.Invoke(manager);
        }

        public static void TriggerManagerReady(IManager manager)
        {
            OnManagerReady?.Invoke(manager);
        }

        public static void TriggerManagerReset(IManager manager)
        {
            OnManagerReset?.Invoke(manager);
        }

        public static void TriggerManagerDestroyed(IManager manager)
        {
            OnManagerDestroyed?.Invoke(manager);
        }

        public static void TriggerSceneLoaded(string sceneName)
        {
            OnSceneLoaded?.Invoke(sceneName);
        }

        public static void TriggerSceneUnloaded(string sceneName)
        {
            OnSceneUnloaded?.Invoke(sceneName);
        }

        public static void TriggerSceneTypeChanged(SceneType sceneType)
        {
            OnSceneTypeChanged?.Invoke(sceneType);
        }

        public static void TriggerGameStateChanged(GameManager.GameState newState)
        {
            OnGameStateChanged?.Invoke(newState);
        }

        public static void TriggerPlayerGameStateChanged(PlayerGameState newState)
        {
            OnPlayerGameStateChanged?.Invoke(newState);
        }

        public static void TriggerPlayerDied()
        {
            OnPlayerDied?.Invoke();
        }

        public static void TriggerGamePaused()
        {
            OnGamePaused?.Invoke();
        }

        public static void TriggerGameResumed()
        {
            OnGameResumed?.Invoke();
        }

        public static void TriggerGhostDetected()
        {
            OnGhostDetected?.Invoke();
        }

        public static void TriggerGhostGone()
        {
            OnGhostGone?.Invoke();
        }

        public static void TriggerGhostAppeared()
        {
            OnGhostAppeared?.Invoke();
        }

        public static void TriggerGhostDisappeared()
        {
            OnGhostDisappeared?.Invoke();
        }

        public static void TriggerSoundPlayed(AudioClip clip, Vector3 position)
        {
            OnSoundPlayed?.Invoke(clip, position);
        }

        public static void TriggerSoundFinished(AudioClip clip)
        {
            OnSoundFinished?.Invoke(clip);
        }

        public static void TriggerUIPanelChanged(string panelName)
        {
            OnUIPanelChanged?.Invoke(panelName);
        }

        public static void TriggerUIShowed()
        {
            OnUIShowed?.Invoke();
        }

        public static void TriggerUIHidden()
        {
            OnUIHidden?.Invoke();
        }

        /// <summary>
        /// 모든 이벤트 구독 해제 (메모리 누수 방지)
        /// </summary>
        public static void ClearAllEvents()
        {
            OnManagerInitialized = null;
            OnManagerReady = null;
            OnManagerReset = null;
            OnManagerDestroyed = null;
            OnSceneLoaded = null;
            OnSceneUnloaded = null;
            OnSceneTypeChanged = null;
            OnGameStateChanged = null;
            OnPlayerGameStateChanged = null;
            OnPlayerDied = null;
            OnGamePaused = null;
            OnGameResumed = null;
            OnGhostDetected = null;
            OnGhostGone = null;
            OnGhostAppeared = null;
            OnGhostDisappeared = null;
            OnSoundPlayed = null;
            OnSoundFinished = null;
            OnUIPanelChanged = null;
            OnUIShowed = null;
            OnUIHidden = null;
        }

        /// <summary>
        /// 특정 이벤트만 구독 해제
        /// </summary>
        /// <param name="eventType">이벤트 타입</param>
        public static void ClearEvent(ManagerEventType eventType)
        {
            switch (eventType)
            {
                case ManagerEventType.ManagerInitialized:
                    OnManagerInitialized = null;
                    break;
                case ManagerEventType.ManagerReady:
                    OnManagerReady = null;
                    break;
                case ManagerEventType.ManagerReset:
                    OnManagerReset = null;
                    break;
                case ManagerEventType.ManagerDestroyed:
                    OnManagerDestroyed = null;
                    break;
                case ManagerEventType.SceneLoaded:
                    OnSceneLoaded = null;
                    break;
                case ManagerEventType.SceneUnloaded:
                    OnSceneUnloaded = null;
                    break;
                case ManagerEventType.SceneTypeChanged:
                    OnSceneTypeChanged = null;
                    break;
                case ManagerEventType.GameStateChanged:
                    OnGameStateChanged = null;
                    break;
                case ManagerEventType.PlayerGameStateChanged:
                    OnPlayerGameStateChanged = null;
                    break;
                case ManagerEventType.PlayerDied:
                    OnPlayerDied = null;
                    break;
                case ManagerEventType.GamePaused:
                    OnGamePaused = null;
                    break;
                case ManagerEventType.GameResumed:
                    OnGameResumed = null;
                    break;
                case ManagerEventType.GhostDetected:
                    OnGhostDetected = null;
                    break;
                case ManagerEventType.GhostGone:
                    OnGhostGone = null;
                    break;
                case ManagerEventType.GhostAppeared:
                    OnGhostAppeared = null;
                    break;
                case ManagerEventType.GhostDisappeared:
                    OnGhostDisappeared = null;
                    break;
                case ManagerEventType.SoundPlayed:
                    OnSoundPlayed = null;
                    break;
                case ManagerEventType.SoundFinished:
                    OnSoundFinished = null;
                    break;
                case ManagerEventType.UIPanelChanged:
                    OnUIPanelChanged = null;
                    break;
                case ManagerEventType.UIShowed:
                    OnUIShowed = null;
                    break;
                case ManagerEventType.UIHidden:
                    OnUIHidden = null;
                    break;
            }
        }
    }

    /// <summary>
    /// 매니저 이벤트 타입 열거형
    /// </summary>
    public enum ManagerEventType
    {
        ManagerInitialized,
        ManagerReady,
        ManagerReset,
        ManagerDestroyed,
        SceneLoaded,
        SceneUnloaded,
        SceneTypeChanged,
        GameStateChanged,
        PlayerGameStateChanged,
        PlayerDied,
        GamePaused,
        GameResumed,
        GhostDetected,
        GhostGone,
        GhostAppeared,
        GhostDisappeared,
        SoundPlayed,
        SoundFinished,
        UIPanelChanged,
        UIShowed,
        UIHidden
    }
}
