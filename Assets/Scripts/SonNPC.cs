using System.Collections;
using Pathfinding;
using TMPro;
using UnityEngine;

public class SonNPC : MonoBehaviour
{
    [Header("Patrol Waypoints")]
    [Tooltip("Empty GameObjects placed around the living room. Son walks between them.")]
    [SerializeField] private Transform[] patrolWaypoints;
    [SerializeField] private float patrolSpeed = 1.4f;
    [SerializeField] private float waypointRadius = 0.3f;

    [Header("Approach")]
    [SerializeField] private float approachSpeed = 2.2f;
    [SerializeField] private float stopDistance = 1.2f;
    [SerializeField] private float lockRadius = 2.0f;
    [SerializeField] private float makeupRadius = 2.5f;

    [Header("Return")]
    [SerializeField] private float returnSpeed = 2.4f;

    [Header("Lead To Mini Game")]
    [Tooltip("Waypoints for each game in order: Element 0 = Clock, Element 1 = Memory, Element 2 = Puzzle")]
    [SerializeField] private Transform[] miniGameWaypoints;
    [SerializeField] private float leadSpeed = 1.8f;

    [Header("Dialogue Sets")]
    [Tooltip("Plays when the Son first approaches the player in the living room.")]
    [SerializeField] private DialogueSet introDialogue;
    [Tooltip("Element 0 = Clock Game, Element 1 = Memory Game, Element 2 = Puzzle Game")]
    [SerializeField] private DialogueSet[] leadToGameDialogues;
    [Tooltip("Plays after the Clock Drawing mini game ends (forced or by player).")]
    [SerializeField] private DialogueSet postMiniGameDialogue;

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
        BeginPatrol();

        if (!_hasPlayedIntro)
        {
            _hasPlayedIntro = true;
            StartCoroutine(IntroAfterDelay());
        }
    }

    private IEnumerator IntroAfterDelay()
    {
        yield return null;
        if (_player == null) _player = FindFirstObjectByType<PlayerController>();
        StopAllCoroutines();
        StartCoroutine(ApproachThenTalk(introDialogue, () => StartCoroutine(LeadToGameRoutine())));
    }

    private void Update()
    {
        switch (_state)
        {
            case State.Patrol: UpdatePatrol(); break;
            case State.Talk: break;
            case State.SpecialSequence: break;
        }
    }

    public void TriggerSceneEntryIntro()
    {
        if (!IsAvailable) return;
        StopAllCoroutines();
        StartCoroutine(ApproachThenTalk(introDialogue, () => StartCoroutine(LeadToGameRoutine())));
    }

    public void TriggerCustomApproach(DialogueSet customDialogue, System.Action onComplete)
    {
        if (!IsAvailable) return;

        StopAllCoroutines();
        StartCoroutine(ApproachThenTalk(customDialogue, () =>
        {
            onComplete?.Invoke();

            StartCoroutine(ReturnRoutine());
        }));
    }

    public void TriggerItemThresholdApproach(int gameIndex)
    {
        if (!IsAvailable) return;

        _currentGameIndex = gameIndex;

        DialogueSet selectedDialogue = null;

        if (gameIndex > 0 && leadToGameDialogues != null)
        {
            int arrayIndex = gameIndex - 1;
            if (arrayIndex < leadToGameDialogues.Length)
            {
                selectedDialogue = leadToGameDialogues[arrayIndex];
            }
        }

        StopAllCoroutines();

        if (selectedDialogue != null)
        {
            StartCoroutine(ApproachThenTalk(selectedDialogue, () => StartCoroutine(LeadToGameRoutine())));
        }
        else
        {
            StartCoroutine(LeadToGameRoutine());
        }
    }

    private void HandleMiniGameCompleted()
    {
        StopAllCoroutines();
        StartCoroutine(ApproachThenTalk(postMiniGameDialogue, () => StartCoroutine(ReturnRoutine())));
    }

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

    private IEnumerator ApproachThenTalk(DialogueSet dialogue, System.Action onComplete)
    {
        _state = State.Approach;
        PauseAI();

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

    private void BeginDialogue(DialogueSet set, System.Action onComplete)
    {
        if (set == null || set.lines == null || set.lines.Length == 0)
        {
            UnlockPlayer();
            onComplete?.Invoke();
            return;
        }

        _state = State.Talk;

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

    private IEnumerator LeadToGameRoutine()
    {
        Transform targetWaypoint = transform;
        if (miniGameWaypoints != null && _currentGameIndex < miniGameWaypoints.Length)
        {
            targetWaypoint = miniGameWaypoints[_currentGameIndex];
        }

        _state = State.LeadToGame;

        UnlockPlayer();

        if (miniGameWaypoints == null || miniGameWaypoints.Length == 0)
        {
            OpenSpecificGameBasedOnIndex();
            yield break;
        }

        if (_aStarAvailable)
        {
            SetAISpeed(leadSpeed, true);

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
            while (Vector3.Distance(transform.position, targetWaypoint.position) > waypointRadius)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, targetWaypoint.position, leadSpeed * Time.deltaTime);
                yield return null;
            }
        }
    }

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
            Debug.LogWarning("[SonNPC] No game matched the current index!");
        }
    }

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

        if (_aStarAvailable) yield return StartCoroutine(AStarApproach());
        else yield return StartCoroutine(DirectApproach());

        bool argDone = false;
        BeginDialogue(argDialogue, () => argDone = true);
        yield return new WaitUntil(() => argDone);

        LockPlayer();

        Vector3 dirTowardSon = (transform.position - _player.transform.position).normalized;
        dirTowardSon.z = 0;
        if (dirTowardSon == Vector3.zero) dirTowardSon = Vector3.right;

        Vector3 playerStart = _player.transform.position;
        Vector3 playerBumpTarget = playerStart + dirTowardSon * 0.3f;

        Vector3 sonStart = transform.position;
        Vector3 sonBumpTarget = sonStart + dirTowardSon * 0.5f;

        if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.2f, 0.15f);

        float t = 0;
        float bumpDuration = 0.35f;
        while (t < bumpDuration)
        {
            t += Time.deltaTime;
            float percent = t / bumpDuration;

            float ease = 1f - Mathf.Pow(1f - percent, 3f);

            _player.transform.position = Vector3.Lerp(playerStart, playerBumpTarget, ease);
            transform.position = Vector3.Lerp(sonStart, sonBumpTarget, ease);
            yield return null;
        }

        if (_aStarAvailable)
        {
            SetAISpeed(returnSpeed * 1.5f, true);
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

        UnlockPlayer();

        while (Vector3.Distance(transform.position, _player.transform.position) > makeupRadius)
        {
            yield return null;
        }

        LockPlayer();

        bool makeupDone = false;
        BeginDialogue(makeDialogue, () => makeupDone = true);
        yield return new WaitUntil(() => makeupDone);

        UnlockPlayer();
        _state = State.Patrol;
        onComplete?.Invoke();
    }

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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _hasPlayedIntro = false;
    }
}