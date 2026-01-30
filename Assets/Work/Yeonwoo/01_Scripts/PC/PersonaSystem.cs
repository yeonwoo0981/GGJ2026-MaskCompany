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
        [field:SerializeField] public float Score { get; private set; }

        public event Action IsAddPerSona;
        public event Action IsMinusPerSona;

        public event Action IsGameOver;
        public event Action IsWin;
        
        private void Start()
        {
            Red = 100;
            Blue = 100;
            Yellow = 100;
            Green = 100;
            Score = Red + Blue + Yellow + Green;
        }

        public void AddPerSona(float value)
        {
            IsAddPerSona?.Invoke();
            if (Score > 1000) // 예를 들어서
            {
                IsWin?.Invoke();
            }
        }

        public void RemovePerSona(float value)
        {
            IsMinusPerSona?.Invoke();
            if (Score < 100)
            {
                IsGameOver?.Invoke();
            }
        }
    }
}