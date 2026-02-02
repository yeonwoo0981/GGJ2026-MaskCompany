using System.Collections;
using UnityEngine;

namespace MaskCompany
{
    /// <summary>
    /// Plays different music tracks based on the player's current mask.
    /// Crossfades between tracks when mask changes.
    /// </summary>
    public class MaskMusicPlayer : MonoBehaviour
    {
        [Header("Music Tracks")]
        [SerializeField] private AudioClip joyMusic;
        [SerializeField] private AudioClip neutralMusic;
        [SerializeField] private AudioClip angerMusic;
        [SerializeField] private AudioClip fearMusic;

        [Header("Settings")]
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] [Range(0f, 1f)] private float volume = 0.5f;

        [Header("Player Reference")]
        [SerializeField] private PlayerController player;

        private AudioSource sourceA;
        private AudioSource sourceB;
        private bool usingSourceA = true;
        private Coroutine fadeCoroutine;

        private void Awake()
        {
            // Create two audio sources for crossfading
            sourceA = gameObject.AddComponent<AudioSource>();
            sourceB = gameObject.AddComponent<AudioSource>();
            
            SetupAudioSource(sourceA);
            SetupAudioSource(sourceB);
        }

        private void SetupAudioSource(AudioSource source)
        {
            source.loop = true;
            source.playOnAwake = false;
            source.volume = 0f;
        }

        private void Start()
        {
            // Find player if not assigned
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
            }

            if (player != null)
            {
                // Subscribe to mask changes
                player.OnMaskChanged += OnMaskChanged;
                
                // Play initial music based on current mask
                PlayMusicForMask(player.CurrentMask, instant: true);
            }
            else
            {
                Debug.LogWarning("[MaskMusicPlayer] No PlayerController found!");
            }
        }

        private void OnDestroy()
        {
            if (player != null)
            {
                player.OnMaskChanged -= OnMaskChanged;
            }
        }

        private void OnMaskChanged(MaskType newMask)
        {
            PlayMusicForMask(newMask, instant: false);
        }

        private void PlayMusicForMask(MaskType mask, bool instant)
        {
            AudioClip clip = GetClipForMask(mask);
            
            if (clip == null)
            {
                Debug.LogWarning($"[MaskMusicPlayer] No music clip assigned for mask: {mask}");
                return;
            }

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            fadeCoroutine = StartCoroutine(CrossfadeToClip(clip, instant));
        }

        private AudioClip GetClipForMask(MaskType mask)
        {
            switch (mask)
            {
                case MaskType.Joy: return joyMusic;
                case MaskType.Neutral: return neutralMusic;
                case MaskType.Anger: return angerMusic;
                case MaskType.Fear: return fearMusic;
                default: return neutralMusic;
            }
        }

        private IEnumerator CrossfadeToClip(AudioClip newClip, bool instant)
        {
            AudioSource fadeOut = usingSourceA ? sourceA : sourceB;
            AudioSource fadeIn = usingSourceA ? sourceB : sourceA;
            usingSourceA = !usingSourceA;

            // Setup new clip
            fadeIn.clip = newClip;
            fadeIn.Play();

            if (instant)
            {
                // Instant switch
                fadeOut.Stop();
                fadeOut.volume = 0f;
                fadeIn.volume = volume;
            }
            else
            {
                // Crossfade
                float elapsed = 0f;
                float startVolumeOut = fadeOut.volume;

                while (elapsed < fadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = elapsed / fadeDuration;

                    fadeOut.volume = Mathf.Lerp(startVolumeOut, 0f, t);
                    fadeIn.volume = Mathf.Lerp(0f, volume, t);

                    yield return null;
                }

                fadeOut.Stop();
                fadeOut.volume = 0f;
                fadeIn.volume = volume;
            }

            fadeCoroutine = null;
        }

        /// <summary>
        /// Set master volume for music
        /// </summary>
        public void SetVolume(float newVolume)
        {
            volume = Mathf.Clamp01(newVolume);
            
            // Update currently playing source
            if (usingSourceA && sourceB.isPlaying)
                sourceB.volume = volume;
            else if (!usingSourceA && sourceA.isPlaying)
                sourceA.volume = volume;
        }
    }
}
