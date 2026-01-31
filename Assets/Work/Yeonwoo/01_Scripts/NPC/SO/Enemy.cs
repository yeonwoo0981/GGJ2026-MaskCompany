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

    public enum SensingType
    {
        Wall,
        Sound,
        None
    }
    
    [CreateAssetMenu(fileName = "Enemy", menuName = "EnemySO/Enemy")]
    public class Enemy : ScriptableObject
    {
        [SerializeField] private string descriptionName;
        [field:SerializeField] public MaskType MaskType { get; private set; }
        [field:SerializeField] public FeelType FeelType { get; private set; }
        [field:SerializeField] public float MoveSpeed { get; private set; }
        [field:SerializeField] [field:Range(1, 1000)] public float AwarenessScore { get; private set; }
        [field:SerializeField] [field:Range(1, 100)] public float MyManScore { get; private set; }
        [field:SerializeField] public float RotationSpeed { get; private set; }
        [field:SerializeField] public SensingType SensingType { get; private set; }
        [field:SerializeField] public float SensingRotation { get; private set; }
        [field:SerializeField] public float SensingRange { get; private set; }
        [field:SerializeField] public int Angle { get; private set; }
    }
}
