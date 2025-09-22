using UnityEngine;
using System.Collections;
using DidYouHear.Ghost;
using DidYouHear.Manager.Gameplay;

namespace DidYouHear.Ghost
{
    /// <summary>
    /// 귀신 등장 연출 컴포넌트
    /// </summary>
    public class GhostAppearance : MonoBehaviour
    {
        [Header("Appearance Settings")]
        public float fadeInDuration = 0.5f;
        public float fadeOutDuration = 0.5f;
        public float appearanceDuration = 2f;
        public float floatHeight = 0.5f;
        public float floatSpeed = 2f;
        
        [Header("Visual Effects")]
        public float glowIntensity = 2f;
        public Color glowColor = Color.white;
        public float distortionStrength = 0.1f;
        
        private GhostManager ghostManager;
        private Renderer ghostRenderer;
        private Material ghostMaterial;
        private Color originalColor;
        private Vector3 originalPosition;
        private bool isInitialized = false;
        
        private void Awake()
        {
            // 컴포넌트 초기화
            ghostRenderer = GetComponent<Renderer>();
            if (ghostRenderer != null)
            {
                ghostMaterial = ghostRenderer.material;
                originalColor = ghostMaterial.color;
            }
            
            originalPosition = transform.position;
        }
        
        private void Start()
        {
            if (isInitialized)
            {
                StartAppearance();
            }
        }
        
        private void Update()
        {
            if (isInitialized)
            {
                UpdateFloating();
            }
        }
        
        /// <summary>
        /// 귀신 등장 초기화
        /// </summary>
        public void Initialize(GhostManager manager)
        {
            ghostManager = manager;
            isInitialized = true;
            
            // 등장 시작
            StartAppearance();
        }
        
        /// <summary>
        /// 등장 연출 시작
        /// </summary>
        private void StartAppearance()
        {
            // 페이드 인 시작
            StartCoroutine(FadeIn());
            
            // 등장 지속 시간 후 페이드 아웃
            StartCoroutine(DisappearAfterDelay());
        }
        
        /// <summary>
        /// 페이드 인 코루틴
        /// </summary>
        private IEnumerator FadeIn()
        {
            if (ghostMaterial == null) yield break;
            
            float startTime = Time.time;
            Color startColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
            Color targetColor = originalColor;
            
            while (Time.time - startTime < fadeInDuration)
            {
                float progress = (Time.time - startTime) / fadeInDuration;
                ghostMaterial.color = Color.Lerp(startColor, targetColor, progress);
                yield return null;
            }
            
            ghostMaterial.color = targetColor;
        }
        
        /// <summary>
        /// 페이드 아웃 코루틴
        /// </summary>
        private IEnumerator FadeOut()
        {
            if (ghostMaterial == null) yield break;
            
            float startTime = Time.time;
            Color startColor = ghostMaterial.color;
            Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
            
            while (Time.time - startTime < fadeOutDuration)
            {
                float progress = (Time.time - startTime) / fadeOutDuration;
                ghostMaterial.color = Color.Lerp(startColor, targetColor, progress);
                yield return null;
            }
            
            ghostMaterial.color = targetColor;
        }
        
        /// <summary>
        /// 지연 후 사라지기
        /// </summary>
        private IEnumerator DisappearAfterDelay()
        {
            yield return new WaitForSeconds(appearanceDuration);
            yield return StartCoroutine(FadeOut());
        }
        
        /// <summary>
        /// 떠다니는 효과 업데이트
        /// </summary>
        private void UpdateFloating()
        {
            if (ghostMaterial == null) return;
            
            // 위아래로 떠다니는 효과
            float newY = originalPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            
            // 글로우 효과
            UpdateGlowEffect();
        }
        
        /// <summary>
        /// 글로우 효과 업데이트
        /// </summary>
        private void UpdateGlowEffect()
        {
            if (ghostMaterial == null) return;
            
            // 글로우 강도 변화
            float glow = glowIntensity + Mathf.Sin(Time.time * 3f) * 0.5f;
            ghostMaterial.SetFloat("_EmissionIntensity", glow);
            
            // 색상 변화
            Color currentColor = Color.Lerp(originalColor, glowColor, Mathf.Sin(Time.time * 2f) * 0.3f + 0.3f);
            ghostMaterial.color = currentColor;
        }
        
        /// <summary>
        /// 귀신 제거
        /// </summary>
        public void Disappear()
        {
            StartCoroutine(FadeOut());
        }
        
        /// <summary>
        /// 즉시 제거
        /// </summary>
        public void DisappearImmediately()
        {
            if (ghostMaterial != null)
            {
                ghostMaterial.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
            }
            
            gameObject.SetActive(false);
        }
    }
}
