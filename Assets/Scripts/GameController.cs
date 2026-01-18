using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    [Header("Panels")]
    private GameObject startMenuPanel;
    private GameObject optionsPanel;

    [Header("Buttons")]
    private Button optionsButton;
    private Button backFromOptionsButton;

    private Button playButton;
    private Button demoLevelButton;
    private Button exitButton;
    public float TotalTime { get; private set; }
    public int CurrentLevel { get; set; }
    public bool IsPaused { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            SetupButtons();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "StartMenu")
        {
            SetupButtons();
        }
    }

    void SetupButtons()
    {
        var canvas = GameObject.Find("StartMenu");
        if (canvas == null)
        {
            Debug.LogWarning("GameController: Canvas not found in StartMenu scene.");
            return;
        }

        startMenuPanel = canvas.transform.Find("StartPanel")?.gameObject;
        optionsPanel = canvas.transform.Find("OptionsPanel")?.gameObject;

        playButton = canvas.transform.Find("StartPanel/PlayButton")?.GetComponent<Button>();
        demoLevelButton = canvas.transform.Find("StartPanel/DemoLevelButton")?.GetComponent<Button>();
        exitButton = canvas.transform.Find("StartPanel/ExitButton")?.GetComponent<Button>();
        optionsButton = canvas.transform.Find("StartPanel/OptionsButton")?.GetComponent<Button>();
        backFromOptionsButton = canvas.transform.Find("OptionsPanel/BackButton")?.GetComponent<Button>();

        if (playButton != null)
        {
            playButton.onClick.RemoveListener(StartGame);
            playButton.onClick.AddListener(StartGame);
        }

        if (demoLevelButton != null)
        {
            demoLevelButton.onClick.RemoveListener(StartDemoLevel);
            demoLevelButton.onClick.AddListener(StartDemoLevel);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(QuitGame);
        }

        if (optionsButton != null)
        {
            optionsButton.onClick.RemoveListener(ShowOptions);
            optionsButton.onClick.AddListener(ShowOptions);
        }

        if (backFromOptionsButton != null)
        {
            backFromOptionsButton.onClick.RemoveListener(HideOptions);
            backFromOptionsButton.onClick.AddListener(HideOptions);
        }

        if (startMenuPanel != null) startMenuPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);

        if (playButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(playButton.gameObject);
        }
    }

    void Update()
    {
        if (!IsPaused)
        {
            TotalTime += Time.deltaTime;
        }
    }

    public void PauseGame()
    {
        IsPaused = true;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        IsPaused = false;
        Time.timeScale = 1f;
    }

    public void ResetTimer()
    {
        TotalTime = 0f;
    }

    public void StartGame()
    {
        ResetTimer();
        CurrentLevel = 1;
        ResumeGame();

        SceneManager.LoadScene("Tutorial1");
    }


    public void RestartLevel()
    {
        ResumeGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }


    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("StartMenu");
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void ShowOptions()
    {
        startMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
        if (backFromOptionsButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(backFromOptionsButton.gameObject);
        }
    }

    void HideOptions()
    {
        optionsPanel.SetActive(false);
        startMenuPanel.SetActive(true);
        if (playButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(playButton.gameObject);
        }
    }

    private void StartDemoLevel()
    {
        ResetTimer();
        CurrentLevel = 0;
        ResumeGame();
        SceneManager.LoadScene("DemoLevel");
    }
}
