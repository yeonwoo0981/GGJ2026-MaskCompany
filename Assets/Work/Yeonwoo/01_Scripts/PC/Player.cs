using UnityEngine;

namespace Work.Yeonwoo._01_Scripts.PC
{
    public class Player : MonoBehaviour
    {
        [field:SerializeField] public PlayerMove PlayerMove { get; set; }
        [field:SerializeField] public PlayerAnim PlayerAnim { get; set; }
        [field:SerializeField] public PlayerInput PlayerInput { get; set; }
    }
}