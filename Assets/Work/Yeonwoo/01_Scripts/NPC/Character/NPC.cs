using Unity.Behavior;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Work.Yeonwoo._01_Scripts.NPC.PathFinder;
using Work.Yeonwoo._01_Scripts.NPC.SO;
using Work.Yeonwoo._01_Scripts.PC;

namespace Work.Yeonwoo._01_Scripts.NPC.Character
{
    public abstract class NPC : Agent
    {
        public BehaviorGraphAgent BtAgent { get; private set; }
        private float _alertThreshold;
        
        private Player _player;
        public UnityEvent playerInSensingRange;

        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            BtAgent = GetComponent<BehaviorGraphAgent>();
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
                else
                {
                    
                }
            }
            else
            {
                
            }
        }
        
        private void OnDrawGizmos()
        {
            Handles.color = Color.red;
            Vector2 startDirection = Quaternion.Euler(0, 0, TypeSo.SensingRotation / 2) * transform.up;
            
            Handles.DrawSolidArc(transform.position, Vector3.back, startDirection, TypeSo.SensingRotation, TypeSo.SensingRange);
        }

        private void OnDrawGameScene() // 게임 씬에서 표시
        {
            
        }
    }
}
