using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaskCompany
{
    [CreateAssetMenu(fileName = "ParticleConfig", menuName = "MaskCompany/Particle Config")]
    public class ParticleConfig : ScriptableObject
    {
        [Header("Great - Happy/Success")]
        public ParticleSettings great = new ParticleSettings
        {
            emissionRate = 0.5f,
            speed = 0.3f,
            lifetime = 2.5f,
            size = 0.6f,
            trembleStrength = 0f,
            verticalMovement = 0.8f
        };

        [Header("Good - Neutral/Okay")]
        public ParticleSettings good = new ParticleSettings
        {
            emissionRate = 0.3f,
            speed = 0.2f,
            lifetime = 2f,
            size = 0.5f,
            trembleStrength = 0f,
            verticalMovement = 0.3f
        };

        [Header("Risky - Nervous/Uncertain")]
        public ParticleSettings risky = new ParticleSettings
        {
            emissionRate = 0.8f,
            speed = 0.2f,
            lifetime = 1.5f,
            size = 0.55f,
            trembleStrength = 0.3f,
            verticalMovement = 0.1f
        };

        [Header("Bad - Angry/Alert")]
        public ParticleSettings bad = new ParticleSettings
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
                CompatibilityResult.Risky => risky,
                CompatibilityResult.Bad => bad,
                _ => good
            };
        }

        /// <summary>
        /// Lerp between two particle settings based on comfort level
        /// </summary>
        public ParticleSettings GetLerpedSettings(float comfortLevel)
        {
            // comfortLevel: -1 (bad) to +1 (great)
            ParticleSettings result = new ParticleSettings();
            
            if (comfortLevel >= 0)
            {
                // Lerp between good and great
                float t = comfortLevel;
                result.emissionRate = Mathf.Lerp(good.emissionRate, great.emissionRate, t);
                result.speed = Mathf.Lerp(good.speed, great.speed, t);
                result.lifetime = Mathf.Lerp(good.lifetime, great.lifetime, t);
                result.size = Mathf.Lerp(good.size, great.size, t);
                result.trembleStrength = Mathf.Lerp(good.trembleStrength, great.trembleStrength, t);
                result.verticalMovement = Mathf.Lerp(good.verticalMovement, great.verticalMovement, t);
                result.sprites = t > 0.5f ? great.sprites : good.sprites;
            }
            else
            {
                // Lerp between good and bad (through risky)
                float t = -comfortLevel; // 0 to 1
                if (t < 0.5f)
                {
                    // good to risky
                    float t2 = t * 2f;
                    result.emissionRate = Mathf.Lerp(good.emissionRate, risky.emissionRate, t2);
                    result.speed = Mathf.Lerp(good.speed, risky.speed, t2);
                    result.lifetime = Mathf.Lerp(good.lifetime, risky.lifetime, t2);
                    result.size = Mathf.Lerp(good.size, risky.size, t2);
                    result.trembleStrength = Mathf.Lerp(good.trembleStrength, risky.trembleStrength, t2);
                    result.verticalMovement = Mathf.Lerp(good.verticalMovement, risky.verticalMovement, t2);
                    result.sprites = t2 > 0.5f ? risky.sprites : good.sprites;
                }
                else
                {
                    // risky to bad
                    float t2 = (t - 0.5f) * 2f;
                    result.emissionRate = Mathf.Lerp(risky.emissionRate, bad.emissionRate, t2);
                    result.speed = Mathf.Lerp(risky.speed, bad.speed, t2);
                    result.lifetime = Mathf.Lerp(risky.lifetime, bad.lifetime, t2);
                    result.size = Mathf.Lerp(risky.size, bad.size, t2);
                    result.trembleStrength = Mathf.Lerp(risky.trembleStrength, bad.trembleStrength, t2);
                    result.verticalMovement = Mathf.Lerp(risky.verticalMovement, bad.verticalMovement, t2);
                    result.oscillateVertical = t2 > 0.5f;
                    result.sprites = t2 > 0.5f ? bad.sprites : risky.sprites;
                }
            }
            
            result.tint = Color.white;
            return result;
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
