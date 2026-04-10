using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager Instance { get; private set; }
    public static string NextSpawnID = "default";

    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeInDuration = 0.5f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(SpawnAndFadeIn());
    }

    private IEnumerator SpawnAndFadeIn()
    {
        yield return null;

        PlayerController pc = FindFirstObjectByType<PlayerController>();

        // ── Mind forest return: restore exact position, skip spawn point lookup ──
        if (MindForestTrigger.IsReturningFromForest)
        {
            if (pc != null)
            {
                pc.transform.position = MindForestTrigger.ReturnPosition;
                pc.MovementLocked = false;
            }
            yield return StartCoroutine(FadeIn());
            yield break;
        }
        // ── Normal spawn point logic ──────────────────────────────────────────────

        SpawnPoint[] points = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
        SpawnPoint target = null;

        foreach (var sp in points)
        {
            if (sp.spawnID == NextSpawnID)
            {
                target = sp;
                break;
            }
        }

        if (target == null && points.Length > 0)
        {
            foreach (var sp in points)
            {
                if (sp.spawnID == "default")
                {
                    target = sp;
                    break;
                }
            }
        }

        if (target == null && points.Length > 0)
        {
            target = points[0];
        }

        if (pc != null && target != null)
        {
            pc.transform.position = target.transform.position;
            pc.MovementLocked = false;
        }

        yield return StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        if (fadeImage == null) yield break;

        Color c = fadeImage.color;
        c.a = 1f;
        fadeImage.color = c;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / fadeInDuration);
            fadeImage.color = c;
            yield return null;
        }
        c.a = 0f;
        fadeImage.color = c;
    }

    public IEnumerator FadeOut(float duration)
    {
        if (fadeImage == null) yield break;
        float elapsed = 0f;
        Color c = fadeImage.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, elapsed / duration);
            fadeImage.color = c;
            yield return null;
        }
        c.a = 1f;
        fadeImage.color = c;
    }
}