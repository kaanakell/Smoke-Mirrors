using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Pathfinding;

public class MindForestManager : MonoBehaviour
{
    private static int _visitCount = 0;

    [Header("NPC")]
    [SerializeField] private Transform npc;
    [SerializeField] private float npcStartX = 8f;
    [SerializeField] private float npcDepartX = 8f;
    [SerializeField] private float npcApproachSpeed = 2f;
    [SerializeField] private float npcDepartSpeed = 2.8f;
    [SerializeField] private float npcFadeInDuration = 1.5f;

    [Header("NPC Timing")]
    [SerializeField] private float timeBeforeNpcApproaches = 25f;

    [Header("Player Lock")]
    [SerializeField] private float playerLockRadius = 2.5f;
    [SerializeField] private float stopDistance = 1.5f;

    [Header("Seasonal Layouts")]
    [Tooltip("Drag the Winter, Spring and Summer Tilemap in order")]
    [SerializeField] private GameObject[] seasonalLayouts;

    [Header("Dialogue Sets  (one per visit, in order)")]
    [Tooltip("Each element is used for the corresponding forest visit.\n" +
             "Visit 1 → index 0,  Visit 2 → index 1, etc.\n" +
             "If loopDialogues is true, cycles back to index 0 after the last.\n" +
             "If false, the last set is reused for all subsequent visits.")]
    [SerializeField] private DialogueSet[] dialogueSets;

    [Tooltip("If true, cycles through dialogue sets endlessly. " +
             "If false, repeats the last set once all are used.")]
    [SerializeField] private bool loopDialogues = false;

    private List<DialogueLine> _lines;
    private bool _dialogueActive;
    private bool _canAdvance;
    private SpriteRenderer _npcSr;
    private PlayerController _player;

