
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RoomTransition : MonoBehaviour
{
    [Header("Destination")]
    [SerializeField] private string targetScene = "";
    [SerializeField] private string spawnPointID = "default";

    [Header("Require Interact (SPACE) to use door")]
    [SerializeField] private bool requireInteract = true;

    [Header("Transition")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.5f;

    private bool _playerInRange = false;
    private bool _transitioning = false;

    private void Update()
    {
        if (!_playerInRange || _transitioning) return;

        bool shouldTransition = !requireInteract || Input.GetKeyDown(KeyCode.Space);
        if (shouldTransition)
            StartCoroutine(DoTransition());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            _playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            _playerInRange = false;
    }

    private IEnumerator DoTransition()
    {
        _transitioning = true;

        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.MovementLocked = true;

        PlayerSpawnManager.NextSpawnID = spawnPointID;

        yield return StartCoroutine(Fade(0f, 1f));

        SceneManager.LoadScene(targetScene);
    }

    private IEnumerator Fade(float from, float to)
    {
        if (fadeImage == null) yield break;

        float elapsed = 0f;
        Color c = fadeImage.color;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, elapsed / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }
        c.a = to;
        fadeImage.color = c;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawIcon(transform.position, "d_SceneAsset Icon", true);
    }
}
