using System;
using UnityEngine;

namespace Work.Yeonwoo._01_Scripts.NPC.PathFinder
{
    public class AgentRenderer : MonoBehaviour
    {
        public bool IsFacingRight { get; private set; } = true;
        public bool IsFacingUp { get; private set; } = true;
        
        private Agent _owner;
        private SpriteRenderer _sr;
        private AgentMover _mover;

        public void Initialize(Agent agent)
        {
            _owner = agent;
            _sr = GetComponent<SpriteRenderer>();
            _mover = _owner.Get<AgentMover>();
            _mover.OnSpeedChange += HandleSpeedChange;
        }

        private void HandleSpeedChange(Vector2 speed)
        {
            if (speed.x < 0 && IsFacingRight)
                FlipHorizontal();
            else if (speed.x > 0 && !IsFacingRight)
                FlipHorizontal();
            
            if (speed.y < 0 && IsFacingUp)
                FlipVertical();
            else if (speed.y > 0 && !IsFacingUp)
                FlipVertical();
        }

        private void FlipHorizontal()
        {
            IsFacingRight = !IsFacingRight;
            if (_sr != null)
                _sr.flipX = ! _sr.flipX;
            else
                _owner.transform.localScale = new Vector3(-_owner.transform.localScale.x, _owner.transform.localScale.y, _owner.transform.localScale.z);
        }

        private void FlipVertical()
        {
            IsFacingUp = !IsFacingUp;
            if (_sr != null)
                _sr.flipY = ! _sr.flipY;
            else
                _owner.transform.localScale = new Vector3(_owner.transform.localScale.x, -_owner.transform.localScale.y, _owner.transform.localScale.z);
        }
        
        private void OnDestroy()
        {
            _mover.OnSpeedChange -= HandleSpeedChange;
        }
    }
}