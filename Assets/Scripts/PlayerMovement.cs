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

    [Tooltip("Hold the down arrow to crouch: the player stops, ducks down to this share of its height and attacks low.")]
    public float crouchHeight = 0.6f;
    public bool IsCrouching { get; private set; }
    private MeleeAttack meleeAttack;

    void Start()
    {
        playerBody = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        playerAnimator = GetComponent<Animator>();
        meleeAttack = GetComponent<MeleeAttack>();
    }

    void Update()
    {
        // Down arrow: crouch (only starting on the ground, and staying down while it is held)
        bool downHeld = Keyboard.current != null && Keyboard.current.downArrowKey.isPressed;
        bool wantCrouch = downHeld && (IsCrouching || IsGrounded());
        if (wantCrouch != IsCrouching)
        {
            SetCrouching(wantCrouch);
        }
        float walkInput = IsCrouching ? 0f : moveInput;   // no walking while crouched

        playerBody.linearVelocity = new Vector2(walkInput * moveSpeed, playerBody.linearVelocity.y);

        // Tells the Animator how fast the player is trying to move
        playerAnimator.SetFloat("Speed", Mathf.Abs(walkInput));

        // NEW: Updates the Animator with jumping/falling data
        playerAnimator.SetBool("isGrounded", IsGrounded());
        playerAnimator.SetFloat("yVelocity", playerBody.linearVelocity.y);

        // Flips the entire character object to face left (-1) or right (1)
        // (the player can still turn around while crouched)
        float bodyHeight = IsCrouching ? crouchHeight : 1f;
        if (moveInput > 0.01f)
        {
            transform.localScale = new Vector3(1, bodyHeight, 1);
        }
        else if (moveInput < -0.01f)
        {
            transform.localScale = new Vector3(-1, bodyHeight, 1);
        }
    }

    // Ducks down or stands back up, keeping the feet on the ground.
    void SetCrouching(bool crouchNow)
    {
        float feetHeight = playerCollider.bounds.min.y;
        IsCrouching = crouchNow;
        Vector3 crouchScale = transform.localScale;
        crouchScale.y = crouchNow ? crouchHeight : 1f;
        transform.localScale = crouchScale;
        Physics2D.SyncTransforms();
        transform.position += new Vector3(0f, feetHeight - playerCollider.bounds.min.y, 0f);
        if (meleeAttack != null)
        {
            meleeAttack.attackLow = crouchNow;
        }
    }

    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>().x;
    }

    void OnJump(InputValue value)
    {
        if (value.isPressed && IsGrounded() && !IsCrouching)
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