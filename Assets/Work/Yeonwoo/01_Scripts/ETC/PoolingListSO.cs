using System.Collections.Generic;
using UnityEngine;

namespace Work.Yeonwoo._01_Scripts.ETC
{
    [CreateAssetMenu(fileName = "PoolingListSO", menuName = "Pooling/PoolingListSO")]
    public class PoolingListSO : ScriptableObject
    {
        public List<PoolItem> items; 
    }
}