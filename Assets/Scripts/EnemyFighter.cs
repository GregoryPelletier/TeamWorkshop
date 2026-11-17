using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

// Put this on an enemy to make it fight.
// It notices the player, moves in, attacks, takes hits, and is defeated after "Hits To Kill" hits.
// Works for the 3D enemies (skeletons, fire imps, golems) and for 2D sprite creatures (wolves, wisps, plants).
public class EnemyFighter : MonoBehaviour, IHittable
{
    public enum MoveStyle { Walk, Fly, StayPut }

    [Header("Toughness")]
    [Tooltip("How many hits it takes to defeat this enemy.")]
    public int hitsToKill = 6;

    [Header("Moving")]
    public MoveStyle moveStyle = MoveStyle.Walk;
    public float moveSpeed = 1.0f;
    [Tooltip("How close (in units) the player has to be before this enemy starts fighting.")]
    public float noticeRange = 4f;
    [Tooltip("The enemy never goes further than this from where it started.")]
    public float stayNearHome = 4f;

    [Header("Attacking")]
    [Tooltip("How close the enemy gets (gap between the two bodies, in units) before it attacks.")]
    public float attackRange = 0.15f;
    [Tooltip("Seconds between attacks.")]
    public float attackCooldown = 1.6f;
    [Tooltip("Seconds into the attack when the hit lands on the player.")]
    public float hitDelay = 0.45f;

    [Header("3D models only")]
    public float faceLeftAngle = -90f;
    public float faceRightAngle = 90f;

    [Header("2D sprite creatures only (leave empty for 3D models)")]
    public Sprite[] idleFrames;
    public Sprite[] walkFrames;
    public Sprite[] attackFrames;
    public Sprite[] hurtFrames;
    public Sprite[] deathFrames;
    public float framesPerSecond = 8f;
    [Tooltip("Tick this if the pictures face right. Untick if they face left.")]
    public bool spriteFacesRight = true;

    [Header("Flying creatures only")]
    public float hoverHeight = 0.9f;     // how far above the player's feet a flyer likes to be
    public float bobAmount = 0.12f;      // gentle up and down float

    [Header("Hit box (leave at 0, 0 to size it automatically)")]
    public Vector2 hitBoxSize = Vector2.zero;
    public Vector2 hitBoxOffset = Vector2.zero;

    [Header("Bone Spear spell")]
    [Tooltip("How many hits one Bone Spear counts as against this enemy (2 for every enemy).")]
    public int boneSpearHits = 2;

    [Header("Cage")]
    [Tooltip("Locked in a cage: stays put, does not attack and cannot be hurt. A BossCage turns this on and off.")]
    public bool caged = false;

    [Header("When defeated")]
    [Tooltip("Something to switch on where this enemy falls, like the spell orb the mini boss drops.")]
    public GameObject dropWhenDefeated;
    public UnityEvent whenDefeated;

    // ---- state ----
    public bool IsDefeated { get; private set; }
    public int HitsLeft { get; private set; }

    PlayerFighter playerFighter;
    Collider2D playerCollider;
    Animator modelAnimator;
    HashSet<string> animatorParameters = new HashSet<string>();
    SpriteRenderer spriteRenderer;
    Renderer[] modelRenderers;
    BoxCollider2D hitBox;
    Transform hitBoxTransform;
    Vector3 homePosition;
    bool facingRight;
    bool isAttacking;
    float nextAttackTime;
    float bobOffset;

    Sprite[] currentFrames;
    bool currentLoops;
    float frameTimer;
    int frameNumber;
    float hurtFlashTimer;
    Color normalColor = Color.white;

    void Awake()
    {
        HitsLeft = hitsToKill;
        homePosition = transform.position;
        bobOffset = Random.Range(0f, 6.28f);

        modelAnimator = GetComponentInChildren<Animator>();
        if (modelAnimator != null)
        {
            foreach (AnimatorControllerParameter animatorParameter in modelAnimator.parameters)
            {
                animatorParameters.Add(animatorParameter.name);
            }
        }
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            normalColor = spriteRenderer.color;
        }
        modelRenderers = GetComponentsInChildren<Renderer>();

