using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager Instance { get; private set; }

    public static string NextSpawnID = "default";

    [Header("Spawn IDs")]
    [Tooltip("SpawnPoint.spawnID used only on the very first scene load. " +
             "Place a SpawnPoint with this ID in your starting scene (Bathroom).")]
    [SerializeField] private string gameStartSpawnID = "game_start";

    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeInDuration = 0.5f;

    private bool _isFirstLoad = true;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
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

        string targetID = _isFirstLoad ? gameStartSpawnID : NextSpawnID;
        _isFirstLoad = false;

        SpawnPoint[] points = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
        SpawnPoint target = FindSpawnPoint(points, targetID)
                           ?? FindSpawnPoint(points, "default")
                           ?? (points.Length > 0 ? points[0] : null);

        if (pc != null && target != null)
        {
            pc.transform.position = target.transform.position;
            pc.MovementLocked = false;
        }

        yield return StartCoroutine(FadeIn());
    }

    private static SpawnPoint FindSpawnPoint(SpawnPoint[] points, string id)
    {
        foreach (var sp in points)
            if (sp != null && sp.spawnID == id) return sp;
        return null;
    }

    private IEnumerator FadeIn()
    {
        if (fadeImage == null) yield break;

        Color c = fadeImage.color;
        c.a = 1f; fadeImage.color = c;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / fadeInDuration);
            fadeImage.color = c;
            yield return null;
        }
        c.a = 0f; fadeImage.color = c;
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
        c.a = 1f; fadeImage.color = c;
    }
}