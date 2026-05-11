using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ItemProgressionManager : MonoBehaviour
{
    public static ItemProgressionManager Instance { get; private set; }

    public event Action<int> OnItemCollected;
    public event Action<int> OnStageUnlocked;

    public static event Action OnMiniGameCompleted;

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

    private int _totalCollected = 0;
    private int _collectedThisRound = 0;
    private int _currentStage = 0;
    private bool _approachPending = false;
    private bool _isMiniGameRevealed = false;

    private readonly List<ProgressionPickupItem> _allItems = new();
    private readonly List<MiniGameStageObject> _miniGames = new();

    private SonNPC _sonNpc;
    private Coroutine _approachCoroutine;

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
        _allItems.Clear();
        foreach (var item in FindObjectsByType<ProgressionPickupItem>(FindObjectsSortMode.None))
            RegisterItem(item);

        _miniGames.RemoveAll(mg => mg == null);
        foreach (var mg in FindObjectsByType<MiniGameStageObject>(FindObjectsSortMode.None))
            RegisterMiniGame(mg);

        _sonNpc = FindFirstObjectByType<SonNPC>();

        RefreshAllItemVisibility();
        RefreshMiniGameVisibility();

        if (_approachPending && _sonNpc != null)
        {
            if (_approachCoroutine != null) StopCoroutine(_approachCoroutine);
            _approachCoroutine = StartCoroutine(DelayedSonApproach(_currentStage));
        }

        if (scene.name == sonSceneName)
        {
            _sonNpc = FindFirstObjectByType<SonNPC>();
            RefreshAllItemVisibility();

            if (_approachPending)
            {
                Debug.Log("[Progression] Resuming pending Son approach after returning to Living Room.");
                if (_approachCoroutine != null) StopCoroutine(_approachCoroutine);
                _approachCoroutine = StartCoroutine(DelayedSonApproach(_currentStage));
            }
        }
    }

    public void RegisterItem(ProgressionPickupItem item)
    {
        if (!_allItems.Contains(item))
            _allItems.Add(item);
        item.SetVisible(ShouldStageBeVisible(item.UnlockStageIndex));
    }

    public void ReportItemCollected(ProgressionPickupItem item, bool mindForestTriggered = false)
    {
        if (item == null) return;

        _totalCollected++;
        OnItemCollected?.Invoke(_totalCollected);

        if (item.UnlockStageIndex == _currentStage)
        {
            _collectedThisRound++;
            int needed = ItemsNeededForStage(_currentStage);

            Debug.Log($"[Progression] Collected {_totalCollected} total, " +
                      $"{_collectedThisRound}/{needed} this round (stage {_currentStage}).");

            if (_collectedThisRound >= needed)
            {
                _collectedThisRound = 0;
                _isMiniGameRevealed = true;
                RefreshMiniGameVisibility();

                bool isFinalStage = _currentStage >= itemsPerStage.Length - 1;

                if (!isFinalStage)
                {
                    _approachPending = true;
                    RefreshMiniGameVisibility();
                    if (!mindForestTriggered)
                    {
                        if (_approachCoroutine != null) StopCoroutine(_approachCoroutine);
                        _approachCoroutine = StartCoroutine(DelayedSonApproach(_currentStage));
                    }
                    else
                    {
                        Debug.Log("[Progression] Mind Forest triggered — son approach queued for later.");
                    }
                }
                else
                {
                    _approachPending = false;
                    Debug.Log("[Progression] Final items collected. No more mini-games to lead to.");
                }
            }
        }
    }

    public void CompleteMiniGame()
    {
        SetMiniGameVisibleForStage(_currentStage, false);

        _currentStage++;
        _collectedThisRound = 0;
        _isMiniGameRevealed = false;

        RefreshAllItemVisibility();

        Debug.Log($"[Progression] Stage {_currentStage} unlocked. Required items: {ItemsNeededForStage(_currentStage)}");
        OnStageUnlocked?.Invoke(_currentStage);

        OnMiniGameCompleted?.Invoke();
    }

    public void RegisterMiniGame(MiniGameStageObject mg)
    {
        if (mg == null || _miniGames.Contains(mg)) return;
        _miniGames.Add(mg);

        mg.SetVisible(mg.ActivationStage == _currentStage);
    }

    private IEnumerator DelayedSonApproach(int targetStage)
    {
        yield return new WaitForSeconds(sonApproachDelay);

        if (_sonNpc == null)
            _sonNpc = FindFirstObjectByType<SonNPC>();

        while (_sonNpc != null && !_sonNpc.IsAvailable)
        {
            yield return new WaitForSeconds(0.5f);
        }

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

            _sonNpc.TriggerItemThresholdApproach(targetStage);
            Debug.Log($"[Progression] Son approach triggered successfully for stage {targetStage}.");
        }
        else
        {
            _approachCoroutine = null;
            Debug.Log("[Progression] Son not found after delay — approach still pending.");
        }
    }

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
            mg.SetVisible(mg.ActivationStage == _currentStage && _isMiniGameRevealed);
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

    private int ItemsNeededForStage(int stage)
    {
        if (itemsPerStage == null || itemsPerStage.Length == 0) return 1;
        int idx = Mathf.Clamp(stage, 0, itemsPerStage.Length - 1);
        return Mathf.Max(1, itemsPerStage[idx]);
    }

    public int TotalCollected => _totalCollected;
    public int CurrentStage => _currentStage;
    public bool ApproachPending => _approachPending;
}

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