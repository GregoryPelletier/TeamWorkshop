using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 7f;
    public float jumpForce = 25f;
    public LayerMask groundLayer;
    public float jumpClipLength = 0.875f; // Change this to the exact length of your Mage_Jump clip in seconds
    private Rigidbody2D playerBody;
    private Collider2D playerCollider;
    private Animator playerAnimator;
    private float moveInput;

    void Start()
    {
        playerBody = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        playerAnimator = GetComponent<Animator>();
    }

    void Update()
    {
        playerBody.linearVelocity = new Vector2(moveInput * moveSpeed, playerBody.linearVelocity.y);

        // Tells the Animator how fast the player is trying to move
        playerAnimator.SetFloat("Speed", Mathf.Abs(moveInput));

        // NEW: Updates the Animator with jumping/falling data
        playerAnimator.SetBool("isGrounded", IsGrounded());
        playerAnimator.SetFloat("yVelocity", playerBody.linearVelocity.y);

        // Flips the entire character object to face left (-1) or right (1)
        if (moveInput > 0.01f)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (moveInput < -0.01f)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>().x;
    }

    void OnJump(InputValue value)
    {
        if (value.isPressed && IsGrounded())
        {
            // Apply the jump force
            playerBody.linearVelocity = new Vector2(playerBody.linearVelocity.x, jumpForce);

            // Calculate hang time: (2 * Initial Velocity) / Gravity
            float gravityStrength = Mathf.Abs(Physics2D.gravity.y * playerBody.gravityScale);
            float totalHangTime = (jumpForce * 2f) / gravityStrength;

            // Set animation speed so clip length perfectly matches hang time
            float calculatedSpeed = jumpClipLength / totalHangTime;
            playerAnimator.SetFloat("JumpSpeedMultiplier", calculatedSpeed);
        }
    }

    private bool IsGrounded()
    {
        // The box is made a little narrower than the player so a wall beside the player
        // does not count as ground (that is what let the player stick to the side of things).
        Vector2 castSize = new Vector2(playerCollider.bounds.size.x * 0.8f, playerCollider.bounds.size.y);
        RaycastHit2D groundHit = Physics2D.BoxCast(playerCollider.bounds.center, castSize, 0f, Vector2.down, 0.1f, groundLayer);
        return groundHit.collider != null;
    }
}