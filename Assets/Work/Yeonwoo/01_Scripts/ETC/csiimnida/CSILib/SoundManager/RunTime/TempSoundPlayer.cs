using UnityEngine;

namespace CSILib.SoundManager.RunTime
{
    public class TempSoundPlayer : MonoBehaviour
    {
        [SerializeField] private string soundName;
        private void Start()
        {
            SoundManager.Instance.PlaySound(soundName);
        }

    }
}