using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MenuClickSound : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField] private AudioClipGroup clickSound;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(PlayClick);
    }

    private void PlayClick()
    {
        if (clickSound != null)
        {
            clickSound.Play();
        }
        else
        {
            Debug.LogWarning($"UIButtonClickSound on {name} has no clickSound assigned.");
        }
    }
}