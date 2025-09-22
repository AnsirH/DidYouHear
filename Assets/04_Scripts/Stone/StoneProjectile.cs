using UnityEngine;
using DidYouHear.Manager.Core;
using DidYouHear.Manager.Gameplay;

namespace DidYouHear.Stone
{
    /// <summary>
    /// 공깃돌 발사체 컴포넌트
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class StoneProjectile : MonoBehaviour
    {
        [Header("Stone Settings")]
        public float lifetime = 10f;
        public int maxBounces = 10;  // 최대 바운스 횟수 증가
        
        [Header("Sound Settings")]
        public AudioClip[] impactSounds;
        public float soundVolume = 1f;
        public float soundPitch = 1f;
        public float soundRandomness = 0.2f;
        
        // 컴포넌트 참조
        private Rigidbody rb;
        private Collider col;
        private AudioSource audioSource;
        private StonePool pool;
        
        // 상태
        private float currentLifetime;
        private int bounceCount = 0;
        private bool hasLanded = false;
        private Vector3 lastVelocity;
        
        // 이벤트
        public System.Action<Vector3> OnStoneLanded;
        public System.Action<Vector3> OnStoneBounced;
        public System.Action OnStoneDestroyed;
        
        private void Awake()
        {
            // 컴포넌트 초기화
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();
            audioSource = GetComponent<AudioSource>();
            
            // AudioSource 설정
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            SetupAudioSource();
            SetupPhysics();
        }
        
        private void Start()
        {
            // 초기화
            currentLifetime = lifetime;
            bounceCount = 0;
            hasLanded = false;
        }
        
        private void Update()
        {
            // 생명주기 관리
            UpdateLifetime();
            
            // 속도 추적 (velocity가 유효할 때만)
            if (rb != null && rb.velocity.magnitude > 0.01f)
            {
                lastVelocity = rb.velocity;
            }
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            HandleCollision(collision);
        }
        
        /// <summary>
        /// AudioSource 설정
        /// </summary>
        private void SetupAudioSource()
        {
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D 사운드
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 50f;
            audioSource.volume = soundVolume;
            audioSource.pitch = soundPitch;
        }
        
        /// <summary>
        /// 물리 설정
        /// </summary>
        private void SetupPhysics()
        {
            // Rigidbody 설정
            rb.mass = 0.5f;  // 질량 증가
            rb.drag = 0.05f;  // 공기 저항 감소
            rb.angularDrag = 0.05f;  // 회전 저항 감소
            rb.useGravity = true;
            
            // Collider 설정
            col.isTrigger = false;
        }
        
        /// <summary>
        /// 생명주기 업데이트
        /// </summary>
        private void UpdateLifetime()
        {
            currentLifetime -= Time.deltaTime;
            
            if (currentLifetime <= 0f)
            {
                DestroyStone();
            }
        }
        
        /// <summary>
        /// 충돌 처리
        /// </summary>
        private void HandleCollision(Collision collision)
        {
            // 바닥 충돌 감지
            if (IsGroundCollision(collision))
            {
                if (!hasLanded)
                {
                    // 첫 번째 착지 - 이벤트 발생 및 사운드 재생
                    OnStoneLanded?.Invoke(collision.contacts[0].point);
                    hasLanded = true;
                    PlayImpactSound(); // 첫 번째 착지에서만 사운드 재생
                    Debug.Log("Stone landed on ground - First impact sound played");
                }
                
                // 바닥 충돌 시 항상 바운스 처리 (첫 번째 착지 포함)
                HandleBounce(collision);
            }
            else
            {
                // 벽이나 다른 오브젝트와의 충돌
                HandleBounce(collision);
            }
        }
        
        /// <summary>
        /// 바닥 충돌 확인
        /// </summary>
        private bool IsGroundCollision(Collision collision)
        {
            // 충돌 지점의 법선 벡터를 확인하여 바닥인지 판단
            Vector3 normal = collision.contacts[0].normal;
            return Vector3.Dot(normal, Vector3.up) > 0.7f; // 45도 이하의 각도
        }
        
        /// <summary>
        /// 바운스 처리
        /// </summary>
        private void HandleBounce(Collision collision)
        {
            if (bounceCount >= maxBounces)
            {
                // 최대 바운스 횟수 초과 시 정지
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                Debug.Log("Stone stopped: Max bounces reached");
                return;
            }

            bounceCount++;
            OnStoneBounced?.Invoke(collision.contacts[0].point);
                        
            Debug.Log($"Stone bounced (Count: {bounceCount}) - No sound on bounce");
        }
        
        /// <summary>
        /// 충돌음 재생 
        /// </summary>
        private void PlayImpactSound()
        {            
            // AudioManager를 통한 3D 사운드 재생
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayStoneImpactSound(transform.position);
            }
            else
            {
                // AudioManager가 없을 경우 로컬 재생
                if (impactSounds == null || impactSounds.Length == 0) return;
                
                // 랜덤 사운드 선택
                AudioClip sound = impactSounds[Random.Range(0, impactSounds.Length)];
                if (sound != null)
                {
                    // 피치 랜덤화
                    float randomPitch = soundPitch + Random.Range(-soundRandomness, soundRandomness);
                    audioSource.pitch = randomPitch;
                    
                    // 사운드 재생
                    audioSource.PlayOneShot(sound);
                    
                    Debug.Log($"Impact sound");
                }
            }
        }
        
