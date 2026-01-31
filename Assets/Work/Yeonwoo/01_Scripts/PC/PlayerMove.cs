using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Work.Yeonwoo._01_Scripts.PC
{
    public class PlayerMove : MonoBehaviour
    {
        [field:SerializeField] public PlayerDataSO Movement { get; private set; }
        [SerializeField] private float currentSpeed = 5f;
        private Rigidbody2D _rb;
        private Vector2 _moveDir;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void OnMove(InputValue value)
        {
            _moveDir = value.Get<Vector2>();
        }
        
        private void FixedUpdate()
        {
            _rb.linearVelocity =_moveDir * currentSpeed;
        }
    }
}