using UnityEngine;
using System.Collections.Generic;
using System;
using DidYouHear.Manager.Interfaces;
using DidYouHear.Manager.System;
using DidYouHear.Core;

namespace DidYouHear.Manager.Core
{
    /// <summary>
    /// 3D 공간 오디오 관리자
    /// </summary>
    public class AudioManager : MonoBehaviour, IManager
    {
        [Header("Audio Settings")]
        public float masterVolume = 1f;
        public float sfxVolume = 1f;
        public float musicVolume = 1f;
        public float ambientVolume = 1f;
        
        [Header("3D Audio Settings")]
        public float maxDistance = 50f;
        public float minDistance = 1f;
        public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
        public float dopplerLevel = 1f;
        
        [Header("Audio Pool Settings")]
        public int audioSourcePoolSize = 20;
        public bool expandPool = true;
        
        [Header("Stone Impact Sounds")]
        public AudioClip[] stoneImpactSounds;
        public float stoneSoundVolume = 0.8f;
        public float stoneSoundPitch = 1f;
        public float stoneSoundRandomness = 0.2f;
        
        [Header("Environmental Sounds")]
        public AudioClip[] ambientSounds;
        public AudioClip[] ghostSounds;
        public AudioClip[] environmentalSounds;
        
        // 오디오 풀
        private Queue<AudioSource> audioSourcePool = new Queue<AudioSource>();
        private List<AudioSource> activeAudioSources = new List<AudioSource>();
        private Transform audioPoolParent;
        
        // 컴포넌트 참조
        private Transform playerTransform;
        private Camera playerCamera;
        
        // 이벤트
        public Action<AudioClip, Vector3> OnSoundPlayed;
        public Action<AudioClip> OnSoundFinished;
        
        // IManager 구현을 위한 필드
        private bool isInitialized = false;
        private bool isActive = true;
        private DependencyContainer dependencyContainer;
        
        // 싱글톤 패턴
        public static AudioManager Instance { get; private set; }
        
        private void Awake()
        {
            // 싱글톤 패턴 구현
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudio();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
        }
        
        private void Update()
        {
            if (isInitialized)
            {
                // 활성 오디오 소스 정리
                CleanupFinishedAudioSources();
            }
        }

        // IManager 인터페이스 구현
        public void Initialize()
        {
            if (isInitialized) return;

            // 플레이어 참조 초기화
            InitializePlayerReferences();
            // 오디오 풀 초기화
            InitializeAudioPool();
            isInitialized = true;
            Debug.Log("AudioManager initialized via IManager interface");
        }

        public void Reset()
        {
            // 오디오 풀 리셋
            // ClearAllAudio();
            Debug.Log("AudioManager reset");
        }


        public bool IsInitialized()
        {
            return isInitialized;
        }

        /// <summary>
        /// 오디오 풀 상태 정보 반환
        /// </summary>
        public string GetAudioStatus()
        {
            return $"AudioManager Status:\n" +
                   $"Master Volume: {masterVolume:F2}\n" +
                   $"SFX Volume: {sfxVolume:F2}\n" +
                   $"Music Volume: {musicVolume:F2}\n" +
                   $"Ambient Volume: {ambientVolume:F2}\n" +
                   $"Pool Size: {audioSourcePoolSize}\n" +
                   $"Available Sources: {audioSourcePool.Count}\n" +
                   $"Active Sources: {activeAudioSources.Count}\n" +
                   $"Max Distance: {maxDistance}\n" +
                   $"Min Distance: {minDistance}";
        }

        /// <summary>
        /// 오디오 시스템 초기화
        /// </summary>
        private void InitializeAudio()
        {
            // 오디오 풀 부모 오브젝트 생성
            GameObject poolObj = new GameObject("AudioPool");
            audioPoolParent = poolObj.transform;
            audioPoolParent.SetParent(transform);
            
            Debug.Log("Audio Manager Initialized");
        }
        
