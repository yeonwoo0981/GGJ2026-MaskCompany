using UnityEngine;

namespace Work.Yeonwoo._01_Scripts.NPC.PathFinder
{
    public class AgentMover : MonoBehaviour, IComponent
    {
        [SerializeField] private new Rigidbody2D rb;
        private Agent _owner;
        
        public void Initialize(Agent agent)
        {
            _owner = agent;
        }
    }
}