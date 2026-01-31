using UnityEngine;

namespace MaskCompany
{
    // Masks inspired by Inside Out
    public enum MaskType
    {
        Joy,
        Neutral,
        Anger,
        Fear
    }

    // NPC Personality types
    public enum PersonalityType
    {
        Angry,
        Cool,
        Weird,
        Loner,
        Lazy,
        Anxious,
        Friendly,
        Scary
    }

    public enum CompatibilityResult
    {
        Great,      // ++ (strong positive, 1.5x speed)
        Good,       // +  (positive)
        Neutral,    // o  (pushes toward 0)
        Bad,        // -  (negative)
        VeryBad     // -- (strong negative, 1.5x speed)
    }
    
    /// <summary>
    /// Global settings for mask influence - easy to tweak!
    /// </summary>
    public static class MaskSettings
    {
        /// <summary>
        /// Global multiplier for ALL mask influence speeds. 
        /// 1.0 = normal, 0.5 = twice slower, 2.0 = twice faster
        /// </summary>
        public static float GlobalSpeedMultiplier = 0.5f;  // Currently set to half speed (twice slower)
    }

    public static class PersonalitySystem
    {
        /*
         * Compatibility Matrix (Joy, Neutral, Anger, Fear)
         * 
         * Angry:    --, +, o, +
         * Cool:     ++, -, -, -      (nerfed: Neutral o→-)
         * Weird:    +, -, o, -
         * Loner:    o, +, -, o
         * Lazy:     -, ++, -, o
         * Anxious:  -, +, --, -
         * Friendly: +, -, -, -
         * Scary:    -, o, +, ++
         */

        public static CompatibilityResult GetCompatibility(MaskType mask, PersonalityType npc)
        {
            return (mask, npc) switch
            {
                // Joy mask
                (MaskType.Joy, PersonalityType.Angry) => CompatibilityResult.VeryBad,  // --
                (MaskType.Joy, PersonalityType.Cool) => CompatibilityResult.Great,     // ++
                (MaskType.Joy, PersonalityType.Weird) => CompatibilityResult.Good,     // +
                (MaskType.Joy, PersonalityType.Loner) => CompatibilityResult.Neutral,  // o
                (MaskType.Joy, PersonalityType.Lazy) => CompatibilityResult.Bad,       // -
                (MaskType.Joy, PersonalityType.Anxious) => CompatibilityResult.Bad,    // -
                (MaskType.Joy, PersonalityType.Friendly) => CompatibilityResult.Good,  // +
                (MaskType.Joy, PersonalityType.Scary) => CompatibilityResult.Bad,      // -

                // Neutral mask (half influence speed applied in GetInfluenceSpeed)
                (MaskType.Neutral, PersonalityType.Angry) => CompatibilityResult.Good,     // +
                (MaskType.Neutral, PersonalityType.Cool) => CompatibilityResult.Bad,       // - (nerfed from o)
                (MaskType.Neutral, PersonalityType.Weird) => CompatibilityResult.Bad,      // -
                (MaskType.Neutral, PersonalityType.Loner) => CompatibilityResult.Good,     // +
                (MaskType.Neutral, PersonalityType.Lazy) => CompatibilityResult.Great,     // ++
                (MaskType.Neutral, PersonalityType.Anxious) => CompatibilityResult.Good,   // +
                (MaskType.Neutral, PersonalityType.Friendly) => CompatibilityResult.Bad,   // -
                (MaskType.Neutral, PersonalityType.Scary) => CompatibilityResult.Neutral,  // o

                // Anger mask
                (MaskType.Anger, PersonalityType.Angry) => CompatibilityResult.Neutral,    // o
                (MaskType.Anger, PersonalityType.Cool) => CompatibilityResult.Bad,         // -
                (MaskType.Anger, PersonalityType.Weird) => CompatibilityResult.Neutral,    // o
                (MaskType.Anger, PersonalityType.Loner) => CompatibilityResult.Bad,        // -
                (MaskType.Anger, PersonalityType.Lazy) => CompatibilityResult.Bad,         // -
                (MaskType.Anger, PersonalityType.Anxious) => CompatibilityResult.VeryBad,  // --
                (MaskType.Anger, PersonalityType.Friendly) => CompatibilityResult.Bad,     // -
                (MaskType.Anger, PersonalityType.Scary) => CompatibilityResult.Good,       // +

                // Fear mask
                (MaskType.Fear, PersonalityType.Angry) => CompatibilityResult.Good,        // +
                (MaskType.Fear, PersonalityType.Cool) => CompatibilityResult.Bad,          // -
                (MaskType.Fear, PersonalityType.Weird) => CompatibilityResult.Bad,         // -
                (MaskType.Fear, PersonalityType.Loner) => CompatibilityResult.Neutral,     // o
                (MaskType.Fear, PersonalityType.Lazy) => CompatibilityResult.Neutral,      // o
                (MaskType.Fear, PersonalityType.Anxious) => CompatibilityResult.Bad,       // -
                (MaskType.Fear, PersonalityType.Friendly) => CompatibilityResult.Bad,      // -
                (MaskType.Fear, PersonalityType.Scary) => CompatibilityResult.Great,       // ++

                _ => CompatibilityResult.Neutral
            };
        }