        /// <summary>
        /// 플레이어 참조 초기화
        /// </summary>
        private void InitializePlayerReferences()
        {
            // PlayerController 찾기
            PlayerController playerController = GameManager.Instance.playerController;
            if (playerController != null)
            {
                playerTransform = playerController.transform;
                playerCamera = playerController.GetComponentInChildren<Camera>();
            }
            else
            {
                Debug.LogWarning("AudioManager: PlayerController not found!");
            }
        }
        
        /// <summary>
        /// 오디오 풀 초기화
        /// </summary>
        private void InitializeAudioPool()
        {
            for (int i = 0; i < audioSourcePoolSize; i++)
            {
                CreateAudioSource();
            }
        }
        
        /// <summary>
        /// AudioSource 생성
        /// </summary>
        private AudioSource CreateAudioSource()
        {
            GameObject audioObj = new GameObject($"AudioSource_{audioSourcePool.Count}");
            audioObj.transform.SetParent(audioPoolParent);
            
            AudioSource audioSource = audioObj.AddComponent<AudioSource>();
            SetupAudioSource(audioSource);
            
            audioSourcePool.Enqueue(audioSource);
            return audioSource;
        }
        
        /// <summary>
        /// AudioSource 설정
        /// </summary>
        private void SetupAudioSource(AudioSource audioSource)
        {
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D 사운드
            audioSource.rolloffMode = rolloffMode;
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
            audioSource.dopplerLevel = dopplerLevel;
            audioSource.volume = sfxVolume * masterVolume;
        }
        
        /// <summary>
        /// 3D 사운드 재생
        /// </summary>
        public void Play3DSound(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            
            AudioSource audioSource = GetAudioSource();
            if (audioSource == null) return;
            
            // 위치 설정
            audioSource.transform.position = position;
            
            // 사운드 설정
            audioSource.clip = clip;
            audioSource.volume = volume * sfxVolume * masterVolume;
            audioSource.pitch = pitch;
            
            // 재생
            audioSource.Play();
            activeAudioSources.Add(audioSource);
            
            OnSoundPlayed?.Invoke(clip, position);
            Debug.Log($"3D Sound played: {clip.name} at {position}");
        }
        
        /// <summary>
        /// 공깃돌 충돌음 재생
        /// </summary>
        public void PlayStoneImpactSound(Vector3 position, float volumeMultiplier = 1f)
        {
            if (stoneImpactSounds == null || stoneImpactSounds.Length == 0) return;
            
            // 랜덤 사운드 선택
            AudioClip sound = stoneImpactSounds[UnityEngine.Random.Range(0, stoneImpactSounds.Length)];
            
            // 피치 랜덤화
            float randomPitch = stoneSoundPitch + UnityEngine.Random.Range(-stoneSoundRandomness, stoneSoundRandomness);
            
            // 속도에 따른 볼륨 적용
            float finalVolume = stoneSoundVolume * volumeMultiplier;
            
            // 3D 사운드 재생
            Play3DSound(sound, position, finalVolume, randomPitch);
        }
        
        /// <summary>
        /// 플레이어 위치 기준 3D 사운드 재생
        /// </summary>
        public void Play3DSoundAtPlayer(AudioClip clip, Vector3 offset, float volume = 1f, float pitch = 1f)
        {
            if (playerTransform == null) return;
            
            Vector3 position = playerTransform.position + offset;
            Play3DSound(clip, position, volume, pitch);
        }
        
        /// <summary>
        /// 뒤쪽 사운드 재생 (공깃돌 던지기)
        /// </summary>
        public void PlayBehindPlayerSound(AudioClip clip, float distance = 2f, float volume = 1f, float pitch = 1f)
        {
            if (playerTransform == null) return;
            
            Vector3 behindPosition = playerTransform.position - playerTransform.forward * distance;
            Play3DSound(clip, behindPosition, volume, pitch);
        }
        
        /// <summary>
        /// 좌우 방향 사운드 재생
        /// </summary>
        public void PlayDirectionalSound(AudioClip clip, Vector3 direction, float distance = 3f, float volume = 1f, float pitch = 1f)
        {
            if (playerTransform == null) return;
            
            Vector3 position = playerTransform.position + direction.normalized * distance;
            Play3DSound(clip, position, volume, pitch);
        }
        
