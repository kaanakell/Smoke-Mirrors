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

    [Header("Glitch Timing")]
    [SerializeField] private float glitchDuration = 0.9f;
    [SerializeField] private float overlayFadeOut = 0.5f;
    [Tooltip("How long to wait AFTER closing the memory before the glitch starts.")]
    [SerializeField] private float delayAfterMemory = 1.0f;

    private string _returnSceneName;
    private Canvas _glitchCanvas;
    private Image _white, _red, _cyan;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildGlitchOverlay();
    }

    public void ForceTrigger(ItemData item, GameObject interactor)
    {
        _returnSceneName = SceneManager.GetActiveScene().name;
        ReturnPosition = interactor != null ? interactor.transform.position : Vector3.zero;

        if (item != null && !string.IsNullOrEmpty(item.memoryText))
            StartCoroutine(MemoryThenForestSequence(item));
        else
            StartCoroutine(GlitchIntoForest());
    }

    public void ReturnToHouse()
    {
        StartCoroutine(GlitchOutOfForest());
    }

    private IEnumerator MemoryThenForestSequence(ItemData itemData)
    {
        if (MemoryDisplay.Instance != null)
        {
            MemoryDisplay.Instance.ShowMemory(itemData.memoryText, itemData.memoryBackground);
        }

        yield return new WaitForSeconds(6.0f);

        yield return StartCoroutine(GlitchIntoForest());
    }

    private IEnumerator GlitchIntoForest()
    {
        _glitchCanvas.gameObject.SetActive(true);
        yield return StartCoroutine(PlayGlitch(glitchDuration));

        SetAlpha(_white, 1f); SetAlpha(_red, 0f); SetAlpha(_cyan, 0f);
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

        SetAlpha(_white, 1f); SetAlpha(_red, 0f); SetAlpha(_cyan, 0f);
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
                Shift(_red, offset); Shift(_cyan, -offset);
                SetAlpha(_white, Random.value < 0.1f ? Random.Range(0.1f, 0.35f) : 0f);
            }
            else
            {
                float phase = (t - 0.55f) / 0.45f;
                SetAlpha(_white, Mathf.Clamp01(Mathf.SmoothStep(0f, 1f, phase) + Random.Range(-0.03f, 0.03f)));
                SetAlpha(_red, 0f); SetAlpha(_cyan, 0f);
            }
            yield return null;
        }
    }

    private IEnumerator FadeOverlayOut(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(_white, Mathf.Lerp(1f, 0f, elapsed / duration));
            yield return null;
        }
        SetAlpha(_white, 0f); SetAlpha(_red, 0f); SetAlpha(_cyan, 0f);
        _glitchCanvas.gameObject.SetActive(false);
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
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return img;
    }

    private void SetAlpha(Image img, float a) { Color c = img.color; c.a = a; img.color = c; }
    private void Shift(Image img, float offsetX) { var rt = img.rectTransform; rt.offsetMin = new Vector2(offsetX, 0); rt.offsetMax = new Vector2(offsetX, 0); }
}