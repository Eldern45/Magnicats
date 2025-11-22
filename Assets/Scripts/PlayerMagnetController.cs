using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMagnetController : MonoBehaviour
{
    // --- Magnet Settings ---
    [Header("Magnet")]
    [SerializeField] private int heroPolarity = +1;
    [SerializeField] private float magnetForceScale = 1f;
    [SerializeField] private MagnetClickMode clickMode = MagnetClickMode.TogglePolarity;
    [SerializeField] private bool magnetEnabled = true;
    [SerializeField] private int lockedPolarity = +1;

    [Header("Hero Sprites")]
    [SerializeField] private Sprite neutralSprite;
    [SerializeField] private Sprite redSprite;       //  +1
    [SerializeField] private Sprite blueSprite;      //  -1

    public enum MagnetClickMode { TogglePolarity, ToggleOnOff }

    // --- Components ---
    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private InputAction _magnetAction;
    private MagnetFieldSource[] _scanners;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
        if (!_sr) _sr.color = Color.white;

        // Fetch input action from the global InputSystem
        _magnetAction = InputSystem.actions?.FindAction("MagnetActivate");
        // Cache the list of all available magnet field sources in the scene
        _scanners = FindObjectsByType<MagnetFieldSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        UpdateSpriteTint();
    }

    private void Update()
    {
        if (GameController.Instance != null && GameController.Instance.IsPaused) return;
        HandleMagnetInput();
    }

    private void FixedUpdate()
    {
        ApplyMagnetForce();
    }

    private void HandleMagnetInput()
    {
        if (_magnetAction == null || !_magnetAction.WasPressedThisFrame()) return;

        switch (clickMode)
        {
            case MagnetClickMode.TogglePolarity:
                heroPolarity *= -1;
                break;

            case MagnetClickMode.ToggleOnOff:
                magnetEnabled = !magnetEnabled;
                heroPolarity = lockedPolarity;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        UpdateSpriteTint();
    }

    private void ApplyMagnetForce()
    {
        bool shouldApply =
            clickMode == MagnetClickMode.TogglePolarity ||
            (clickMode == MagnetClickMode.ToggleOnOff && magnetEnabled);

        if (!shouldApply || !_rb) return;

        // Accumulate forces from all active scanners (per-magnet settings)
        if (_scanners == null || _scanners.Length == 0)
        {
            _scanners = FindObjectsByType<MagnetFieldSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        Vector2 total = Vector2.zero;
        if (_scanners != null)
        {
            for (int i = 0; i < _scanners.Length; i++)
            {
                var s = _scanners[i];
                if (!s) continue;
                total += s.GetForceAt(_rb.position, heroPolarity);
            }
        }

        Vector2 magForce = total * magnetForceScale;
        _rb.AddForce(magForce, ForceMode2D.Force);

        // NOTE: Damping removed to prevent it from reducing jump velocity
        // The damping was applying to all velocity including jumps, causing them to feel weak
    }

    private void UpdateSpriteTint()
    {
        if (!_sr) return;
        _sr.color = Color.white;

        if (clickMode == MagnetClickMode.ToggleOnOff && !magnetEnabled)
        {
            _sr.sprite = neutralSprite;
        }
        else
        {
            _sr.sprite = heroPolarity == 1 ? redSprite : blueSprite;
        }
    }
}