    private void Start()
    {
        // FIX: Increment once per visit.
        _visitCount++;
        Debug.Log($"<color=orange>[MindForest] Scene Started! Visit count is now: {_visitCount}. Playing Dialogue Index: {_visitCount - 1}</color>");
        // 1. Seasonal Layout Activation
        if (seasonalLayouts != null && seasonalLayouts.Length > 0)
        {
            // Visit 1 (index 0) = Winter, Visit 2 (index 1) = Autumn, etc.
            int layoutIndex = (_visitCount - 1) % seasonalLayouts.Length;

            for (int i = 0; i < seasonalLayouts.Length; i++)
            {
                if (seasonalLayouts[i] != null)
                    seasonalLayouts[i].SetActive(i == layoutIndex);
            }
        }

        _npcSr = npc != null ? npc.GetComponent<SpriteRenderer>() : null;

        if (npc != null)
        {
            npc.position = new Vector3(npcStartX, npc.position.y, npc.position.z);
            SetNpcAlpha(0f);
            var ai = npc.GetComponent<AIPath>();
            if (ai != null) ai.canMove = false;
        }

        // 3. Prepare Dialogue
        _lines = BuildDialogueForThisVisit();

        // 4. Start the sequence
        StartCoroutine(ForestRoutine());
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticVariables()
    {
        _visitCount = 0;
    }

    private void Update()
    {
        if (!_dialogueActive || !_canAdvance) return;
    }

    private List<DialogueLine> BuildDialogueForThisVisit()
    {
        // DO NOT increment _visitCount here. We did it in Start().

        // We use (_visitCount - 1) because the first visit is 1, 
        // but the first array index is 0.
        int index = _visitCount - 1;

        if (dialogueSets == null || dialogueSets.Length == 0)
        {
            return new List<DialogueLine> { new DialogueLine { speaker = "???", text = "..." } };
        }

        // Handle index out of bounds (looping or clamping)
        if (index >= dialogueSets.Length)
        {
            index = loopDialogues ? (index % dialogueSets.Length) : (dialogueSets.Length - 1);
        }

        // Return the lines from the correct set
        return new List<DialogueLine>(dialogueSets[index].lines);
    }

    private IEnumerator ForestRoutine()
    {
        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(WaitForPlayer());

        if (_player == null)
        {
            Debug.LogError("[MindForestManager] Player not found.");
            yield break;
        }

        yield return new WaitForSeconds(timeBeforeNpcApproaches);

        StartCoroutine(FadeNpc(0f, 1f, npcFadeInDuration));
        yield return StartCoroutine(NpcApproach());

        yield return new WaitForSeconds(0.4f);
        BeginDialogue();
    }

    private IEnumerator WaitForPlayer()
    {
        float timeout = 5f, elapsed = 0f;
        while (_player == null && elapsed < timeout)
        {
            _player = FindFirstObjectByType<PlayerController>();
            elapsed += Time.deltaTime;
            if (_player == null) yield return null;
        }
    }

    private IEnumerator NpcApproach()
    {
        if (npc == null || _player == null) yield break;

        var ai = npc.GetComponent<AIPath>();
        bool graphReady = AstarPath.active != null &&
                          AstarPath.active.graphs?.Length > 0;

        if (ai != null && graphReady) { yield return StartCoroutine(AStarApproach(ai)); yield break; }
        yield return StartCoroutine(DirectApproach());
    }

    private IEnumerator AStarApproach(AIPath ai)
    {
        ai.maxSpeed = npcApproachSpeed;
        ai.canMove = true;
        float timeout = 20f, elapsed = 0f;
        while (elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            float dist = Vector2.Distance(npc.position, _player.transform.position);
            if (dist <= stopDistance) break;
            ai.destination = _player.transform.position;
            if (dist <= playerLockRadius) LockPlayer();
            yield return null;
        }
        ai.canMove = false;
        LockPlayer();
    }

    private IEnumerator DirectApproach()
    {
        while (true)
        {
            if (_player == null) yield break;
            float dist = Vector2.Distance(npc.position, _player.transform.position);
            if (dist <= stopDistance) break;
            npc.position = Vector3.MoveTowards(npc.position, _player.transform.position,
                                               npcApproachSpeed * Time.deltaTime);
            if (dist <= playerLockRadius) LockPlayer();
            yield return null;
        }
        LockPlayer();
    }

    private void LockPlayer()
    {
        if (_player != null && !_player.MovementLocked) _player.MovementLocked = true;
    }

    private IEnumerator NpcDepart()
    {
        //StartCoroutine(FadeNpc(1f, 0f, npcFadeInDuration));

        var ai = npc != null ? npc.GetComponent<AIPath>() : null;
        bool graphReady = AstarPath.active != null && AstarPath.active.graphs?.Length > 0;

        if (ai != null && graphReady)
        {
            ai.maxSpeed = npcDepartSpeed;
            ai.canMove = true;
            ai.destination = new Vector3(npcDepartX, npc.position.y, 0f);
            float timeout = 6f, elapsed = 0f;
            while (!ai.reachedDestination && elapsed < timeout) { elapsed += Time.deltaTime; yield return null; }
            ai.canMove = false;
            yield break;
        }

        Vector3 target = new Vector3(npcDepartX, npc.position.y, npc.position.z);
        while (Vector3.Distance(npc.position, target) > 0.1f)
        {
            npc.position = Vector3.MoveTowards(npc.position, target, npcDepartSpeed * Time.deltaTime);
            yield return null;
        }
    }

    private void SetNpcAlpha(float a)
    {
        if (_npcSr == null) return;
        Color c = _npcSr.color; c.a = a; _npcSr.color = c;
    }

    private IEnumerator FadeNpc(float from, float to, float duration)
    {
        if (_npcSr == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetNpcAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        SetNpcAlpha(to);
    }

    private void BeginDialogue()
    {
        // Figure out which dialogue set to use based on the visit count
        int index = _visitCount - 1 % dialogueSets.Length;
        DialogueSet currentSet = dialogueSets[index];

        // Start the dialogue using our global manager!
        // When it finishes, it will automatically run the EndSequence coroutine.
        DialogueManager.Instance.StartDialogue(currentSet, () =>
        {
            StartCoroutine(EndSequence());
        });
    }

    private IEnumerator EndSequence()
    {
        if (_player != null) _player.MovementLocked = false;
        yield return StartCoroutine(NpcDepart());
        yield return new WaitForSeconds(0.4f);
        MindForestTrigger.Instance?.ReturnToHouse();
    }
}