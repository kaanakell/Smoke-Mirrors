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

    [Header("Scene Settings")]
    [Tooltip("Name of the scene that contains SonNPC (e.g. 'LivingRoomScene').")]
    [SerializeField] private string sonSceneName = "LivingRoomScene";

    [Header("Items Per Approach")]
    [SerializeField] private int itemsPerApproach = 2;

    [Header("Approach Delay")]
    [Tooltip("Seconds after scene load before triggering son (lets scene settle).")]
    [SerializeField] private float sonApproachDelay = 1.2f;

    [Header("Unlock Stages")]
    [Tooltip("Stage 0 = visible at start.  Stage N = revealed after Nth mini-game.")]
    [SerializeField] private UnlockStage[] unlockStages;

    private int _totalCollected = 0;
    private int _collectedThisRound = 0;
    private int _currentStage = 0;
    private bool _approachPending = false;

    private readonly List<ProgressionPickupItem> _allItems = new();
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

        _sonNpc = FindFirstObjectByType<SonNPC>();

        RefreshAllItemVisibility();

        if (_approachPending && _sonNpc != null)
        {
            if (_approachCoroutine != null) StopCoroutine(_approachCoroutine);
            _approachCoroutine = StartCoroutine(DelayedSonApproach());
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
        _totalCollected++;
        _collectedThisRound++;
        OnItemCollected?.Invoke(_totalCollected);

        Debug.Log($"[Progression] Collected {_totalCollected} total, " +
                  $"{_collectedThisRound}/{itemsPerApproach} this round.");

        if (_collectedThisRound >= itemsPerApproach)
        {
            _collectedThisRound = 0;
            _approachPending = true;

            if (!mindForestTriggered)
            {
                TryTriggerSonApproach();
            }
            else
            {

                Debug.Log("[Progression] Mind Forest triggered! Son approach delayed");
            }
        }
    }

    public void ReportMiniGameCompleted()
    {
        _currentStage++;
        Debug.Log($"[Progression] Mini-game completed — advancing to stage {_currentStage}.");

        _allItems.Clear();
        foreach (var item in FindObjectsByType<ProgressionPickupItem>(FindObjectsSortMode.None))
        {
            if (!_allItems.Contains(item)) _allItems.Add(item);
        }

        RefreshAllItemVisibility();
        OnStageUnlocked?.Invoke(_currentStage);
    }

    private void TryTriggerSonApproach()
    {
        if (_sonNpc == null)
        {
            Debug.Log("[Progression] Son not in current scene — approach queued.");
            return;
        }

        if (_approachCoroutine != null) StopCoroutine(_approachCoroutine);
        _approachCoroutine = StartCoroutine(DelayedSonApproach());
    }

    private IEnumerator DelayedSonApproach()
    {
        yield return new WaitForSeconds(sonApproachDelay);

        if (_sonNpc == null)
            _sonNpc = FindFirstObjectByType<SonNPC>();

        if (_sonNpc != null)
        {
            _approachPending = false;
            _approachCoroutine = null;
            _sonNpc.TriggerApproach();
            Debug.Log("[Progression] Son approach triggered successfully.");
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

    private bool ShouldStageBeVisible(int stageIndex)
    {
        if (stageIndex == 0) return true;
        if (stageIndex > _currentStage) return false;
        if (unlockStages != null && stageIndex < unlockStages.Length)
            return unlockStages[stageIndex].enabled;
        return true;
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