using UnityEngine;
using Work.Yeonwoo._01_Scripts.ETC;

namespace Work.Yeonwoo._01_Scripts.PC
{
    public class Player : MonoSingleton<Player>
    {
        [field:SerializeField] public PlayerMove PlayerMove { get; set; }
        [field:SerializeField] public PlayerAnim PlayerAnim { get; set; }
    }
}