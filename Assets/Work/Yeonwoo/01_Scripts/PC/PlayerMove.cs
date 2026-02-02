using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Work.Yeonwoo._01_Scripts.ETC.csiimnida.CSILib.SoundManager.RunTime;

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
            bool isMoving = _moveDir.sqrMagnitude > 0.01f;

            if (isMoving)
                SoundManager.Instance.PlaySound("Walk");
            else
                SoundManager.Instance.StopSound("Walk");
            float targetSpeed = isMoving ? Movement.MaxSpeed : 0f;
            float accel = isMoving ? Movement.acceleration : Movement.deceleration;

            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                targetSpeed,
                accel * Time.fixedDeltaTime
            );

            _rb.linearVelocity = _moveDir.normalized * currentSpeed;
        }
    }
}