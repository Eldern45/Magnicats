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

        // Получаем действие инпута из общего InputSystem
        _magnetAction = InputSystem.actions?.FindAction("MagnetActivate");
        // Кэшируем список всех доступных магнитов (тайлмап-сканеров) в сцене
        _scanners = FindObjectsByType<MagnetFieldSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        UpdateSpriteTint();
    }

    private void Update()
    {
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

        // Накапливаем силы от всех активных сканеров (пер-магнитная настройка)
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

        // Небольшое демпфирование при сильном магнитном воздействии
        float magFactor = Mathf.Clamp01(magForce.magnitude / 50f);
        _rb.linearVelocity *= Mathf.Lerp(1f, 0.9f, magFactor);
    }

    private void UpdateSpriteTint()
    {
        if (!_sr) return;

        if (clickMode == MagnetClickMode.ToggleOnOff && !magnetEnabled)
        {
            _sr.color = Color.white;
        }
        else
        {
            _sr.color = heroPolarity == 1 ? Color.cyan : Color.red;
        }
    }
}
