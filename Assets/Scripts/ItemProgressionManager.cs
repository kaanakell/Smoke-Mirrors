// ItemProgressionManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ItemProgressionManager : MonoBehaviour
{
    public static ItemProgressionManager Instance { get; private set; }

    [Header("Unlock Stages")]
    [SerializeField] private UnlockStage[] unlockStages;

    private int _currentStage = 0;
    private bool _isMiniGameRevealed = false;
    private List<MiniGameStageObject> _miniGames = new();
    private List<ProgressionPickupItem> _allItems = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _allItems.Clear();
        _miniGames.Clear();
    }

    public void RegisterItem(ProgressionPickupItem item)
    {
        if (!_allItems.Contains(item))
            _allItems.Add(item);

        item.SetVisible(item.UnlockStageIndex == _currentStage);
    }

    public void RegisterMiniGame(MiniGameStageObject mg)
    {
        if (!_miniGames.Contains(mg)) _miniGames.Add(mg);
        mg.SetVisible(mg.ActivationStage == _currentStage && _isMiniGameRevealed);
    }

    public void SetStage(int newStage)
    {
        SetMiniGameVisibleForStage(_currentStage, false);
        _currentStage = newStage;
        _isMiniGameRevealed = false;
        RefreshAllItemVisibility();
        RefreshMiniGameVisibility();
        Debug.Log($"[IPM] Stage → {_currentStage}");
    }

    public void SetMiniGameVisible(bool visible)
    {
        _isMiniGameRevealed = visible;
        RefreshMiniGameVisibility();
    }

    public void ReportItemCollected(ProgressionPickupItem item, bool isForestTrigger) { }

    private void RefreshAllItemVisibility()
    {
        foreach (var item in _allItems)
        {
            if (item == null) continue;
            item.SetVisible(item.UnlockStageIndex == _currentStage);
        }
    }

    public void RefreshMiniGameVisibility()
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
}

[Serializable]
public class UnlockStage
{
    public bool enabled = true;
    public string stageName = "Stage";
}