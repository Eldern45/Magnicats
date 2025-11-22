using UnityEngine;

public class PlayAgainButton : MonoBehaviour
{
    private void Awake()
    {
        // Listen to click event
        GetComponent<UnityEngine.UI.Button>().onClick.AddListener(GoToNextLevel);
    }

    public void GoToNextLevel()
    {
        GameController.Instance.StartGame();
    }
}
