using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Put this on the Player. It gives the player health, a staff strike, and fighting looks.
//   J or right mouse button = staff strike (the fireball is still F or left mouse button).
//   While hurt, 1 health comes back every 6 seconds (the wait starts over each time you are hit).
//   Health is shown on the player himself: he fills with red from his feet up as he gets hit.
public class PlayerFighter : MonoBehaviour, IHittable
{
    [Header("Health")]
    [Tooltip("How many hits the player can take before being defeated.")]
    public int hitsToKill = 10;
    [Tooltip("Seconds without being hit before 1 health comes back. It keeps giving 1 back every this many seconds until health is full.")]
    public float healDelay = 6f;
    public int healAmount = 1;

    [Header("Health on the player")]
    [Tooltip("How fast the red rises or drops when health changes.")]
    public float fillSpeed = 3f;

    [Header("Staff strike")]
    public float strikeReach = 0.45f;
    public float strikeCooldown = 0.45f;
    [Tooltip("Seconds into the swing when the staff connects.")]
    public float strikeHitDelay = 0.15f;
    public string[] strikeAnimations = { "AttackDefault1", "AttackDefault2" };

    [Header("Looks")]
    public string castAnimation = "AttackCastBall";
    public string idleAnimation = "IdleAnim";
    public string defeatAnimation = "DeathAnim";
    [Tooltip("Seconds after being defeated before the level starts again.")]
    public float restartDelay = 3f;

    public bool IsDefeated { get; private set; }
    public int HitsLeft { get; private set; }

    Animator playerAnimator;
    SpriteRenderer playerSprite;
    Collider2D playerCollider;
    Rigidbody2D playerBody;
    PlayerMovement playerMovement;
    FireballCaster fireballCaster;
    Color normalColor = Color.white;
    float nextStrikeTime;
    int strikeNumber;
    float hurtFlashTimer;
    Coroutine backToIdle;
    float healTimer;
    float shownRedAmount;      // 0 = no red, 1 = red all the way to the top
    Material fillMaterial;

    void Awake()
    {
        HitsLeft = hitsToKill;
        playerAnimator = GetComponent<Animator>();
        playerSprite = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();
        playerBody = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerMovement>();
        fireballCaster = GetComponent<FireballCaster>();
        if (playerSprite != null)
        {
            normalColor = playerSprite.color;
        }
        // the Health Fill material on the player's Sprite Renderer draws the red
        if (playerSprite != null && playerSprite.material.HasProperty("_FillLevel"))
        {
            fillMaterial = playerSprite.material;
        }
    }

    void Update()
    {
        UpdateHurtFlash();
        UpdateRedFill();
        if (IsDefeated)
        {
            return;
        }
        UpdateHealing();

        Keyboard keyboardInput = Keyboard.current;
        Mouse mouseInput = Mouse.current;
        bool strikePressed = (keyboardInput != null && keyboardInput.jKey.wasPressedThisFrame) ||
                             (mouseInput != null && mouseInput.rightButton.wasPressedThisFrame);
        if (strikePressed)
        {
            StaffStrike();
        }

        // Show the casting pose when a fireball goes out.
        bool castPressed = (keyboardInput != null && keyboardInput.fKey.wasPressedThisFrame) ||
                           (mouseInput != null && mouseInput.leftButton.wasPressedThisFrame);
        if (castPressed && fireballCaster != null && fireballCaster.Mana >= fireballCaster.manaCost)
        {
            PlayFightAnimation(castAnimation);
        }
    }

    float FacingDirection()
    {
        return transform.localScale.x < 0f ? -1f : 1f;
    }

    void StaffStrike()
    {
        if (Time.time < nextStrikeTime)
        {
            return;
        }
        nextStrikeTime = Time.time + strikeCooldown;
        if (strikeAnimations.Length > 0)
        {
            PlayFightAnimation(strikeAnimations[strikeNumber % strikeAnimations.Length]);
            strikeNumber = strikeNumber + 1;
        }
        StartCoroutine(StrikeLands());
    }

    IEnumerator StrikeLands()
    {
        yield return new WaitForSeconds(strikeHitDelay);
        if (IsDefeated)
        {
            yield break;
        }

        Bounds playerBox = playerCollider.bounds;
        float facingDirection = FacingDirection();
        Vector2 strikeCenter = new Vector2(playerBox.center.x + facingDirection * (playerBox.extents.x + strikeReach / 2f), playerBox.center.y);
        Vector2 strikeSize = new Vector2(strikeReach, playerBox.size.y);

        HashSet<IHittable> alreadyHit = new HashSet<IHittable>();
        foreach (Collider2D touchedCollider in Physics2D.OverlapBoxAll(strikeCenter, strikeSize, 0f))
        {
            EnemyFighter enemyFighter = touchedCollider.GetComponentInParent<EnemyFighter>();
            if (enemyFighter != null && !enemyFighter.IsDefeated && alreadyHit.Add(enemyFighter))
            {
                enemyFighter.TakeHit();
            }
            // the staff cannot break a cage, but the cage says so
            BossCage touchedCage = touchedCollider.GetComponentInParent<BossCage>();
            if (touchedCage != null && !touchedCage.IsDefeated && alreadyHit.Add(touchedCage))
            {
                touchedCage.TakeHit();
            }
        }
    }

