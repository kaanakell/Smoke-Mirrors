using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Attach to a manager GameObject in the Credits scene.
/// The credits content itself is just a full-screen Image panel set up in the Inspector.
///
/// Behaviour:
///   • Plays menu music on arrival (same track as main menu).
///   • Optional auto-redirect to main menu after a configurable delay.
///   • "Back to Main Menu" button can also be wired up.
/// </summary>
public class CreditsScreen : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    [Header("Auto-redirect")]
    [Tooltip("If true, automatically goes to main menu after the delay below.")]
    [SerializeField] private bool autoRedirect = true;
    [Tooltip("Seconds to wait before auto-redirecting. Only used when autoRedirect is true.")]
    [SerializeField] private float autoRedirectDelay = 12f;

    [Header("Fade In (optional)")]
    [Tooltip("Assign a full-screen black Image here for a fade-in on arrival.")]
    [SerializeField] private UnityEngine.UI.Image fadeOverlay;
    [SerializeField] private float fadeInDuration = 1.2f;

    private void Start()
    {
        // Credits use the same music as the main menu
        AudioManager.Instance?.PlayMenuMusic();

        if (fadeOverlay != null)
            StartCoroutine(FadeIn());

        if (autoRedirect)
            StartCoroutine(AutoRedirectRoutine());
    }

    // ── Button callback (wire to "Back to Main Menu" button onClick) ────────
    public void OnBackToMenuClicked()
    {
        StopAllCoroutines();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // ── Internal ─────────────────────────────────────────────────────────────
    private IEnumerator AutoRedirectRoutine()
    {
        yield return new WaitForSeconds(autoRedirectDelay);
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private IEnumerator FadeIn()
    {
        Color c = fadeOverlay.color;
        c.a = 1f;
        fadeOverlay.color = c;

        for (float t = 0f; t < fadeInDuration; t += Time.deltaTime)
        {
            c.a = Mathf.Lerp(1f, 0f, t / fadeInDuration);
            fadeOverlay.color = c;
            yield return null;
        }
        c.a = 0f;
        fadeOverlay.color = c;
        fadeOverlay.gameObject.SetActive(false);
    }
}