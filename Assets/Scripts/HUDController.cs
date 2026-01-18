using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Image doubleJumpIcon;
    [SerializeField] private Image dashIcon;
    [SerializeField] private Image shieldIcon;

    private PlayerMovement playerMovement;

    void Start()
    {
        UpdatePlayerReference();
    }

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

        UpdatePlayerReference();
        UpdatePowerupIcons();
    }

    private void UpdatePlayerReference()
    {
        if (playerMovement == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerMovement = player.GetComponent<PlayerMovement>();
            }
        }
    }

    private void UpdatePowerupIcons()
    {
        if (playerMovement != null)
        {
            SetIconActive(doubleJumpIcon, playerMovement.canDoubleJump);
            SetIconActive(dashIcon, playerMovement.canDash);
            SetIconActive(shieldIcon, playerMovement.hasShield);
        }
        else
        {
            SetIconActive(doubleJumpIcon, false);
            SetIconActive(dashIcon, false);
            SetIconActive(shieldIcon, false);
        }
    }

    private void SetIconActive(Image icon, bool isActive)
    {
        if (icon != null)
        {
            icon.enabled = isActive;
        }
    }
}
