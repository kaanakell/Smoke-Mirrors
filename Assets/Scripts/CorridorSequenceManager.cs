using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class CorridorSequenceManager : MonoBehaviour
{
    public static bool IsEmergencyLoopActive = false;
    public static int LoopCount = 0;

    [Header("Settings")]
    [SerializeField] private int loopsBeforeAccident = 5;

    [Header("Cinematic Elements")]
    [SerializeField] private GameObject puddleGraphic;
    [SerializeField] private Transform sonTransform;
    [SerializeField] private SpriteRenderer sonRenderer;
    [SerializeField] private Transform bathroomDoorTarget;

    [Header("Dialogues")]
    [SerializeField] private DialogueSet fatherShameMonologue;
    [SerializeField] private DialogueSet sonComfortDialogue;

    private PlayerController _player;

    private void Start()
    {
        if (!IsEmergencyLoopActive)
        {
            if (puddleGraphic != null) puddleGraphic.SetActive(false);
            if (sonTransform != null) sonTransform.gameObject.SetActive(false);
            return;
        }

        _player = FindFirstObjectByType<PlayerController>();

        if (puddleGraphic != null) puddleGraphic.SetActive(false);
        if (sonTransform != null) sonTransform.gameObject.SetActive(true);
        if (sonRenderer != null) SetAlpha(sonRenderer, 0f);

        if (LoopCount >= loopsBeforeAccident)
        {
            StartCoroutine(AccidentCinematic());
        }
    }

    private IEnumerator AccidentCinematic()
    {
        yield return new WaitForSeconds(1.5f);

        if (_player != null) _player.MovementLocked = true;

        yield return new WaitForSeconds(0.5f);
        if (puddleGraphic != null && _player != null)
        {
            Vector3 puddlePos = _player.transform.position;

            puddlePos.y -= 0.5f; 

            puddlePos.z = puddleGraphic.transform.position.z;

            puddleGraphic.transform.position = puddlePos;
            puddleGraphic.SetActive(true);
            if (_player != null) _player.MovementLocked = true;
        }
        yield return new WaitForSeconds(1.5f);

        bool fatherDone = false;
        DialogueManager.Instance.StartDialogue(fatherShameMonologue, () => fatherDone = true);
        yield return new WaitUntil(() => fatherDone);

        yield return StartCoroutine(FadeSprite(sonRenderer, 0f, 1f, 1.5f));

        Vector3 targetPos = _player.transform.position + new Vector3(1f, 0, 0);
        while (Vector3.Distance(sonTransform.position, targetPos) > 0.2f)
        {
            sonTransform.position = Vector3.MoveTowards(sonTransform.position, targetPos, 1.2f * Time.deltaTime);
            if (_player != null) _player.MovementLocked = true;
            yield return null;
        }

        bool sonDone = false;
        DialogueManager.Instance.StartDialogue(sonComfortDialogue, () => sonDone = true);
        yield return new WaitUntil(() => sonDone);

        while (Vector3.Distance(_player.transform.position, bathroomDoorTarget.position) > 0.5f)
        {
            _player.transform.position = Vector3.MoveTowards(_player.transform.position, bathroomDoorTarget.position, 1.5f * Time.deltaTime);
            sonTransform.position = Vector3.MoveTowards(sonTransform.position, bathroomDoorTarget.position, 1.5f * Time.deltaTime);
            yield return null;
        }

        IsEmergencyLoopActive = false;
        LoopCount = 0;

        if (PlayerSpawnManager.Instance != null)
        {
            yield return StartCoroutine(PlayerSpawnManager.Instance.FadeOut(0.8f));
        }

        PlayerSpawnManager.NextSpawnID = "from_corridor";
        SceneManager.LoadScene("LivingRoom");
    }

    private void SetAlpha(SpriteRenderer sr, float a)
    {
        if (sr == null) return;
        Color c = sr.color; c.a = a; sr.color = c;
    }

    private IEnumerator FadeSprite(SpriteRenderer sr, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(sr, Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        SetAlpha(sr, to);
    }
}