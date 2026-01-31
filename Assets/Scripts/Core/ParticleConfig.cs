using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaskCompany
{
    [CreateAssetMenu(fileName = "ParticleConfig", menuName = "MaskCompany/Particle Config")]
    public class ParticleConfig : ScriptableObject
    {
        [Header("Great (++) - Very Happy")]
        public ParticleSettings great = new ParticleSettings
        {
            emissionRate = 0.5f,
            speed = 0.3f,
            lifetime = 2.5f,
            size = 0.6f,
            trembleStrength = 0f,
            verticalMovement = 0.8f
        };

        [Header("Good (+) - Happy")]
        public ParticleSettings good = new ParticleSettings
        {
            emissionRate = 0.3f,
            speed = 0.2f,
            lifetime = 2f,
            size = 0.5f,
            trembleStrength = 0f,
            verticalMovement = 0.3f
        };

        [Header("Neutral (o) - Calm/Returning to baseline")]
        public ParticleSettings neutral = new ParticleSettings
        {
            emissionRate = 0.2f,
            speed = 0.15f,
            lifetime = 1.8f,
            size = 0.45f,
            trembleStrength = 0.05f,
            verticalMovement = 0.15f
        };

        [Header("Bad (-) - Upset")]
        public ParticleSettings bad = new ParticleSettings
        {
            emissionRate = 0.8f,
            speed = 0.3f,
            lifetime = 1.5f,
            size = 0.55f,
            trembleStrength = 0.3f,
            verticalMovement = 0.2f
        };

        [Header("VeryBad (--) - Angry/Alert")]
        public ParticleSettings veryBad = new ParticleSettings
        {
            emissionRate = 1.2f,
            speed = 0.5f,
            lifetime = 1.2f,
            size = 0.65f,
            trembleStrength = 0.6f,
            verticalMovement = 0.4f,
            oscillateVertical = true
        };

        public ParticleSettings GetSettings(CompatibilityResult result)
        {
            return result switch
            {
                CompatibilityResult.Great => great,
                CompatibilityResult.Good => good,
                CompatibilityResult.Neutral => neutral,
                CompatibilityResult.Bad => bad,
                CompatibilityResult.VeryBad => veryBad,
                _ => neutral
            };
        }

        /// <summary>
        /// Lerp between particle settings based on comfort level.
        /// Positive: neutral → good → great
        /// Negative: neutral → bad → veryBad
        /// </summary>
        public ParticleSettings GetLerpedSettings(float comfortLevel)
        {
            ParticleSettings result = new ParticleSettings();
            
            if (comfortLevel >= 0)
            {
                // Positive: neutral (0) → good (0.5) → great (1)
                float t = comfortLevel;
                if (t < 0.5f)
                {
                    float t2 = t * 2f; // 0 to 1
                    result = LerpSettings(neutral, good, t2);
                    result.sprites = t2 > 0.5f ? good.sprites : neutral.sprites;
                }
                else
                {
                    float t2 = (t - 0.5f) * 2f; // 0 to 1
                    result = LerpSettings(good, great, t2);
                    result.sprites = t2 > 0.5f ? great.sprites : good.sprites;
                }
            }
            else
            {
                // Negative: neutral (0) → bad (-0.5) → veryBad (-1)
                float t = -comfortLevel; // 0 to 1
                if (t < 0.5f)
                {
                    float t2 = t * 2f; // 0 to 1
                    result = LerpSettings(neutral, bad, t2);
                    result.sprites = t2 > 0.5f ? bad.sprites : neutral.sprites;
                }
                else
                {
                    float t2 = (t - 0.5f) * 2f; // 0 to 1
                    result = LerpSettings(bad, veryBad, t2);
                    result.oscillateVertical = t2 > 0.3f;
                    result.sprites = t2 > 0.5f ? veryBad.sprites : bad.sprites;
                }
            }
            
            result.tint = Color.white;
            return result;
        }

        private ParticleSettings LerpSettings(ParticleSettings a, ParticleSettings b, float t)
        {
            return new ParticleSettings
            {
                emissionRate = Mathf.Lerp(a.emissionRate, b.emissionRate, t),
                speed = Mathf.Lerp(a.speed, b.speed, t),
                lifetime = Mathf.Lerp(a.lifetime, b.lifetime, t),
                size = Mathf.Lerp(a.size, b.size, t),
                trembleStrength = Mathf.Lerp(a.trembleStrength, b.trembleStrength, t),
                verticalMovement = Mathf.Lerp(a.verticalMovement, b.verticalMovement, t),
                oscillateVertical = a.oscillateVertical || b.oscillateVertical
            };
        }
    }

    [Serializable]
    public class ParticleSettings
    {
        public List<Sprite> sprites;
        
        [Header("Spawn")]
        [Range(0.1f, 5f)] public float emissionRate = 0.5f;
        [Range(0.3f, 5f)] public float lifetime = 2f;
        [Range(0.1f, 2f)] public float size = 0.6f;
        public Color tint = Color.white;

        [Header("Movement")]
        [Range(0.1f, 2f)] public float speed = 0.3f;
        [Range(0f, 1f)] public float verticalMovement = 0.5f;
        [Range(0f, 1f)] public float trembleStrength = 0f;
        public bool oscillateVertical = false;

        public Sprite GetRandomSprite()
        {
            if (sprites == null || sprites.Count == 0) return null;
            return sprites[UnityEngine.Random.Range(0, sprites.Count)];
        }
    }
}
