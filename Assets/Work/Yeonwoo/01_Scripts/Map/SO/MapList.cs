using UnityEngine;

namespace Work.Yeonwoo._01_Scripts.Map.SO
{
    [CreateAssetMenu(fileName = "MapList", menuName = "MapSO/MapList")]
    public class MapList : ScriptableObject
    {
        [field: SerializeField] public MapData[] MapData { get; set; }
    }
}