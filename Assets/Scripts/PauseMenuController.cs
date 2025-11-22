using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class PauseMenuController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Slider volumeSlider;

    [Header("Audio")]
    [SerializeField] private AudioMixer audioMixer;

    private const string VOLUME_KEY   = "MasterVolume";
    private const string MIXER_PARAM  = "MasterVolume";

    void Start()
    {
        restartButton.onClick.AddListener(OnRestartClicked);
        mainMenuButton.onClick.AddListener(OnExitClicked);

        float savedVolume = PlayerPrefs.GetFloat(VOLUME_KEY, 1f);
        volumeSlider.value = savedVolume;
        SetMixerVolume(savedVolume);

        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    void SetMixerVolume(float value)
    {
        float dB = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;

        if (!audioMixer.SetFloat(MIXER_PARAM, dB))
        {
            Debug.LogWarning($"No exposed parameter named '{MIXER_PARAM}' on this AudioMixer.");
        }
    }

    void OnVolumeChanged(float value)
    {
        SetMixerVolume(value);
        PlayerPrefs.SetFloat(VOLUME_KEY, value);
    }

    public void OnRestartClicked()
    {
        UIController.Instance.HidePauseMenu();
        GameController.Instance.StartGame();
    }

    public void OnExitClicked()
    {
        UIController.Instance.HidePauseMenu();
        GameController.Instance.ReturnToMainMenu();
    }
}