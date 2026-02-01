using UnityEngine;
using UnityEngine.SceneManagement;

namespace MaskCompany
{
    /// <summary>
    /// Quick reset: Click 8 times → load Tuto, Click 9 times → load GameNew
    /// Add this to any persistent object or main camera
    /// </summary>
    public class QuickReset : MonoBehaviour
    {
        [SerializeField] private float resetTimeout = 1f; // Time window for clicks
        [SerializeField] private string tutoSceneName = "tuto";
        [SerializeField] private string gameSceneName = "GameNew";
        
        private int clickCount;
        private float lastClickTime;

        private void Update()
        {
            // Reset counter if too much time passed
            if (clickCount > 0 && Time.unscaledTime - lastClickTime > resetTimeout)
            {
                clickCount = 0;
            }

            // Check for click (mouse or touch)
            if (Input.GetMouseButtonDown(0))
            {
                clickCount++;
                lastClickTime = Time.unscaledTime;

                if (clickCount == 9)
                {
                    Debug.Log("[QuickReset] 9 clicks → Loading GameNew");
                    clickCount = 0;
                    SceneManager.LoadScene(gameSceneName);
                }
                else if (clickCount == 8)
                {
                    // Wait a tiny bit to see if they click again for 9
                    // If they don't click again within timeout, we'll load tuto
                    Invoke(nameof(CheckForTutoLoad), resetTimeout);
                }
            }
        }

        private void CheckForTutoLoad()
        {
            // Only load tuto if still at exactly 8 clicks (didn't reach 9)
            if (clickCount == 8)
            {
                Debug.Log("[QuickReset] 8 clicks → Loading Tuto");
                clickCount = 0;
                SceneManager.LoadScene(tutoSceneName);
            }
        }
    }
}
