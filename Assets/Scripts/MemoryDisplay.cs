using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MemoryDisplay : MonoBehaviour
{
    public static MemoryDisplay Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI memoryText;

    [Header("Background")]
    [Tooltip("Assign an Image component that sits behind the memory text." + "Its sprite will be swapped per item. Leave unassigned to skip.")]
    [SerializeField] private Image backgroundImage;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.6f;
    [SerializeField] private float fadeOutDuration = 0.8f;
    // Removed holdDuration since it is now manual!

    public event Action OnComplete;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (backgroundImage != null) backgroundImage.gameObject.SetActive(false);
    }

    public void ShowMemory(string text, Sprite background = null)
    {
        if (memoryText != null) memoryText.text = text;

        if (backgroundImage != null)
        {
            if (background != null)
            {
                backgroundImage.sprite = background;
                backgroundImage.gameObject.SetActive(true);
            }
            else
            {
                backgroundImage.gameObject.SetActive(false);
            }
        }
        StopAllCoroutines();
        StartCoroutine(DisplayRoutine());
    }

    private IEnumerator DisplayRoutine()
    {
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.MovementLocked = true;

        yield return StartCoroutine(Fade(0f, 1f, fadeInDuration));

        // BUFFER: Wait a tiny bit so they don't accidentally skip it while mashing space
        yield return new WaitForSeconds(0.5f);

        // WAIT FOR INPUT: Pause the coroutine until Space or Click is pressed
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0));

        yield return StartCoroutine(Fade(1f, 0f, fadeOutDuration));

        if (pc != null) pc.MovementLocked = false;

        OnComplete?.Invoke();
        OnComplete = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}