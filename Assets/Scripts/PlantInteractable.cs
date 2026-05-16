using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class PlantInteractable : MonoBehaviour, IInteractable
{
    [Header("Growth Stages")]
    [SerializeField] private Sprite[] growthSprites;

    [Header("Dialogues")]
    [Tooltip("Plays when the player manually waters the plant.")]
    [SerializeField] private DialogueSet waterPlantDialogue;

    [Tooltip("The automatic monologue reminding the player to water the plant.")]
    [SerializeField] private DialogueSet reminderMonologue;

    private static int _growthStage = 0;
    private static bool _wasWateredThisVisit = false;

    private float _reminderTimer = 0f;
    private float _nextReminderTime = 0f;

    private SpriteRenderer _sr;

    public string PromptText => "[SPACE]  Water the plant";
    public bool CanInteract => true;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (MindForestTrigger.IsReturningFromForest && _wasWateredThisVisit)
        {
            _growthStage = Mathf.Min(_growthStage + 1, growthSprites.Length - 1);
            _wasWateredThisVisit = false;
        }

        UpdateSprite();
        SetNextReminderTime();
    }

    private void Update()
    {
        // Check if StoryManager exists and if a minigame is actively running
        if (StoryManager.Instance != null && StoryManager.Instance.isMiniGameActiveInCurrentPhase)
        {
            // Freeze the reminder timer completely while playing a minigame
            _reminderTimer = 0f;
            return;
        }

        if (!_wasWateredThisVisit)
        {
            _reminderTimer += Time.deltaTime;

            if (_reminderTimer >= _nextReminderTime)
            {
                TriggerReminder();
                _reminderTimer = 0f;
                SetNextReminderTime();
            }
        }
    }

    private void TriggerReminder()
    {
        if (DialogueManager.Instance != null &&
            reminderMonologue != null &&
            !DialogueManager.Instance.IsDialogueActive)
        {
            DialogueManager.Instance.StartDialogue(reminderMonologue);
        }
    }

    private void SetNextReminderTime()
    {
        _nextReminderTime = Random.Range(30f, 180f);
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract) return;

        _wasWateredThisVisit = true;

        if (DialogueManager.Instance != null && waterPlantDialogue != null)
        {
            DialogueManager.Instance.StartDialogue(waterPlantDialogue);
        }
    }

    private void UpdateSprite()
    {
        if (growthSprites != null && growthSprites.Length > 0)
        {
            int index = Mathf.Clamp(_growthStage, 0, growthSprites.Length - 1);
            _sr.sprite = growthSprites[index];
        }
    }
}