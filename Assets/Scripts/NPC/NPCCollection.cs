using System.Collections.Generic;
using UnityEngine;

namespace MaskCompany
{
    [CreateAssetMenu(fileName = "NPCCollection", menuName = "MaskCompany/NPC Collection")]
    public class NPCCollection : ScriptableObject
    {
        public List<NPCConfig> configs = new List<NPCConfig>();

        public NPCConfig GetRandom()
        {
            if (configs == null || configs.Count == 0) return null;
            return configs[Random.Range(0, configs.Count)];
        }

        public NPCConfig GetByPersonality(PersonalityType personality)
        {
            return configs.Find(c => c.personality == personality);
        }

        public List<NPCConfig> GetAllByPersonality(PersonalityType personality)
        {
            return configs.FindAll(c => c.personality == personality);
        }
    }
}
