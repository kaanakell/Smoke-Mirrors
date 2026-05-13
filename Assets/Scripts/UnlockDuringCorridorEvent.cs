using UnityEngine;

[RequireComponent(typeof(RoomTransition))]
public class UnlockDuringCorridorEvent : MonoBehaviour
{
    private void Start()
    {
        if (CorridorSequenceManager.IsEmergencyLoopActive)
        {
            GetComponent<RoomTransition>().SetLocked(false);
            Debug.Log("[Corridor] Exit door unlocked for emergency loop.");
        }
    }
}