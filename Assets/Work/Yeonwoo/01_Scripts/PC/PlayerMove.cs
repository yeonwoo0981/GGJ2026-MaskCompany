using System;
using UnityEngine;

namespace Work.Yeonwoo._01_Scripts.PC
{
    public class PlayerMove : MonoBehaviour
    {
        private Rigidbody2D _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }
    }
}