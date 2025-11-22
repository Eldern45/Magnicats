using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorToNextLevel : MonoBehaviour
{
    public String nextLevelSceneName;
    public Sprite closedDoorSprite;
    public Sprite openDoorSprite;

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

    private float DistanceFromPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return Vector2.Distance(transform.position, player.transform.position);
        }
        return float.MaxValue;
    }

    private void Update()
    {
        float distance = DistanceFromPlayer();

        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            if (distance < 3.0f)
            {
                sr.sprite = openDoorSprite;
            }
            else
            {
                sr.sprite = closedDoorSprite;
            }
        }
    }
}
