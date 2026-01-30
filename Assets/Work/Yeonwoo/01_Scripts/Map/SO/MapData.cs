using UnityEngine;

namespace Work.Yeonwoo._01_Scripts.Map.SO
{
    [CreateAssetMenu(fileName = "MapData", menuName = "MapSO/MapData")]
    public class MapData : ScriptableObject
    {
        [field:SerializeField] public GameObject Map { get; set; } // ex
    }
}