using UnityEngine;

namespace Work.Yeonwoo._01_Scripts.PC
{
    [CreateAssetMenu(fileName = "PlayerDataSO", menuName = "PlayerSO/PlayerDataSO")]
    public class PlayerDataSO : ScriptableObject
    {
        [field:SerializeField] public float MaxSpeed { get; private set; }
        [Range(0.1f, 100)] public float acceleration = 50, deceleration = 50;
    }
}
