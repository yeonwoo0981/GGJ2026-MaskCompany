using UnityEngine;

namespace MaskCompany
{
    [CreateAssetMenu(fileName = "NPCConfig", menuName = "MaskCompany/NPC Config")]
    public class NPCConfig : ScriptableObject
    {
        [Header("Identity")]
        public string npcName;
        public Sprite sprite;
        public PersonalityType personality;

        [Header("Stats")]
        public float moveSpeed = 2f;
        public float detectionRange = 2f;

        [Header("Visuals")]
        public Color tintColor = Color.white;
    }
}
