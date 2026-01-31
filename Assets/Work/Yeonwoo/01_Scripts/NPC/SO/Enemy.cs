using UnityEngine;

namespace Work.Yeonwoo._01_Scripts.NPC.SO
{
    public enum MaskType
    {
        Red,
        Blue,
        Yellow,
        Green
    }

    public enum FeelType
    {
        Happy,
        Sad,
        Angry,
        Fear
    }
    [CreateAssetMenu(fileName = "Enemy", menuName = "EnemySO/Enemy")]
    public class Enemy : ScriptableObject
    {
        [field:SerializeField] public MaskType MaskType { get; private set; }
        [field:SerializeField] public FeelType FeelType { get; private set; }
        [field:SerializeField] public float AwarenessScore { get; private set; }
        [field:SerializeField] public float MyManScore { get; private set; }
    }
}
