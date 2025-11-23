using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

public class OptionsMenuController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Dropdown displayModeDropdown;

    [Header("Audio")]
    [SerializeField] private AudioMixer audioMixer;
    private const string VOLUME_KEY = "MasterVolume";
    private const string DISPLAYMODE_KEY = "DisplayMode";
    private const string MIXER_PARAM = "MasterVolume";

    void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat(VOLUME_KEY, 1f);
        volumeSlider.value = savedVolume;
        ApplyVolume(savedVolume);
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        int savedDisplayMode = PlayerPrefs.GetInt(DISPLAYMODE_KEY, 0);
        displayModeDropdown.value = savedDisplayMode;
        displayModeDropdown.RefreshShownValue();
        ApplyDisplayMode(savedDisplayMode);

        displayModeDropdown.onValueChanged.AddListener(OnDisplayModeChanged);
    }

    void OnVolumeChanged(float value)
    {
        ApplyVolume(value);
        PlayerPrefs.SetFloat(VOLUME_KEY, value);
    }

    void ApplyVolume(float value)
    {
        float dB = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;

        if (!audioMixer.SetFloat(MIXER_PARAM, dB))
        {
            Debug.LogWarning($"No exposed parameter named '{MIXER_PARAM}' on this AudioMixer.");
        }
    }

    void OnDisplayModeChanged(int index)
    {
        ApplyDisplayMode(index);
        PlayerPrefs.SetInt(DISPLAYMODE_KEY, index);
    }

    void ApplyDisplayMode(int index)
    {
        switch (index)
        {
            case 0:
                SetFullscreenBorderless();
                break;

            case 1:
            default:
                SetWindowed720p();
                break;
        }
    }

    void SetFullscreenBorderless()
    {
        int width  = Display.main.systemWidth;
        int height = Display.main.systemHeight;

        Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
    }

    void SetWindowed720p()
    {
        Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
    }
}