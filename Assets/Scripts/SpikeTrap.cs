using UnityEngine;

// A row of spikes that hurts the player when touched.
// Pop up spikes: give it Pop Frames. The spikes hide in the grass, pop up, then sink back down,
// and only hurt while they are up. Thorn patches: leave Pop Frames empty and they always hurt.
// The player gets bounced up a little when hurt, so they never get stuck standing in the spikes.
public class SpikeTrap : MonoBehaviour
{
    [Tooltip("How much health the player loses per hit.")]
    public int damage = 1;
    [Tooltip("Seconds before this trap can hurt the player again.")]
    public float hurtCooldown = 1f;
    [Tooltip("How fast the player is bounced up when hurt.")]
    public float bounceSpeed = 4f;

    [Header("Pop up spikes (leave Pop Frames empty for spikes that are always up)")]
    public Sprite[] popFrames = new Sprite[0];
    [Tooltip("The first and last pop frame (counting from 0) where the spikes are up and sharp.")]
    public int firstSharpFrame = 5;
    public int lastSharpFrame = 10;
    public float framesPerSecond = 10f;
    [Tooltip("Seconds the spikes stay hidden before popping up again.")]
    public float downTime = 1.2f;
    [Tooltip("Extra wait at the start, so traps next to each other pop at different times.")]
    public float startDelay = 0f;

    SpriteRenderer[] spikeRenderers;
    float cycleStart;
    float nextHurtTime;
    bool isSharp = true;
    int shownFrame = -1;

    void Start()
    {
        spikeRenderers = GetComponentsInChildren<SpriteRenderer>();
        cycleStart = Time.time + startDelay;
    }

    void Update()
    {
        if (popFrames == null || popFrames.Length == 0)
        {
            isSharp = true;
            return;
        }
        float popTime = popFrames.Length / framesPerSecond;
        float cycleLength = downTime + popTime;
        float cycleTime = Time.time - cycleStart;
        int frameIndex = 0;
        if (cycleTime > 0f)
        {
            float loopTime = cycleTime % cycleLength;
            if (loopTime > downTime)
            {
                frameIndex = Mathf.FloorToInt((loopTime - downTime) * framesPerSecond);
                frameIndex = Mathf.Clamp(frameIndex, 0, popFrames.Length - 1);
            }
        }
        isSharp = frameIndex >= firstSharpFrame && frameIndex <= lastSharpFrame;
        if (frameIndex == shownFrame)
        {
            return;
        }
        shownFrame = frameIndex;
        foreach (SpriteRenderer spikeRenderer in spikeRenderers)
        {
            spikeRenderer.sprite = popFrames[frameIndex];
        }
    }

    void OnTriggerStay2D(Collider2D otherCollider)
    {
        if (!isSharp || Time.time < nextHurtTime)
        {
            return;
        }
        Health playerHealth = otherCollider.GetComponentInParent<Health>();
        if (playerHealth == null || playerHealth.team != Team.Player)
        {
            return;
        }
        bool tookHit = playerHealth.TakeDamage(damage, DamageType.Physical, Team.Enemy);
        if (tookHit)
        {
            nextHurtTime = Time.time + hurtCooldown;
            Rigidbody2D playerBody = playerHealth.GetComponent<Rigidbody2D>();
            if (playerBody != null)
            {
                playerBody.linearVelocity = new Vector2(playerBody.linearVelocity.x, bounceSpeed);
            }
        }
    }
}
