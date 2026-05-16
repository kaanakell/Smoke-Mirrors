using UnityEngine;
using System.Collections;

public class CigaretteSequence : MonoBehaviour
{
    [Header("Dialogues")]
    [SerializeField] private DialogueSet argumentDialogue;
    [SerializeField] private DialogueSet makeupDialogue;

    [Header("Locations")]
    [SerializeField] private Transform runawayPoint;

    private PlayerController _player;

    public void PlaySequence()
    {
        if (MemoryDisplay.Instance != null)
        {
            MemoryDisplay.Instance.OnComplete += StartSequenceLogic;
        }
        else
        {
            StartSequenceLogic();
        }
    }

    private void StartSequenceLogic()
    {
        StartCoroutine(SequenceEnforcerRoutine());
    }

    private IEnumerator SequenceEnforcerRoutine()
    {
        SonNPC son = FindFirstObjectByType<SonNPC>();
        _player = FindFirstObjectByType<PlayerController>();

        if (son == null) yield break;

        if (_player != null)
        {
            _player.MovementLocked = true;
            _player.ForceFacePosition(son.transform.position);
        }

        bool sequenceFinished = false;
        son.TriggerArgumentSequence(argumentDialogue, makeupDialogue, runawayPoint, () =>
        {
            sequenceFinished = true;
        });

        yield return new WaitUntil(() => sequenceFinished);

        if (_player != null) _player.MovementLocked = false;

        if (StoryManager.Instance != null)
            StoryManager.Instance.OnMakeupEventFinished();
    }
}