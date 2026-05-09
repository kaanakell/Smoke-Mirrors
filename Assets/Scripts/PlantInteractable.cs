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

    // --- Static Memory ---
    private static int _growthStage = 0;
    private static bool _wasWateredThisVisit = false;

    // --- Automatic Reminder Timer ---
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
        SetNextReminderTime(); // Start the clock!
    }

    private void Update()
    {
        // If it hasn't been watered yet, run the reminder timer
        if (!_wasWateredThisVisit)
        {
            _reminderTimer += Time.deltaTime;

            if (_reminderTimer >= _nextReminderTime)
            {
                TriggerReminder();
                _reminderTimer = 0f; // Reset timer
                SetNextReminderTime(); // Pick a new random time
            }
        }
    }

    private void TriggerReminder()
    {
        // Only show the reminder if the dialogue manager exists, has the dialogue, 
        // and ISN'T currently playing another conversation!
        if (DialogueManager.Instance != null &&
            reminderMonologue != null &&
            !DialogueManager.Instance.IsDialogueActive)
        {
            DialogueManager.Instance.StartDialogue(reminderMonologue);
        }
    }

    private void SetNextReminderTime()
    {
        // Picks a random time between 1 and 3 minutes (60 to 180 seconds)
        _nextReminderTime = Random.Range(60f, 180f);
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