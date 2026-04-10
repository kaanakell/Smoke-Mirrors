using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Pathfinding;

public class MindForestManager : MonoBehaviour
{
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

    [Header("Dialogue Sets")]
    [SerializeField] private DialogueSet[] dialogueSets;

    // ── Internal state ────────────────────────────────────────────
    private List<DialogueLine> _lines;
    private int _lineIndex;
    private bool _dialogueActive;
    private bool _canAdvance;
    private Coroutine _typeCoroutine;
    private SpriteRenderer _npcSr;

    // Player is found lazily inside the approach coroutine — NOT in Start.
    // This avoids the build timing issue entirely.
    private PlayerController _player;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Start()
    {
        dialoguePanel.SetActive(false);
        if (continueHint) continueHint.SetActive(false);

        _npcSr = npc != null ? npc.GetComponent<SpriteRenderer>() : null;

        if (npc != null)
            npc.position = new Vector3(npcStartX, npc.position.y, npc.position.z);

        SetNpcAlpha(0f);

        var ai = npc != null ? npc.GetComponent<AIPath>() : null;
        if (ai != null) ai.canMove = false;


        // Build dialogue list
        if (dialogueSets != null && dialogueSets.Length > 0)
        {
            var set = dialogueSets[Random.Range(0, dialogueSets.Length)];
            _lines = new List<DialogueLine>(set.lines);
        }
        else
        {
            _lines = new List<DialogueLine>
            {
                new DialogueLine { speaker = "FIGURE", text = "You found it again." },
                new DialogueLine { speaker = "FIGURE", text = "Keep looking. The house wants you to find it." },
            };
        }

        StartCoroutine(ForestRoutine());
    }

    private void Update()
    {
        if (!_dialogueActive || !_canAdvance) return;

        if (Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetMouseButtonDown(0))
        {
            AdvanceDialogue();
        }
    }

    // ── Main sequence ─────────────────────────────────────────────

    private IEnumerator ForestRoutine()
    {
        // ── 1. Wait for player to be fully spawned ────────────────
        // PlayerSpawnManager does yield return null before positioning,
        // so we wait a few frames to be safe in builds.
        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(WaitForPlayer());

        if (_player == null)
        {
            Debug.LogError("[MindForestManager] Player not found after waiting. Check that Player prefab exists in MindForestScene.");
            yield break;
        }

        // ── 2. Player roams freely for the set time ───────────────
        yield return new WaitForSeconds(timeBeforeNpcApproaches);

        // ── 3. NPC fades in and walks to player ───────────────────
        StartCoroutine(FadeNpc(0f, 1f, npcFadeInDuration));
        yield return StartCoroutine(NpcApproach());

        // ── 4. Brief pause, then dialogue ────────────────────────
        yield return new WaitForSeconds(0.4f);
        BeginDialogue();
    }

    // ── Player finding ────────────────────────────────────────────

    /// <summary>
    /// Retries finding the player every frame for up to 5 seconds.
    /// Handles any execution order difference between builds and editor.
    /// </summary>
    private IEnumerator WaitForPlayer()
    {
        float timeout = 5f;
        float elapsed = 0f;

        while (_player == null && elapsed < timeout)
        {
            _player = FindFirstObjectByType<PlayerController>();
            elapsed += Time.deltaTime;
            if (_player == null) yield return null;
        }

        if (_player != null)
            Debug.Log($"[MindForestManager] Player found at {_player.transform.position}");
        else
            Debug.LogError("[MindForestManager] Could not find PlayerController in scene.");
    }

    // ── NPC approach ──────────────────────────────────────────────

    private IEnumerator NpcApproach()
    {
        if (npc == null || _player == null) yield break;


        var ai = npc.GetComponent<AIPath>();
        bool graphReady = AstarPath.active != null &&
                          AstarPath.active.graphs != null &&
                          AstarPath.active.graphs.Length > 0;

        if (ai != null && graphReady)
        {
            yield return StartCoroutine(AStarApproach(ai));
            yield break;
        }

        yield return StartCoroutine(DirectApproach());
    }


    private IEnumerator AStarApproach(AIPath ai)
    {
        ai.maxSpeed = npcApproachSpeed;
        ai.canMove  = true;

        float timeout = 20f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            float dist = Vector2.Distance(npc.position, _player.transform.position);

            if (dist <= stopDistance) break;

            ai.destination = _player.transform.position;

            if (dist <= playerLockRadius)
                LockPlayer();

            yield return null;
        }

        ai.canMove = false;
        LockPlayer();
    }


    /// <summary>
    /// Pure Vector3.MoveTowards — zero dependencies, always works.
    /// Used as fallback when A* is unavailable.
    /// </summary>
    private IEnumerator DirectApproach()
    {
        while (true)
        {
            if (_player == null) yield break;

            float dist = Vector2.Distance(npc.position, _player.transform.position);
            if (dist <= stopDistance) break;

            npc.position = Vector3.MoveTowards(
                npc.position,
                _player.transform.position,
                npcApproachSpeed * Time.deltaTime
            );

            if (dist <= playerLockRadius)
                LockPlayer();

            yield return null;
        }

        LockPlayer();
    }

    private void LockPlayer()
    {
        if (_player != null && !_player.MovementLocked)
            _player.MovementLocked = true;
    }

    // ── NPC depart ────────────────────────────────────────────────

    private IEnumerator NpcDepart()
    {
        StartCoroutine(FadeNpc(1f, 0f, npcFadeInDuration));


        var ai = npc != null ? npc.GetComponent<AIPath>() : null;
        bool graphReady = AstarPath.active != null &&
                          AstarPath.active.graphs != null &&
                          AstarPath.active.graphs.Length > 0;

        if (ai != null && graphReady)
        {
            ai.maxSpeed   = npcDepartSpeed;
            ai.canMove    = true;
            ai.destination = new Vector3(npcDepartX, npc.position.y, 0f);

            float timeout = 6f;
            float elapsed = 0f;
            while (!ai.reachedDestination && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            ai.canMove = false;
            yield break;
        }

        // Direct fallback depart
        Vector3 departTarget = new Vector3(npcDepartX, npc.position.y, npc.position.z);
        while (Vector3.Distance(npc.position, departTarget) > 0.1f)
        {
            npc.position = Vector3.MoveTowards(npc.position, departTarget, npcDepartSpeed * Time.deltaTime);
            yield return null;
        }
    }

    // ── NPC alpha ─────────────────────────────────────────────────

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

    // ── Dialogue ──────────────────────────────────────────────────

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
        if (continueHint) continueHint.SetActive(false);

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
        if (continueHint) continueHint.SetActive(true);
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

    // ── Exit ──────────────────────────────────────────────────────

    private IEnumerator EndSequence()
    {
        if (_player != null) _player.MovementLocked = false;

        yield return StartCoroutine(NpcDepart());
        yield return new WaitForSeconds(0.4f);

        MindForestTrigger.Instance?.ReturnToHouse();
    }
}