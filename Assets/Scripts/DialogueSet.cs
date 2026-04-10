using UnityEngine;

// Keep this file at: Assets/Scripts/DialogueSet.cs
// Do NOT wrap these in a namespace — the ScriptableObject assets
// were created without one and Unity matches by class name + namespace.

[System.Serializable]
public class DialogueLine
{
    public string speaker;
    [TextArea(1, 4)]
    public string text;
}

[CreateAssetMenu(menuName = "Smoke And Mirrors/Dialogue Set", fileName = "DialogueSet_")]
public class DialogueSet : ScriptableObject
{
    public DialogueLine[] lines;
}