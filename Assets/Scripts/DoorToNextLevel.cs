using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorToNextLevel : MonoBehaviour
{
    public SceneAsset nextLevelScene;

    public void GoToNextLevel()
    {
        if (nextLevelScene != null)
        {
            SceneManager.LoadScene(nextLevelScene.name);
        }
        else
        {
            Debug.LogError("Next level scene is not assigned.");
        }
    }

    private void OnDrawGizmos()
    {
        if (nextLevelScene != null)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.yellow;
            style.alignment = TextAnchor.MiddleCenter;
            Handles.Label(transform.position + Vector3.up * 1.5f, "To: " + nextLevelScene.name, style);
        }
    }
}
