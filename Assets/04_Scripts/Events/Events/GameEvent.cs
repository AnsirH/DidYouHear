using UnityEngine;
using System;
using DidYouHear.Manager.Gameplay;

namespace DidYouHear.Events
{
    /// <summary>
    /// 모든 게임 이벤트의 기본 클래스 (데이터 관리용)
    /// </summary>
    public abstract class GameEvent
    {
        protected EventManager eventManager;
        protected bool isInitialized = false;
        protected bool isCompleted = false;
        
        /// <summary>
        /// 이벤트 초기화
        /// </summary>
        public virtual void Initialize(EventManager manager)
        {
            eventManager = manager;
            isInitialized = true;
            isCompleted = false;
        }
        
        /// <summary>
        /// 이벤트 실행
        /// </summary>
        public abstract void Execute();
        
        /// <summary>
        /// 이벤트 업데이트 (EventManager에서 매 프레임 호출)
        /// </summary>
        public virtual void UpdateEvent() { }

        /// <summary>
        /// 이벤트 완료 처리
        /// </summary>
        protected void Complete(bool success)
        {
            isCompleted = true;
            OnEventCompleted?.Invoke(success);
            Debug.Log($"{GetEventType()} Event Completed. Success: {success}");
        }

        /// <summary>
        /// 이벤트 실패 처리
        /// </summary>
        protected void Fail()
        {
            isCompleted = true;
            OnEventCompleted?.Invoke(false);
            Debug.Log($"{GetEventType()} Event Failed.");
        }

        /// <summary>
        /// 반응 시간 초과 처리
        /// </summary>
        public virtual void OnReactionTimeout()
        {
            Fail(); // 기본적으로 반응 시간 초과는 실패로 처리
        }

        public Action<bool> OnEventCompleted; // bool: success
        
        /// <summary>
        /// 이벤트 타입 반환
        /// </summary>
        public abstract EventManager.EventType GetEventType();
        
        /// <summary>
        /// 초기화 여부 반환
        /// </summary>
        public bool IsInitialized()
        {
            return isInitialized;
        }
        
        /// <summary>
        /// 완료 여부 반환
        /// </summary>
        public bool IsCompleted()
        {
            return isCompleted;
        }
    }
}
