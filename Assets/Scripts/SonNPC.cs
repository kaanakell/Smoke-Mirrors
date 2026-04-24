using System.Collections;
using System.Collections.Generic;
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

    [Header("Return")]
    [SerializeField] private float returnSpeed = 2.4f;

    [Header("Dialogue")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private GameObject continueHint;
    [SerializeField] private float typewriterDelay = 0.032f;

    [Header("Approach Dialogue Lines")]
    [Tooltip("What the son says when he approaches after every 2 items. " +
             "Add as many lines as you want — all are shown in order.")]
    [SerializeField]
    private string[] approachLines = new string[]
    {
        "It's time for your mental activity, Dad.",
        "It'll only take a few minutes. Come on."
    };

    [Header("Speaker Label")]
    [SerializeField] private string sonSpeakerName = "SON";

    private AIPath _ai;
    private bool _aStarAvailable;

    private enum State { Patrol, Approach, Talk, Return }
    private State _state = State.Patrol;

    private PlayerController _player;
    private int _patrolIndex = 0;
    private Vector3 _returnTarget;
    private bool _dialogueActive;
    private bool _canAdvance;
    private int _lineIndex;
    private Coroutine _typeCoroutine;
    private List<string> _activeLines;

    private void Awake()
    {
        _ai = GetComponent<AIPath>();
        _aStarAvailable = _ai != null && AstarPath.active != null && AstarPath.active.graphs?.Length > 0;

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (continueHint != null) continueHint.SetActive(false);
    }

    private void Start()
    {
        _player = FindFirstObjectByType<PlayerController>();
        BeginPatrol();
    }

    private void Update()
    {
        switch (_state)
        {
            case State.Patrol: UpdatePatrol(); break;
            case State.Talk: UpdateDialogue(); break;
        }
    }

    public void TriggerApproach()
    {
        if (_state != State.Patrol) return;
        StopAllCoroutines();
        StartCoroutine(ApproachRoutine());
    }

    private void BeginPatrol()
    {
        _state = State.Patrol;
        if (_aStarAvailable) SetAIDestination(NextWaypoint());
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
            transform.position = Vector3.MoveTowards(transform.position, target.position, patrolSpeed * Time.deltaTime);

            if (Vector2.Distance(transform.position, target.position) < waypointRadius)
                AdvanceWaypoint();
        }
    }

    private void AdvanceWaypoint()
    {
        _patrolIndex = (_patrolIndex + 1) % patrolWaypoints.Length;
        if (_aStarAvailable) SetAIDestination(patrolWaypoints[_patrolIndex]);
    }

    private Transform NextWaypoint()
    {
        return (patrolWaypoints != null && patrolWaypoints.Length > 0) ? patrolWaypoints[_patrolIndex] : null;
    }

    private IEnumerator ApproachRoutine()
    {
        _state = State.Approach;
        PauseAI();

        if (_player == null) _player = FindFirstObjectByType<PlayerController>();

        if (_aStarAvailable)
        {
            yield return StartCoroutine(AStarApproach());
        }
        else
        {
            yield return StartCoroutine(DirectApproach());
        }

        yield return new WaitForSeconds(0.3f);
        BeginDialogue();
    }

    private IEnumerator AStarApproach()
    {
        _ai.maxSpeed = approachSpeed;
        _ai.canMove = true;

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
        _ai.canMove = false;
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

    private void BeginDialogue()
    {
        _state = State.Talk;
        _dialogueActive = true;
        _lineIndex = 0;

        _activeLines = new List<string>(approachLines);
        if (_activeLines.Count == 0)
            _activeLines.Add("It's time for your mental activity, Dad.");

        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        ShowLine(0);
    }

    private void ShowLine(int index)
    {
        _canAdvance = false;
        if (continueHint != null) continueHint.SetActive(false);

        if (speakerText != null) speakerText.text = sonSpeakerName;
        if (bodyText != null) bodyText.text = string.Empty;

        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
        _typeCoroutine = StartCoroutine(TypewriterRoutine(_activeLines[index]));
    }

    private IEnumerator TypewriterRoutine(string text)
    {
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
        if (_lineIndex >= _activeLines.Count)
        {
            FinishDialogue();
        }
        else
        {
            ShowLine(_lineIndex);
        }
    }

    private void FinishDialogue()
    {
        _dialogueActive = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        // Unlock player before returning
        if (_player != null) _player.MovementLocked = false;

        _returnTarget = patrolWaypoints != null && patrolWaypoints.Length > 0
                        ? patrolWaypoints[_patrolIndex].position
                        : transform.position;

        StartCoroutine(ReturnRoutine());
    }

    private IEnumerator ReturnRoutine()
    {
        _state = State.Return;

        if (_aStarAvailable)
        {
            _ai.maxSpeed = returnSpeed;
            _ai.canMove = true;
            _ai.destination = _returnTarget;

            float timeout = 10f, elapsed = 0f;
            while (!_ai.reachedDestination && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            _ai.canMove = false;
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

    private void LockPlayer()
    {
        if (_player != null && !_player.MovementLocked)
            _player.MovementLocked = true;
    }

    private void PauseAI()
    {
        if (_aStarAvailable) _ai.canMove = false;
    }

    private void SetAIDestination(Transform target)
    {
        if (target == null || !_aStarAvailable) return;
        _ai.maxSpeed = patrolSpeed;
        _ai.canMove = true;
        _ai.destination = target.position;
    }

    private void OnDrawGizmosSelected()
    {
        if (patrolWaypoints == null) return;
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
}
