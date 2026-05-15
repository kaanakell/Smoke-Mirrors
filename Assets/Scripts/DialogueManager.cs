using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI Components")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image dialogueBoxBackground;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueBodyText;
    [SerializeField] private GameObject continuePrompt;

    [Header("Dialogue Box Sprites")]
    [SerializeField] private Sprite pcBoxSprite;
    [SerializeField] private Sprite[] sonBoxSprites;
    [SerializeField] private Sprite defaultBoxSprite;

    [Header("Settings")]
    [SerializeField] private float typeSpeed = 0.03f;
    [SerializeField] private float glitchRefreshRate = 0.08f;

    private DialogueSet _currentSet;
    private int _currentLineIndex = 0;
    private bool _isTyping = false;
    private Coroutine _typeCoroutine;
    private PlayerController _player;
    private Action _onDialogueEndedCallback;

    private string _currentRawSpeaker = "";
    private string _currentRawBody = "";
    private int _visibleCharCount;
    private float _glitchTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    private void Update()
    {
        if (!IsDialogueActive) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            HandleInteraction();
        }

        _glitchTimer += Time.deltaTime;
        if (_glitchTimer >= glitchRefreshRate)
        {
            _glitchTimer = 0;
            RefreshDialogueUI();
        }
    }

    private void HandleInteraction()
    {
        if (_isTyping)
        {
            string cleanText = Regex.Replace(_currentRawBody, @"<glitch>(.*?)</glitch>", "$1");
            _visibleCharCount = cleanText.Length;
            _isTyping = false;
            if (continuePrompt != null) continuePrompt.SetActive(true);
        }
        else
        {
            NextLine();
        }
    }

    private void RefreshDialogueUI()
    {
        speakerNameText.text = ProcessGlitchTags(_currentRawSpeaker);

        string fullyScrambledBody = ProcessGlitchTags(_currentRawBody);

        if (!string.IsNullOrEmpty(fullyScrambledBody))
        {
            int lengthToShow = Mathf.Min(_visibleCharCount, fullyScrambledBody.Length);
            dialogueBodyText.text = fullyScrambledBody.Substring(0, lengthToShow);
        }
    }

    public void StartDialogue(DialogueSet set, Action onEnded = null)
    {
        _currentSet = set;
        _onDialogueEndedCallback = onEnded;
        _currentLineIndex = 0;

        if (dialoguePanel != null) dialoguePanel.SetActive(true);

        if (_player == null) _player = FindFirstObjectByType<PlayerController>();
        if (_player != null) _player.MovementLocked = true;

        ShowLine();
    }

    private void ShowLine()
    {
        if (_currentSet == null || _currentLineIndex >= _currentSet.lines.Length) return;

        DialogueLine line = _currentSet.lines[_currentLineIndex];

        _currentRawSpeaker = line.speaker ?? "";
        _currentRawBody = line.text ?? "";
        _visibleCharCount = 0;

        UpdateDialogueBoxSprite(_currentRawSpeaker);

        if (continuePrompt != null) continuePrompt.SetActive(false);
        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);

        _typeCoroutine = StartCoroutine(TypeRoutine());
    }

    private IEnumerator TypeRoutine()
    {
        _isTyping = true;

        string cleanText = Regex.Replace(_currentRawBody, @"<glitch>(.*?)</glitch>", "$1");
        int targetLength = cleanText.Length;

        while (_visibleCharCount < targetLength)
        {
            if (!_isTyping) break;

            _visibleCharCount++;
            yield return new WaitForSeconds(typeSpeed);
        }

        _isTyping = false;
        if (continuePrompt != null) continuePrompt.SetActive(true);
    }

    private void UpdateDialogueBoxSprite(string speaker)
    {
        string speakerLower = speaker.ToLower();
        if (speakerLower.Contains("son"))
        {
            int index = Mathf.Clamp(_currentLineIndex % 2, 0, sonBoxSprites.Length - 1);
            dialogueBoxBackground.sprite = sonBoxSprites[index];
        }
        else if (speakerLower.Contains("father") || speakerLower.Contains("you") || speakerLower.Contains("pc"))
        {
            dialogueBoxBackground.sprite = pcBoxSprite;
        }
        else
        {
            dialogueBoxBackground.sprite = defaultBoxSprite != null ? defaultBoxSprite : pcBoxSprite;
        }
    }

    private string ProcessGlitchTags(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return Regex.Replace(text, @"<glitch>(.*?)</glitch>", match =>
        {
            return GenerateGibberish(match.Groups[1].Value.Length);
        });
    }

    private string GenerateGibberish(int length)
    {
        string chars = "!@#$%^&*()_+-=X{}[]\\|;:<>/?";
        char[] scrambled = new char[length];
        for (int i = 0; i < length; i++)
            scrambled[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
        return new string(scrambled);
    }

    private void NextLine()
    {
        _currentLineIndex++;
        if (_currentLineIndex < _currentSet.lines.Length)
        {
            ShowLine();
            if (_player != null) _player.MovementLocked = true;
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        dialogueBodyText.text = "";
        speakerNameText.text = "";
        if(continuePrompt != null)
            continuePrompt.SetActive(false);

        _currentSet = null;
        _currentRawSpeaker = "";
        _currentRawBody = "";

        StartCoroutine(CloseDialogueNextFrame());
    }

    private IEnumerator CloseDialogueNextFrame()
    {
        yield return null;

        if(dialoguePanel != null)
            dialoguePanel.SetActive(false);
        if(_player != null)
            _player.MovementLocked = false;

        _onDialogueEndedCallback?.Invoke();
        _onDialogueEndedCallback = null;
    }

    public bool IsDialogueActive => dialoguePanel != null && dialoguePanel.activeSelf;
}