using System.Collections;
using Pathfinding;
using TMPro;
using UnityEngine;

public class SonNPC : MonoBehaviour
{
    // ── Patrol ────────────────────────────────────────────────────────────────
    [Header("Patrol Waypoints")]
    [Tooltip("Empty GameObjects placed around the living room. Son walks between them.")]
    [SerializeField] private Transform[] patrolWaypoints;
    [SerializeField] private float patrolSpeed = 1.4f;
    [SerializeField] private float waypointRadius = 0.3f;

    // ── Approach ──────────────────────────────────────────────────────────────
    [Header("Approach")]
    [SerializeField] private float approachSpeed = 2.2f;
    [SerializeField] private float stopDistance = 1.2f;
    [SerializeField] private float lockRadius = 2.0f;

    // ── Return ────────────────────────────────────────────────────────────────
    [Header("Return")]
    [SerializeField] private float returnSpeed = 2.4f;

    // ── Lead To Mini Game ────────────────────────────────────────────────────
    [Header("Lead To Mini Game")]
    [Tooltip("The position the Son walks to before opening the Clock Drawing mini game.")]
    [SerializeField] private Transform miniGameWaypoint;
    [SerializeField] private float leadSpeed = 1.8f;

    // ── Dialogue UI ───────────────────────────────────────────────────────────
    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private GameObject continueHint;
    [SerializeField] private float typewriterDelay = 0.032f;
    [Tooltip("Fallback speaker label when a DialogueLine's speaker field is left blank.")]
    [SerializeField] private string defaultSpeaker = "SON";

    // ── Dialogue Sets ─────────────────────────────────────────────────────────
    [Header("Dialogue Sets")]
    [Tooltip("Plays when the Son first approaches the player in the living room.")]
    [SerializeField] private DialogueSet introDialogue;
    [Tooltip("Plays when enough items are collected, leading to the mini-game.")]
    [SerializeField] private DialogueSet itemCollectionDialogue;
    [Tooltip("Plays after the Clock Drawing mini game ends (forced or by player).")]
    [SerializeField] private DialogueSet postMiniGameDialogue;

    // ── Internals ─────────────────────────────────────────────────────────────
    private static bool _hasPlayedIntro = false;
    private AIPath _ai;
    private bool _aStarAvailable;

    private enum State { Patrol, Approach, Talk, LeadToGame, Return }
    private State _state = State.Patrol;

    private PlayerController _player;
    private int _patrolIndex = 0;
    private Vector3 _returnTarget;

    // Dialogue runtime
    private bool _dialogueActive;
    private bool _canAdvance;
    private int _lineIndex;
    private Coroutine _typeCoroutine;
    private DialogueSet _activeSet;
    private System.Action _onDialogueComplete;

    public bool IsAvailable => _state == State.Patrol;

    // =========================================================================
    // Unity lifecycle
    // =========================================================================

    private void Awake()
    {
        _ai = GetComponent<AIPath>();
        _aStarAvailable = _ai != null
                          && AstarPath.active != null
                          && AstarPath.active.graphs?.Length > 0;

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (continueHint != null) continueHint.SetActive(false);
    }

    private void OnEnable()
    {
        ClockDrawingGame.OnMiniGameCompleted += HandleMiniGameCompleted;
    }

    private void OnDisable()
    {
        ClockDrawingGame.OnMiniGameCompleted -= HandleMiniGameCompleted;
    }

    private void Start()
    {
        _player = FindFirstObjectByType<PlayerController>();

        // 1. Put the Son in the patrol state first so he registers as "Available"
        BeginPatrol();

        // 2. Check if this is the player's first time entering
        if (!_hasPlayedIntro)
        {
            // Mark it as played so it never fires again this session
            _hasPlayedIntro = true;

            // Trigger the intro immediately!
            TriggerSceneEntryIntro();
        }
    }

    private void Update()
    {
        switch (_state)
        {
            case State.Patrol: UpdatePatrol(); break;
            case State.Talk: UpdateDialogue(); break;
        }
    }

    // =========================================================================
    // Public API
    // =========================================================================

    /// <summary>
    /// Called by the living-room trigger the first time the player enters.
    /// The Son approaches, plays the intro DialogueSet, then leads the player
    /// to the Clock Drawing mini game.
    /// </summary>
    public void TriggerSceneEntryIntro()
    {
        if (!IsAvailable) return;
        StopAllCoroutines();
        StartCoroutine(ApproachThenTalk(introDialogue, () => StartCoroutine(LeadToGameRoutine())));
    }

