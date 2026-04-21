using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MindForestTrigger : MonoBehaviour
{
    public static MindForestTrigger Instance { get; private set; }

    public static bool IsReturningFromForest { get; private set; }
    public static Vector3 ReturnPosition { get; private set; }

    [Header("Scene")]
    [SerializeField] private string mindForestSceneName = "MindForestScene";

    [Header("Trigger Probability")]
    [Range(0f, 1f)]
    [SerializeField] private float triggerChance = 0.30f;
    [SerializeField] private int minimumPickupsBefore = 2;
    [SerializeField] private float cooldownSeconds = 90f;

    [Header("Glitch Timing")]
    [SerializeField] private float glitchDuration = 0.9f;
    [SerializeField] private float overlayFadeOut = 0.5f;

    private int _totalPickups = 0;
    private float _lastTriggerTime = -999f;
    private string _returnSceneName;

    private Canvas _glitchCanvas;
    private Image _white;
    private Image _red;
    private Image _cyan;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildGlitchOverlay();
    }

    public bool TryTrigger(ItemData item, GameObject interactor)
    {
        if (!item.canTriggerMindForest) return false;

        _totalPickups++;
        if (!RollSucceeds()) return false;

        _lastTriggerTime = Time.time;
        _returnSceneName = SceneManager.GetActiveScene().name;

        PlayerController pc = interactor.GetComponent<PlayerController>()
                           ?? FindFirstObjectByType<PlayerController>();
        ReturnPosition = pc != null ? pc.transform.position : Vector3.zero;

        if (!string.IsNullOrEmpty(item.memoryText))
            StartCoroutine(MemoryThenForest(item.memoryText, pc));
        else
        {
            if (pc != null) pc.MovementLocked = true;
            StartCoroutine(GlitchIntoForest());
        }

        return true;
    }

    public void ReturnToHouse()
    {
        StartCoroutine(GlitchOutOfForest());
    }

    private IEnumerator MemoryThenForest(string memoryText, PlayerController pc)
    {
        bool done = false;
        MemoryDisplay.Instance.OnComplete += () => done = true;
        MemoryDisplay.Instance.ShowMemory(memoryText);

        yield return new WaitUntil(() => done);
        yield return new WaitForSeconds(0.3f);

        if (pc != null) pc.MovementLocked = true;
        yield return StartCoroutine(GlitchIntoForest());
    }

    private IEnumerator GlitchIntoForest()
    {
        _glitchCanvas.gameObject.SetActive(true);
        yield return StartCoroutine(PlayGlitch(glitchDuration));

        SetAlpha(_white, 1f);
        SetAlpha(_red, 0f);
        SetAlpha(_cyan, 0f);

        IsReturningFromForest = false;
        PlayerSpawnManager.NextSpawnID = "forest_entry";

        AsyncOperation load = SceneManager.LoadSceneAsync(mindForestSceneName);
        yield return new WaitUntil(() => load.isDone);

        yield return StartCoroutine(FadeOverlayOut(overlayFadeOut));
    }

    private IEnumerator GlitchOutOfForest()
    {
        _glitchCanvas.gameObject.SetActive(true);
        yield return StartCoroutine(PlayGlitch(glitchDuration));

        SetAlpha(_white, 1f);
        SetAlpha(_red, 0f);
        SetAlpha(_cyan, 0f);

        IsReturningFromForest = true;

        AsyncOperation load = SceneManager.LoadSceneAsync(_returnSceneName);
        yield return new WaitUntil(() => load.isDone);

        yield return StartCoroutine(FadeOverlayOut(overlayFadeOut));
    }

    private IEnumerator PlayGlitch(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (t < 0.55f)
            {
                float phase = t / 0.55f;
                float offset = Mathf.Lerp(0f, 40f, phase) + Random.Range(-4f, 4f);
                SetAlpha(_red, Random.Range(0f, 0.15f * phase));
                SetAlpha(_cyan, Random.Range(0f, 0.15f * phase));
                Shift(_red, offset);
                Shift(_cyan, -offset);
                SetAlpha(_white, Random.value < 0.1f ? Random.Range(0.1f, 0.35f) : 0f);
            }
            else
            {
                float phase = (t - 0.55f) / 0.45f;
                SetAlpha(_white, Mathf.Clamp01(
                    Mathf.SmoothStep(0f, 1f, phase) + Random.Range(-0.03f, 0.03f)));
                SetAlpha(_red, 0f);
                SetAlpha(_cyan, 0f);
            }
            yield return null;
        }
    }

    private IEnumerator FadeOverlayOut(float duration)
    {
        yield return null;
        yield return null;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(_white, Mathf.Lerp(1f, 0f, elapsed / duration));
            yield return null;
        }

        SetAlpha(_white, 0f);
        SetAlpha(_red, 0f);
        SetAlpha(_cyan, 0f);
        _glitchCanvas.gameObject.SetActive(false);
    }

    private bool RollSucceeds()
    {
        if (_totalPickups < minimumPickupsBefore) return false;
        if (Time.time - _lastTriggerTime < cooldownSeconds) return false;
        return Random.value < triggerChance;
    }

    private void BuildGlitchOverlay()
    {
        var go = new GameObject("MindGlitchCanvas");
        go.transform.SetParent(transform);
        _glitchCanvas = go.AddComponent<Canvas>();
        _glitchCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _glitchCanvas.sortingOrder = 999;
        go.AddComponent<UnityEngine.UI.CanvasScaler>();
        go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        go.SetActive(false);

        _white = MakeImage(go, "White", new Color(1f, 1f, 1f, 0f));
        _red = MakeImage(go, "Red", new Color(1f, 0.1f, 0.1f, 0f));
        _cyan = MakeImage(go, "Cyan", new Color(0f, 0.9f, 0.85f, 0f));
    }

    private Image MakeImage(GameObject parent, string goName, Color color)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return img;
    }

    private void SetAlpha(Image img, float a)
    {
        Color c = img.color; c.a = a; img.color = c;
    }

    private void Shift(Image img, float offsetX)
    {
        var rt = img.rectTransform;
        rt.offsetMin = new Vector2(offsetX, 0);
        rt.offsetMax = new Vector2(offsetX, 0);
    }
}