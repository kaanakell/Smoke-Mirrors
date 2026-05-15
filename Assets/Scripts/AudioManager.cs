using UnityEngine;
using System.Collections;

/// <summary>
/// Singleton audio manager. Persists across all scenes.
/// Handles music (menu/gameplay) with cross-fading and SFX playback.
/// Volume settings are saved to PlayerPrefs.
///
/// Setup: Create a GameObject in your first scene (e.g. MainMenu), add this
/// component, then assign the two AudioSource children and your two music clips.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("AudioSource used for background music.")]
    [SerializeField] private AudioSource musicSource;
    [Tooltip("AudioSource used for one-shot SFX.")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Music Clips")]
    [Tooltip("Plays on Main Menu and Credits screens.")]
    [SerializeField] private AudioClip menuMusicClip;
    [Tooltip("Plays during gameplay and forest scenes (loops).")]
    [SerializeField] private AudioClip gameplayMusicClip;

    [Header("Transition")]
    [SerializeField] private float fadeDuration = 1.5f;

    // ── Backing fields ──────────────────────────────────────────────────────
    private float _musicVolume;
    private float _sfxVolume;
    private Coroutine _fadeCoroutine;

    // ── Public volume properties (used by VolumeSettingsUI sliders) ─────────
    public float MusicVolume
    {
        get => _musicVolume;
        set
        {
            _musicVolume = Mathf.Clamp01(value);
            // Only apply directly if not in the middle of a fade
            if (_fadeCoroutine == null) musicSource.volume = _musicVolume;
            PlayerPrefs.SetFloat("MusicVolume", _musicVolume);
        }
    }

    public float SFXVolume
    {
        get => _sfxVolume;
        set
        {
            _sfxVolume = Mathf.Clamp01(value);
            sfxSource.volume = _sfxVolume;
            PlayerPrefs.SetFloat("SFXVolume", _sfxVolume);
        }
    }

    // ── Lifecycle ───────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        _sfxVolume   = PlayerPrefs.GetFloat("SFXVolume",   1f);

        musicSource.volume = _musicVolume;
        sfxSource.volume   = _sfxVolume;
        musicSource.loop   = true;
    }

    // ── Public API ──────────────────────────────────────────────────────────

    /// <summary>Plays the menu/credits music. Ignored if it's already playing.</summary>
    public void PlayMenuMusic()
    {
        if (musicSource.clip == menuMusicClip && musicSource.isPlaying) return;
        CrossFadeTo(menuMusicClip, loop: true);
    }

    /// <summary>Plays the looping gameplay music. Ignored if it's already playing.</summary>
    public void PlayGameplayMusic()
    {
        if (musicSource.clip == gameplayMusicClip && musicSource.isPlaying) return;
        CrossFadeTo(gameplayMusicClip, loop: true);
    }

    /// <summary>Fades out and stops music (e.g. before a cinematic ending).</summary>
    public void StopMusicFade(float customDuration = -1f)
    {
        float dur = customDuration > 0f ? customDuration : fadeDuration;
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeOutRoutine(dur));
    }

    /// <summary>Plays a one-shot SFX clip through the SFX source.</summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, _sfxVolume);
    }

    // ── Internal ─────────────────────────────────────────────────────────────
    private void CrossFadeTo(AudioClip newClip, bool loop)
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(CrossFadeRoutine(newClip, loop));
    }

    private IEnumerator CrossFadeRoutine(AudioClip newClip, bool loop)
    {
        float halfDur = fadeDuration * 0.5f;

        // Fade out current track
        if (musicSource.isPlaying)
        {
            float startVol = musicSource.volume;
            for (float t = 0f; t < halfDur; t += Time.unscaledDeltaTime)
            {
                musicSource.volume = Mathf.Lerp(startVol, 0f, t / halfDur);
                yield return null;
            }
        }

        musicSource.Stop();
        musicSource.clip   = newClip;
        musicSource.loop   = loop;
        musicSource.volume = 0f;
        musicSource.Play();

        // Fade in new track
        for (float t = 0f; t < halfDur; t += Time.unscaledDeltaTime)
        {
            musicSource.volume = Mathf.Lerp(0f, _musicVolume, t / halfDur);
            yield return null;
        }
        musicSource.volume = _musicVolume;
        _fadeCoroutine = null;
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        float startVol = musicSource.volume;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            musicSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
            yield return null;
        }
        musicSource.Stop();
        musicSource.volume = _musicVolume; // reset so next play starts at correct vol
        _fadeCoroutine = null;
    }
}
