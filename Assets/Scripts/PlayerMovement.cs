using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(Collider2D))]
public class PlayerMovement : MonoBehaviour
{
    // --- Audio Settings ---
    [Header("Audio")]
    public AudioClipGroup jumpSound;
    public AudioClipGroup landSound;
    public AudioClipGroup dashSound;
    public AudioClipGroup deathSound;

    // --- Animation Settings ---
    [Header("Animation")]
    [SerializeField] private Animator _anim;

    [Header("Idle Randomization")]
    [Range(0f, 1f)]
    [SerializeField] private float idleRareChance = 0.20f; // chance for _2 and for _3 (each)

    [Header("Spin")]
    [Tooltip("Degrees per second. Applied by rotating THIS GameObject (colliders included).")]
    [SerializeField] private float spinRotationSpeed = 720f;

    // --- Movement Settings ---
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float accelGround = 80f;
    public float accelAir = 60f;
    private const float DeadZone = 0.1f;

    // --- Jump Settings ---
    [Header("Jump")]
    public float jumpForce = 5f;
    public float jumpBufferTime = 0.2f;
    public float coyoteTime = 0.12f;
    private float _lastJumpPressedTime = float.NegativeInfinity;
    private float _lastGroundedTime = float.NegativeInfinity;

    // --- Abilities ---
    [Header("Abilities (One-Time)")]
    public bool canDoubleJump = false;
    private bool doubleJumpAvailable = false;

    public bool canDash = false;
    public float dashSpeed = 20f;
    public float dashDuration = 0.15f;
    private bool dashAvailable = false;
    private bool isDashing = false;
    private float dashEndTime = 0f;

    public bool hasShield = false;
    private bool shieldActive = false;

    private bool isKnockedBack = false;
    private float knockbackEndTime = 0f;

    // --- Input ---
    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _dashAction;
    private Vector2 _moveInput;

    // --- Components ---
    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Collider2D _collider;

    // --- Ground Check ---
    [Header("Ground Check")]
    public Vector2 boxSize = new(0.5f, 0.1f);
    public float castDistance = 0.1f;
    public LayerMask groundLayer;
    private bool _isGrounded;

    // --- Physics Materials ---
    [Header("Physics Materials")]
    public PhysicsMaterial2D groundMaterial;
    public PhysicsMaterial2D airMaterial;

    private PlayerMagnetController _magnetController;

    // --- Animation bookkeeping ---
    private string _currentAnimState = "";
    private string _currentIdleColorSuffix = "";
    private int _currentIdleVariant = 1;

    // --- Spin state control ---
    private bool _spinUntilGround = false;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();

        _moveAction = InputSystem.actions.FindAction("Move");
        _jumpAction = InputSystem.actions.FindAction("Jump");
        _dashAction = InputSystem.actions.FindAction("Dash");

        _magnetController = GetComponent<PlayerMagnetController>();

