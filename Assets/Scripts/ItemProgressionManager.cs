using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ItemProgressionManager : MonoBehaviour
{
    public static ItemProgressionManager Instance { get; private set; }

    // ── Events ────────────────────────────────────────────────────────────────

    public event Action<int> OnItemCollected;
    public event Action<int> OnStageUnlocked;

    /// <summary>
    /// Fired after ANY mini game completes and the stage has been advanced.
    /// SonNPC subscribes to this to trigger its post-game dialogue for every
    /// mini game — not just the clock drawing one.
    /// </summary>
    public static event Action OnMiniGameCompleted;

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Scene Settings")]
    [Tooltip("Name of the scene that contains SonNPC (e.g. 'LivingRoomScene').")]
    [SerializeField] private string sonSceneName = "LivingRoomScene";

    [Header("Items Per Stage")]
    [Tooltip("How many items must be collected at each stage before the Son approaches.\n" +
             "Index 0 = bathroom phase (before clock game)\n" +
             "Index 1 = after clock game\n" +
             "Index 2 = after mini game 2\n" +
             "Index 3 = after mini game 3\n" +
             "Add more entries to extend the game.")]
    [SerializeField] private int[] itemsPerStage = { 1, 1, 2, 3 };

    [Header("Approach Delay")]
    [Tooltip("Seconds after scene load before triggering son (lets scene settle).")]
    [SerializeField] private float sonApproachDelay = 1.2f;

    [Header("Unlock Stages")]
    [Tooltip("Stage 0 = visible at start. Stage N = revealed after Nth mini-game.")]
    [SerializeField] private UnlockStage[] unlockStages;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private int _totalCollected = 0;
    private int _collectedThisRound = 0;
    private int _currentStage = 0;
    private bool _approachPending = false;

    private readonly List<ProgressionPickupItem> _allItems = new();
    private readonly List<MiniGameStageObject> _miniGames = new();

    private SonNPC _sonNpc;
    private Coroutine _approachCoroutine;

    // =========================================================================
    // Unity lifecycle
    // =========================================================================

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
        // ── Re-discover items ──────────────────────────────────────────────────
        _allItems.Clear();
        foreach (var item in FindObjectsByType<ProgressionPickupItem>(FindObjectsSortMode.None))
            RegisterItem(item);

        // ── Re-discover mini game stations ────────────────────────────────────
        // Remove destroyed entries from previous scenes first
        _miniGames.RemoveAll(mg => mg == null);
        foreach (var mg in FindObjectsByType<MiniGameStageObject>(FindObjectsSortMode.None))
            RegisterMiniGame(mg);

        // ── Find SonNPC in new scene ───────────────────────────────────────────
        _sonNpc = FindFirstObjectByType<SonNPC>();

        RefreshAllItemVisibility();
        RefreshMiniGameVisibility();

        // ── Trigger queued son approach if the Son is now available ───────────
        if (_approachPending && _sonNpc != null)
        {
            if (_approachCoroutine != null) StopCoroutine(_approachCoroutine);
            _approachCoroutine = StartCoroutine(DelayedSonApproach(_currentStage));
        }
    }

    // =========================================================================
    // Item API
    // =========================================================================

    public void RegisterItem(ProgressionPickupItem item)
    {
        if (!_allItems.Contains(item))
            _allItems.Add(item);
        item.SetVisible(ShouldStageBeVisible(item.UnlockStageIndex));
    }

    public void ReportItemCollected(ProgressionPickupItem item, bool isForestTrigger)
    {
        if (item == null) return;

        _totalCollected++;
        OnItemCollected?.Invoke(_totalCollected);

        if (item.UnlockStageIndex == _currentStage)
        {
            _collectedThisRound++;

            // Get the target number from your new MiniGameStageObject logic
            int neededItems = ItemsNeededForStage(_currentStage);

            Debug.Log($"[Progression] Stage {_currentStage} item collected: {_collectedThisRound} / {neededItems}");

            // THE FIX: Ensure neededItems is greater than 0 so it doesn't auto-trigger!
            if (neededItems > 0 && _collectedThisRound >= neededItems)
            {
                _approachPending = true;
                if (_approachCoroutine != null) StopCoroutine(_approachCoroutine);

                _approachCoroutine = StartCoroutine(DelayedSonApproach(_currentStage));
                RefreshMiniGameVisibility();
            }
        }
    }

    // =========================================================================
    // Mini game API
    // =========================================================================

    /// <summary>
    /// Call this from any mini game (ClockDrawingGame, PuzzleGame, MemoryMatchGame)
    /// when the player finishes or the timer runs out.
    /// </summary>
    public void CompleteMiniGame()
    {
        // Hide the station that just finished
        SetMiniGameVisibleForStage(_currentStage, false);

        _currentStage++;
        _collectedThisRound = 0;

        RefreshAllItemVisibility();

        Debug.Log($"[Progression] Stage {_currentStage} unlocked. Required items: {ItemsNeededForStage(_currentStage)}");
        OnStageUnlocked?.Invoke(_currentStage);

        // Notify SonNPC (and any other listeners) so the post-game dialogue plays
        OnMiniGameCompleted?.Invoke();
    }

    // =========================================================================
    // Mini game station registry
    // =========================================================================

    public void RegisterMiniGame(MiniGameStageObject mg)
    {
        if (mg == null || _miniGames.Contains(mg)) return;
        _miniGames.Add(mg);

        // Apply correct visibility immediately
        mg.SetVisible(mg.ActivationStage == _currentStage);
    }

    // =========================================================================
    // Son approach
    // =========================================================================

    private IEnumerator DelayedSonApproach(int targetStage)
    {
        yield return new WaitForSeconds(sonApproachDelay);

        if (_sonNpc == null)
            _sonNpc = FindFirstObjectByType<SonNPC>();

        while (_sonNpc != null && !_sonNpc.IsAvailable)
        {
            yield return new WaitForSeconds(0.5f);
        }

        // THE FIX: If the player manually finished the game while we were waiting,
        // the stage naturally advanced. Abort this queued approach so we don't open the NEXT game!
        if (targetStage != _currentStage)
        {
            _approachPending = false;
            _approachCoroutine = null;
            Debug.Log($"[Progression] Aborting approach. Stage changed from {targetStage} to {_currentStage}.");
            yield break;
        }

        if (_sonNpc != null)
        {
            _approachPending = false;
            _approachCoroutine = null;

            // Trigger the Son for the stage we memorized
            _sonNpc.TriggerItemThresholdApproach(targetStage);
            Debug.Log($"[Progression] Son approach triggered successfully for stage {targetStage}.");
        }
        else
        {
            _approachCoroutine = null;
            Debug.Log("[Progression] Son not found after delay — approach still pending.");
        }
    }

    // =========================================================================
    // Visibility helpers
    // =========================================================================

    private void RefreshAllItemVisibility()
    {
        foreach (var item in _allItems)
        {
            if (item == null) continue;
            item.SetVisible(ShouldStageBeVisible(item.UnlockStageIndex));
        }
    }

    private void RefreshMiniGameVisibility()
    {
        foreach (var mg in _miniGames)
        {
            if (mg == null) continue;
            mg.SetVisible(mg.ActivationStage == _currentStage);
        }
    }

    private void SetMiniGameVisibleForStage(int stage, bool visible)
    {
        foreach (var mg in _miniGames)
            if (mg != null && mg.ActivationStage == stage)
                mg.SetVisible(visible);
    }

    private bool ShouldStageBeVisible(int stageIndex)
    {
        if (stageIndex == 0) return true;
        if (stageIndex > _currentStage) return false;
        if (unlockStages != null && stageIndex < unlockStages.Length)
            return unlockStages[stageIndex].enabled;
        return true;
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>Returns how many items must be collected at a given stage.</summary>
    private int ItemsNeededForStage(int stage)
    {
        if (itemsPerStage == null || itemsPerStage.Length == 0) return 1;
        // Clamp so the last entry applies to any stage beyond the array bounds
        int idx = Mathf.Clamp(stage, 0, itemsPerStage.Length - 1);
        return Mathf.Max(1, itemsPerStage[idx]);
    }

    // =========================================================================
    // Public read-only state
    // =========================================================================

    public int TotalCollected => _totalCollected;
    public int CurrentStage => _currentStage;
    public bool ApproachPending => _approachPending;
}

// ── Data class ────────────────────────────────────────────────────────────────

[Serializable]
public class UnlockStage
{
    [Tooltip("Uncheck to skip this stage entirely.")]
    public bool enabled = true;
    [Tooltip("Label for your reference only.")]
    public string stageName = "Stage";
    [Min(0)]
    [Tooltip("Informational — actual visibility is per-item via unlockStageIndex.")]
    public int itemsVisibleThisStage = 2;
}