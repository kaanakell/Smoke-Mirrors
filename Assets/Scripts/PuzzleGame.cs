using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PuzzleGame : MonoBehaviour
{
    public static PuzzleGame Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private Button closeButton;

    [Header("Puzzle Settings")]
    [SerializeField] private PuzzlePiece[] pieces;
    [Tooltip("How close (in pixels) the piece needs to be to snap into place.")]
    [SerializeField] private float snapThreshold = 40f;

    [Header("Glitch Effect")]
    [Tooltip("An image (like white noise or pure red/white) to flash rapidly.")]
    [SerializeField] private Image glitchOverlay;

    public bool IsPlayingGlitch { get; private set; }
    private int _snappedCount = 0;
    private PlayerController _player;

    private void Awake()
    {
        Instance = this;

        if (glitchOverlay != null) glitchOverlay.gameObject.SetActive(false);

        if (panel != null) panel.SetActive(false);
    }

    public void OpenGame(Sprite overrideSprite = null)
    {
        _player = FindFirstObjectByType<PlayerController>();
        if (_player != null) _player.MovementLocked = true;

        panel.SetActive(true);
        if (closeButton != null) closeButton.gameObject.SetActive(true);
        _snappedCount = 0;
        IsPlayingGlitch = false;

        if (pieces != null)
        {
            foreach (var p in pieces)
            {
                p.Setup(this, mainCanvas);
                p.isLocked = false;

                float randomX = Random.Range(-400f, 400f);
                float randomY = Random.Range(-250f, 250f);
                p.SnapTo(new Vector2(randomX, randomY));
            }
        }
    }

    public void CheckPiecePlacement(PuzzlePiece piece)
    {
        float dist = Vector2.Distance(piece.rectTransform.anchoredPosition, piece.correctPosition);

        if (dist <= snapThreshold)
        {
            piece.SnapTo(piece.correctPosition);
            piece.isLocked = true;
            _snappedCount++;

            if (_snappedCount >= pieces.Length)
            {
                StartCoroutine(GlitchSequence());
            }
        }
    }

    private IEnumerator GlitchSequence()
    {
        IsPlayingGlitch = true;
        if (closeButton != null) closeButton.gameObject.SetActive(false);

        yield return new WaitForSeconds(1.0f);

        float glitchDuration = 1.2f;
        float elapsed = 0f;

        if (glitchOverlay != null) glitchOverlay.gameObject.SetActive(true);

        while (elapsed < glitchDuration)
        {
            foreach (var p in pieces)
            {
                p.rectTransform.anchoredPosition = p.correctPosition + new Vector2(Random.Range(-80f, 80f), Random.Range(-80f, 80f));
            }

            if (glitchOverlay != null)
            {
                Color c = glitchOverlay.color;
                c.a = Random.Range(0.2f, 0.8f);
                glitchOverlay.color = c;
            }

            yield return new WaitForSeconds(0.06f);
            elapsed += 0.06f;
        }

        if (glitchOverlay != null) glitchOverlay.gameObject.SetActive(false);

        foreach (var p in pieces)
        {
            p.SnapTo(p.scrambledPosition);
        }

        yield return new WaitForSeconds(2.5f);

        CompleteMiniGame();
    }

    private void CompleteMiniGame()
    {
        if (StoryManager.Instance != null) StoryManager.Instance.OnMiniGameCompleted(2);

        CloseGame();
    }

    public void CloseGame()
    {
        if (panel != null) panel.SetActive(false);
        if (_player != null) _player.MovementLocked = false;
    }
}