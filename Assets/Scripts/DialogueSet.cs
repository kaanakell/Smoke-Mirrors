using UnityEngine;

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