        MakeHitBox();
        facingRight = !IsFacingLeftNow();
        PlayFrames(idleFrames, true);
    }

    void Start()
    {
        playerFighter = FindAnyObjectByType<PlayerFighter>();
        if (playerFighter != null)
        {
            playerCollider = playerFighter.GetComponent<Collider2D>();
        }
    }

    // A flat 2D box around the enemy so fireballs and the staff can hit it.
    // It lives on its own child object so it stays flat even when a 3D model turns around.
    void MakeHitBox()
    {
        Bounds worldBounds = new Bounds(transform.position, Vector3.zero);
        bool foundRenderer = false;
        foreach (Renderer partRenderer in modelRenderers)
        {
            if (partRenderer is ParticleSystemRenderer)
            {
                continue;
            }
            if (partRenderer.gameObject.name == SkeletonCape.CapeObjectName)
            {
                continue;   // the cape should not make the hit box bigger
            }
            if (!foundRenderer)
            {
                worldBounds = partRenderer.bounds;
                foundRenderer = true;
            }
            else
            {
                worldBounds.Encapsulate(partRenderer.bounds);
            }
        }
        if (!foundRenderer)
        {
            worldBounds = new Bounds(transform.position + Vector3.up * 0.25f, new Vector3(0.4f, 0.5f, 0.1f));
        }
        float shrinkWidth = 0.8f;
        float shrinkHeight = 0.9f;
        if (hitBoxSize.x > 0f && hitBoxSize.y > 0f)
        {
            worldBounds = new Bounds(transform.position + (Vector3)hitBoxOffset, new Vector3(hitBoxSize.x, hitBoxSize.y, 0.1f));
            shrinkWidth = 1f;
            shrinkHeight = 1f;
        }

        GameObject hitBoxObject = new GameObject("Hit Box");
        hitBoxObject.layer = gameObject.layer;
        hitBoxTransform = hitBoxObject.transform;
        hitBoxTransform.SetParent(transform, false);
        hitBoxTransform.position = new Vector3(worldBounds.center.x, worldBounds.center.y, transform.position.z);
        hitBoxTransform.rotation = Quaternion.identity;

        hitBox = hitBoxObject.AddComponent<BoxCollider2D>();
        Vector3 worldScale = hitBoxTransform.lossyScale;
        float boxWidth = Mathf.Max(0.15f, worldBounds.size.x * shrinkWidth);
        float boxHeight = Mathf.Max(0.15f, worldBounds.size.y * shrinkHeight);
        hitBox.size = new Vector2(boxWidth / Mathf.Abs(worldScale.x), boxHeight / Mathf.Abs(worldScale.y));

        Rigidbody2D hitBoxBody = hitBoxObject.AddComponent<Rigidbody2D>();
        hitBoxBody.bodyType = RigidbodyType2D.Kinematic;
    }

    void Update()
    {
        if (hitBoxTransform != null)
        {
            hitBoxTransform.rotation = Quaternion.identity;
        }
        AnimateFrames();
        UpdateHurtFlash();
        if (IsDefeated || isAttacking)
        {
            return;
        }

        if (caged)
        {
            SetWalking(false);
            if (playerFighter != null)
            {
                Face(PlayerCenter().x > hitBox.bounds.center.x);
            }
            return;
        }

        if (playerFighter == null || playerFighter.IsDefeated)
        {
            SetWalking(false);
            return;
        }

        Vector3 playerPosition = PlayerCenter();
        Vector3 enemyCenter = hitBox.bounds.center;
        float sideDistance = playerPosition.x - enemyCenter.x;
        float upDistance = playerPosition.y - enemyCenter.y;

        bool playerIsNear = Mathf.Abs(sideDistance) < noticeRange && Mathf.Abs(upDistance) < 2.5f;
        if (!playerIsNear)
        {
            ReturnHome();
            return;
        }

        Face(sideDistance > 0f);

        if (GapToPlayer() > attackRange)
        {
            MoveToward(playerPosition);
        }
        else
        {
            SetWalking(false);
            if (Time.time >= nextAttackTime)
            {
                StartCoroutine(Attack());
            }
        }
    }

    Vector3 PlayerCenter()
    {
        if (playerCollider != null)
        {
            return playerCollider.bounds.center;
        }
        return playerFighter.transform.position;
    }

    // Space between the edge of this enemy and the edge of the player.
    float GapToPlayer()
    {
        if (playerCollider == null)
        {
            return Vector2.Distance(hitBox.bounds.center, playerFighter.transform.position);
        }
        Bounds enemyBox = hitBox.bounds;
        Bounds playerBox = playerCollider.bounds;
        float sideGap = Mathf.Abs(playerBox.center.x - enemyBox.center.x) - (playerBox.extents.x + enemyBox.extents.x);
        if (moveStyle != MoveStyle.Fly)
        {
            return sideGap;
        }
        float upGap = Mathf.Abs(playerBox.center.y - enemyBox.center.y) - (playerBox.extents.y + enemyBox.extents.y);
        return Mathf.Max(sideGap, upGap);
    }

    void MoveToward(Vector3 targetPosition)
    {
        if (moveStyle == MoveStyle.StayPut)
        {
            SetWalking(false);
            return;
        }

        Vector3 nextPosition = transform.position;
        float stepSize = moveSpeed * Time.deltaTime;
        float sideDirection = Mathf.Sign(targetPosition.x - hitBox.bounds.center.x);
        nextPosition.x += sideDirection * stepSize;

        if (moveStyle == MoveStyle.Fly)
        {
            float wantedHeight = (playerCollider != null ? playerCollider.bounds.min.y : targetPosition.y) + hoverHeight;
            wantedHeight += Mathf.Sin(Time.time * 3f + bobOffset) * bobAmount;
            nextPosition.y = Mathf.MoveTowards(transform.position.y, wantedHeight, stepSize);
        }
        else if (WallAhead(sideDirection))
        {
            SetWalking(false);
            return;
        }

        if (Mathf.Abs(nextPosition.x - homePosition.x) > stayNearHome)
        {
            SetWalking(false);
            return;
        }

        transform.position = nextPosition;
        SetWalking(true);
    }

    void ReturnHome()
    {
        float homeDistance = homePosition.x - transform.position.x;
        bool awayInHeight = moveStyle == MoveStyle.Fly && Mathf.Abs(homePosition.y - transform.position.y) > 0.05f;
        if (moveStyle == MoveStyle.StayPut || (Mathf.Abs(homeDistance) < 0.05f && !awayInHeight))
        {
            SetWalking(false);
            if (moveStyle == MoveStyle.Fly)
            {
                Vector3 floatPosition = transform.position;
                floatPosition.y = homePosition.y + Mathf.Sin(Time.time * 2f + bobOffset) * bobAmount;
                transform.position = floatPosition;
            }
            return;
        }
        Face(homeDistance > 0f);
        float stepSize = moveSpeed * 0.6f * Time.deltaTime;
        Vector3 nextPosition = transform.position;
        nextPosition.x = Mathf.MoveTowards(nextPosition.x, homePosition.x, stepSize);
        if (moveStyle == MoveStyle.Fly)
        {
            nextPosition.y = Mathf.MoveTowards(nextPosition.y, homePosition.y, stepSize);
        }
        transform.position = nextPosition;
        SetWalking(true);
    }

    // Walkers stop at tree trunks instead of walking through them.
    bool WallAhead(float sideDirection)
    {
        Bounds enemyBox = hitBox.bounds;
        Vector2 rayStart = new Vector2(enemyBox.center.x + sideDirection * enemyBox.extents.x, enemyBox.min.y + 0.1f);
        RaycastHit2D[] wallHits = Physics2D.RaycastAll(rayStart, new Vector2(sideDirection, 0f), 0.08f);
        foreach (RaycastHit2D wallHit in wallHits)
        {
            if (wallHit.collider.GetComponentInParent<Tilemap>() != null)
            {
                return true;
            }
        }
        return false;
    }

    IEnumerator Attack()
    {
        isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;
        SetWalking(false);
        SetTrigger("attack");
        PlayFrames(attackFrames, false);

        yield return new WaitForSeconds(hitDelay);
        if (!IsDefeated && playerFighter != null && !playerFighter.IsDefeated && GapToPlayer() <= attackRange + 0.25f)
        {
            playerFighter.TakeHit();
        }
        yield return new WaitForSeconds(0.35f);
        isAttacking = false;
        if (!IsDefeated)
        {
            PlayFrames(idleFrames, true);
        }
    }

    public void TakeHit()
    {
        if (IsDefeated || caged)
        {
            return;
        }
        HitsLeft = HitsLeft - 1;
        hurtFlashTimer = 0.15f;
        if (HitsLeft <= 0)
        {
            BeDefeated();
            return;
        }
        SetTrigger("hit");
        if (!isAttacking)
        {
            PlayFrames(hurtFrames, false);
        }
    }

    // A Bone Spear counts as several hits.
    public void TakeSpellHit()
    {
        for (int spellHit = 0; spellHit < boneSpearHits && !IsDefeated; spellHit++)
        {
            TakeHit();
        }
    }

    void BeDefeated()
    {
        IsDefeated = true;
        StopAllCoroutines();
        isAttacking = false;
        SetWalking(false);
        SetTrigger("die");
        PlayFrames(deathFrames, false);
        if (hitBox != null)
        {
            hitBox.enabled = false;
        }
        if (dropWhenDefeated != null)
        {
            Vector3 dropPoint = hitBoxTransform != null ? hitBoxTransform.position : transform.position + Vector3.up * 0.3f;
            dropWhenDefeated.transform.position = new Vector3(dropPoint.x, dropPoint.y, 0f);
            dropWhenDefeated.SetActive(true);
        }
        whenDefeated.Invoke();
        StartCoroutine(FadeAway());
    }

    IEnumerator FadeAway()
    {
        yield return new WaitForSeconds(2.5f);
        if (spriteRenderer != null)
        {
            for (float fadeTime = 0f; fadeTime < 0.5f; fadeTime += Time.deltaTime)
            {
                Color fadeColor = spriteRenderer.color;
                fadeColor.a = 1f - fadeTime / 0.5f;
                spriteRenderer.color = fadeColor;
                yield return null;
            }
        }
        gameObject.SetActive(false);
    }

    // ---------- looks ----------

    bool IsFacingLeftNow()
    {
        if (spriteRenderer != null)
        {
            return spriteRenderer.flipX == spriteFacesRight;
        }
        float turnAngle = Mathf.DeltaAngle(0f, transform.eulerAngles.y);
        return Mathf.Abs(turnAngle - faceLeftAngle) < 45f;
    }

    void Face(bool wantRight)
    {
        if (wantRight == facingRight)
        {
            return;
        }
        facingRight = wantRight;
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = (facingRight != spriteFacesRight);
        }
        else
        {
            Vector3 turnAngles = transform.eulerAngles;
            turnAngles.y = facingRight ? faceRightAngle : faceLeftAngle;
            transform.eulerAngles = turnAngles;
        }
    }

    void SetWalking(bool isWalking)
    {
        if (animatorParameters.Contains("isWalking"))
        {
            modelAnimator.SetBool("isWalking", isWalking);
        }
        if (spriteRenderer != null && !isAttacking && !IsDefeated)
        {
            Sprite[] wantedFrames = isWalking && walkFrames != null && walkFrames.Length > 0 ? walkFrames : idleFrames;
            if (currentFrames != wantedFrames && (currentLoops || FramesFinished()))
            {
                PlayFrames(wantedFrames, true);
            }
        }
    }

    void SetTrigger(string triggerName)
    {
        if (animatorParameters.Contains(triggerName))
        {
            modelAnimator.SetTrigger(triggerName);
        }
    }

    void PlayFrames(Sprite[] newFrames, bool shouldLoop)
    {
        if (spriteRenderer == null || newFrames == null || newFrames.Length == 0)
        {
            return;
        }
        currentFrames = newFrames;
        currentLoops = shouldLoop;
        frameNumber = 0;
        frameTimer = 0f;
        spriteRenderer.sprite = newFrames[0];
    }

    bool FramesFinished()
    {
        return currentFrames == null || frameNumber >= currentFrames.Length - 1;
    }

    void AnimateFrames()
    {
        if (spriteRenderer == null || currentFrames == null || currentFrames.Length == 0)
        {
            return;
        }
        frameTimer += Time.deltaTime;
        if (frameTimer < 1f / framesPerSecond)
        {
            return;
        }
        frameTimer = 0f;
        if (frameNumber < currentFrames.Length - 1)
        {
            frameNumber = frameNumber + 1;
        }
        else if (currentLoops)
        {
            frameNumber = 0;
        }
        else if (!IsDefeated && currentFrames == hurtFrames)
        {
            PlayFrames(idleFrames, true);
            return;
        }
        spriteRenderer.sprite = currentFrames[frameNumber];
    }

    void UpdateHurtFlash()
    {
        if (spriteRenderer == null)
        {
            return;
        }
        if (hurtFlashTimer > 0f)
        {
            hurtFlashTimer -= Time.deltaTime;
            spriteRenderer.color = new Color(1f, 0.45f, 0.45f, spriteRenderer.color.a);
        }
        else if (!IsDefeated)
        {
            spriteRenderer.color = normalColor;
        }
    }

    // Small health bar over the enemy once it has been hit.
    void OnGUI()
    {
        if (IsDefeated || HitsLeft == hitsToKill || hitBox == null || Camera.main == null)
        {
            return;
        }
        Vector3 barPoint = Camera.main.WorldToScreenPoint(new Vector3(hitBox.bounds.center.x, hitBox.bounds.max.y + 0.08f, 0f));
        if (barPoint.z < 0f)
        {
            return;
        }
        float barWidth = 50f;
        float barHeight = 6f;
        Rect backRect = new Rect(barPoint.x - barWidth / 2f, Screen.height - barPoint.y - barHeight, barWidth, barHeight);
        GUI.color = Color.black;
        GUI.DrawTexture(backRect, Texture2D.whiteTexture);
        GUI.color = Color.red;
        Rect fillRect = new Rect(backRect.x + 1, backRect.y + 1, (barWidth - 2) * HitsLeft / (float)hitsToKill, barHeight - 2);
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
