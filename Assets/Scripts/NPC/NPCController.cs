using UnityEngine;
using DG.Tweening;

namespace MaskCompany
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class NPCController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private NPCCollection npcCollection;
        [SerializeField] private NPCConfig currentConfig;

        [Header("Range Indicator")]
        [SerializeField] private Sprite rangeSprite;
        [SerializeField] private bool showRangeGizmo = true;

        [Header("Breathing Animation")]
        [SerializeField] private float baseBreatheDuration = 1.5f;
        [SerializeField] private float breatheScale = 0.05f;

        [Header("Particles")]
        [SerializeField] private ParticleConfig particleConfig;
        [SerializeField] private bool useParticles = true;

        [Header("Comfort System")]
        [SerializeField] private float comfortChangeSpeed = 1f;    // Multiplier for player influence
        [SerializeField] private float comfortDecaySpeed = 0.05f;  // How fast comfort returns to neutral (slow)

        [Header("Emotion State")]
        [SerializeField, Range(-1f, 1f)] private float comfortLevel; // -1 (upset) to +1 (happy)
        [SerializeField] private CompatibilityResult currentResult;
        
        [Header("Runtime Debug")]
        [SerializeField] private float targetComfort;
        [SerializeField] private bool isPlayerInRange;
        [SerializeField] private MaskType detectedPlayerMask;
        [SerializeField] private float currentBreatheSpeed;
        [SerializeField] private float currentParticleRate;

        private SpriteRenderer spriteRenderer;
        private SpriteRenderer rangeIndicator;
        private PlayerController playerInRange;
        private Rigidbody2D rb;
        private Tweener breatheTween;
        private ParticleSystem emotionParticles;
        private Vector3 originalScale;
        private CompatibilityResult lastParticleResult;

        public PersonalityType Personality => currentConfig != null ? currentConfig.personality : PersonalityType.Loner;
        public float DetectionRange => currentConfig != null ? currentConfig.detectionRange : 3f;
        public float ComfortLevel => comfortLevel;
        public NPCConfig CurrentConfig => currentConfig;

        /// <summary>
        /// Right-click menu: Apply current config visuals (sprite, color)
        /// </summary>
        [ContextMenu("Apply Current Config")]
        private void ApplyCurrentConfig()
        {
            if (currentConfig == null)
            {
                Debug.LogWarning("No current config to apply!");
                return;
            }

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            ApplyConfig();
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(spriteRenderer);
            #endif
            
            Debug.Log($"Applied visuals from: {currentConfig.name} ({currentConfig.personality})");
        }

        /// <summary>
        /// Right-click menu: Assign random config from collection
        /// </summary>
        [ContextMenu("Assign Random Config")]
        private void AssignRandomConfig()
        {
            if (npcCollection == null)
            {
                Debug.LogWarning("No NPC Collection assigned!");
                return;
            }

            currentConfig = npcCollection.GetRandom();
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
            
            Debug.Log($"Assigned config: {currentConfig.name} ({currentConfig.personality}) - use 'Apply Current Config' to see visuals");
        }

        /// <summary>
        /// Right-click menu: Clear config (will randomize at runtime)
        /// </summary>
        [ContextMenu("Clear Config (Randomize at Runtime)")]
        private void ClearConfig()
        {
            currentConfig = null;
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
            
            Debug.Log("Config cleared - will randomize at runtime");
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            originalScale = transform.localScale;
            
            rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
            }
        }

        private void Start()
        {
            if (currentConfig == null && npcCollection != null)
            {
                currentConfig = npcCollection.GetRandom();
            }

            ApplyConfig();
            CreateRangeIndicator();
            StartBreatheAnimation();
            
            if (useParticles)
            {
                CreateParticleSystem();
            }
        }

        private void OnDestroy()
        {
            breatheTween?.Kill();
        }

        private void ApplyConfig()
        {
            if (currentConfig == null) return;

            if (currentConfig.sprite != null)
            {
                spriteRenderer.sprite = currentConfig.sprite;
            }

            if (currentConfig.tintColor != Color.white)
            {
                spriteRenderer.color = currentConfig.tintColor;
            }
            else
            {
                spriteRenderer.color = PersonalitySystem.GetPersonalitySolidColor(currentConfig.personality);
            }

            gameObject.name = $"NPC_{currentConfig.npcName}_{currentConfig.personality}";
        }

        private void Update()
        {
            CheckForPlayer();
            UpdateComfort();
            UpdateVisuals();
        }

        #region Comfort System

        private void UpdateComfort()
        {
            if (playerInRange != null)
            {
                var mask = playerInRange.CurrentMask;
                var compatibility = PersonalitySystem.GetCompatibility(mask, Personality);
                
                // Get target and speed based on compatibility result
                targetComfort = PersonalitySystem.GetTargetComfort(compatibility);
                float influenceSpeed = PersonalitySystem.GetInfluenceSpeed(mask, compatibility);
                
                // Gradual change toward target (speed varies: 1.5x for harsh, 0.5x for normal, halved for Neutral mask)
                comfortLevel = Mathf.MoveTowards(comfortLevel, targetComfort, influenceSpeed * comfortChangeSpeed * Time.deltaTime);
            }
            else
            {
                // Slowly return to neutral when player not in range
                targetComfort = 0f;
                comfortLevel = Mathf.MoveTowards(comfortLevel, 0f, comfortDecaySpeed * Time.deltaTime);
            }

            // Clamp
            comfortLevel = Mathf.Clamp(comfortLevel, -1f, 1f);

            // Update current result for debug display
            currentResult = PersonalitySystem.ComfortToResult(comfortLevel);
        }

        private void UpdateVisuals()
        {
            UpdateRangeColor();
            UpdateBreatheSpeed();
            UpdateParticles();
        }

        #endregion

        #region Range Indicator

        private void CreateRangeIndicator()
        {
            GameObject rangeObj = new GameObject("RangeIndicator");
            rangeObj.transform.SetParent(transform);
            rangeObj.transform.localPosition = Vector3.zero;

            rangeIndicator = rangeObj.AddComponent<SpriteRenderer>();
            rangeIndicator.sprite = rangeSprite != null ? rangeSprite : CreateGradientCircleSprite();
            rangeIndicator.sortingOrder = -1;

            UpdateRangeScale();
            UpdateRangeColor();
        }

        private Sprite CreateGradientCircleSprite()
        {
            int resolution = 128;
            Texture2D texture = new Texture2D(resolution, resolution);
            texture.filterMode = FilterMode.Bilinear;
            Color[] colors = new Color[resolution * resolution];

            Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
            float radius = resolution / 2f;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float t = Mathf.Clamp01(dist / radius);
                    float alpha = 1f - Mathf.Pow(t, 0.7f);
                    colors[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(colors);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), resolution / 2f);
        }

        private void UpdateRangeScale()
        {
            if (rangeIndicator == null || rangeIndicator.sprite == null) return;

            float spriteWorldSize = rangeIndicator.sprite.bounds.size.x;
            float targetDiameter = DetectionRange * 2f;
            float scale = targetDiameter / spriteWorldSize;
            
            rangeIndicator.transform.localScale = Vector3.one * scale;
        }

        private void UpdateRangeColor()
        {
            if (rangeIndicator == null) return;

            // Blend color based on comfort level
            Color baseColor = PersonalitySystem.GetPersonalityColor(Personality);
            
            // Tint based on comfort: green when happy, red when upset
            if (comfortLevel > 0)
            {
                baseColor = Color.Lerp(baseColor, Color.green, comfortLevel * 0.5f);
            }
            else if (comfortLevel < 0)
            {
                baseColor = Color.Lerp(baseColor, Color.red, -comfortLevel * 0.5f);
            }

            baseColor.a = playerInRange != null ? 0.6f : 0.25f;
            rangeIndicator.color = baseColor;
        }

        #endregion

        #region Breathing Animation

        private void StartBreatheAnimation()
        {
            float duration = GetBreatheDuration();
            
            breatheTween = transform
                .DOScale(originalScale * (1f + breatheScale), duration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void UpdateBreatheSpeed()
        {
            if (breatheTween == null) return;

            float duration = GetBreatheDuration();
            breatheTween.timeScale = baseBreatheDuration / duration;
            currentBreatheSpeed = breatheTween.timeScale;
        }

        private float GetBreatheDuration()
        {
            // Faster breathing when upset, slower when happy
            // comfort -1 to +1 maps to speed multiplier
            float speedMultiplier = Mathf.Lerp(0.4f, 1.3f, (comfortLevel + 1f) / 2f);
            return baseBreatheDuration * speedMultiplier;
        }

        #endregion

        #region Particles

        private void CreateParticleSystem()
        {
            GameObject particleObj = new GameObject("EmotionParticles");
            particleObj.transform.SetParent(transform);
            particleObj.transform.localPosition = Vector3.up * 0.5f;

            emotionParticles = particleObj.AddComponent<ParticleSystem>();
            
            var main = emotionParticles.main;
            main.startSize = 0.5f;
            main.startLifetime = 2f;
            main.startSpeed = 0.3f;
            main.maxParticles = 10;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = emotionParticles.emission;
            emission.rateOverTime = 0;

            var shape = emotionParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.2f;

            var colorOverLifetime = emotionParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            var velocity = emotionParticles.velocityOverLifetime;
            velocity.enabled = true;
            // Set all axes to same mode (TwoConstants)
            velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var noise = emotionParticles.noise;
            noise.enabled = false;
            noise.strength = 0.2f;
            noise.frequency = 2f;
            noise.scrollSpeed = 0.5f;
            noise.damping = true;

            var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 10;
            renderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        private void UpdateParticles()
        {
            if (emotionParticles == null) return;

            var main = emotionParticles.main;
            var emission = emotionParticles.emission;
            var velocity = emotionParticles.velocityOverLifetime;
            var noise = emotionParticles.noise;
            var renderer = emotionParticles.GetComponent<ParticleSystemRenderer>();

            // Only emit when player in range - low threshold so particles appear early as warning
            if (playerInRange == null || Mathf.Abs(comfortLevel) < 0.02f)
            {
                emission.rateOverTime = 0;
                currentParticleRate = 0;
                return;
            }

            if (particleConfig != null)
            {
                // Use lerped settings based on comfort level
                var settings = particleConfig.GetLerpedSettings(comfortLevel);
                
                main.startSpeed = settings.speed;
                main.startLifetime = settings.lifetime;
                main.startSize = settings.size;
                main.startColor = settings.tint;
                emission.rateOverTime = settings.emissionRate;
                currentParticleRate = settings.emissionRate;

                // Always use two constants mode for consistency
                float minY = settings.oscillateVertical ? settings.verticalMovement * -0.5f : settings.verticalMovement * 0.8f;
                float maxY = settings.verticalMovement;
                velocity.y = new ParticleSystem.MinMaxCurve(minY, maxY);

                if (settings.trembleStrength > 0)
                {
                    noise.enabled = true;
                    noise.strength = settings.trembleStrength;
                    noise.frequency = 3f + settings.trembleStrength * 5f;
                }
                else
                {
                    noise.enabled = false;
                }

                // Set sprite only when result category changes
                if (currentResult != lastParticleResult)
                {
                    lastParticleResult = currentResult;
                    var resultSettings = particleConfig.GetSettings(currentResult);
                    Sprite sprite = resultSettings.GetRandomSprite();
                    if (sprite != null)
                    {
                        renderer.material.mainTexture = sprite.texture;
                    }
                }
            }
            else
            {
                // Fallback
                float rate = Mathf.Lerp(0.3f, 1.2f, Mathf.Abs(comfortLevel));
                emission.rateOverTime = rate;
                currentParticleRate = rate;
            }
        }

        #endregion

        #region Detection

        private void CheckForPlayer()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, DetectionRange);
            PlayerController foundPlayer = null;

            foreach (var hit in hits)
            {
                PlayerController player = hit.GetComponent<PlayerController>();
                if (player != null)
                {
                    foundPlayer = player;
                    break;
                }
            }

            if (foundPlayer != null && playerInRange == null)
            {
                OnPlayerEnterRange(foundPlayer);
            }
            else if (foundPlayer == null && playerInRange != null)
            {
                OnPlayerExitRange();
            }

            playerInRange = foundPlayer;
            isPlayerInRange = playerInRange != null;

            if (playerInRange != null)
            {
                detectedPlayerMask = playerInRange.CurrentMask;
            }
        }

        private void OnPlayerEnterRange(PlayerController player)
        {
            Debug.Log($"[{gameObject.name}] Player entered range");
            
            transform.DOKill(complete: true);
            transform.localScale = originalScale;
            transform.DOPunchScale(Vector3.one * 0.15f, 0.3f, 5).OnComplete(StartBreatheAnimation);
        }

        private void OnPlayerExitRange()
        {
            Debug.Log($"[{gameObject.name}] Player left range");
        }

        #endregion

        private void OnValidate()
        {
            UpdateRangeScale();
            
            if (currentConfig != null && spriteRenderer != null)
            {
                if (currentConfig.tintColor != Color.white)
                    spriteRenderer.color = currentConfig.tintColor;
                else
                    spriteRenderer.color = PersonalitySystem.GetPersonalitySolidColor(currentConfig.personality);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!showRangeGizmo) return;
            Gizmos.color = PersonalitySystem.GetPersonalityColor(Personality);
            Gizmos.DrawWireSphere(transform.position, DetectionRange);
        }
    }
}
