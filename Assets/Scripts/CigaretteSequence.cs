using UnityEngine;

public class CigaretteSequence : MonoBehaviour
{
    [Header("Dialogues")]
    [SerializeField] private DialogueSet argumentDialogue;
    [SerializeField] private DialogueSet makeupDialogue;

    [Header("Locations")]
    [Tooltip("Where the son runs off to sulk after the argument.")]
    [SerializeField] private Transform runawayPoint;

    public void PlaySequence()
    {
        SonNPC son = FindFirstObjectByType<SonNPC>();
        if (son != null)
        {
            son.TriggerArgumentSequence(argumentDialogue, makeupDialogue, runawayPoint, () =>
            {
                Debug.Log("[Sequence] Argument sequence concluded.");
            });
        }
    }
}