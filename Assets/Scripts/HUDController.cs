using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI timerText;
    
    void Update()
    {
        if (GameController.Instance != null)
        {
            levelText.text = $"Level: {GameController.Instance.CurrentLevel}";
            
            float time = GameController.Instance.TotalTime;
            int hours = Mathf.FloorToInt(time / 3600f);
            int minutes = Mathf.FloorToInt((time % 3600f) / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            
            if (hours > 0)
            {
                timerText.text = $"{hours:00}:{minutes:00}:{seconds:00}";
            }
            else
            {
                timerText.text = $"{minutes:00}:{seconds:00}";
            }
        }
    }
}