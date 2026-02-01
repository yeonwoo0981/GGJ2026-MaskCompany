using UnityEngine;
using Work.Yeonwoo._01_Scripts.ETC;
using CSILib.SoundManager.RunTime;

namespace Work.Yeonwoo._01_Scripts.PC
{
    public class Player : MonoSingleton<Player>
    {
        [field:SerializeField] public PlayerMove PlayerMove { get; private set; }
        [field:SerializeField] public PlayerAnim PlayerAnim { get; private set; }
        [field:SerializeField] public PersonaSystem PersonaSystem { get; private set; }
    }
}