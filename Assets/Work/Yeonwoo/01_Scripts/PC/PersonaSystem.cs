using System;
using UnityEngine;

namespace Work.Yeonwoo._01_Scripts.PC
{
    public class PersonaSystem : MonoBehaviour
    {
        [field:SerializeField] public float Red { get; set; }
        [field:SerializeField] public float Blue { get; set; }
        [field:SerializeField] public float Yellow { get; set; }
        [field:SerializeField] public float Green { get; set; }

        public event Action IsAddPerSona;
        public event Action IsMinusPerSona;

        public Action IsGameOver;
        public Action IsWin;
        
        private void Start()
        {
            Red = 100;
            Blue = 100;
            Yellow = 100;
            Green = 100;
        }

        public void AddPerSona(float value)
        {
            IsAddPerSona?.Invoke();
            //if (페르소나 점수가 특이점을 넘어서 증가하면) {IsWin}
        }

        public void RemovePerSona(float value)
        {
            IsMinusPerSona?.Invoke();
            //if (페르소나 점수가 특이점을 넘어서 감소하면) {IsGameOver}
        }
    }
}