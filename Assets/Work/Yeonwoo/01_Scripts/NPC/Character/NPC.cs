using Unity.Behavior;
using UnityEngine;
using UnityEngine.Events;
using Work.Yeonwoo._01_Scripts.NPC.PathFinder;
using Work.Yeonwoo._01_Scripts.PC;

namespace Work.Yeonwoo._01_Scripts.NPC.Character
{
    public abstract class NPC : Agent
    {
        private float _alertThreshold;
        
        private Player _player;
        public UnityEvent playerInSensingRange;

        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            GetComponent<BehaviorGraphAgent>();
        }

        protected override void Awake()
        {
            base.Awake();
            _player = Player.Instance;
        }

        protected virtual void Start()
        {
            _alertThreshold = Mathf.Cos(TypeSo.SensingRotation / 2 * Mathf.Deg2Rad);
        }
        
        protected virtual void Update()
        {
            if (TypeSo == null) return;

            if (TypeSo.AwarenessScore < 0)
            {
                Destroy(gameObject);
                return;
            }

            CheckAlert();
        }

        private void CheckAlert()
        {
            Vector2 targetDir = _player.gameObject.transform.position - transform.position;

            if (targetDir.magnitude <= TypeSo.SensingRange)
            {
                float dot = Vector2.Dot(transform.up, targetDir.normalized);
                
                if (dot >= _alertThreshold) 
                {
                    playerInSensingRange?.Invoke();
                    Debug.Log("감지 범위 안에 들어옴");
                }
            }
        }
        
        
    }
}