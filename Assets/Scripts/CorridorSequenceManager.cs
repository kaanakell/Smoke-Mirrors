using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class CorridorSequenceManager : MonoBehaviour
{
    // Static variables to track the loop across scene reloads
    public static bool IsEmergencyLoopActive = false;
    public static int LoopCount = 0;

    [Header("Settings")]
    [SerializeField] private int loopsBeforeAccident = 5;

    [Header("Cinematic Elements")]
    [SerializeField] private GameObject puddleGraphic;
    [SerializeField] private Transform sonTransform;
    [SerializeField] private SpriteRenderer sonRenderer;
    [SerializeField] private Transform bathroomDoorTarget; // Where they walk to at the end

    [Header("Dialogues")]
    [SerializeField] private DialogueSet fatherShameMonologue;
    [SerializeField] private DialogueSet sonComfortDialogue;

    private PlayerController _player;

    private void Start()
    {
        if (!IsEmergencyLoopActive)
        {
            // Not an emergency. Hide all special event props.
            if (puddleGraphic != null) puddleGraphic.SetActive(false);
            if (sonTransform != null) sonTransform.gameObject.SetActive(false);
            return;
        }

        // We ARE in an emergency!
        _player = FindFirstObjectByType<PlayerController>();

        // Hide the Son and Puddle initially
        if (puddleGraphic != null) puddleGraphic.SetActive(false);
        if (sonTransform != null) sonTransform.gameObject.SetActive(true);
        if (sonRenderer != null) SetAlpha(sonRenderer, 0f);

        if (LoopCount >= loopsBeforeAccident)
        {
            // It's time for the accident sequence
            StartCoroutine(AccidentCinematic());
        }
    }

    private IEnumerator AccidentCinematic()
    {
        // 1. Let the player walk for a second to feel like they are still playing
        yield return new WaitForSeconds(1.5f);

        // 2. Lock them down
        if (_player != null) _player.MovementLocked = true;

        // 3. The Accident (Spawn puddle at player's feet)
        yield return new WaitForSeconds(0.5f);
        if (puddleGraphic != null && _player != null)
        {
            // Get the player's position
            Vector3 puddlePos = _player.transform.position;

            // Optional: If your player's pivot point is in the center of their body, 
            // you might want to uncomment the line below to lower the puddle to their feet.
            puddlePos.y -= 0.5f; 

            // Keep the puddle's original Z value so it stays behind the player
            puddlePos.z = puddleGraphic.transform.position.z;

            puddleGraphic.transform.position = puddlePos;
            puddleGraphic.SetActive(true);
        }
        // Optional: AudioSource.PlayClipAtPoint(peeSound, transform.position);
        yield return new WaitForSeconds(1.5f);

        // 4. Father's shame monologue
        bool fatherDone = false;
        DialogueManager.Instance.StartDialogue(fatherShameMonologue, () => fatherDone = true);
        yield return new WaitUntil(() => fatherDone);

        // 5. Son fades in
        yield return StartCoroutine(FadeSprite(sonRenderer, 0f, 1f, 1.5f));

        // 6. Son walks slowly to Father
        Vector3 targetPos = _player.transform.position + new Vector3(1f, 0, 0); // Stand slightly to the side
        while (Vector3.Distance(sonTransform.position, targetPos) > 0.2f)
        {
            sonTransform.position = Vector3.MoveTowards(sonTransform.position, targetPos, 1.2f * Time.deltaTime);
            yield return null;
        }

        // 7. Son comfort dialogue
        bool sonDone = false;
        DialogueManager.Instance.StartDialogue(sonComfortDialogue, () => sonDone = true);
        yield return new WaitUntil(() => sonDone);

        // 8. Cinematic walk together to the bathroom door
        while (Vector3.Distance(_player.transform.position, bathroomDoorTarget.position) > 0.5f)
        {
            // Move both of them automatically
            _player.transform.position = Vector3.MoveTowards(_player.transform.position, bathroomDoorTarget.position, 1.5f * Time.deltaTime);
            sonTransform.position = Vector3.MoveTowards(sonTransform.position, bathroomDoorTarget.position, 1.5f * Time.deltaTime);
            yield return null;
        }

        // 9. End Sequence & Transition to Living Room
        IsEmergencyLoopActive = false; // Turn off loop mode!
        LoopCount = 0;

        if (PlayerSpawnManager.Instance != null)
        {
            yield return StartCoroutine(PlayerSpawnManager.Instance.FadeOut(0.8f));
        }

        PlayerSpawnManager.NextSpawnID = "from_corridor"; // Standard spawn ID
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