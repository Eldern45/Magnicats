using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 8f;
    public float accelGround = 80f;
    public float accelAir = 60f;

    [Header("Jump")]
    public float jumpForce = 5f;
    public float jumpBufferTime = 0.2f;
    public float coyoteTime = 0.12f;
    private float _lastJumpPressedTime = float.NegativeInfinity;
    private float _lastGroundedTime = float.NegativeInfinity;

    [Header("Input")]
    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _mouseAction;
    private Vector2 _moveInput;

    [Header("Components")]
    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private const float DeadZone = 0.1f;

    [Header("Ground Check")]
    public Vector2 boxSize;
    public float castDistance;
    public LayerMask groundLayer;
    private bool _isGrounded;

    [Header("Physics Materials")]
    public PhysicsMaterial2D groundMaterial;
    public PhysicsMaterial2D airMaterial;
    private Collider2D _collider;

    [Header("Magnet")]
    public int heroPolarity = +1;     // +1 attached to red (the cat is blue), -1 vice versa
    public float magnetForceScale = 1f;

    [Header("Magnet Mode")]
    public MagnetClickMode clickMode = MagnetClickMode.TogglePolarity;
    public enum MagnetClickMode { TogglePolarity, ToggleOnOff }
    public bool magnetEnabled = true;
    public int lockedPolarity = +1;

    private void Start()
    {
        _moveAction = InputSystem.actions.FindAction("Move");
        _jumpAction = InputSystem.actions.FindAction("Jump");
        _mouseAction = InputSystem.actions.FindAction("MagnetActivate");
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();
    }

    public void Update()
    {
        _moveInput = _moveAction.ReadValue<Vector2>();

        if (_jumpAction.WasPressedThisFrame())
            _lastJumpPressedTime = Time.time;

        if (_mouseAction.WasPressedThisFrame())
        {
            if (clickMode == MagnetClickMode.TogglePolarity)
            {
                heroPolarity = (heroPolarity == 1) ? -1 : 1;
            }
            else
            {
                magnetEnabled = !magnetEnabled;
            }
            UpdateSpriteTint();
            // Debug.Log($"Polarity: {heroPolarity}");

            if (clickMode == MagnetClickMode.ToggleOnOff)
                heroPolarity = lockedPolarity;
        }


        if (Mathf.Abs(_moveInput.x) > DeadZone)
        {
            _sr.flipX = _moveInput.x < 0f; // right -> false, left -> true
        }
    }

    void FixedUpdate()
    {
        if (!_rb) return;

        _isGrounded = IsGrounded();
        if (_collider) _collider.sharedMaterial = _isGrounded ? groundMaterial : airMaterial;
        if (_isGrounded) _lastGroundedTime = Time.time;

        // Target horizontal speed
        bool hasInput = Mathf.Abs(_moveInput.x) > DeadZone;
        float targetVx = hasInput ? _moveInput.x * moveSpeed : 0f;


        float vx = _rb.linearVelocity.x;
        float neededAccel = (targetVx - vx) / Time.fixedDeltaTime;

        // Limit "sharpness" (to avoid jitter and overshoot):
        float maxAccel = _isGrounded ? accelGround : accelAir;
        neededAccel = Mathf.Clamp(neededAccel, -maxAccel, maxAccel);

        // Convert acceleration to force and apply as regular physics:
        _rb.AddForce(new Vector2(neededAccel * _rb.mass, 0f), ForceMode2D.Force);

        // clamping velocity if moving too fast (play with this)
        if (Mathf.Abs(_rb.linearVelocity.x) > moveSpeed)
        {
            _rb.linearVelocity = new Vector2(
                Mathf.Sign(_rb.linearVelocity.x) * moveSpeed,
                _rb.linearVelocity.y
            );
        }


        bool bufferedJump = (Time.time <= _lastJumpPressedTime + jumpBufferTime);
        bool canCoyote = (Time.time <= _lastGroundedTime + coyoteTime);

        if (bufferedJump && (_isGrounded || canCoyote))
        {
            DoJump();
            _lastJumpPressedTime = float.NegativeInfinity;
            _lastGroundedTime = float.NegativeInfinity;
        }

        bool applyMagnet = (clickMode == MagnetClickMode.TogglePolarity) || (clickMode == MagnetClickMode.ToggleOnOff && magnetEnabled);

        if (applyMagnet && MagnetFieldManager.Instance)
        {
            Vector2 magForce = MagnetFieldManager.Instance.GetForceAt(_rb.position, heroPolarity) * magnetForceScale;
            _rb.AddForce(magForce, ForceMode2D.Force);

            // Damping proportional to magnet force
            float magFactor = Mathf.Clamp01(magForce.magnitude / 50f);
            _rb.linearVelocity *= Mathf.Lerp(1f, 0.9f, magFactor);
        }

    }


    bool IsGrounded()
    {
        return Physics2D.BoxCast(
                transform.position, boxSize, 0f, Vector2.down, castDistance, groundLayer
                );
    }

    private void DoJump()
    {
        if (!_rb) return;
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
        _rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    private void Die()
    {
        Destroy(gameObject);

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Hazards"))
        {
            Die();
        }
    }

    public void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position + Vector3.down * castDistance, boxSize);
    }

    private void UpdateSpriteTint()
    {
        if (clickMode == MagnetClickMode.ToggleOnOff && !magnetEnabled)
        {
            _sr.color = Color.white;
            return;
        }
        _sr.color = (heroPolarity == 1) ? Color.cyan : Color.red;
    }
}