        if (_anim == null)
            _anim = GetComponent<Animator>();
    }

    private void Update()
    {
        if (GameController.Instance != null && GameController.Instance.IsPaused) return;

        ReadInput();
        HandleJumpBuffer();
        UpdateSpriteFacing();
        HandleDashInput();

        ApplySpinRotation();
        UpdateAnimationState();
    }

    private void FixedUpdate()
    {
        UpdateGroundedState();
        UpdateKnockbackState();
        ApplyMovement();

        if (!isDashing)
            HandleJumpExecution();
        else
            ApplyDashMovement();
    }

    // --- Input Handling ---

    private void ReadInput() => _moveInput = _moveAction.ReadValue<Vector2>();

    private void HandleJumpBuffer()
    {
        if (_jumpAction.WasPressedThisFrame())
            _lastJumpPressedTime = Time.time;
    }

    private void UpdateSpriteFacing()
    {
        if (Mathf.Abs(_moveInput.x) > DeadZone)
            _sr.flipX = _moveInput.x < 0f;
    }

    private void ApplySpinRotation()
    {
        // Spin only while "spin state" is active and we're airborne.
        if (!_spinUntilGround || _isGrounded) return;

        // Unity +Z = counterclockwise. We want:
        // facing right (flipX=false) => clockwise => negative Z
        // facing left  (flipX=true)  => counterclockwise => positive Z
        float zDelta = (_sr.flipX ? +1f : -1f) * spinRotationSpeed * Time.deltaTime;
        transform.Rotate(0f, 0f, zDelta);
    }

    private string GetColorSuffix()
    {
        string colorSuffix = "Red";

        if (_magnetController != null)
        {
            switch (_magnetController.CurrentColor)
            {
                case PlayerMagnetController.HeroColor.Neutral:
                    colorSuffix = "Gray";
                    break;
                case PlayerMagnetController.HeroColor.Red:
                    colorSuffix = "Red";
                    break;
                case PlayerMagnetController.HeroColor.Blue:
                    colorSuffix = "Blue";
                    break;
            }
        }

        return colorSuffix;
    }

    private int RollIdleVariant()
    {
        // _2 and _3 each have idleRareChance, otherwise _1.
        float r = Random.value;
        float p2 = idleRareChance;
        float p3 = idleRareChance;
        float p1 = Mathf.Clamp01(1f - (p2 + p3)); // default ~0.60 when rareChance=0.20

        if (r < p1) return 1;
        if (r < p1 + p2) return 2;
        return 3;
    }

    private void UpdateAnimationState()
    {
        if (_anim == null) return;

        string colorSuffix = GetColorSuffix();

        bool isMovingHoriz = Mathf.Abs(_moveInput.x) > DeadZone && _isGrounded;
        bool inAir = !_isGrounded;

        string desiredState;

        if (_spinUntilGround && inAir)
        {
            desiredState = $"Spin_{colorSuffix}";
        }
        else if (inAir)
        {
            desiredState = $"Jump_{colorSuffix}";
        }
        else if (isMovingHoriz)
        {
            desiredState = $"Walk_{colorSuffix}";
        }
        else
        {
            // Roll only when ENTERING idle, or if color changes while idle.
            bool enteringIdle = !_currentAnimState.StartsWith("Idle_");
            bool colorChangedWhileIdle = _currentAnimState.StartsWith("Idle_") && _currentIdleColorSuffix != colorSuffix;

            if (enteringIdle || colorChangedWhileIdle)
            {
                _currentIdleVariant = RollIdleVariant();
                _currentIdleColorSuffix = colorSuffix;

                // Optional: make sure you end upright once you actually settle into idle.
                transform.rotation = Quaternion.identity;
            }

            desiredState = $"Idle_{colorSuffix}_{_currentIdleVariant}";
        }

        // Prevent restarting the same clip every frame
        if (_currentAnimState != desiredState)
        {
            _currentAnimState = desiredState;
            _anim.Play(desiredState);
        }
    }

    // --- Movement & Jumping ---

    private void UpdateGroundedState()
    {
        bool wasGrounded = _isGrounded;
        _isGrounded = IsGrounded();
        _collider.sharedMaterial = _isGrounded ? groundMaterial : airMaterial;

        if (_isGrounded)
        {
            _lastGroundedTime = Time.time;
            doubleJumpAvailable = canDoubleJump; // refresh

            // Landing cancels spin, just like Jump -> Idle/Walk
            if (_spinUntilGround)
            {
                _spinUntilGround = false;
                transform.rotation = Quaternion.identity; // keep upright after landing
            }

            if (!wasGrounded)
                landSound?.Play();
        }
    }

    private void ApplyMovement()
    {
        float targetVx = Mathf.Abs(_moveInput.x) > DeadZone ? _moveInput.x * moveSpeed : 0f;
        float currentVx = _rb.linearVelocity.x;

        // During dash, only allow control if moving against the dash direction
        if (isDashing)
        {
            // Only apply deceleration if trying to move opposite to current velocity
            if (Mathf.Sign(targetVx - currentVx) != Mathf.Sign(currentVx) && Mathf.Abs(currentVx) > moveSpeed)
            {
                float dashAccel = (_isGrounded ? accelGround : accelAir) * Mathf.Sign(targetVx - currentVx);
                float dashVx = Mathf.MoveTowards(currentVx, targetVx, Mathf.Abs(dashAccel) * Time.fixedDeltaTime);
                _rb.linearVelocity = new Vector2(dashVx, _rb.linearVelocity.y);
            }
            return;
        }

        float accel = (_isGrounded ? accelGround : accelAir) * Mathf.Sign(targetVx - currentVx);
        float newVx = Mathf.MoveTowards(currentVx, targetVx, Mathf.Abs(accel) * Time.fixedDeltaTime);
        _rb.linearVelocity = new Vector2(newVx, _rb.linearVelocity.y);
    }

    private void HandleJumpExecution()
    {
        bool bufferedJump = Time.time <= _lastJumpPressedTime + jumpBufferTime;
        bool canCoyote = Time.time <= _lastGroundedTime + coyoteTime;

        if (bufferedJump)
        {
            // normal jump
            if (_isGrounded || canCoyote)
            {
                Jump();
                _lastJumpPressedTime = float.NegativeInfinity;
                _lastGroundedTime = float.NegativeInfinity;
            }
            // double jump
            else if (doubleJumpAvailable)
            {
                doubleJumpAvailable = false;
                canDoubleJump = false; // remove ability after use

                // Double jump triggers Spin_[color] until landing
                _spinUntilGround = true;

                Jump();
                _lastJumpPressedTime = float.NegativeInfinity;
            }
        }
    }

    private void Jump()
    {
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
        _rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        jumpSound?.Play();
    }

    // --- Dash ---

    private void HandleDashInput()
    {
        if (canDash && !isDashing && (_dashAction?.WasPressedThisFrame() ?? false))
        {
            dashAvailable = true;
        }

        if (dashAvailable)
        {
            StartDash();
            dashAvailable = false;
        }
    }

    private void StartDash()
    {
        isDashing = true;
        dashEndTime = Time.time + dashDuration;
        canDash = false; // remove ability after use

        // Dash triggers Spin_[color] until landing
        _spinUntilGround = true;

        float direction = _sr.flipX ? -1f : 1f;
        _rb.linearVelocity = new Vector2(direction * dashSpeed, 12f);
        dashSound?.Play();
    }

    private void ApplyDashMovement()
    {
        if (Time.time >= dashEndTime)
        {
            isDashing = false;
        }
    }

    private void UpdateKnockbackState()
    {
        if (isKnockedBack && Time.time >= knockbackEndTime)
        {
            isKnockedBack = false;
        }
    }

    // --- Shield Collision Handling ---

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Hazards"))
        {
            if (shieldActive)
            {
                shieldActive = false;
                hasShield = false; // remove ability after use

                // knockback - use closest point on hazard for accurate direction
                Vector2 playerPos = transform.position;
                Vector2 hazardPoint = other.ClosestPoint(playerPos);
                Vector2 knockDir = (playerPos - hazardPoint).normalized;
                _rb.linearVelocity = knockDir * 20f;
                isKnockedBack = true;
                knockbackEndTime = Time.time + 0.3f;
            }
            else
            {
                RestartScene();
            }
        }
        else if (other.CompareTag("DoorToNextLevel"))
        {
            other.GetComponent<DoorToNextLevel>()?.GoToNextLevel();
        }
    }

    // PUBLIC API for consumables
    public void GrantDoubleJump()
    {
        canDoubleJump = true;
        doubleJumpAvailable = true;
    }

    public void GrantDash() => canDash = true;

    public void GrantShield()
    {
        hasShield = true;
        shieldActive = true;
    }

    private void RestartScene()
    {
        deathSound?.Play();
        Destroy(gameObject);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private bool IsGrounded()
    {
        return Physics2D.BoxCast(transform.position, boxSize, 0f, Vector2.down, castDistance, groundLayer);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position + Vector3.down * castDistance, boxSize);
    }
}
