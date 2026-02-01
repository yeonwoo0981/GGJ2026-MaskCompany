using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace CSILib.SoundManager.RunTime
{
    public class SoundManager : MonoSingleton<SoundManager>
    {
    
        [SerializeField] private SoundListSo _soundListSo;
        [SerializeField] private AudioMixer _mixer;

        protected override void Awake()
        {
            if (_soundListSo == null)
            {
                Debug.Assert(_soundListSo != null,$"SoundListSo asset is null");
            }
            if (_mixer == null)
            {
                Debug.LogError("AudioMixer가 할당되지 않았습니다. SoundManager를 사용하기 전에 할당해주세요.");
            }
        }
        public void PlaySound(string soundName)
        {
            GameObject obj = new GameObject();
            obj.name = soundName + " Sound";
            AudioSource source = obj.AddComponent<AudioSource>();
            SoundSo so = _soundListSo.SoundsDictionary[soundName];
            if (_mixer == null)
            {
                Debug.LogWarning("Mixer가 할당되지 않았습니다. SoundManager를 사용하기 전에 할당해주세요.");
                SetAudio(source,so);
                return;
            }
            if(so.soundType == SoundType.SFX)
                source.outputAudioMixerGroup = _mixer.FindMatchingGroups("SFX")[0];
            else if(so.soundType == SoundType.BGM)
            {
                source.outputAudioMixerGroup = _mixer.FindMatchingGroups("BGM")[0];
            }
            else
            {
                Debug.LogWarning("Type이 없습니다");
                source.outputAudioMixerGroup = _mixer.FindMatchingGroups("Master")[0];

            }
            SetAudio(source,so);
        
        }

        public void StopSound(string soundName, float duration = 0.5f, bool stopAll = true)
        {
            string targetName = soundName + " Sound";
            AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsSortMode.InstanceID);
            bool found = false;

            foreach (var src in sources)
            {
                if (src == null || src.gameObject == null) continue;
                if (src.gameObject.name == targetName)
                {
                    found = true;
                    StartCoroutine(StopCoroutine(src, duration));
                    if (!stopAll) break;
                }
            }

            if (!found)
            {
                Debug.LogWarning($"StopSoundFade: '{soundName}'에 해당하는 오디오 객체를 찾을 수 없습니다.");
            }
        }

        private IEnumerator StopCoroutine(AudioSource src, float duration)
        {
            if (src == null)
            {
                yield break;
            }

            float startVol = src.volume;
            float t = 0f;
            while (t < duration && src != null)
            {
                t += Time.unscaledDeltaTime;
                if (src != null) src.volume = Mathf.Lerp(startVol, 0f, t / duration);
                yield return null;
            }

            if (src != null)
            {
                src.Stop();
                Destroy(src.gameObject);
            }
        }
        
        private void SetAudio(AudioSource source,SoundSo sounds)
        {
            source.clip = sounds.clip;
            source.loop = sounds.loop;
            source.priority = sounds.Priority;
            source.volume = sounds.volume;
            source.pitch = sounds.pitch;
            source.panStereo = sounds.stereoPan;
            source.spatialBlend = sounds.SpatialBlend;
            if (sounds.RandomPitch)
            {
                sounds.pitch = Random.Range(sounds.MinPitch, sounds.MaxPitch);
            }
            if (sounds.pitch < 0)
            {
                source.time = 1;
            }
            source.Play();
            if (!sounds.loop) { StartCoroutine(DestroyCo(source.clip.length,source.gameObject)); }

        }

        IEnumerator DestroyCo(float endTime,GameObject obj)
        {
            yield return new WaitForSecondsRealtime(endTime);
            Destroy(obj);
        }
    }

    public enum SoundType
    {
        BGM,
        SFX
    }
}