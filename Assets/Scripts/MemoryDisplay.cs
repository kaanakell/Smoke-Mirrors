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
    [SerializeField] private float holdDuration = 2.5f;
    [SerializeField] private float fadeOutDuration = 0.8f;

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

        if(backgroundImage != null) backgroundImage.gameObject.SetActive(false);
    }

    public void ShowMemory(string text, Sprite background = null)
    {
        if (memoryText != null) memoryText.text = text;

        if(backgroundImage != null)
        {
            if(background != null)
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
        yield return new WaitForSeconds(holdDuration);
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