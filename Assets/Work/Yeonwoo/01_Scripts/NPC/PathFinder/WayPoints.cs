using UnityEngine;

namespace Work.Yeonwoo._01_Scripts.NPC.PathFinder
{
    public class WayPoints : MonoBehaviour
    {
        [SerializeField] private WayPoint[] wayPoints;

        private int _currentIdx;

        public Vector3 GetNextWayPoint()
        {
            Debug.Assert(wayPoints.Length >= 1, "There must be at least one waypoint");
            _currentIdx = (_currentIdx + 1) % wayPoints.Length;
            return wayPoints[_currentIdx].Position;
        }
    }
}