    public void TriggerItemThresholdApproach()
    {
        if (!IsAvailable) return;
        StopAllCoroutines();
        StartCoroutine(ApproachThenTalk(itemCollectionDialogue, () => StartCoroutine(ReturnRoutine())));
    }


    // =========================================================================
    // Event handler — ClockDrawingGame.OnMiniGameCompleted
    // =========================================================================

    private void HandleMiniGameCompleted()
    {
        StopAllCoroutines();
        StartCoroutine(ApproachThenTalk(postMiniGameDialogue, () => StartCoroutine(ReturnRoutine())));
    }

    // =========================================================================
    // Patrol
    // =========================================================================

    private void BeginPatrol()
    {
        _state = State.Patrol;
        SetAISpeed(patrolSpeed, true);
        if (_aStarAvailable && patrolWaypoints?.Length > 0)
            _ai.destination = patrolWaypoints[_patrolIndex].position;
    }

    private void UpdatePatrol()
    {
        if (patrolWaypoints == null || patrolWaypoints.Length == 0) return;

        Transform target = patrolWaypoints[_patrolIndex];
        if (target == null) { AdvanceWaypoint(); return; }

        if (_aStarAvailable)
        {
            if (_ai.reachedDestination || _ai.remainingDistance < waypointRadius)
                AdvanceWaypoint();
        }
        else
        {
            transform.position = Vector3.MoveTowards(
                transform.position, target.position, patrolSpeed * Time.deltaTime);
            if (Vector2.Distance(transform.position, target.position) < waypointRadius)
                AdvanceWaypoint();
        }
    }

    private void AdvanceWaypoint()
    {
        _patrolIndex = (_patrolIndex + 1) % patrolWaypoints.Length;
        if (_aStarAvailable) _ai.destination = patrolWaypoints[_patrolIndex].position;
    }

    // =========================================================================
    // Approach → Talk pipeline
    // =========================================================================

    /// <summary>
    /// Walks toward the player, locks movement on both sides, then starts
    /// the given DialogueSet. <paramref name="onComplete"/> is invoked after
    /// the last line is dismissed.
    /// </summary>
    private IEnumerator ApproachThenTalk(DialogueSet dialogue, System.Action onComplete)
    {
        _state = State.Approach;
        PauseAI();                           // freeze NPC movement immediately

        if (_player == null)
            _player = FindFirstObjectByType<PlayerController>();

        if (_aStarAvailable) yield return StartCoroutine(AStarApproach());
        else yield return StartCoroutine(DirectApproach());

        yield return new WaitForSeconds(0.3f);
        BeginDialogue(dialogue, onComplete);
    }

    private IEnumerator AStarApproach()
    {
        SetAISpeed(approachSpeed, true);

        float timeout = 15f, elapsed = 0f;
        while (elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            if (_player == null) break;

            float dist = Vector2.Distance(transform.position, _player.transform.position);
            if (dist <= lockRadius) LockPlayer();
            if (dist <= stopDistance) break;

            _ai.destination = _player.transform.position;
            yield return null;
        }

        PauseAI();
        LockPlayer();
    }

    private IEnumerator DirectApproach()
    {
        while (true)
        {
            if (_player == null) yield break;

            float dist = Vector2.Distance(transform.position, _player.transform.position);
            if (dist <= lockRadius) LockPlayer();
            if (dist <= stopDistance) break;

            transform.position = Vector3.MoveTowards(
                transform.position, _player.transform.position, approachSpeed * Time.deltaTime);
            yield return null;
        }
        LockPlayer();
    }

    // =========================================================================
    // Dialogue
    // =========================================================================

    private void BeginDialogue(DialogueSet set, System.Action onComplete)
    {
        // Guard: empty set → skip straight to callback
        if (set == null || set.lines == null || set.lines.Length == 0)
        {
            UnlockPlayer();
            onComplete?.Invoke();
            return;
        }

        _state = State.Talk;
        _activeSet = set;
        _onDialogueComplete = onComplete;
        _lineIndex = 0;
        _dialogueActive = true;

        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        ShowLine(0);
    }

