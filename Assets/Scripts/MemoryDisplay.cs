using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class MemoryDisplay : MonoBehaviour
{
    public static MemoryDisplay Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI memoryText;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.6f;
    [SerializeField] private float holdDuration = 2.5f;
    [SerializeField] private float fadeOutDuration = 0.8f;

    // Fires once when the memory display fully finishes (after fade out)
    // MindForestTrigger subscribes to this when it needs to chain forest after memory
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
    }

    public void ShowMemory(string text)
    {
        if (memoryText != null) memoryText.text = text;
        StopAllCoroutines();
        StartCoroutine(DisplayRoutine());
    }

    private IEnumerator DisplayRoutine()
    {
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.MovementLocked = true;

        yield return StartCoroutine(Fade(0f, 1f, fadeInDuration));
        yield return new WaitForSeconds(holdDuration);
        yield return StartCoroutine(Fade(1f, 0f, fadeOutDuration));

        if (pc != null) pc.MovementLocked = false;

        // Notify any listener that memory is done
        OnComplete?.Invoke();
        OnComplete = null; // clear so it doesn't fire again next time
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