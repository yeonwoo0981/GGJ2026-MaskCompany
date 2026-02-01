using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

namespace MaskCompany
{
    public class SceneFadeManager : MonoBehaviour
    {
        [Header("Fade Settings")]
        [SerializeField] private Image fadeImage;
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 1f;

        private bool isTransitioning;

        private void Start()
        {
            // Subscribe to level failed event
            if (LevelGoalHandler.Instance != null)
            {
                LevelGoalHandler.Instance.OnLevelFailed += OnLevelFailed;
            }

            // Fade in on scene load (from black to transparent)
            if (fadeImage != null)
            {
                fadeImage.gameObject.SetActive(true);
                Color c = fadeImage.color;
                c.a = 1f;
                fadeImage.color = c;
                
                fadeImage.DOFade(0f, fadeInDuration).OnComplete(() =>
                {
                    fadeImage.gameObject.SetActive(false);
                });
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (LevelGoalHandler.Instance != null)
            {
                LevelGoalHandler.Instance.OnLevelFailed -= OnLevelFailed;
            }
        }

        private void OnLevelFailed()
        {
            if (isTransitioning) return;
            isTransitioning = true;

            // Fade out and reload current scene
            if (fadeImage != null)
            {
                fadeImage.gameObject.SetActive(true);
                Color c = fadeImage.color;
                c.a = 0f;
                fadeImage.color = c;

                fadeImage.DOFade(1f, fadeOutDuration).OnComplete(() =>
                {
                    // Reload current scene
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                });
            }
            else
            {
                // No fade image, just reload
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
    }
}
