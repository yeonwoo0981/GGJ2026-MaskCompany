using System;
using UnityEngine;

namespace Work.Yeonwoo._01_Scripts.NPC.PathFinder
{
    public class AgentMover : MonoBehaviour, IComponent
    {
        [SerializeField] private Rigidbody2D rb;
        private Agent _owner;
        private Vector2 _movementInput;

        public Action<Vector2> OnSpeedChange;
        
        public void Initialize(Agent agent)
        {
            _owner = agent;
        }

        public void StopImmediately()
        {
            _movementInput = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
        }

        public void SetMovementInput(Vector2 input)
        {
            _movementInput = input.normalized;
        }

        private void FixedUpdate()
        {
            rb.linearVelocity = _movementInput * _owner.TypeSo.MoveSpeed;
            OnSpeedChange?.Invoke(rb.linearVelocity);
        }
    }
}