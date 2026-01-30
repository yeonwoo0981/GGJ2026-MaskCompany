using System.Collections.Generic;
using UnityEngine;

namespace Work.Yeonwoo._01_Scripts.ETC
{
    public class Pool
    {
        private Stack<IPoolable> _pool;
        private Transform _parent;
        private IPoolable _poolable;
        private GameObject _prefab;

        public Pool(IPoolable poolable, Transform parent, int initCount)
        {
            _pool = new Stack<IPoolable>();
            _parent = parent;
            _poolable = poolable;
            _prefab = poolable.GameObject;

            for (int i = 0; i < initCount; i++)
            {
                GameObject item = Object.Instantiate(_prefab, _parent);
                item.name = _poolable.ItemName;
                item.SetActive(false);
                IPoolable poolableItem = item.GetComponent<IPoolable>(); 
                _pool.Push(poolableItem);
                item.SetActive(false);
            }
        }

        public IPoolable Pop()
        {
            IPoolable item = null;
            if (_pool.Count == 0)
            {
                GameObject gameObj = Object.Instantiate(_prefab, _parent);
                gameObj.name = _poolable.ItemName;
                item = gameObj.GetComponent<IPoolable>();
            }
            else
            {
                item = _pool.Pop();
                item.GameObject.SetActive(true);
            }

            return item;
        }

        public void Push(IPoolable item)
        {
            item.GameObject.SetActive(false);
            _pool.Push(item);
        }
    }
}