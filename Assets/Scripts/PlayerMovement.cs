using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 8f;
    
    [Header("Jump")]
    public float jumpForce = 5f;
    public float jumpBufferTime = 0.2f;
    private float _lastJumpPressedTime = -999f;
    
    [Header("Input")]
    public InputAction moveAction;
    public InputAction jumpAction;
    private Vector2 _moveInput;
    
    [Header("Components")]
    private Rigidbody2D rb;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    public bool isGrounded;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        _moveInput = moveAction.ReadValue<Vector2>();
        
        if (jumpAction.WasPressedThisFrame())
            _lastJumpPressedTime = Time.time;
    }

    void FixedUpdate()
    {
        if (!rb) return;
        rb.linearVelocity = new Vector2(_moveInput.x * moveSpeed, rb.linearVelocity.y);
        
        isGrounded = groundCheck ? Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) : false;

        if (isGrounded && (Time.time <= jumpBufferTime + _lastJumpPressedTime))
        {
            DoJump();
            _lastJumpPressedTime = float.NegativeInfinity;
        }
    }

    private void DoJump()
    {
        if (!rb) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    void OnDrawGizmos()
    {
        if (!groundCheck) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}