using System;
using UnityEngine;

namespace Work.Yeonwoo._01_Scripts.NPC.PathFinder
{
    public class WayPoint : MonoBehaviour
    {
        public Vector3 Position => transform.position;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.position, 0.15f);
        }
    }
}