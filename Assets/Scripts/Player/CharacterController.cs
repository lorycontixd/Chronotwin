using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 7f;

    [Header("Wall Slide Settings")]
    [SerializeField] private Transform rightWallCheck;
    [SerializeField] private Transform leftWallCheck;
    [SerializeField] private float wallSlideSpeed = 2f;
    [SerializeField] private Vector2 wallJumpForce = new Vector2(5f, 10f);
    [SerializeField] private float wallCheckDistance = 0.05f;
    [SerializeField] private int maxWallJumpCount = 1;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private bool isGrounded;
    private bool isWallSliding;
    private int wallDirection;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private int wallJumpCount;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Optimize Rigidbody2D for platformer
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Update()
    {
        // Check ground and wall states
        CheckGrounded();
        CheckWallSlide();

        // Handle horizontal movement
        float moveHorizontal = Input.GetAxis("Horizontal");
        HandleHorizontalMovement(moveHorizontal);

        // Handle jumping
        HandleJumping();
    }

    void CheckGrounded()
    {
        // Ground detection
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Manage coyote time
        if (isGrounded)
        {
            coyoteTimeCounter = 0.2f;

            // Reset jump buffer if grounded
            if (jumpBufferCounter > 0f)
            {
                jumpBufferCounter = 0f;
            }

            // Reset wall jump count if grounded
            wallJumpCount = 0;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }


    }

    void CheckWallSlide()
    {
        // Wall detection
        RaycastHit2D wallHitLeft = Physics2D.Raycast(
            leftWallCheck.position,
            Vector2.left,
            wallCheckDistance,
            groundLayer
        );

        RaycastHit2D wallHitRight = Physics2D.Raycast(
            rightWallCheck.position,
            Vector2.right,
            wallCheckDistance,
            groundLayer
        );

        // Wall slide logic
        if ((wallHitLeft || wallHitRight) && !isGrounded)
        {
            isWallSliding = true;
            wallDirection = wallHitLeft ? -1 : 1;

            // Slow descent while wall sliding
            if (rb.velocity.y < 0)
            {
                rb.velocity = new Vector2(rb.velocity.x, -wallSlideSpeed);
            }
        }
        else
        {
            isWallSliding = false;
        }
    }

    void HandleHorizontalMovement(float moveHorizontal)
    {
        // Regular horizontal movement
        if (!isWallSliding)
        {
            rb.velocity = new Vector2(moveHorizontal * moveSpeed, rb.velocity.y);
        }

        // Sprite flipping (optional)
        /*if (moveHorizontal > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else if (moveHorizontal < 0)
            transform.localScale = new Vector3(-1, 1, 1);*/
    }

    void HandleJumping()
    {
        // Jump buffer tracking
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = 0.1f;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // Regular ground jump
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }

        // Wall jump
        if (isWallSliding && Input.GetButtonDown("Jump") && wallJumpCount < maxWallJumpCount)
        {
            Vector2 jumpForceVector = new Vector2(
                wallJumpForce.x * -wallDirection,
                wallJumpForce.y
            );

            rb.velocity = jumpForceVector;
            wallJumpCount++;
            isWallSliding = false;
        }
    }
}