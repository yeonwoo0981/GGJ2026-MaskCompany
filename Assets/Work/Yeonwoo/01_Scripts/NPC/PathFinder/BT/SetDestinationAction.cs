using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Work.Yeonwoo._01_Scripts.NPC.PathFinder.BT
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Set destination", story: "[Self] navigate to [NextPosition]", category: "Action/Path", id: "c11f161e72e5af1332d47f02e6ca51bd")]
    public partial class SetDestinationAction : Action
    {
        [SerializeReference] public BlackboardVariable<Agent> Self;
        [SerializeReference] public BlackboardVariable<Vector3> NextPosition;

        
        protected override Status OnStart()
        {
            //Self.Value.get
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            return Status.Success;
        }

        protected override void OnEnd()
        {
        }
    }
}

