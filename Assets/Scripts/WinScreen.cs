using UnityEngine;
using TMPro;

public class WinScreen : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timeText;

    [Header("Audio")]
    public AudioClipGroup winSound;

    void Start()
    {
        if (GameController.Instance != null)
        {
            float total = GameController.Instance.TotalTime;
            timeText.text = "Time: " + FormatTime(total);
            GameController.Instance.PauseGame();
            winSound?.Play();
        }
    }

    private string FormatTime(float time)
    {
        int hours   = (int)(time / 3600f);
        int minutes = (int)((time % 3600f) / 60f);
        int seconds = (int)(time % 60f);
        int millis  = (int)((time * 1000f) % 1000f);

        string result = "";

        if (hours > 0)
            result += $"{hours}h ";

        if (minutes > 0)
            result += $"{minutes}min ";

        result += $"{seconds}.{millis:000}s";

        return result.TrimEnd();
    }
}