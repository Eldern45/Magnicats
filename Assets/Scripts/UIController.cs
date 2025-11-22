using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }
    
    [SerializeField] private GameObject pauseMenuPrefab;
    [SerializeField] private GameObject hudPrefab;
    
    private GameObject pauseMenuInstance;
    private GameObject hudInstance;
    
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
            if (hudInstance != null)
            {
                Destroy(hudInstance);
                hudInstance = null;
            }
            
            if (pauseMenuInstance != null)
            {
                Destroy(pauseMenuInstance);
                pauseMenuInstance = null;
            }
        }
        else
        {
            SpawnHUD();
        }
    }
    
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (SceneManager.GetActiveScene().name != "StartMenu")
            {
                TogglePauseMenu();
            }
        }
    }
    
    public void SpawnHUD()
    {
        if (hudInstance == null && hudPrefab != null)
        {
            hudInstance = Instantiate(hudPrefab);
            DontDestroyOnLoad(hudInstance);
        }
    }
    
    public void TogglePauseMenu()
    {
        if (pauseMenuInstance == null)
        {
            ShowPauseMenu();
        }
        else
        {
            HidePauseMenu();
        }
    }
    
    public void ShowPauseMenu()
    {
        if (pauseMenuPrefab != null && GameController.Instance != null)
        {
            pauseMenuInstance = Instantiate(pauseMenuPrefab);
            GameController.Instance.PauseGame();
        }
    }
    
    public void HidePauseMenu()
    {
        if (pauseMenuInstance != null)
        {
            Destroy(pauseMenuInstance);
            pauseMenuInstance = null;
            
            if (GameController.Instance != null)
            {
                GameController.Instance.ResumeGame();
            }
        }
    }
}