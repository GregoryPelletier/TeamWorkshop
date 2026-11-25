using UnityEngine;

// A rock hanging under a branch. When the player walks underneath it shakes for a moment,
// then drops. It hurts the player if it lands on them, breaks apart, and grows back a few seconds later.
public class FallingRock : MonoBehaviour
{
    [Tooltip("How much health the player loses if the rock lands on them.")]
    public int damage = 2;
    [Tooltip("How far left or right of the rock the player can be to set it off.")]
    public float triggerWidth = 0.6f;
    [Tooltip("Seconds the rock shakes before it drops (the player's warning).")]
    public float shakeTime = 0.45f;
    public float fallGravity = 16f;
    [Tooltip("Seconds after breaking before the rock starts growing back.")]
    public float regrowDelay = 3f;
    [Tooltip("What the rock lands on (the Ground layer).")]
    public LayerMask groundLayer;

    [Header("Pictures")]
    [Tooltip("The child picture that moves and changes.")]
    public SpriteRenderer rockRenderer;
    [Tooltip("The small bit left hanging after the rock drops.")]
    public SpriteRenderer stubRenderer;
    public Sprite hangingSprite;
    public Sprite fallingSprite;
    public Sprite[] breakFrames = new Sprite[0];
    public Sprite[] regrowFrames = new Sprite[0];
    public float framesPerSecond = 10f;

    enum RockState { Hanging, Shaking, Falling, Breaking, Waiting, Regrowing }
    RockState rockState = RockState.Hanging;
    float stateTime;
    float fallSpeed;
    float groundHeight;
    float breakHeight;
    Vector3 hangPosition;
    Health playerHealth;

    void Start()
    {
        hangPosition = rockRenderer.transform.position;
        Vector2 rayStart = hangPosition + Vector3.down * 0.3f;
        RaycastHit2D groundHit = Physics2D.Raycast(rayStart, Vector2.down, 20f, groundLayer);
        if (groundHit.collider != null)
        {
            groundHeight = groundHit.point.y;
        }
        else
        {
            groundHeight = hangPosition.y - 2f;
        }
        SetState(RockState.Hanging);
    }

    void Update()
    {
        stateTime += Time.deltaTime;
        if (rockState == RockState.Hanging)
        {
            if (PlayerUnderneath())
            {
                SetState(RockState.Shaking);
            }
        }
        else if (rockState == RockState.Shaking)
        {
            float shakeOffset = Mathf.Sin(stateTime * 70f) * 0.03f;
            rockRenderer.transform.position = hangPosition + new Vector3(shakeOffset, 0f, 0f);
            if (stateTime >= shakeTime)
            {
                SetState(RockState.Falling);
            }
        }
        else if (rockState == RockState.Falling)
        {
            fallSpeed += fallGravity * Time.deltaTime;
            Vector3 rockPosition = rockRenderer.transform.position;
            rockPosition.y -= fallSpeed * Time.deltaTime;
            rockRenderer.transform.position = rockPosition;
            if (HitPlayer())
            {
                breakHeight = rockRenderer.bounds.min.y;
                SetState(RockState.Breaking);
            }
            else if (rockRenderer.bounds.min.y <= groundHeight)
            {
                breakHeight = groundHeight;
                SetState(RockState.Breaking);
            }
        }
        else if (rockState == RockState.Breaking)
        {
            int frameIndex = Mathf.FloorToInt(stateTime * framesPerSecond);
            if (frameIndex >= breakFrames.Length)
            {
                SetState(RockState.Waiting);
            }
            else
            {
                rockRenderer.sprite = breakFrames[frameIndex];
            }
        }
        else if (rockState == RockState.Waiting)
        {
            if (stateTime >= regrowDelay)
            {
                SetState(RockState.Regrowing);
            }
        }
        else if (rockState == RockState.Regrowing)
        {
            int frameIndex = Mathf.FloorToInt(stateTime * framesPerSecond * 0.4f);
            if (frameIndex >= regrowFrames.Length)
            {
                SetState(RockState.Hanging);
            }
            else
            {
                rockRenderer.sprite = regrowFrames[frameIndex];
            }
        }
    }

    void SetState(RockState newState)
    {
        rockState = newState;
        stateTime = 0f;
        if (newState == RockState.Hanging || newState == RockState.Shaking)
        {
            rockRenderer.enabled = true;
            rockRenderer.sprite = hangingSprite;
            rockRenderer.transform.position = hangPosition;
            stubRenderer.enabled = false;
        }
        else if (newState == RockState.Falling)
        {
            fallSpeed = 0f;
            stubRenderer.enabled = true;
            rockRenderer.sprite = fallingSprite;
            float stubHeight = stubRenderer.bounds.size.y;
            float rockHalf = rockRenderer.bounds.extents.y;
            rockRenderer.transform.position = hangPosition + Vector3.down * (stubHeight * 0.6f + rockHalf);
        }
        else if (newState == RockState.Breaking)
        {
            rockRenderer.transform.position = new Vector3(hangPosition.x, breakHeight, hangPosition.z);
            if (breakFrames.Length > 0)
            {
                rockRenderer.sprite = breakFrames[0];
            }
        }
        else if (newState == RockState.Waiting)
        {
            rockRenderer.enabled = false;
        }
        else if (newState == RockState.Regrowing)
        {
            rockRenderer.enabled = true;
            rockRenderer.transform.position = hangPosition;
            stubRenderer.enabled = false;
            if (regrowFrames.Length > 0)
            {
                rockRenderer.sprite = regrowFrames[0];
            }
        }
    }

    Health FindPlayer()
    {
        if (playerHealth != null)
        {
            return playerHealth;
        }
        foreach (Health oneHealth in Health.All)
        {
            if (oneHealth.team == Team.Player)
            {
                playerHealth = oneHealth;
            }
        }
        return playerHealth;
    }

    bool PlayerUnderneath()
    {
        Health targetPlayer = FindPlayer();
        if (targetPlayer == null || targetPlayer.IsDefeated)
        {
            return false;
        }
        Bounds playerBox = targetPlayer.BodyBounds();
        float sideGap = Mathf.Abs(playerBox.center.x - hangPosition.x);
        return sideGap <= triggerWidth && playerBox.max.y < hangPosition.y;
    }

    bool HitPlayer()
    {
        Health targetPlayer = FindPlayer();
        if (targetPlayer == null || targetPlayer.IsDefeated)
        {
            return false;
        }
        Bounds rockBox = rockRenderer.bounds;
        rockBox.Expand(new Vector3(-0.2f, -0.1f, 0f));
        if (!rockBox.Intersects(targetPlayer.BodyBounds()))
        {
            return false;
        }
        targetPlayer.TakeDamage(damage, DamageType.Physical, Team.Enemy);
        return true;
    }
}
