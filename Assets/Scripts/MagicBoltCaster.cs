using UnityEngine;
using UnityEngine.InputSystem;

// Attach to the Player. Left mouse button fires the starter spell.
// Mega Man style: very quick shots, no mana cost, and only a few bolts alive at once.
public class MagicBoltCaster : MonoBehaviour
{
    public GameObject boltPrefab;
    public float fireInterval = 0.2f;    // seconds between shots (lower = faster)
    public int maxBolts = 3;             // the classic limit of 3 shots on screen
    public bool holdToAutoFire = true;   // hold the button for a stream of shots; untick for one shot per click
    public bool alsoUseJKey = false;     // tick to allow J as a second fire button
    public Vector2 spawnOffset = new Vector2(0.27f, -0.1f);

    // The shoot pose is restarted from its first frame on every shot, so rapid fire looks good.
    // Type the exact name of the state in the Player's Animator (for example AttackDefault1).
    // Leave empty to use the trigger below instead.
    public string animationStateName = "AttackDefault1";
    public string animationTrigger = "Shoot";

    int activeBolts;
    float nextShotTime;
    float facing = 1f;   // 1 = right, -1 = left
    Animator animator;
    int stateHash;
    bool hasState;
    bool hasTrigger;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator != null)
        {
            if (!string.IsNullOrEmpty(animationStateName))
            {
                stateHash = Animator.StringToHash(animationStateName);
                hasState = animator.HasState(0, stateHash);
            }
            if (!string.IsNullOrEmpty(animationTrigger))
            {
                foreach (var p in animator.parameters)
                    if (p.name == animationTrigger) hasTrigger = true;
            }
        }
        if (!hasState && !hasTrigger)
            Debug.LogWarning("MagicBoltCaster: no matching state or trigger on the Animator, so the shoot animation is skipped. Bolts still fire.");
    }

    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null) return;

        if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) facing = -1f;
        if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) facing = 1f;

        bool pressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
        bool held = mouse != null && mouse.leftButton.isPressed;
        if (alsoUseJKey)
        {
            pressed |= kb.jKey.wasPressedThisFrame;
            held |= kb.jKey.isPressed;
        }

        if (pressed || (holdToAutoFire && held)) Shoot();
    }

    void Shoot()
    {
        if (boltPrefab == null) return;
        if (Time.time < nextShotTime) return;
        if (activeBolts >= maxBolts) return;

        nextShotTime = Time.time + fireInterval;
        activeBolts++;

        PlayShootAnimation();

        Vector3 pos = transform.position + new Vector3(spawnOffset.x * facing, spawnOffset.y, 0f);
        GameObject go = Instantiate(boltPrefab, pos, Quaternion.identity);
        go.GetComponent<MagicBolt>().Init(new Vector2(facing, 0f), this);
    }

    void PlayShootAnimation()
    {
        if (animator == null) return;

        if (hasState)
            animator.Play(stateHash, 0, 0f);          // jump to frame 0 every shot, restarting the pose
        else if (hasTrigger)
            animator.SetTrigger(animationTrigger);
    }

    // called by the bolt when it is destroyed
    public void BoltGone()
    {
        activeBolts = Mathf.Max(0, activeBolts - 1);
    }
}
