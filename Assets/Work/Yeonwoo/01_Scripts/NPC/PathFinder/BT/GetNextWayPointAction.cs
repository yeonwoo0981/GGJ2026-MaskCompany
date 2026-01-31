using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Work.Yeonwoo._01_Scripts.NPC.PathFinder.BT
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "GetNextWayPoint", story: "Get [NextPathPoint] from [WayPoints]", category: "Action/Path", id: "dc58ecce1e021f5d0cdc83a443100d47")]
    public partial class GetNextWayPointAction : Action
    {
        [SerializeReference] public BlackboardVariable<Vector3> NextPathPoint;
        [SerializeReference] public BlackboardVariable<WayPoints> WayPoints;

        protected override Status OnStart()
        { 
            NextPathPoint.Value = WayPoints.Value.GetNextWayPoint();
            return Status.Success;
        }
    }
}