        /// <summary>
        /// Get base influence rate (direction and magnitude).
        /// Positive = toward +1, Negative = toward -1, 0 = toward neutral
        /// </summary>
        public static float GetInfluenceRate(MaskType mask, PersonalityType npc)
        {
            var result = GetCompatibility(mask, npc);
            return result switch
            {
                CompatibilityResult.Great => 0.5f,      // toward +1
                CompatibilityResult.Good => 0.3f,       // toward +1
                CompatibilityResult.Neutral => 0f,      // toward 0 (handled separately)
                CompatibilityResult.Bad => -0.3f,       // toward -1
                CompatibilityResult.VeryBad => -0.5f,   // toward -1
                _ => 0f
            };
        }

        /// <summary>
        /// Get influence speed multiplier based on result strength and mask type.
        /// Harsh (++/--) = 1.5x, Normal (+/-/o) = 1x, Neutral mask = 0.5x multiplier
        /// All values are then multiplied by MaskSettings.GlobalSpeedMultiplier
        /// </summary>
        public static float GetInfluenceSpeed(MaskType mask, CompatibilityResult result)
        {
            float baseSpeed = result switch
            {
                CompatibilityResult.Great => 1.5f,    // harsh positive
                CompatibilityResult.Good => 1f,       // normal positive
                CompatibilityResult.Neutral => 1f,    // neutral push (same speed, different target)
                CompatibilityResult.Bad => 1f,        // normal negative
                CompatibilityResult.VeryBad => 1.5f,  // harsh negative
                _ => 1f
            };

            // Neutral mask has half influence speed
            if (mask == MaskType.Neutral)
            {
                baseSpeed *= 0.5f;
            }

            // Apply global speed multiplier for easy balancing
            return baseSpeed * MaskSettings.GlobalSpeedMultiplier;
        }

        public static float GetTargetComfort(CompatibilityResult result)
        {
            // All positive results aim for +1, all negative aim for -1
            // Speed determines how fast they get there, not the target
            return result switch
            {
                CompatibilityResult.Great => 1f,      // fast to +1
                CompatibilityResult.Good => 1f,       // slow to +1
                CompatibilityResult.Neutral => 0f,    // pushes toward 0
                CompatibilityResult.Bad => -1f,       // slow to -1
                CompatibilityResult.VeryBad => -1f,   // fast to -1
                _ => 0f
            };
        }

        public static CompatibilityResult ComfortToResult(float comfort)
        {
            if (comfort >= 0.8f) return CompatibilityResult.Great;
            if (comfort >= 0.3f) return CompatibilityResult.Good;
            if (comfort >= -0.3f) return CompatibilityResult.Neutral;
            if (comfort >= -0.8f) return CompatibilityResult.Bad;
            return CompatibilityResult.VeryBad;
        }

        // Personality colors (for NPC sprites/range indicators)
        public static Color GetPersonalityColor(PersonalityType type)
        {
            Color c = GetPersonalitySolidColor(type);
            c.a = 0.5f;
            return c;
        }

        public static Color GetPersonalitySolidColor(PersonalityType type)
        {
            return type switch
            {
                PersonalityType.Angry => new Color(0.9f, 0.3f, 0.2f),      // Red-orange
                PersonalityType.Cool => new Color(0.2f, 0.8f, 0.85f),     // Cyan/Teal
                PersonalityType.Weird => new Color(0.9f, 0.4f, 0.7f),     // Pink/Magenta
                PersonalityType.Loner => new Color(0.3f, 0.3f, 0.6f),     // Dark indigo
                PersonalityType.Lazy => new Color(0.5f, 0.7f, 0.4f),      // Muted green
                PersonalityType.Anxious => new Color(0.7f, 0.5f, 0.9f),   // Light purple
                PersonalityType.Friendly => new Color(0.4f, 0.9f, 0.5f),  // Bright green
                PersonalityType.Scary => new Color(0.4f, 0.2f, 0.5f),     // Dark purple
                _ => Color.gray
            };
        }

        // Mask colors (Inside Out inspired)
        public static Color GetMaskColor(MaskType mask)
        {
            return mask switch
            {
                MaskType.Joy => new Color(1f, 0.9f, 0.2f),      // Yellow (Joy)
                MaskType.Neutral => new Color(0.3f, 0.5f, 0.9f), // Blue (Sadness-ish, calm)
                MaskType.Anger => new Color(0.9f, 0.2f, 0.1f),   // Red (Anger)
                MaskType.Fear => new Color(0.7f, 0.3f, 0.9f),    // Purple (Fear)
                _ => Color.white
            };
        }
    }
}
