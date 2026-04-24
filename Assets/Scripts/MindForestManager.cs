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

    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private GameObject continueHint;
    [SerializeField] private float typewriterDelay = 0.032f;

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
    private int _lineIndex;
    private bool _dialogueActive;
    private bool _canAdvance;
    private Coroutine _typeCoroutine;
    private SpriteRenderer _npcSr;
    private PlayerController _player;

    private void Start()
    {
        dialoguePanel.SetActive(false);
        if (continueHint != null) continueHint.SetActive(false);

        _npcSr = npc != null ? npc.GetComponent<SpriteRenderer>() : null;
        if (npc != null)
            npc.position = new Vector3(npcStartX, npc.position.y, npc.position.z);
        SetNpcAlpha(0f);

        var ai = npc != null ? npc.GetComponent<AIPath>() : null;
        if (ai != null) ai.canMove = false;

        _lines = BuildDialogueForThisVisit();

        StartCoroutine(ForestRoutine());
    }

    private void Update()
    {
        if (!_dialogueActive || !_canAdvance) return;
        if (Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetMouseButtonDown(0))
            AdvanceDialogue();
    }

    private List<DialogueLine> BuildDialogueForThisVisit()
    {
        int currentVisit = _visitCount;
        _visitCount++;

        if (dialogueSets != null && dialogueSets.Length > 0)
        {
            int index;
            if (loopDialogues)
                index = currentVisit % dialogueSets.Length;
            else
                index = Mathf.Min(currentVisit, dialogueSets.Length - 1);

            var set = dialogueSets[index];
            if (set != null && set.lines != null && set.lines.Length > 0)
            {
                Debug.Log($"[MindForest] Visit {currentVisit + 1}, using dialogue set [{index}]: {set.name}");
                return new List<DialogueLine>(set.lines);
            }
        }

        return new List<DialogueLine>
        {
            new DialogueLine { speaker = "FIGURE", text = "You found it again." },
            new DialogueLine { speaker = "FIGURE", text = "Keep looking. The house wants you to find it." }
        };
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
        StartCoroutine(FadeNpc(1f, 0f, npcFadeInDuration));

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
        LockPlayer();
        _dialogueActive = true;
        _lineIndex = 0;
        dialoguePanel.SetActive(true);
        ShowLine(0);
    }

    private void ShowLine(int index)
    {
        _canAdvance = false;
        if (continueHint != null) continueHint.SetActive(false);
        speakerText.text = _lines[index].speaker;
        bodyText.text = string.Empty;
        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
        _typeCoroutine = StartCoroutine(TypewriterRoutine(_lines[index].text));
    }

    private IEnumerator TypewriterRoutine(string fullText)
    {
        foreach (char c in fullText)
        {
            bodyText.text += c;
            yield return new WaitForSeconds(typewriterDelay);
        }
        _canAdvance = true;
        if (continueHint != null) continueHint.SetActive(true);
    }

    private void AdvanceDialogue()
    {
        _lineIndex++;
        if (_lineIndex >= _lines.Count)
        {
            _dialogueActive = false;
            dialoguePanel.SetActive(false);
            StartCoroutine(EndSequence());
        }
        else
        {
            ShowLine(_lineIndex);
        }
    }

    private IEnumerator EndSequence()
    {
        if (_player != null) _player.MovementLocked = false;
        yield return StartCoroutine(NpcDepart());
        yield return new WaitForSeconds(0.4f);
        MindForestTrigger.Instance?.ReturnToHouse();
    }
}