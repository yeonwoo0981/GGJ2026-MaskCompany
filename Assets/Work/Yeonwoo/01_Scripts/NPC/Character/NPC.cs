using System;
using UnityEngine;
using Work.Yeonwoo._01_Scripts.NPC.SO;

namespace Work.Yeonwoo._01_Scripts.NPC.Character
{
    public abstract class NPC : MonoBehaviour
    {
        [SerializeField] protected Enemy typeSO;
        
        protected virtual void Update()
        {
            if (typeSO.AwarenessScore < 0)
            {
                Destroy(gameObject);
            }
        }
    }
}