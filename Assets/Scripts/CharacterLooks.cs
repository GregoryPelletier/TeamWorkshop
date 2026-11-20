using System.Collections;
using UnityEngine;

// Everything you SEE for a character or object: which way it faces, its animations
// (an Animator for 3D models and the player, or lists of pictures for 2D creatures),
// the red flash when hurt, a shield (flames), its health on screen, and how it goes away when defeated.
// Works with Health; ChaseAI, MeleeAttack and SpellCaster ask it to face, walk and play attacks.
public class CharacterLooks : MonoBehaviour
{
    public enum FacingStyle { FlipScale, FlipSprite, TurnModel }

    [Header("Facing")]
    [Tooltip("Flip Scale: flips the whole object (the player). Flip Sprite: flips the picture (2D creatures). Turn Model: turns a 3D model.")]
    public FacingStyle facingStyle = FacingStyle.FlipSprite;
    [Tooltip("Tick if the pictures face right. Untick if they face left.")]
    public bool spriteFacesRight = true;
    public float faceLeftAngle = -90f;
    public float faceRightAngle = 90f;

    [Header("Animator (3D models and the player): state or trigger names, empty = none")]
    public string[] attackAnimations = { "attack" };
    public string castAnimation = "";
    public string hurtAnimation = "hit";
    public string defeatAnimation = "die";
    [Tooltip("A state to go back to after an attack (the player). Leave empty if the Animator does this itself.")]
    public string idleAnimation = "";
    [Tooltip("A true/false Animator setting that is on while walking.")]
    public string walkingSetting = "isWalking";

    [Header("Pictures (2D creatures, leave empty for 3D models)")]
    public Sprite[] idleFrames;
    public Sprite[] walkFrames;
    public Sprite[] attackFrames;
    public Sprite[] hurtFrames;
    public Sprite[] defeatFrames;
    public float framesPerSecond = 8f;

    [Header("Hurt")]
    public Color hurtColor = new Color(1f, 0.45f, 0.45f);
    [Tooltip("Shake a little when hit (a cage).")]
    public bool shakeWhenHurt = false;

    [Header("Shield (shown while the Health's shield is up)")]
    public Sprite[] shieldFrames;

    [Header("Health on screen")]
    [Tooltip("A small health bar above it once it has been hurt (enemies).")]
    public bool showHealthBar = true;
    [Tooltip("Fill with red from the feet up as health goes down (needs the Health Fill material).")]
    public bool showRedFill = false;
    public float fillSpeed = 3f;
    [Tooltip("Health X / Y in the bottom left corner, and 'Defeated' when it is.")]
    public bool showHealthNumber = false;
    [Tooltip("Draw the GameMessage text across the screen (tick on the player only).")]
    public bool showMessages = false;

    [Header("When defeated")]
    [Tooltip("Seconds of flickering fire before it goes (a wooden gate). 0 = none.")]
    public float burnTime = 0f;
    [Tooltip("A picture to swap to (a broken cage, a burnt gate).")]
    public Sprite defeatedSprite;
    [Tooltip("Switch off its colliders so you can walk through what is left.")]
    public bool turnOffColliders = false;
    [Tooltip("Seconds before it fades away and disappears. -1 = it stays.")]
    public float fadeAfter = 2.5f;
    public float fadeTime = 0.5f;

    Health myHealth;
    Animator modelAnimator;
    SpriteRenderer spriteRenderer;
    SpriteRenderer shieldSprite;
    Material fillMaterial;
    Color normalColor = Color.white;
    bool facingRight = true;
    float hurtTimer;
    float shakeTimer;
    Vector3 shakeHome;
    float shownRedAmount;
    int attackNumber;
    Coroutine backToIdle;

    Sprite[] currentFrames;
    bool currentLoops;
    bool isMoving;
    float frameTimer;
    int frameNumber;

