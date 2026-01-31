using UnityEngine;

namespace Work.Yeonwoo._01_Scripts.ETC.csiimnida.CSILib.SoundManager.RunTime
{
    public class TempSoundPlayer : MonoBehaviour
    {
        [SerializeField] private string soundName;
        private void Start()
        {
            Work.Yeonwoo._01_Scripts.ETC.csiimnida.CSILib.SoundManager.RunTime.SoundManager.Instance.PlaySound(soundName);
        }

    }
}