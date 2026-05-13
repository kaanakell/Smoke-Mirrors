using UnityEngine;

public class CigaretteSequence : MonoBehaviour
{
    [Header("Dialogues")]
    [SerializeField] private DialogueSet argumentDialogue;
    [SerializeField] private DialogueSet makeupDialogue;

    [Header("Locations")]
    [SerializeField] private Transform runawayPoint;

    public void PlaySequence()
    {
        SonNPC son = FindFirstObjectByType<SonNPC>();
        if (son != null)
        {
            son.TriggerArgumentSequence(argumentDialogue, makeupDialogue, runawayPoint, () =>
            {
                Debug.Log("[Sequence] Argument sequence concluded.");

                if (StoryManager.Instance != null)
                {
                    StoryManager.Instance.OnMakeupEventFinished();
                }
            });
        }
    }
}