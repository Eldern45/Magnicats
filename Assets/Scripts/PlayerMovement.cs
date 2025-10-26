using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(Collider2D))]
public class PlayerMovement : MonoBehaviour
{
    // --- Movement Settings ---
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float accelGround = 80f;
    [SerializeField] private float accelAir = 60f;
    private const float DeadZone = 0.1f;

    // --- Jump Settings ---
    [Header("Jump")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float jumpBufferTime = 0.2f;
    [SerializeField] private float coyoteTime = 0.12f;

    private float _lastJumpPressedTime = float.NegativeInfinity;
    private float _lastGroundedTime = float.NegativeInfinity;

    // --- Input Actions ---
    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _magnetAction;
    private Vector2 _moveInput;

    // --- Components ---
    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Collider2D _collider;

    // --- Ground Check ---
    [Header("Ground Check")]
    [SerializeField] private Vector2 boxSize = new(0.5f, 0.1f);
    [SerializeField] private float castDistance = 0.1f;
    [SerializeField] private LayerMask groundLayer;
    private bool _isGrounded;

    // --- Physics Materials ---
    [Header("Physics Materials")]
    [SerializeField] private PhysicsMaterial2D groundMaterial;
    [SerializeField] private PhysicsMaterial2D airMaterial;

    // --- Magnet Settings ---
    [Header("Magnet")]
    [SerializeField] private int heroPolarity = +1;
    [SerializeField] private float magnetForceScale = 1f;
    [SerializeField] private MagnetClickMode clickMode = MagnetClickMode.TogglePolarity;
    [SerializeField] private bool magnetEnabled = true;
    [SerializeField] private int lockedPolarity = +1;

    public enum MagnetClickMode { TogglePolarity, ToggleOnOff }

    // --- Unity Events ---

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();

        _moveAction = InputSystem.actions.FindAction("Move");
        _jumpAction = InputSystem.actions.FindAction("Jump");
        _magnetAction = InputSystem.actions.FindAction("MagnetActivate");
    }

    private void Update()
    {
        ReadInput();
        HandleJumpBuffer();
        HandleMagnetInput();
        UpdateSpriteFacing();
    }

    private void FixedUpdate()
    {
        UpdateGroundedState();
        ApplyMovement();
        HandleJumpExecution();
        ApplyMagnetForce();
    }

    // --- Input Handling ---

    private void ReadInput() => _moveInput = _moveAction.ReadValue<Vector2>();

    private void HandleJumpBuffer()
    {
        if (_jumpAction.WasPressedThisFrame())
            _lastJumpPressedTime = Time.time;
    }

    private void HandleMagnetInput()
    {
        if (!_magnetAction.WasPressedThisFrame()) return;

        switch (clickMode)
        {
            case MagnetClickMode.TogglePolarity:
                heroPolarity *= -1;
                break;

            case MagnetClickMode.ToggleOnOff:
                magnetEnabled = !magnetEnabled;
                heroPolarity = lockedPolarity;
                break;
        }

        UpdateSpriteTint();
    }

    private void UpdateSpriteFacing()
    {
        if (Mathf.Abs(_moveInput.x) > DeadZone)
            _sr.flipX = _moveInput.x < 0f;
    }

    // --- Movement & Jumping ---

    private void UpdateGroundedState()
    {
        _isGrounded = IsGrounded();
        _collider.sharedMaterial = _isGrounded ? groundMaterial : airMaterial;
        if (_isGrounded) _lastGroundedTime = Time.time;
    }

    private void ApplyMovement()
    {
        float targetVx = Mathf.Abs(_moveInput.x) > DeadZone ? _moveInput.x * moveSpeed : 0f;
        float currentVx = _rb.linearVelocity.x;
        float accel = (_isGrounded ? accelGround : accelAir) * Mathf.Sign(targetVx - currentVx);

        float newVx = Mathf.MoveTowards(currentVx, targetVx, Mathf.Abs(accel) * Time.fixedDeltaTime);
        _rb.linearVelocity = new Vector2(newVx, _rb.linearVelocity.y);
    }

    private void HandleJumpExecution()
    {
        bool bufferedJump = Time.time <= _lastJumpPressedTime + jumpBufferTime;
        bool canCoyote = Time.time <= _lastGroundedTime + coyoteTime;

        if (bufferedJump && (_isGrounded || canCoyote))
        {
            Jump();
            _lastJumpPressedTime = float.NegativeInfinity;
            _lastGroundedTime = float.NegativeInfinity;
        }
    }

    private void Jump()
    {
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
        _rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    // --- Magnet Force ---

    private void ApplyMagnetForce()
    {
        bool shouldApply =
            clickMode == MagnetClickMode.TogglePolarity ||
            (clickMode == MagnetClickMode.ToggleOnOff && magnetEnabled);

        if (!shouldApply || MagnetFieldManager.Instance == null) return;

        Vector2 magForce = MagnetFieldManager.Instance.GetForceAt(_rb.position, heroPolarity) * magnetForceScale;
        _rb.AddForce(magForce, ForceMode2D.Force);

        // Light damping when strong magnetic forces apply
        float magFactor = Mathf.Clamp01(magForce.magnitude / 50f);
        _rb.linearVelocity *= Mathf.Lerp(1f, 0.9f, magFactor);
    }

    // --- Ground Check ---

    private bool IsGrounded()
    {
        return Physics2D.BoxCast(transform.position, boxSize, 0f, Vector2.down, castDistance, groundLayer);
    }

    // --- Collisions / Triggers ---

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Hazards"))
        {
            RestartScene();
        }
        else if (other.CompareTag("DoorToNextLevel"))
        {
            other.GetComponent<DoorToNextLevel>()?.GoToNextLevel();
        }
    }

    // --- Utilities ---

    private void RestartScene()
    {
        Destroy(gameObject);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void UpdateSpriteTint()
    {
        if (clickMode == MagnetClickMode.ToggleOnOff && !magnetEnabled)
        {
            _sr.color = Color.white;
        }
        else
        {
            _sr.color = heroPolarity == 1 ? Color.cyan : Color.red;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position + Vector3.down * castDistance, boxSize);
    }
}
