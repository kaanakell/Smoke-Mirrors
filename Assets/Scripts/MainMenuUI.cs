using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to a Main Menu canvas or manager GameObject.
/// Wire the three buttons in the Inspector or via UnityEvent onClick.
///
/// Inspector fields:
///   gameplaySceneName → name of your main gameplay scene
///   creditsSceneName  → name of your credits scene
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string gameplaySceneName = "MainScene";
    [SerializeField] private string creditsSceneName = "CreditsScene";

    [Header("Settings Panel (optional)")]
    [Tooltip("Assign the settings/volume panel here if it lives in this scene.")]
    [SerializeField] private GameObject settingsPanel;

    private void Start()
    {
        // Make sure menu music is playing when we land here
        AudioManager.Instance?.PlayMenuMusic();

        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    // ── Button callbacks (assign in Inspector → Button → OnClick) ──────────

    public void OnPlayClicked()
    {
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void OnCreditsClicked()
    {
        SceneManager.LoadScene(creditsSceneName);
    }

    public void OnSettingsClicked()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    public void OnQuitClicked()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}