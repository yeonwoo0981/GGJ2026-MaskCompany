using UnityEngine;

namespace Work.Yeonwoo._01_Scripts.ETC
{
    public interface IPoolable
    {
        public string ItemName { get; }
        public GameObject GameObject { get; }
        public void ResetItem();
    }
}