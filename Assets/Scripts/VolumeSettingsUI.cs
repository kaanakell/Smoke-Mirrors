using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to a Settings panel that contains two Sliders.
/// Works on both the Main Menu and any in-game pause menu.
///
/// Inspector wiring:
///   musicSlider  → the Slider controlling music volume  (min 0, max 1)
///   sfxSlider    → the Slider controlling SFX volume    (min 0, max 1)
/// </summary>
public class VolumeSettingsUI : MonoBehaviour
{
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private void OnEnable()
    {
        // Sync slider positions to current (possibly saved) values every time the panel opens
        if (AudioManager.Instance == null) return;

        musicSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
        sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SFXVolume);

        musicSlider.onValueChanged.AddListener(OnMusicChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXChanged);
    }

    private void OnDisable()
    {
        musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
        sfxSlider.onValueChanged.RemoveListener(OnSFXChanged);
    }

    private void OnMusicChanged(float value) => AudioManager.Instance.MusicVolume = value;
    private void OnSFXChanged(float value) => AudioManager.Instance.SFXVolume = value;
}