        /// <summary>
        /// AudioSource 가져오기
        /// </summary>
        private AudioSource GetAudioSource()
        {
            AudioSource audioSource = null;
            
            if (audioSourcePool.Count > 0)
            {
                audioSource = audioSourcePool.Dequeue();
            }
            else if (expandPool)
            {
                audioSource = CreateAudioSource();
                Debug.Log("AudioManager: Audio pool expanded");
            }
            else
            {
                Debug.LogWarning("AudioManager: No available audio sources in pool!");
                return null;
            }
            
            return audioSource;
        }
        
        /// <summary>
        /// AudioSource 반환
        /// </summary>
        private void ReturnAudioSource(AudioSource audioSource)
        {
            if (audioSource == null) return;
            
            // 정지
            audioSource.Stop();
            audioSource.clip = null;
            
            // 활성 목록에서 제거
            if (activeAudioSources.Contains(audioSource))
            {
                activeAudioSources.Remove(audioSource);
            }
            
            // 풀로 반환
            audioSourcePool.Enqueue(audioSource);
        }
        
        /// <summary>
        /// 완료된 오디오 소스 정리
        /// </summary>
        private void CleanupFinishedAudioSources()
        {
            for (int i = activeAudioSources.Count - 1; i >= 0; i--)
            {
                AudioSource audioSource = activeAudioSources[i];
                if (audioSource == null || !audioSource.isPlaying)
                {
                    if (audioSource != null)
                    {
                        OnSoundFinished?.Invoke(audioSource.clip);
                    }
                    ReturnAudioSource(audioSource);
                }
            }
        }
        
        /// <summary>
        /// 볼륨 설정
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateAllAudioSourceVolumes();
        }
        
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            UpdateAllAudioSourceVolumes();
        }
        
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
        }
        
        public void SetAmbientVolume(float volume)
        {
            ambientVolume = Mathf.Clamp01(volume);
        }
        
        /// <summary>
        /// 모든 AudioSource 볼륨 업데이트
        /// </summary>
        private void UpdateAllAudioSourceVolumes()
        {
            foreach (AudioSource audioSource in activeAudioSources)
            {
                if (audioSource != null)
                {
                    audioSource.volume = sfxVolume * masterVolume;
                }
            }
        }
        
        /// <summary>
        /// 3D 오디오 설정 업데이트
        /// </summary>
        public void Update3DAudioSettings(float maxDist, float minDist, AudioRolloffMode rolloff)
        {
            maxDistance = maxDist;
            minDistance = minDist;
            rolloffMode = rolloff;
            
            // 모든 AudioSource 업데이트
            foreach (AudioSource audioSource in activeAudioSources)
            {
                if (audioSource != null)
                {
                    audioSource.maxDistance = maxDistance;
                    audioSource.minDistance = minDistance;
                    audioSource.rolloffMode = rolloffMode;
                }
            }
        }
        
        /// <summary>
        /// 공깃돌 사운드 설정
        /// </summary>
        public void SetStoneSoundSettings(AudioClip[] sounds, float volume, float pitch, float randomness)
        {
            stoneImpactSounds = sounds;
            stoneSoundVolume = volume;
            stoneSoundPitch = pitch;
            stoneSoundRandomness = randomness;
        }
        
        /// <summary>
        /// 모든 사운드 정지
        /// </summary>
        public void StopAllSounds()
        {
            foreach (AudioSource audioSource in activeAudioSources)
            {
                if (audioSource != null)
                {
                    audioSource.Stop();
                }
            }
        }
        
        // IManager 인터페이스 구현
        public int GetPriority()
        {
            return (int)ManagerType.System; // 10
        }
        
        public ManagerType GetManagerType()
        {
            return ManagerType.System;
        }
        
        public ManagerScope GetManagerScope()
        {
            return ManagerScope.Global; // 모든 씬에서 유지
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
            return $"AudioManager: Initialized={isInitialized}, Active={isActive}, " +
                   $"PoolSize={audioSourcePool.Count}, ActiveSources={activeAudioSources.Count}";
        }
    }
}
