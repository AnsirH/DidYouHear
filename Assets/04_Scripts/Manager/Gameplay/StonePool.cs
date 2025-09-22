using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;
using System;
using DidYouHear.Manager.Interfaces;
using DidYouHear.Manager.System;
using DidYouHear.Stone;

namespace DidYouHear.Manager.Gameplay
{
    /// <summary>
    /// 공깃돌 오브젝트 풀 관리자 (Unity ObjectPool 사용)
    /// </summary>
    public class StonePool : MonoBehaviour, IManager
    {
        [Header("Pool Settings")]
        public GameObject stonePrefab;
        public int defaultCapacity = 10;
        public int maxSize = 20;
        
        // Unity ObjectPool
        private ObjectPool<GameObject> objectPool;
        private Transform poolParent;
        
        // 활성 오브젝트 추적
        private List<GameObject> activeStones = new List<GameObject>();
        
        // 이벤트
        public Action<GameObject> OnStoneSpawned;
        public Action<GameObject> OnStoneReturned;
        
        // IManager 구현을 위한 필드
        private bool isInitialized = false;
        private bool isActive = true;
        private DependencyContainer dependencyContainer;

        private void OnDestroy()
        {
            // ObjectPool 정리
            if (objectPool != null)
            {
                objectPool.Clear();
            }
        }

        /// <summary>
        /// 풀 초기화
        /// </summary>
        public void Initialize(GameObject prefab, int capacity)
        {
            stonePrefab = prefab;
            defaultCapacity = capacity;
            poolParent = transform;
            
            // Unity ObjectPool 생성
            CreateObjectPool();
            
            Debug.Log($"StonePool: Initialized with Unity ObjectPool (Capacity: {defaultCapacity}, MaxSize: {maxSize})");
        }
        
        /// <summary>
        /// Unity ObjectPool 생성
        /// </summary>
        private void CreateObjectPool()
        {
            if (stonePrefab == null)
            {
                Debug.LogError("StonePool: Stone prefab is null!");
                return;
            }
            
            // ObjectPool 생성
            objectPool = new ObjectPool<GameObject>(
                createFunc: CreateStone,           // 오브젝트 생성 함수
                actionOnGet: OnGetStone,           // 풀에서 가져올 때 실행
                actionOnRelease: OnReleaseStone,   // 풀로 반환할 때 실행
                actionOnDestroy: OnDestroyStone,   // 오브젝트 파괴할 때 실행
                collectionCheck: false,            // 릴리즈 모드에서는 false로 설정하여 성능 최적화
                defaultCapacity: defaultCapacity,  // 기본 용량
                maxSize: maxSize                   // 최대 크기
            );
        }
        
        /// <summary>
        /// 공깃돌 생성 (ObjectPool에서 호출)
        /// </summary>
        private GameObject CreateStone()
        {
            GameObject stone = Instantiate(stonePrefab, poolParent);
            
            // StoneProjectile 컴포넌트 추가
            StoneProjectile stoneProjectile = stone.GetComponent<StoneProjectile>();
            if (stoneProjectile == null)
            {
                stoneProjectile = stone.AddComponent<StoneProjectile>();
            }
            
            // 풀 참조 설정
            stoneProjectile.SetPool(this);
            
            return stone;
        }
        
        /// <summary>
        /// 풀에서 가져올 때 실행 (ObjectPool에서 호출)
        /// </summary>
        private void OnGetStone(GameObject stone)
        {
            if (stone == null) return;
            
            // 월드 좌표계로 스폰하기 위해 부모를 null로 설정
            stone.transform.SetParent(null);
            
            // 활성화
            stone.SetActive(true);
            activeStones.Add(stone);
            
            OnStoneSpawned?.Invoke(stone);
        }
        
        /// <summary>
        /// 풀로 반환할 때 실행 (ObjectPool에서 호출)
        /// </summary>
        private void OnReleaseStone(GameObject stone)
        {
            if (stone == null) return;
            
            // 활성 목록에서 제거
            if (activeStones.Contains(stone))
            {
                activeStones.Remove(stone);
            }
            
            // 비활성화 (OnDisable에서 자동으로 ResetPhysicsState 호출됨)
            stone.SetActive(false);
            stone.transform.SetParent(poolParent);
            
            OnStoneReturned?.Invoke(stone);
        }
        
        /// <summary>
        /// 오브젝트 파괴할 때 실행 (ObjectPool에서 호출)
        /// </summary>
        private void OnDestroyStone(GameObject stone)
        {
            if (stone != null)
            {
                DestroyImmediate(stone);
            }
        }
        