    // Plays one of the player's attack animations, then goes back to normal.
    public void PlayFightAnimation(string animationName)
    {
        if (playerAnimator == null || string.IsNullOrEmpty(animationName))
        {
            return;
        }
        int stateHash = Animator.StringToHash(animationName);
        if (!playerAnimator.HasState(0, stateHash))
        {
            return;
        }
        playerAnimator.Play(stateHash, 0, 0f);
        if (backToIdle != null)
        {
            StopCoroutine(backToIdle);
        }
        backToIdle = StartCoroutine(BackToIdle());
    }

    IEnumerator BackToIdle()
    {
        yield return null;  // wait one frame so the new animation's length is known
        float animationLength = playerAnimator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(Mathf.Clamp(animationLength, 0.2f, 1.2f));
        if (!IsDefeated)
        {
            playerAnimator.Play(idleAnimation, 0, 0f);
        }
        backToIdle = null;
    }

    public void TakeHit()
    {
        if (IsDefeated)
        {
            return;
        }
        HitsLeft = HitsLeft - 1;
        hurtFlashTimer = 0.2f;
        if (HitsLeft <= 0)
        {
            StartCoroutine(BeDefeated());
            return;
        }
        healTimer = 0f;   // the wait for the next health starts over
    }

    // While hurt, 1 health comes back each time healDelay seconds go by without a hit.
    void UpdateHealing()
    {
        if (HitsLeft >= hitsToKill)
        {
            healTimer = 0f;
            return;
        }
        healTimer += Time.deltaTime;
        if (healTimer >= healDelay)
        {
            healTimer = 0f;
            HitsLeft = Mathf.Min(hitsToKill, HitsLeft + healAmount);
        }
    }

    IEnumerator BeDefeated()
    {
        IsDefeated = true;
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }
        if (fireballCaster != null)
        {
            fireballCaster.enabled = false;
        }
        if (playerBody != null)
        {
            playerBody.linearVelocity = new Vector2(0f, playerBody.linearVelocity.y);
        }
        if (backToIdle != null)
        {
            StopCoroutine(backToIdle);
        }
        if (playerAnimator != null && playerAnimator.HasState(0, Animator.StringToHash(defeatAnimation)))
        {
            playerAnimator.Play(defeatAnimation, 0, 0f);
        }
        yield return new WaitForSeconds(restartDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Fills the player with red from the feet up: each hit missing adds one step of red.
    void UpdateRedFill()
    {
        if (fillMaterial == null || playerCollider == null)
        {
            return;
        }
        float wantedRedAmount = IsDefeated ? 1f : (hitsToKill - HitsLeft) / (float)hitsToKill;
        shownRedAmount = Mathf.MoveTowards(shownRedAmount, wantedRedAmount, fillSpeed * Time.deltaTime);
        Bounds bodyBounds = playerCollider.bounds;
        float feetHeight = bodyBounds.min.y - 0.05f;           // a little below the feet
        float headTop = bodyBounds.max.y + 0.05f;        // a little above the hood
        float fillHeight = shownRedAmount <= 0f ? -10000f : Mathf.Lerp(feetHeight, headTop, shownRedAmount);
        fillMaterial.SetFloat("_FillLevel", fillHeight);
    }

    void UpdateHurtFlash()
    {
        if (playerSprite == null)
        {
            return;
        }
        if (hurtFlashTimer > 0f)
        {
            hurtFlashTimer -= Time.deltaTime;
            playerSprite.color = new Color(1f, 0.4f, 0.4f, normalColor.a);
        }
        else
        {
            playerSprite.color = normalColor;
        }
    }

    // The player himself shows his health (the red fill). This just adds the number in the corner.
    void OnGUI()
    {
        GUIStyle smallText = new GUIStyle(GUI.skin.label) { fontSize = 14 };
        GUI.Label(new Rect(20f, Screen.height - 40f, 300f, 22f), "Health " + HitsLeft + " / " + hitsToKill, smallText);
        if (IsDefeated)
        {
            GUIStyle bigText = new GUIStyle(GUI.skin.label) { fontSize = 36, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(0, Screen.height / 2f - 30f, Screen.width, 60f), "Defeated", bigText);
        }
    }
}
