using UnityEngine;

namespace MaskCompany
{
    public enum PersonalityType
    {
        Neutral,
        Dominant,
        Submissive,
        Friendly,
        Hostile
    }

    public enum MaskType
    {
        Agreeable,
        Assertive,
        Analytical,
        Expressive
    }

    public enum CompatibilityResult
    {
        Great,      // Pass freely
        Good,       // Pass with acknowledgment
        Risky,      // Warning state
        Bad         // Detection/alert
    }

    public static class PersonalitySystem
    {
        public static CompatibilityResult GetCompatibility(MaskType mask, PersonalityType npc)
        {
            return (mask, npc) switch
            {
                // Agreeable
                (MaskType.Agreeable, PersonalityType.Dominant) => CompatibilityResult.Great,
                (MaskType.Agreeable, PersonalityType.Submissive) => CompatibilityResult.Great,
                (MaskType.Agreeable, PersonalityType.Friendly) => CompatibilityResult.Good,
                (MaskType.Agreeable, PersonalityType.Hostile) => CompatibilityResult.Bad,
                (MaskType.Agreeable, PersonalityType.Neutral) => CompatibilityResult.Good,

                // Assertive
                (MaskType.Assertive, PersonalityType.Dominant) => CompatibilityResult.Good,
                (MaskType.Assertive, PersonalityType.Submissive) => CompatibilityResult.Bad,
                (MaskType.Assertive, PersonalityType.Friendly) => CompatibilityResult.Good,
                (MaskType.Assertive, PersonalityType.Hostile) => CompatibilityResult.Great,
                (MaskType.Assertive, PersonalityType.Neutral) => CompatibilityResult.Good,

                // Analytical
                (MaskType.Analytical, PersonalityType.Dominant) => CompatibilityResult.Bad,
                (MaskType.Analytical, PersonalityType.Submissive) => CompatibilityResult.Good,
                (MaskType.Analytical, PersonalityType.Friendly) => CompatibilityResult.Risky,
                (MaskType.Analytical, PersonalityType.Hostile) => CompatibilityResult.Good,
                (MaskType.Analytical, PersonalityType.Neutral) => CompatibilityResult.Great,

                // Expressive
                (MaskType.Expressive, PersonalityType.Dominant) => CompatibilityResult.Risky,
                (MaskType.Expressive, PersonalityType.Submissive) => CompatibilityResult.Risky,
                (MaskType.Expressive, PersonalityType.Friendly) => CompatibilityResult.Great,
                (MaskType.Expressive, PersonalityType.Hostile) => CompatibilityResult.Bad,
                (MaskType.Expressive, PersonalityType.Neutral) => CompatibilityResult.Good,

                _ => CompatibilityResult.Good
            };
        }

        /// <summary>
        /// Returns the influence rate: how fast comfort changes per second
        /// Positive = getting happier, Negative = getting upset
        /// </summary>
        public static float GetInfluenceRate(MaskType mask, PersonalityType npc)
        {
            var result = GetCompatibility(mask, npc);
            return result switch
            {
                CompatibilityResult.Great => 0.5f,   // Quickly becomes happy
                CompatibilityResult.Good => 0.15f,   // Slowly improves
                CompatibilityResult.Risky => -0.2f,  // Slowly gets uncomfortable
                CompatibilityResult.Bad => -0.6f,    // Quickly gets upset
                _ => 0f
            };
        }

        /// <summary>
        /// Returns target comfort level for this compatibility
        /// </summary>
        public static float GetTargetComfort(CompatibilityResult result)
        {
            return result switch
            {
                CompatibilityResult.Great => 1f,
                CompatibilityResult.Good => 0.3f,
                CompatibilityResult.Risky => -0.4f,
                CompatibilityResult.Bad => -1f,
                _ => 0f
            };
        }

        /// <summary>
        /// Convert comfort level to compatibility result for display
        /// </summary>
        public static CompatibilityResult ComfortToResult(float comfort)
        {
            if (comfort >= 0.6f) return CompatibilityResult.Great;
            if (comfort >= 0f) return CompatibilityResult.Good;
            if (comfort >= -0.5f) return CompatibilityResult.Risky;
            return CompatibilityResult.Bad;
        }

        // For range indicators (with alpha)
        public static Color GetPersonalityColor(PersonalityType type)
        {
            Color c = GetPersonalitySolidColor(type);
            c.a = 0.5f;
            return c;
        }

        // For sprites (full opacity)
        public static Color GetPersonalitySolidColor(PersonalityType type)
        {
            return type switch
            {
                PersonalityType.Dominant => new Color(0.8f, 0.3f, 0.3f),    // Red
                PersonalityType.Submissive => new Color(0.4f, 0.7f, 0.9f), // Light blue
                PersonalityType.Friendly => new Color(0.4f, 0.85f, 0.5f),  // Green
                PersonalityType.Hostile => new Color(0.7f, 0.2f, 0.7f),    // Purple
                PersonalityType.Neutral => new Color(0.6f, 0.6f, 0.6f),    // Gray
                _ => Color.white
            };
        }
    }
}
