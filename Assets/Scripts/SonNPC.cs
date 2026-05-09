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
    [SerializeField] private float makeupRadius = 2.5f; // How close father must be to apologize

    // ── Return ────────────────────────────────────────────────────────────────
    [Header("Return")]
    [SerializeField] private float returnSpeed = 2.4f;

    // ── Lead To Mini Game ────────────────────────────────────────────────────
    [Header("Lead To Mini Game")]
    [Tooltip("Waypoints for each game in order: Element 0 = Clock, Element 1 = Memory, Element 2 = Puzzle")]
    [SerializeField] private Transform[] miniGameWaypoints;
    [SerializeField] private float leadSpeed = 1.8f;

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

    private enum State { Patrol, Approach, Talk, LeadToGame, Return, SpecialSequence }
    private State _state = State.Patrol;

    private PlayerController _player;
    private int _patrolIndex = 0;
    private Vector3 _returnTarget;
    private int _currentGameIndex = 0;

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
            case State.Talk: break;
            case State.SpecialSequence: break; // Do nothing, coroutine handles it
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

    /// <summary>
    /// Allows external scripts (like a Trash Item) to call the Son over.
    /// He will approach, play the provided dialogue, fire the callback, and return to patrol.
    /// </summary>
    public void TriggerCustomApproach(DialogueSet customDialogue, System.Action onComplete)
    {
        if (!IsAvailable) return;

        StopAllCoroutines();
        StartCoroutine(ApproachThenTalk(customDialogue, () =>
        {
            // Trigger whatever the item wants to do (like removing it from inventory)
            onComplete?.Invoke();

            // Go back to walking around
            StartCoroutine(ReturnRoutine());
        }));
    }

    public void TriggerItemThresholdApproach(int gameIndex)
    {
        if (!IsAvailable) return;

        _currentGameIndex = gameIndex; // Store the current game stage

        StopAllCoroutines();
        StartCoroutine(ApproachThenTalk(itemCollectionDialogue, () => StartCoroutine(LeadToGameRoutine())));
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

        _state = State.Talk; // Mark the Son as busy talking

        // Call the new global manager
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(set, () =>
            {
                UnlockPlayer();
                onComplete?.Invoke();
            });
        }
        else
        {
            Debug.LogWarning("[SonNPC] DialogueManager missing! Skipping dialogue.");
            UnlockPlayer();
            onComplete?.Invoke();
        }
    }

    // =========================================================================
    // Lead To Mini Game
    // =========================================================================

    private IEnumerator LeadToGameRoutine()
    {
        // 1. Get the correct destination for this stage
        Transform targetWaypoint = transform;
        if (miniGameWaypoints != null && _currentGameIndex < miniGameWaypoints.Length)
        {
            // Here is where we pick the specific "car" out of the "parking lot"
            targetWaypoint = miniGameWaypoints[_currentGameIndex];
        }

        _state = State.LeadToGame;

        // Player is free to follow during this phase
        UnlockPlayer();

        // 2. Check if we have valid waypoints to walk to
        if (miniGameWaypoints == null || miniGameWaypoints.Length == 0)
        {
            // Fallback: Open immediately where we stand
            OpenSpecificGameBasedOnIndex();
            yield break;
        }

        // 3. Walk to the chosen waypoint using targetWaypoint.position
        if (_aStarAvailable)
        {
            SetAISpeed(leadSpeed, true);

            // FIXED: Using targetWaypoint.position
            _ai.destination = targetWaypoint.position;

            float timeout = 20f, elapsed = 0f;
            while (!_ai.reachedDestination && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            PauseAI();
            _state = State.Patrol;
            if (_state == State.Patrol)
            {
                StartCoroutine(ReturnRoutine());
            }
        }
        else
        {
            // FIXED: Using targetWaypoint.position in the fallback movement too
            while (Vector3.Distance(transform.position, targetWaypoint.position) > waypointRadius)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, targetWaypoint.position, leadSpeed * Time.deltaTime);
                yield return null;
            }
        }

        // 4. Finally, open the specific mini-game after arriving
        //OpenSpecificGameBasedOnIndex();
    }

    /// <summary>
    /// Helper method to keep the coroutine clean. Opens the right game based on the stage.
    /// </summary>
    private void OpenSpecificGameBasedOnIndex()
    {
        if (_currentGameIndex == 0 && ClockDrawingGame.Instance != null)
        {
            ClockDrawingGame.Instance.OpenGame(null);
        }
        else if (_currentGameIndex == 1 && MemoryMatchGame.Instance != null)
        {
            MemoryMatchGame.Instance.OpenGame();
        }
        else if (_currentGameIndex == 2 && PuzzleGame.Instance != null)
        {
            PuzzleGame.Instance.OpenGame(null);
        }
        else
        {
            // Fallback in case OpenMiniGame() is still needed
            // OpenMiniGame(); 
            Debug.LogWarning("[SonNPC] No game matched the current index!");
        }
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
    // Special Argument Sequence
    // =========================================================================

    public void TriggerArgumentSequence(DialogueSet argDialogue, DialogueSet makeupDialogue, Transform runawayPoint, System.Action onComplete)
    {
        if (!IsAvailable) return;
        StopAllCoroutines();
        StartCoroutine(ArgumentRoutine(argDialogue, makeupDialogue, runawayPoint, onComplete));
    }

    private IEnumerator ArgumentRoutine(DialogueSet argDialogue, DialogueSet makeDialogue, Transform runawayPoint, System.Action onComplete)
    {
        _state = State.SpecialSequence;
        PauseAI();
        if (_player == null) _player = FindFirstObjectByType<PlayerController>();

        // 1. Walk to player
        if (_aStarAvailable) yield return StartCoroutine(AStarApproach());
        else yield return StartCoroutine(DirectApproach());

        // 2. Play argument dialogue
        bool argDone = false;
        BeginDialogue(argDialogue, () => argDone = true);
        yield return new WaitUntil(() => argDone);

        // 3. THE POLISHED BUMP
        LockPlayer();

        // Calculate the direction from the Father to the Son
        Vector3 dirTowardSon = (transform.position - _player.transform.position).normalized;
        dirTowardSon.z = 0;
        if (dirTowardSon == Vector3.zero) dirTowardSon = Vector3.right;

        // Set up the subtle movement targets
        Vector3 playerStart = _player.transform.position;
        Vector3 playerBumpTarget = playerStart + dirTowardSon * 0.3f; // Father lunges slightly

        Vector3 sonStart = transform.position;
        Vector3 sonBumpTarget = sonStart + dirTowardSon * 0.5f; // Son stumbles back slightly

        // Trigger Camera Shake!
        if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.2f, 0.15f);

        float t = 0;
        float bumpDuration = 0.35f;
        while (t < bumpDuration)
        {
            t += Time.deltaTime;
            float percent = t / bumpDuration;

            // Cubic Ease-Out: makes the animation start fast and slow down naturally
            float ease = 1f - Mathf.Pow(1f - percent, 3f);

            _player.transform.position = Vector3.Lerp(playerStart, playerBumpTarget, ease);
            transform.position = Vector3.Lerp(sonStart, sonBumpTarget, ease);
            yield return null;
        }

        // 4. Run away
        if (_aStarAvailable)
        {
            SetAISpeed(returnSpeed * 1.5f, true); // Run away
            _ai.destination = runawayPoint.position;
            while (!_ai.reachedDestination) yield return null;
            PauseAI();
        }
        else
        {
            while (Vector3.Distance(transform.position, runawayPoint.position) > waypointRadius)
            {
                transform.position = Vector3.MoveTowards(transform.position, runawayPoint.position, (returnSpeed * 1.5f) * Time.deltaTime);
                yield return null;
            }
        }

        // 5. Sulk and wait for player to enter the "Trigger" radius
        UnlockPlayer();

        // This loop acts exactly like a physical Trigger Collider!
        while (Vector3.Distance(transform.position, _player.transform.position) > makeupRadius)
        {
            yield return null;
        }

        LockPlayer();

        // 6. Play makeup dialogue
        bool makeupDone = false;
        BeginDialogue(makeDialogue, () => makeupDone = true);
        yield return new WaitUntil(() => makeupDone);

        // 7. Finish
        UnlockPlayer();
        onComplete?.Invoke();
        StartCoroutine(ReturnRoutine());
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
        Transform targetWaypoint = transform;
        if (miniGameWaypoints != null && _currentGameIndex < miniGameWaypoints.Length)
        {
            targetWaypoint = miniGameWaypoints[_currentGameIndex];
        }
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

        if (miniGameWaypoints != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(targetWaypoint.position, 0.2f);
            Gizmos.DrawWireSphere(targetWaypoint.position, stopDistance);
        }
    }
}