        /// <summary>
        /// 공깃돌 파괴
        /// </summary>
        public void DestroyStone()
        {
            OnStoneDestroyed?.Invoke();
            
            if (pool != null)
            {
                // 풀로 반환
                pool.ReturnStone(gameObject);
            }
            else
            {
                // 직접 파괴
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// 풀 참조 설정
        /// </summary>
        public void SetPool(StonePool stonePool)
        {
            pool = stonePool;
        }
        
        /// <summary>
        /// 공깃돌 초기화 ( 활성화 시 자동 호출 )
        /// </summary>
        public void Initialize()
        {
            // 상태 초기화
            currentLifetime = lifetime;
            bounceCount = 0;
            hasLanded = false;
            
            // velocity는 던지기 시에만 설정되므로 여기서는 리셋하지 않음

            Debug.Log("StoneProjectile initialized");
        }
        
        /// <summary>
        /// 물리 상태 리셋 (비활성화 시 자동 호출)
        /// </summary>
        public void ResetPhysicsState()
        {
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            // 상태 리셋
            currentLifetime = lifetime;
            bounceCount = 0;
            hasLanded = false;
            
            Debug.Log("StoneProjectile physics state reset");
        }
        
        /// <summary>
        /// 생명주기 설정
        /// </summary>
        public void SetLifetime(float newLifetime)
        {
            lifetime = newLifetime;
            currentLifetime = newLifetime;
        }
        
        /// <summary>
        /// 바운스 설정
        /// </summary>
        public void SetBounceSettings(int maxBounces)
        {
            // 바운스 로직이 제거되어 이 메서드는 maxBounces만 설정
            this.maxBounces = maxBounces;
        }
        
        /// <summary>
        /// 사운드 설정
        /// </summary>
        public void SetSoundSettings(AudioClip[] sounds, float volume, float pitch, float randomness)
        {
            impactSounds = sounds;
            soundVolume = volume;
            soundPitch = pitch;
            soundRandomness = randomness;
            
            // AudioSource 업데이트
            audioSource.volume = volume;
            audioSource.pitch = pitch;
        }
        
        /// <summary>
        /// 현재 상태 정보 반환
        /// </summary>
        public string GetProjectileStatus()
        {
            return $"StoneProjectile Status:\n" +
                   $"Lifetime: {currentLifetime:F2}s / {lifetime:F2}s\n" +
                   $"Bounce Count: {bounceCount} / {maxBounces}\n" +
                   $"Has Landed: {hasLanded}\n" +
                   $"Velocity: {rb.velocity.magnitude:F2} m/s\n" +
                   $"Position: {transform.position}";
        }
        
        private void OnEnable()
        {
            // 오브젝트가 활성화될 때 초기화
            Initialize();
        }
        
        private void OnDisable()
        {
            // 오브젝트가 비활성화될 때 물리 상태 리셋
            ResetPhysicsState();
        }
    }
}