    private void ShowLine(int index)
    {
        _canAdvance = false;
        if (continueHint != null) continueHint.SetActive(false);

        DialogueLine line = _activeSet.lines[index];

        if (speakerText != null)
            speakerText.text = string.IsNullOrWhiteSpace(line.speaker) ? defaultSpeaker : line.speaker;

        if (bodyText != null)
            bodyText.text = string.Empty;

        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
        _typeCoroutine = StartCoroutine(TypewriterRoutine(line.text));
    }

    private IEnumerator TypewriterRoutine(string text)
    {
        if (text == null) text = string.Empty;
        foreach (char c in text)
        {
            if (bodyText != null) bodyText.text += c;
            yield return new WaitForSeconds(typewriterDelay);
        }
        _canAdvance = true;
        if (continueHint != null) continueHint.SetActive(true);
    }

    private void UpdateDialogue()
    {
        if (!_dialogueActive || !_canAdvance) return;

        if (Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetMouseButtonDown(0))
        {
            AdvanceDialogue();
        }
    }

    private void AdvanceDialogue()
    {
        _lineIndex++;
        if (_lineIndex >= _activeSet.lines.Length)
            FinishDialogue();
        else
            ShowLine(_lineIndex);
    }

    private void FinishDialogue()
    {
        _dialogueActive = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        UnlockPlayer();

        // Fire the chained callback (e.g. LeadToGame or Return)
        System.Action callback = _onDialogueComplete;
        _onDialogueComplete = null;
        callback?.Invoke();
    }

    // =========================================================================
    // Lead To Mini Game
    // =========================================================================

    private IEnumerator LeadToGameRoutine()
    {
        _state = State.LeadToGame;
        // Player is free to follow during this phase
        UnlockPlayer();

        if (miniGameWaypoint == null)
        {
            // No waypoint configured — open immediately where we stand
            OpenMiniGame();
            yield break;
        }

        if (_aStarAvailable)
        {
            SetAISpeed(leadSpeed, true);
            _ai.destination = miniGameWaypoint.position;

            float timeout = 20f, elapsed = 0f;
            while (!_ai.reachedDestination && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            PauseAI();
        }
        else
        {
            while (Vector3.Distance(transform.position, miniGameWaypoint.position) > waypointRadius)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, miniGameWaypoint.position, leadSpeed * Time.deltaTime);
                yield return null;
            }
        }

        OpenMiniGame();
    }

    private void OpenMiniGame()
    {
        LockPlayer();                             // lock for the mini game
        ClockDrawingGame.Instance?.OpenGame();
    }

    // =========================================================================
    // Return to patrol
    // =========================================================================

    private IEnumerator ReturnRoutine()
    {
        _state = State.Return;
        _returnTarget = (patrolWaypoints != null && patrolWaypoints.Length > 0)
            ? patrolWaypoints[_patrolIndex].position
            : transform.position;

        if (_aStarAvailable)
        {
            SetAISpeed(returnSpeed, true);
            _ai.destination = _returnTarget;

            float timeout = 10f, elapsed = 0f;
            while (!_ai.reachedDestination && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            PauseAI();
        }
        else
        {
            while (Vector3.Distance(transform.position, _returnTarget) > waypointRadius)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, _returnTarget, returnSpeed * Time.deltaTime);
                yield return null;
            }
        }

        BeginPatrol();
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private void LockPlayer()
    {
        if (_player != null && !_player.MovementLocked)
            _player.MovementLocked = true;
    }

    private void UnlockPlayer()
    {
        if (_player != null && _player.MovementLocked)
            _player.MovementLocked = false;
    }

    private void PauseAI()
    {
        if (_aStarAvailable) _ai.canMove = false;
    }

    private void SetAISpeed(float speed, bool canMove)
    {
        if (!_aStarAvailable) return;
        _ai.maxSpeed = speed;
        _ai.canMove = canMove;
    }

    // =========================================================================
    // Editor gizmos
    // =========================================================================

    private void OnDrawGizmosSelected()
    {
        if (patrolWaypoints != null)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < patrolWaypoints.Length; i++)
            {
                if (patrolWaypoints[i] == null) continue;
                Gizmos.DrawSphere(patrolWaypoints[i].position, 0.15f);
                int next = (i + 1) % patrolWaypoints.Length;
                if (patrolWaypoints[next] != null)
                    Gizmos.DrawLine(patrolWaypoints[i].position, patrolWaypoints[next].position);
            }
        }

        if (miniGameWaypoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(miniGameWaypoint.position, 0.2f);
            Gizmos.DrawWireSphere(miniGameWaypoint.position, stopDistance);
        }
    }
}