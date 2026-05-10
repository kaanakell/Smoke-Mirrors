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

        // Ensure the glitch overlay is off
        if (glitchOverlay != null) glitchOverlay.gameObject.SetActive(false);

        // THE FIX: Force the main puzzle panel to turn off as soon as the game starts
        if (panel != null) panel.SetActive(false);
    }

    // THE FIX: Renamed from StartGame to OpenGame and added the optional parameter
    public void OpenGame(Sprite overrideSprite = null)
    {
        _player = FindFirstObjectByType<PlayerController>();
        if (_player != null) _player.MovementLocked = true;

        panel.SetActive(true);
        if (closeButton != null) closeButton.gameObject.SetActive(true);
        _snappedCount = 0;
        IsPlayingGlitch = false;

        // Initialize pieces
        if (pieces != null)
        {
            foreach (var p in pieces)
            {
                p.Setup(this, mainCanvas);
                p.isLocked = false;

                // Scatter the pieces randomly around the screen to start
                float randomX = Random.Range(-400f, 400f);
                float randomY = Random.Range(-250f, 250f);
                p.SnapTo(new Vector2(randomX, randomY));
            }
        }
    }

    public void CheckPiecePlacement(PuzzlePiece piece)
    {
        // Check distance between current position and target position
        float dist = Vector2.Distance(piece.rectTransform.anchoredPosition, piece.correctPosition);

        if (dist <= snapThreshold)
        {
            // SNAP!
            piece.SnapTo(piece.correctPosition);
            piece.isLocked = true;
            _snappedCount++;

            // If all pieces are placed, trigger the nightmare
            if (_snappedCount >= pieces.Length)
            {
                StartCoroutine(GlitchSequence());
            }
        }
    }

    private IEnumerator GlitchSequence()
    {
        IsPlayingGlitch = true;
        if (closeButton != null) closeButton.gameObject.SetActive(false); // Don't let them escape

        // 1. Give the player 1 second of satisfaction seeing the completed puzzle
        yield return new WaitForSeconds(1.0f);

        // 2. The Glitch (Violent shaking and flashing)
        float glitchDuration = 1.2f;
        float elapsed = 0f;

        if (glitchOverlay != null) glitchOverlay.gameObject.SetActive(true);

        while (elapsed < glitchDuration)
        {
            // Shake the pieces violently
            foreach (var p in pieces)
            {
                p.rectTransform.anchoredPosition = p.correctPosition + new Vector2(Random.Range(-80f, 80f), Random.Range(-80f, 80f));
            }

            // Flash the overlay
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

        // 3. The Scrambled State (The tragedy)
        foreach (var p in pieces)
        {
            p.SnapTo(p.scrambledPosition);
        }

        // 4. Force the player to look at their failure for 2.5 seconds
        yield return new WaitForSeconds(2.5f);

        CompleteMiniGame();
    }

    private void CompleteMiniGame()
    {
        // Tell the progression manager the game is done
        if (ItemProgressionManager.Instance != null)
        {
            ItemProgressionManager.Instance.CompleteMiniGame();
        }

        CloseGame();
    }

    public void CloseGame()
    {
        if (panel != null) panel.SetActive(false);
        if (_player != null) _player.MovementLocked = false;
    }
}