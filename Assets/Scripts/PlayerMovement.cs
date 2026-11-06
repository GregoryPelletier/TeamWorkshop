using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 7f;
    public float jumpForce = 25f;
    public LayerMask groundLayer;
    public float jumpClipLength = 0.875f; // Change this to the exact length of your Mage_Jump clip in seconds
    private Rigidbody2D rb;
    private Collider2D coll;
    private Animator anim;
    private float moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        // Tells the Animator how fast the player is trying to move
        anim.SetFloat("Speed", Mathf.Abs(moveInput));

        // NEW: Updates the Animator with jumping/falling data
        anim.SetBool("isGrounded", IsGrounded());
        anim.SetFloat("yVelocity", rb.linearVelocity.y);

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
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            // Calculate hang time: (2 * Initial Velocity) / Gravity
            float gravity = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
            float totalHangTime = (jumpForce * 2f) / gravity;

            // Set animation speed so clip length perfectly matches hang time
            float calculatedSpeed = jumpClipLength / totalHangTime;
            anim.SetFloat("JumpSpeedMultiplier", calculatedSpeed);
        }
    }

    private bool IsGrounded()
    {
        RaycastHit2D hit = Physics2D.BoxCast(coll.bounds.center, coll.bounds.size, 0f, Vector2.down, 0.1f, groundLayer);
        return hit.collider != null;
    }
}