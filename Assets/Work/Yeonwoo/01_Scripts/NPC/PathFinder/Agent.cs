using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Work.Yeonwoo._01_Scripts.NPC.SO;

namespace Work.Yeonwoo._01_Scripts.NPC.PathFinder
{
    public abstract class Agent : MonoBehaviour
    {
        [field:SerializeField] public Enemy TypeSo { get; private set; }
        private Dictionary<Type, IComponent> _componentDict = new Dictionary<Type, IComponent>();

        protected virtual void Awake()
        {
            _componentDict = GetComponentsInChildren<IComponent>()
                .ToDictionary(compo => compo.GetType());
            InitializeComponents();
        }

        protected virtual void InitializeComponents()
        {
            foreach (IComponent compo in _componentDict.Values)
            {
                compo.Initialize(this);
            }
        }

        public T Get<T>() where T : IComponent
        {
            Type type = typeof(T);
            return (T)_componentDict.GetValueOrDefault(type);
        }
    }
}