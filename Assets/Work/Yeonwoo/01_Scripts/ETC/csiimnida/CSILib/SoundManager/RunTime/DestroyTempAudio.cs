using UnityEngine;

namespace CSILib.SoundManager.RunTime
{
    public class DestroyTempAudio : MonoBehaviour
    {
        private void Start()
        {
            Destroy(gameObject);
        }
    }
}