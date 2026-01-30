using UnityEngine;
using UnityEngine.InputSystem;

namespace Work.Yeonwoo._01_Scripts.PC
{
    public class PlayerInput : MonoBehaviour
    {
        public Vector2 MoveDir { get; private set; }
        public void OnMove(InputValue value) => MoveDir = value.Get<Vector2>();
    }
}