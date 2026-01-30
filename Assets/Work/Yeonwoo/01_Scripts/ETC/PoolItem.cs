using UnityEngine;

namespace Work.Yeonwoo._01_Scripts.ETC
{
    [CreateAssetMenu(fileName = "PoolItem", menuName = "Pooling/PoolItem")]
    public class PoolItem : ScriptableObject
    {
        public string poolName;
        public GameObject prefab;
        public int count;

        private void OnValidate()
        {
            if (prefab == null) return;
            IPoolable item = prefab.GetComponent<IPoolable>();
            if (item == null)
            {
                Debug.LogWarning($"Can not find IPoolable in {prefab.name}");
                prefab = null;
                return;
            }

            poolName = item.ItemName;
        }
    }
}