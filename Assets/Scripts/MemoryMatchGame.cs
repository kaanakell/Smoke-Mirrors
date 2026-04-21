using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MemoryMatchGame : MonoBehaviour
{
    public static MemoryMatchGame Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject panel;
    [SerializeField] private RectTransform cardGrid;

    [Header("Card Prefab")]
    [Tooltip("Prefab with RawImage + MemoryMatchCard. " +
             "Size it in the prefab; GridLayoutGroup handles layout.")]
    [SerializeField] private GameObject cardPrefab;

    [Header("Card Pairs")]
    [Tooltip("Each MemoryCardData here becomes a pair (2 cards) in the grid.")]
    [SerializeField] private MemoryCardData[] cardPairs;

    [Header("Unrecognizable Cards")]
    [Tooltip("How many pairs have one scrambled/unrecognizable copy. " +
             "E.g. 2 → the first 2 pairs will each have one scrambled card.")]
    [Range(0, 20)]
    [SerializeField] private int unrecognizableCount = 2;

    [Header("Match Feedback")]
    [SerializeField] private float matchConfirmDelay = 0.4f;
    [SerializeField] private float wrongPairDelay = 0.6f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button closeButton;

    private readonly List<MemoryMatchCard> _cards = new();
    private MemoryMatchCard _firstSelected;
    private bool _waitingForClear;
    private int _matchedPairs;
    private int _totalPairs;
    private PlayerController _player;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    private void Start()
    {
        closeButton?.onClick.AddListener(CloseGame);
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
            CloseGame();
    }

    public void OpenGame()
    {
        _player = FindFirstObjectByType<PlayerController>();
        if (_player != null) _player.MovementLocked = true;

        if (panel != null) panel.SetActive(true);

        BuildGrid();
        UpdateStatus();
    }

    public void CloseGame()
    {
        StopAllCoroutines();
        DestroyCards();
        if (panel != null) panel.SetActive(false);
        if (_player != null) { _player.MovementLocked = false; _player = null; }
    }

    public void OnCardClicked(MemoryMatchCard card)
    {
        if (_waitingForClear) return;

        if (_firstSelected == null)
        {
            _firstSelected = card;
            card.SetSelected(true);
        }
        else
        {
            if (_firstSelected == card) return;

            card.SetSelected(true);
            StartCoroutine(EvaluatePair(_firstSelected, card));
            _firstSelected = null;
        }
    }

    private IEnumerator EvaluatePair(MemoryMatchCard a, MemoryMatchCard b)
    {
        _waitingForClear = true;

        if (a.MatchID == b.MatchID)
        {
            yield return new WaitForSeconds(matchConfirmDelay);
            a.SetMatched();
            b.SetMatched();
            _matchedPairs++;
            UpdateStatus();

            if (_matchedPairs >= _totalPairs)
                yield return StartCoroutine(OnAllMatched());
        }
        else
        {
            yield return new WaitForSeconds(wrongPairDelay);
            a.SetSelected(false);
            b.SetSelected(false);
        }

        _waitingForClear = false;
    }

    private IEnumerator OnAllMatched()
    {
        if (statusText != null) statusText.text = "All pairs found.";
        yield return new WaitForSeconds(1.5f);
        CloseGame();
    }

    private void BuildGrid()
    {
        DestroyCards();
        _matchedPairs = 0;
        _firstSelected = null;
        _waitingForClear = false;

        if (cardPairs == null || cardPairs.Length == 0)
        {
            Debug.LogError("[MemoryMatchGame] No MemoryCardData assigned to cardPairs.");
            return;
        }
        if (cardPrefab == null)
        {
            Debug.LogError("[MemoryMatchGame] No card prefab assigned.");
            return;
        }

        _totalPairs = cardPairs.Length;
        int scrambleCount = Mathf.Clamp(unrecognizableCount, 0, cardPairs.Length);

        var defs = new List<(int matchID, Texture2D tex, bool scramble)>();

        for (int i = 0; i < cardPairs.Length; i++)
        {
            if (cardPairs[i] == null || cardPairs[i].cardSprite == null)
            {
                Debug.LogWarning($"[MemoryMatchGame] cardPairs[{i}] or its sprite is null — skipped.");
                _totalPairs--;
                continue;
            }

            Texture2D tex = ExtractReadableTexture(cardPairs[i].cardSprite);

            defs.Add((i, tex, false));
            defs.Add((i, tex, i < scrambleCount));
        }

        for (int i = defs.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (defs[i], defs[j]) = (defs[j], defs[i]);
        }

        foreach (var d in defs)
        {
            GameObject go = Instantiate(cardPrefab, cardGrid);
            MemoryMatchCard card = go.GetComponent<MemoryMatchCard>();
            if (card != null)
                card.Init(d.matchID, d.tex, d.scramble, this);
            _cards.Add(card);
        }
    }

    private void DestroyCards()
    {
        foreach (var c in _cards)
        {
            if (c == null) continue;
            var img = c.GetComponent<RawImage>();
            if (img != null && img.texture != null) Destroy(img.texture);
            Destroy(c.gameObject);
        }
        _cards.Clear();
    }

    private void UpdateStatus()
    {
        if (statusText != null)
            statusText.text = $"Matched: {_matchedPairs} / {_totalPairs} pairs";
    }

    private static Texture2D ExtractReadableTexture(Sprite sprite)
    {
        Rect rect = sprite.rect;
        int w = Mathf.Max(1, (int)rect.width);
        int h = Mathf.Max(1, (int)rect.height);

        RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(sprite.texture, rt);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D result = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        result.ReadPixels(new Rect(rect.x, rect.y, w, h), 0, 0);
        result.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }
}