    void Awake()
    {
        myHealth = GetComponent<Health>();
        modelAnimator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            normalColor = spriteRenderer.color;
            if (showRedFill && spriteRenderer.material.HasProperty("_FillLevel"))
            {
                fillMaterial = spriteRenderer.material;
            }
        }
        facingRight = !IsFacingLeftNow();
        PlayFrames(idleFrames, true);
    }

    void OnEnable()
    {
        if (myHealth == null)
        {
            myHealth = GetComponent<Health>();
        }
        if (myHealth != null)
        {
            myHealth.WasHurt += WhenHurt;
            myHealth.WasBlocked += WhenBlocked;
            myHealth.WasDefeated += WhenDefeated;
            myHealth.WasRevived += WhenRevived;
        }
    }

    void OnDisable()
    {
        if (myHealth != null)
        {
            myHealth.WasHurt -= WhenHurt;
            myHealth.WasBlocked -= WhenBlocked;
            myHealth.WasDefeated -= WhenDefeated;
            myHealth.WasRevived -= WhenRevived;
        }
    }

    void Start()
    {
        MakeShield();
        shakeHome = transform.position;
    }

    // ---------------- facing ----------------

    bool IsFacingLeftNow()
    {
        if (facingStyle == FacingStyle.FlipScale)
        {
            return transform.localScale.x < 0f;
        }
        if (facingStyle == FacingStyle.FlipSprite && spriteRenderer != null)
        {
            return spriteRenderer.flipX == spriteFacesRight;
        }
        float turnAngle = Mathf.DeltaAngle(0f, transform.eulerAngles.y);
        return Mathf.Abs(turnAngle - faceLeftAngle) < 45f;
    }

    public float FacingDirection()
    {
        if (facingStyle == FacingStyle.FlipScale)
        {
            return transform.localScale.x < 0f ? -1f : 1f;
        }
        return facingRight ? 1f : -1f;
    }

    public void Face(bool wantRight)
    {
        if (wantRight == facingRight && facingStyle != FacingStyle.FlipScale)
        {
            return;
        }
        facingRight = wantRight;
        if (facingStyle == FacingStyle.FlipScale)
        {
            Vector3 flipScale = transform.localScale;
            flipScale.x = Mathf.Abs(flipScale.x) * (wantRight ? 1f : -1f);
            transform.localScale = flipScale;
        }
        else if (facingStyle == FacingStyle.FlipSprite)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = (facingRight != spriteFacesRight);
            }
        }
        else
        {
            Vector3 turnAngles = transform.eulerAngles;
            turnAngles.y = facingRight ? faceRightAngle : faceLeftAngle;
            transform.eulerAngles = turnAngles;
        }
    }

    // ---------------- animations ----------------

    public void SetMoving(bool movingNow)
    {
        isMoving = movingNow;
        if (modelAnimator != null && HasSetting(walkingSetting, AnimatorControllerParameterType.Bool))
        {
            modelAnimator.SetBool(walkingSetting, movingNow);
        }
        if (spriteRenderer != null && myHealth != null && !myHealth.IsDefeated)
        {
            Sprite[] wantedFrames = movingNow && walkFrames != null && walkFrames.Length > 0 ? walkFrames : idleFrames;
            if (currentFrames != wantedFrames && (currentLoops || FramesFinished()))
            {
                PlayFrames(wantedFrames, true);
            }
        }
    }

    public void PlayAttack()
    {
        if (attackAnimations != null && attackAnimations.Length > 0)
        {
            PlayNamed(attackAnimations[attackNumber % attackAnimations.Length]);
            attackNumber = attackNumber + 1;
        }
        PlayFrames(attackFrames, false);
    }

    public void PlayCast()
    {
        PlayNamed(castAnimation);
    }

    bool HasSetting(string settingName, AnimatorControllerParameterType settingType)
    {
        if (modelAnimator == null || string.IsNullOrEmpty(settingName) || modelAnimator.runtimeAnimatorController == null)
        {
            return false;
        }
        foreach (AnimatorControllerParameter oneSetting in modelAnimator.parameters)
        {
            if (oneSetting.name == settingName && oneSetting.type == settingType)
            {
                return true;
            }
        }
        return false;
    }

    // Plays an Animator state with this name, or pulls a trigger with this name.
    void PlayNamed(string animationName)
    {
        if (modelAnimator == null || string.IsNullOrEmpty(animationName) || modelAnimator.runtimeAnimatorController == null)
        {
            return;
        }
        int stateHash = Animator.StringToHash(animationName);
        if (modelAnimator.HasState(0, stateHash))
        {
            modelAnimator.Play(stateHash, 0, 0f);
            if (!string.IsNullOrEmpty(idleAnimation))
            {
                if (backToIdle != null)
                {
                    StopCoroutine(backToIdle);
                }
                backToIdle = StartCoroutine(BackToIdle());
            }
            return;
        }
        if (HasSetting(animationName, AnimatorControllerParameterType.Trigger))
        {
            modelAnimator.SetTrigger(animationName);
        }
    }

    IEnumerator BackToIdle()
    {
        yield return null;  // wait one frame so the new animation's length is known
        float animationLength = modelAnimator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(Mathf.Clamp(animationLength, 0.2f, 1.2f));
        if (myHealth == null || !myHealth.IsDefeated)
        {
            modelAnimator.Play(idleAnimation, 0, 0f);
        }
        backToIdle = null;
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
        else if (myHealth != null && !myHealth.IsDefeated)
        {
            // an attack or hurt animation finished: back to walking or standing
            PlayFrames(isMoving && walkFrames != null && walkFrames.Length > 0 ? walkFrames : idleFrames, true);
            return;
        }
        spriteRenderer.sprite = currentFrames[frameNumber];
    }

    // ---------------- being hurt ----------------

    void WhenHurt()
    {
        hurtTimer = 0.2f;
        if (shakeWhenHurt)
        {
            shakeTimer = 0.3f;
        }
        if (myHealth != null && myHealth.IsDefeated)
        {
            return;
        }
        PlayNamed(hurtAnimation);
        if (hurtFrames != null && hurtFrames.Length > 0 && currentFrames != attackFrames)
        {
            PlayFrames(hurtFrames, false);
        }
    }

    void WhenBlocked()
    {
        hurtTimer = 0.12f;
        if (shakeWhenHurt)
        {
            shakeTimer = 0.15f;
        }
    }

    void Update()
    {
        AnimateFrames();
        UpdateHurtLook();
        UpdateShield();
        UpdateRedFill();
    }

    void UpdateHurtLook()
    {
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float shakeOffset = Mathf.Sin(Time.time * 70f) * 0.03f * (shakeTimer / 0.3f);
            transform.position = shakeHome + new Vector3(shakeOffset, 0f, 0f);
            if (shakeTimer <= 0f)
            {
                transform.position = shakeHome;
            }
        }
        if (spriteRenderer == null || (myHealth != null && myHealth.IsDefeated))
        {
            return;
        }
        if (hurtTimer > 0f)
        {
            hurtTimer -= Time.deltaTime;
            spriteRenderer.color = new Color(hurtColor.r, hurtColor.g, hurtColor.b, spriteRenderer.color.a);
        }
        else
        {
            spriteRenderer.color = normalColor;
        }
    }

    // ---------------- shield ----------------

    void MakeShield()
    {
        if (myHealth == null || !myHealth.hasShield || shieldFrames == null || shieldFrames.Length == 0)
        {
            return;
        }
        Bounds bodyBox = myHealth.BodyBounds();
        GameObject shieldObject = new GameObject("Shield");
        shieldObject.transform.position = new Vector3(bodyBox.center.x, bodyBox.min.y, transform.position.z - 0.3f);
        Transform followBody = myHealth.Body != null ? myHealth.Body.transform : transform;
        shieldObject.transform.SetParent(followBody, true);
        shieldSprite = shieldObject.AddComponent<SpriteRenderer>();
        shieldSprite.sprite = shieldFrames[0];
        shieldSprite.sortingOrder = 6;
        foreach (Health oneHealth in Health.All)
        {
            SpriteRenderer otherSprite = oneHealth.GetComponent<SpriteRenderer>();
            if (oneHealth.team == Team.Player && otherSprite != null)
            {
                shieldSprite.sortingLayerID = otherSprite.sortingLayerID;
            }
        }
        // size it to wrap the body
        Vector2 pictureSize = shieldFrames[0].bounds.size;
        Vector3 parentScale = followBody.lossyScale;
        shieldObject.transform.localScale = new Vector3(bodyBox.size.x * 1.5f / pictureSize.x / Mathf.Abs(parentScale.x),
                                                        bodyBox.size.y * 1.15f / pictureSize.y / Mathf.Abs(parentScale.y), 1f);
    }

    void UpdateShield()
    {
        if (shieldSprite == null)
        {
            return;
        }
        bool showShield = myHealth.ShieldUp();
        float timeUntilBack = myHealth.ShieldBackIn();
        if (!myHealth.IsDefeated && timeUntilBack > 0f && timeUntilBack < 1f)
        {
            showShield = Mathf.Repeat(Time.time * 8f, 1f) < 0.5f;   // blink before it comes back
        }
        shieldSprite.enabled = showShield;
        if (showShield)
        {
            shieldSprite.sprite = shieldFrames[(int)(Time.time * 10f) % shieldFrames.Length];
        }
    }

    // ---------------- defeated ----------------

    void WhenDefeated()
    {
        StopAllCoroutines();
        if (modelAnimator != null && HasSetting(walkingSetting, AnimatorControllerParameterType.Bool))
        {
            modelAnimator.SetBool(walkingSetting, false);
        }
        StartCoroutine(GoAway());
    }

    // Health brought it back: show it alive again, standing and at full colour.
    void WhenRevived()
    {
        StopAllCoroutines();
        backToIdle = null;
        hurtTimer = 0f;
        if (turnOffColliders)
        {
            foreach (Collider2D oneCollider in GetComponentsInChildren<Collider2D>(true))
            {
                oneCollider.enabled = true;
            }
        }
        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }
        if (modelAnimator != null)
        {
            modelAnimator.Rebind();
            modelAnimator.Update(0f);
        }
        PlayFrames(idleFrames, true);
    }

    IEnumerator GoAway()
    {
        transform.position = shakeTimer > 0f ? shakeHome : transform.position;
        // a wooden gate flickers with fire first
        for (float burnTimer = 0f; burnTimer < burnTime; burnTimer += Time.deltaTime)
        {
            if (spriteRenderer != null)
            {
                float flickerAmount = 0.5f + 0.5f * Mathf.Sin(Time.time * 30f);
                spriteRenderer.color = Color.Lerp(new Color(1f, 0.45f, 0.15f), new Color(1f, 0.8f, 0.3f), flickerAmount);
            }
            yield return null;
        }
        if (turnOffColliders)
        {
            foreach (Collider2D oneCollider in GetComponentsInChildren<Collider2D>())
            {
                oneCollider.enabled = false;
            }
        }
        PlayNamed(defeatAnimation);
        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
            if (defeatedSprite != null)
            {
                spriteRenderer.sprite = defeatedSprite;
                currentFrames = null;
            }
            else
            {
                PlayFrames(defeatFrames, false);
            }
        }
        if (fadeAfter < 0f)
        {
            yield break;
        }
        yield return new WaitForSeconds(fadeAfter);
        if (spriteRenderer != null)
        {
            for (float fadeTimer = 0f; fadeTimer < fadeTime; fadeTimer += Time.deltaTime)
            {
                Color fadeColor = spriteRenderer.color;
                fadeColor.a = 1f - fadeTimer / fadeTime;
                spriteRenderer.color = fadeColor;
                yield return null;
            }
        }
        gameObject.SetActive(false);
    }

    // ---------------- health on screen ----------------

    // Fills the character with red from the feet up: each missing point of health adds one step.
    void UpdateRedFill()
    {
        if (fillMaterial == null || myHealth == null)
        {
            return;
        }
        float wantedRedAmount = myHealth.IsDefeated ? 1f : 1f - myHealth.HealthShare();
        shownRedAmount = Mathf.MoveTowards(shownRedAmount, wantedRedAmount, fillSpeed * Time.deltaTime);
        Bounds bodyBounds = myHealth.BodyBounds();
        float feetHeight = bodyBounds.min.y - 0.05f;
        float headTop = bodyBounds.max.y + 0.05f;
        float fillHeight = shownRedAmount <= 0f ? -10000f : Mathf.Lerp(feetHeight, headTop, shownRedAmount);
        fillMaterial.SetFloat("_FillLevel", fillHeight);
    }

    void OnGUI()
    {
        if (myHealth != null && showHealthBar)
        {
            DrawHealthBar();
        }
        if (myHealth != null && showHealthNumber)
        {
            // the player's health in the bottom left corner, red when it is low
            int healthSize = ScreenText.SizeFor(0.045f);
            float screenMargin = Screen.height * 0.025f;
            Color healthColor = myHealth.HealthShare() <= 0.34f ? new Color(1f, 0.35f, 0.35f) : Color.white;
            Rect healthArea = new Rect(screenMargin, Screen.height - screenMargin - healthSize * 1.4f, Screen.width * 0.5f, healthSize * 1.4f);
            ScreenText.Draw(healthArea, "Health " + myHealth.CurrentHealth + " / " + myHealth.maxHealth, healthSize, TextAnchor.LowerLeft, healthColor, true);
            if (myHealth.IsDefeated)
            {
                int defeatedSize = ScreenText.SizeFor(0.1f);
                Rect defeatedArea = new Rect(0f, Screen.height * 0.4f, Screen.width, defeatedSize * 1.5f);
                ScreenText.Draw(defeatedArea, "Defeated", defeatedSize, TextAnchor.MiddleCenter, new Color(1f, 0.3f, 0.3f), true);
            }
        }
        if (showMessages && GameMessage.TimeLeft > 0f)
        {
            // messages across the upper middle of the screen, in a soft yellow
            int messageSize = ScreenText.SizeFor(0.042f);
            Rect messageArea = new Rect(0f, Screen.height * 0.26f, Screen.width, messageSize * 1.6f);
            ScreenText.Draw(messageArea, GameMessage.Text, messageSize, TextAnchor.MiddleCenter, new Color(1f, 0.93f, 0.55f), true);
        }
    }

    // Small health bar over it once it has been hurt.
    void DrawHealthBar()
    {
        if (myHealth.IsDefeated || myHealth.CurrentHealth >= myHealth.maxHealth || Camera.main == null)
        {
            return;
        }
        Bounds bodyBox = myHealth.BodyBounds();
        Vector3 barPoint = Camera.main.WorldToScreenPoint(new Vector3(bodyBox.center.x, bodyBox.max.y + 0.08f, 0f));
        if (barPoint.z < 0f)
        {
            return;
        }
        float barWidth = Mathf.Max(50f, Screen.height * 0.07f);
        float barHeight = Mathf.Max(6f, Screen.height * 0.011f);
        Rect backRect = new Rect(barPoint.x - barWidth / 2f, Screen.height - barPoint.y - barHeight, barWidth, barHeight);
        GUI.color = Color.black;
        GUI.DrawTexture(backRect, Texture2D.whiteTexture);
        GUI.color = Color.red;
        Rect fillRect = new Rect(backRect.x + 1f, backRect.y + 1f, (barWidth - 2f) * myHealth.HealthShare(), barHeight - 2f);
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
