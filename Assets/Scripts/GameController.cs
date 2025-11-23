using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject startMenuPanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Buttons")]
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button backFromOptionsButton;

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
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SetupButtons();
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
        playButton = GameObject.Find("PlayButton")?.GetComponent<Button>();

        if (playButton != null)
        {
            playButton.onClick.RemoveListener(StartGame);
            playButton.onClick.AddListener(StartGame);
        }

        demoLevelButton = GameObject.Find("DemoLevelButton")?.GetComponent<Button>();
        if (demoLevelButton != null)
        {
            demoLevelButton.onClick.RemoveAllListeners();
            demoLevelButton.onClick.AddListener(() =>
            {
                ResetTimer();
                CurrentLevel = 0;
                ResumeGame();
                SceneManager.LoadScene("DemoLevel");
            });
        }

        exitButton = GameObject.Find("ExitButton")?.GetComponent<Button>();
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(QuitGame);
        }

        startMenuPanel.SetActive(true);
        optionsPanel.SetActive(false);

        optionsButton.onClick.AddListener(ShowOptions);
        backFromOptionsButton.onClick.AddListener(HideOptions);
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

        SceneManager.LoadScene("Level1New");
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
    }

    void HideOptions()
    {
        optionsPanel.SetActive(false);
        startMenuPanel.SetActive(true);
    }
}
