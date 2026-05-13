using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance { get; private set; }
    public enum StoryPhase { Intro, ClockGameDone, Forest1Done, MemoryGameDone, Forest2Done, PuzzleGameDone }
    public StoryPhase currentPhase = StoryPhase.Intro;
    public int itemsCollectedThisPhase = 0;
    public bool isMiniGameActiveInCurrentPhase = false;

    public bool corridorEventHasOccurred = false;

    [HideInInspector] public SonNPC sonNPC;

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
        sonNPC = FindFirstObjectByType<SonNPC>();
        SyncItemStages();

        if (MindForestTrigger.IsReturningFromForest && currentPhase == StoryPhase.Forest2Done)
        {
            StartCoroutine(RevealPuzzleAfterForest2());
        }
    }

    private IEnumerator RevealPuzzleAfterForest2()
    {
        yield return null;

        isMiniGameActiveInCurrentPhase = true;
        ItemProgressionManager.Instance?.SetMiniGameVisible(true);

        if (sonNPC == null) sonNPC = FindFirstObjectByType<SonNPC>();
        if (sonNPC != null) sonNPC.TriggerItemThresholdApproach(2);
    }

    private IEnumerator GuideSonToMemoryMatch()
    {
        if (sonNPC == null) sonNPC = FindFirstObjectByType<SonNPC>();

        float elapsed = 0f;
        const float timeout = 10f;
        while (sonNPC != null && !sonNPC.IsAvailable && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (sonNPC != null && sonNPC.IsAvailable)
        {
            sonNPC.TriggerItemThresholdApproach(1);
        }
        else
        {
            Debug.LogWarning("[StoryManager] GuideSonToMemoryMatch: son not available after timeout.");
        }
    }

    public void SyncItemStages()
    {
        int stage = GetStageForPhase(currentPhase);
        if (ItemProgressionManager.Instance != null)
        {
            ItemProgressionManager.Instance.SetStage(stage);
            ItemProgressionManager.Instance.SetMiniGameVisible(isMiniGameActiveInCurrentPhase);
        }
    }

    public bool OnItemCollected(ItemData item, GameObject interactor)
    {
        itemsCollectedThisPhase++;

        if (currentPhase == StoryPhase.Intro && itemsCollectedThisPhase >= 1)
            RevealMiniGame();

        switch (currentPhase)
        {
            case StoryPhase.Intro:
                if (itemsCollectedThisPhase >= 1) ItemProgressionManager.Instance?.SetMiniGameVisible(true);
                break;

            case StoryPhase.ClockGameDone:
                if (itemsCollectedThisPhase >= 2) return TriggerForest(item, interactor);
                break;

            case StoryPhase.Forest1Done:
                break;

            case StoryPhase.MemoryGameDone:
                if (itemsCollectedThisPhase >= 2) return TriggerForest(item, interactor);
                break;

            case StoryPhase.PuzzleGameDone:
                if (itemsCollectedThisPhase >= 1) return TriggerForest(item, interactor);
                break;
        }
        return false;
    }

    private void RevealMiniGame()
    {
        isMiniGameActiveInCurrentPhase = true;
        ItemProgressionManager.Instance?.SetMiniGameVisible(true);
    }

    private bool TriggerForest(ItemData item, GameObject interactor)
    {
        currentPhase++;
        itemsCollectedThisPhase = 0;
        isMiniGameActiveInCurrentPhase = false;
        MindForestTrigger.Instance.ForceTrigger(item, interactor);
        return true;
    }

    public void OnMakeupEventFinished()
    {
        itemsCollectedThisPhase = 0;
        isMiniGameActiveInCurrentPhase = true;
        StartCoroutine(RevealPuzzleAfterForest2());
        ItemProgressionManager.Instance?.SetMiniGameVisible(true);

        if (sonNPC == null) sonNPC = FindFirstObjectByType<SonNPC>();
        if (sonNPC != null) sonNPC.TriggerItemThresholdApproach(1);
    }

    public void OnMiniGameCompleted(int index)
    {
        isMiniGameActiveInCurrentPhase = false;
        itemsCollectedThisPhase = 0;

        if (index == 0) currentPhase = StoryPhase.ClockGameDone;
        if (index == 1) currentPhase = StoryPhase.MemoryGameDone;
        if (index == 2) currentPhase = StoryPhase.PuzzleGameDone;

        SyncItemStages();
    }

    private int GetStageForPhase(StoryPhase phase)
    {
        switch (phase)
        {
            case StoryPhase.Intro: return 0;
            case StoryPhase.ClockGameDone: return 1;
            case StoryPhase.Forest1Done: return 2;
            case StoryPhase.MemoryGameDone: return 3;
            case StoryPhase.Forest2Done: return 4;
            case StoryPhase.PuzzleGameDone: return 5;
            default: return 0;
        }
    }
}