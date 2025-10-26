using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayAgainButton : MonoBehaviour
{
    public String firstLevelSceneName;

    private void Awake()
    {
        // Listen to click event
        GetComponent<UnityEngine.UI.Button>().onClick.AddListener(GoToNextLevel);
    }

    public void GoToNextLevel()
    {
        if (firstLevelSceneName != null)
        {
            SceneManager.LoadScene(firstLevelSceneName);
        }
        else
        {
            Debug.LogError("Next level scene is not assigned.");
        }
    }
}
