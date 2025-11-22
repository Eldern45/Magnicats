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

    // --- Magnet Component ---
    private PlayerMagnetController _magnetController;

    // --- Unity Events ---

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();

        _moveAction = InputSystem.actions.FindAction("Move");
        _jumpAction = InputSystem.actions.FindAction("Jump");
        _magnetController = GetComponent<PlayerMagnetController>();
    }

    private void Update()
    {
        if (GameController.Instance != null && GameController.Instance.IsPaused) return;
        ReadInput();
        HandleJumpBuffer();
        UpdateSpriteFacing();
    }

    private void FixedUpdate()
    {
        UpdateGroundedState();
        ApplyMovement();
        HandleJumpExecution();
    }

    // --- Input Handling ---

    private void ReadInput() => _moveInput = _moveAction.ReadValue<Vector2>();

    private void HandleJumpBuffer()
    {
        if (_jumpAction.WasPressedThisFrame())
            _lastJumpPressedTime = Time.time;
    }

    // Magnet input and force are handled by PlayerMagnetController component now

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

    // Magnet force is handled by PlayerMagnetController component now

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

    // Sprite tint related to magnet is handled by PlayerMagnetController component now

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position + Vector3.down * castDistance, boxSize);
    }
}
