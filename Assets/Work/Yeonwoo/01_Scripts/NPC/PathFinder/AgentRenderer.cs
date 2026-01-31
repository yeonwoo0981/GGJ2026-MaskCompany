using UnityEngine;

namespace Work.Yeonwoo._01_Scripts.NPC.PathFinder
{
    public class AgentRenderer : MonoBehaviour
    {
        public bool IsFacingRight { get; private set; } = true;
        
        private Agent _owner;
        private SpriteRenderer _sr;

        public void Initialize(Agent agent)
        {
            _owner = agent;
            _sr = GetComponent<SpriteRenderer>();
        }
    }
}