        /// <summary>
        /// 풀에서 공깃돌 가져오기
        /// </summary>
        public GameObject GetStone()
        {
            if (objectPool == null)
            {
                Debug.LogError("StonePool: ObjectPool not initialized!");
                return null;
            }
            
            return objectPool.Get();
        }
        
        /// <summary>
        /// 공깃돌을 풀로 반환
        /// </summary>
        public void ReturnStone(GameObject stone)
        {
            if (stone == null || objectPool == null) return;
            
            objectPool.Release(stone);
        }
        
        /// <summary>
        /// 모든 활성 공깃돌 반환
        /// </summary>
        public void ReturnAllStones()
        {
            for (int i = activeStones.Count - 1; i >= 0; i--)
            {
                if (activeStones[i] != null)
                {
                    ReturnStone(activeStones[i]);
                }
            }
        }
        
        /// <summary>
        /// 풀 상태 정보 반환
        /// </summary>
        public string GetPoolStatus()
        {
            if (objectPool == null)
            {
                return "StonePool: ObjectPool not initialized";
            }

            return $"StonePool Status (Unity ObjectPool):\n" +
                   $"Default Capacity: {defaultCapacity}\n" +
                   $"Max Size: {maxSize}\n" +
                   $"Active: {activeStones.Count}\n";
        }
        
        /// <summary>
        /// 풀 크기 설정
        /// </summary>
        public void SetPoolSize(int newCapacity, int newMaxSize)
        {
            defaultCapacity = newCapacity;
            maxSize = newMaxSize;
            
            // ObjectPool 재생성
            if (objectPool != null)
            {
                objectPool.Clear();
                CreateObjectPool();
            }
        }
        
        
        /// <summary>
        /// 풀 초기화 (모든 공깃돌 제거)
        /// </summary>
        public void ClearPool()
        {
            if (objectPool != null)
            {
                objectPool.Clear();
            }
            
            activeStones.Clear();
            Debug.Log("StonePool: Pool cleared");
        }
        
        /// <summary>
        /// 풀 통계 정보 반환
        /// </summary>
        public string GetPoolStatistics()
        {
            if (objectPool == null)
            {
                return "ObjectPool not initialized";
            }
            
            return $"Pool Statistics:\n" +
                   $"Count All: {objectPool.CountAll}\n" +
                   $"Count Active: {objectPool.CountActive}\n" +
                   $"Count Inactive: {objectPool.CountInactive}";
        }
        
        /// <summary>
        /// 풀 리셋 (게임 재시작 시 호출)
        /// </summary>
        public void ResetPool()
        {
            // 모든 활성화된 공깃돌 비활성화
            if (objectPool != null)
            {
                foreach (var stone in activeStones)
                {
                    if (stone != null)
                    {
                        stone.gameObject.SetActive(false);
                    }
                }
            }
            
            Debug.Log("StonePool reset successfully");
        }
        
        // IManager 인터페이스 구현
        public int GetPriority()
        {
            return (int)ManagerType.Gameplay + 5; // 25
        }
        
        public ManagerType GetManagerType()
        {
            return ManagerType.Gameplay;
        }
        
        public ManagerScope GetManagerScope()
        {
            return ManagerScope.Gameplay; // 인게임 전용
        }
        
        public void SetDependencyContainer(DependencyContainer container)
        {
            dependencyContainer = container;
        }
        
        public void SetActive(bool active)
        {
            isActive = active;
            gameObject.SetActive(active);
        }
        
        public string GetStatus()
        {
            return $"StonePool: Initialized={isInitialized}, Active={isActive}, " +
                   $"ActiveStones={activeStones.Count}, PoolSize={objectPool?.CountAll ?? 0}";
        }
        
        public void Initialize()
        {
            if (!isInitialized)
            {
                // 기본 설정으로 풀 초기화
                if (stonePrefab != null)
                {
                    Initialize(stonePrefab, defaultCapacity);
                }
                isInitialized = true;
            }
        }
        
        private void InitializePool()
        {
            // 풀 초기화 로직
            if (stonePrefab != null)
            {
                Initialize(stonePrefab, defaultCapacity);
            }
        }
        
        public void Reset()
        {
            isInitialized = false;
            isActive = false;
            activeStones.Clear();
            if (objectPool != null)
            {
                objectPool.Clear();
            }
        }
        
        public bool IsInitialized()
        {
            return isInitialized;
        }
    }
}
