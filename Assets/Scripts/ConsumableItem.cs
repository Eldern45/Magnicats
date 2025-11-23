using UnityEngine;

public class ConsumableItem : MonoBehaviour
{
    public enum ConsumableType
    {
        DoubleJump,
        Dash,
        Shield
    }

    public ConsumableType itemType;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player != null)
        {
            switch (itemType)
            {
                case ConsumableType.DoubleJump:
                    player.GrantDoubleJump();
                    Debug.Log("Double Jump Consumed");
                    break;
                case ConsumableType.Dash:
                    player.GrantDash();
                    Debug.Log("Dash Consumed");
                    break;
                case ConsumableType.Shield:
                    player.GrantShield();
                    Debug.Log("Shield Consumed");
                    break;
            }
            Destroy(gameObject);
        }
    }
}
