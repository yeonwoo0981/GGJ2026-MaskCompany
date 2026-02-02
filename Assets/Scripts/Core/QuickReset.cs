using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MaskCompany
{
    /// <summary>
    /// Quick reset: Press 8 → load Tuto, Press 9 → load GameNew
    /// Add this to any persistent object or main camera
    /// </summary>
    public class QuickReset : MonoBehaviour
    {
        [SerializeField] private string tutoSceneName = "tuto";
        [SerializeField] private string gameSceneName = "GameNew";

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.digit8Key.wasPressedThisFrame)
            {
                Debug.Log("[QuickReset] 8 pressed → Loading Tuto");
                SceneManager.LoadScene(tutoSceneName);
            }
            
            if (kb.digit9Key.wasPressedThisFrame)
            {
                Debug.Log("[QuickReset] 9 pressed → Loading GameNew");
                SceneManager.LoadScene(gameSceneName);
            }
        }
    }
}
