using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.EventSystems;

public class PauseMenuController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button backFromOptionsButton;

    [Header("Audio")]
    [SerializeField] private AudioMixer audioMixer;

    private const string VOLUME_KEY   = "MasterVolume";
    private const string MIXER_PARAM  = "MasterVolume";

    void Start()
    {
        pausePanel.SetActive(true);
        optionsPanel.SetActive(false);

        restartButton.onClick.AddListener(OnRestartClicked);
        mainMenuButton.onClick.AddListener(OnExitClicked);
        optionsButton.onClick.AddListener(ShowOptions);
        backFromOptionsButton.onClick.AddListener(HideOptions);

        float savedVolume = PlayerPrefs.GetFloat(VOLUME_KEY, 1f);
        volumeSlider.value = savedVolume;
        SetMixerVolume(savedVolume);

        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        EventSystem.current.SetSelectedGameObject(optionsButton.gameObject);
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
        GameController.Instance.RestartLevel();
    }

    public void OnExitClicked()
    {
        UIController.Instance.HidePauseMenu();
        GameController.Instance.ReturnToMainMenu();
    }

    void ShowOptions()
    {
        pausePanel.SetActive(false);
        optionsPanel.SetActive(true);
        EventSystem.current.SetSelectedGameObject(backFromOptionsButton.gameObject);
    }

    void HideOptions()
    {
        optionsPanel.SetActive(false);
        pausePanel.SetActive(true);
        EventSystem.current.SetSelectedGameObject(optionsButton.gameObject);
    }

}