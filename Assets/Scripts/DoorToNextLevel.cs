using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorToNextLevel : MonoBehaviour
{
    public String nextLevelSceneName;

    public void GoToNextLevel()
    {
        if (nextLevelSceneName != null)
        {
            SceneManager.LoadScene(nextLevelSceneName);
        }
        else
        {
            Debug.LogError("Next level scene is not assigned.");
        }
    }
}
