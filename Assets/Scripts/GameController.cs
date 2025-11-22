using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    private Button playButton;
    public float TotalTime { get; private set; }
    public int CurrentLevel { get; set; }
    public bool IsPaused { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SetupPlayButton();
    }

    void SetupPlayButton()
    {
        playButton = GameObject.Find("PlayButton")?.GetComponent<Button>();
        
        if (playButton != null)
        {
            playButton.onClick.AddListener(StartGame);
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
        
        if (UIController.Instance != null)
        {
            UIController.Instance.gameObject.SetActive(true);
        }
        
        SceneManager.LoadScene("Level1New");
    }
    
    public void ReturnToMainMenu()
    {
        if (UIController.Instance != null)
        {
            UIController.Instance.gameObject.SetActive(false);
        }
        
        SceneManager.LoadScene("MainMenu");
    }
}