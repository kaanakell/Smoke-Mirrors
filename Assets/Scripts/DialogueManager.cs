using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI Components")]
    [SerializeField] private GameObject dialoguePanel;
    [Tooltip("The Image component that holds the dialogue box graphic.")]
    [SerializeField] private Image dialogueBoxBackground;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueBodyText;
    [SerializeField] private GameObject continuePrompt;

    [Header("Dialogue Box Sprites")]
    [Tooltip("Drag the PCDialogueBox.png here")]
    [SerializeField] private Sprite pcBoxSprite;

    [Tooltip("Drag both of the Son's dialogue boxes here!")]
    [SerializeField] private Sprite[] sonBoxSprites;

    [Tooltip("Fallback box if the speaker is neither")]
    [SerializeField] private Sprite defaultBoxSprite;

    [Header("Settings")]
    [SerializeField] private float typeSpeed = 0.03f;

    private DialogueSet _currentSet;
    private int _currentLineIndex = 0;
    private bool _isTyping = false;
    private Coroutine _typeCoroutine;
    private PlayerController _player;
    private Action _onDialogueEndedCallback;
    private int _sonBoxIndex = 0;

    public bool IsDialogueActive => dialoguePanel != null && dialoguePanel.activeSelf;

    private void Awake()
    {
        // Setup Singleton
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        //DontDestroyOnLoad(gameObject);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        _player = FindFirstObjectByType<PlayerController>();
    }

    private void Update()
    {
        if (!dialoguePanel.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            if (_isTyping)
            {
                StopCoroutine(_typeCoroutine);
                dialogueBodyText.text = _currentSet.lines[_currentLineIndex].text;
                _isTyping = false;
                if (continuePrompt != null) continuePrompt.SetActive(true);
            }
            else
            {
                NextLine();
            }
        }
    }

    public void StartDialogue(DialogueSet dialogueSet, Action onComplete = null)
    {
        if (dialogueSet == null || dialogueSet.lines.Length == 0) return;

        _currentSet = dialogueSet;
        _currentLineIndex = 0;

        _onDialogueEndedCallback = onComplete;

        dialoguePanel.SetActive(true);

        if (_player != null) _player.MovementLocked = true;

        if (sonBoxSprites != null && sonBoxSprites.Length > 0)
        {
            _sonBoxIndex = (_sonBoxIndex + 1) % sonBoxSprites.Length;
        }

        ShowLine();
    }

    private void ShowLine()
    {
        DialogueLine line = _currentSet.lines[_currentLineIndex];

        string speakerLower = line.speaker.ToLower();

        if (speakerLower.Contains("son"))
        {
            if (sonBoxSprites != null && sonBoxSprites.Length > 0)
            {
                dialogueBoxBackground.sprite = sonBoxSprites[_sonBoxIndex];
            }
        }
        else if (speakerLower.Contains("father") || speakerLower.Contains("you") || speakerLower.Contains("pc"))
        {
            dialogueBoxBackground.sprite = pcBoxSprite;
        }
        else
        {
            dialogueBoxBackground.sprite = defaultBoxSprite != null ? defaultBoxSprite : pcBoxSprite;
        }

        speakerNameText.text = line.speaker;
        dialogueBodyText.text = "";

        if (continuePrompt != null) continuePrompt.SetActive(false);

        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
        _typeCoroutine = StartCoroutine(TypeRoutine(line.text));
    }

    private IEnumerator TypeRoutine(string text)
    {
        _isTyping = true;
        foreach (char c in text.ToCharArray())
        {
            dialogueBodyText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
        _isTyping = false;
        if (continuePrompt != null) continuePrompt.SetActive(true);
    }

    private void NextLine()
    {
        _currentLineIndex++;
        if (_currentLineIndex < _currentSet.lines.Length)
        {
            ShowLine();
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        if (_player != null) _player.MovementLocked = false;
        _currentSet = null;

        _onDialogueEndedCallback?.Invoke();
        _onDialogueEndedCallback = null;
    }
}