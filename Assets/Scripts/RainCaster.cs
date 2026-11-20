using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// The Thunderstorm spell. Attach to the Player, next to FireballCaster.
// SpellInput calls Cast() when Thunderstorm is the selected spell.
// A dark storm cloud appears in front of the player. While it rains, everything under it
// takes Water damage every half second (which also puts out a fire boss's flames),
// and bolts of lightning strike the enemies below it.
public class RainCaster : MonoBehaviour
{
    public Sprite cloudSprite;
    public Sprite dropSprite;
    public float cooldown = 4f;
    [Tooltip("Seconds after casting before the player can attack with the staff or cast again.")]
    public float castLockTime = 0.6f;
    [Tooltip("Seconds the storm lasts.")]
    public float duration = 2.5f;
    [Tooltip("Water damage from the rain, every half second.")]
    public int damage = 1;
    [Tooltip("How far in front of the player the cloud appears.")]
    public float cloudDistance = 1.1f;
    [Tooltip("How high above the player's feet the cloud floats.")]
    public float cloudHeight = 1.5f;
    public float cloudWidth = 1.4f;
    public Color cloudColor = new Color(0.42f, 0.42f, 0.52f, 1f);

    [Header("Lightning")]
    [Tooltip("Seconds between lightning strikes.")]
    public float lightningEvery = 0.6f;
    [Tooltip("Damage from each lightning strike.")]
    public int lightningDamage = 1;
    public Color lightningColor = new Color(1f, 0.97f, 0.65f, 1f);
    [Tooltip("How strongly the screen flashes white with each strike (0 = no flash).")]
    public float screenFlash = 0.25f;

    float nextCastTime;
    Health casterHealth;
    CharacterLooks casterLooks;

    void Awake()
    {
        casterHealth = GetComponent<Health>();
        casterLooks = GetComponent<CharacterLooks>();
    }

    public void Cast()
    {
        if (Time.time < nextCastTime || cloudSprite == null)
        {
            return;
        }
        if (casterHealth != null && casterHealth.IsDefeated)
        {
            return;
        }
        // no casting in the middle of a staff attack (stops switching between the two to attack faster)
        MeleeAttack staffAttack = GetComponent<MeleeAttack>();
        if (staffAttack != null && !staffAttack.FreeToAct())
        {
            return;
        }
        nextCastTime = Time.time + cooldown;
        if (staffAttack != null)
        {
            staffAttack.BusyFor(castLockTime);
        }

        Bounds casterBox = casterHealth != null ? casterHealth.BodyBounds() : new Bounds(transform.position, Vector3.one * 0.5f);
        float facingDirection = casterLooks != null ? casterLooks.FacingDirection() : (transform.localScale.x < 0f ? -1f : 1f);
        Vector3 cloudPoint = new Vector3(casterBox.center.x + cloudDistance * facingDirection, casterBox.min.y + cloudHeight, 0f);

        // the rain falls down to whatever is under the cloud (the ground, a log or a branch)
        float groundBelow = casterBox.min.y - 1f;
        foreach (RaycastHit2D downHit in Physics2D.RaycastAll(cloudPoint, Vector2.down, 6f))
        {
            if (downHit.collider.GetComponentInParent<Tilemap>() != null)
            {
                groundBelow = downHit.point.y;
                break;
            }
        }

        GameObject cloudObject = new GameObject("Storm Cloud");
        cloudObject.transform.position = cloudPoint;
        SpriteRenderer cloudRenderer = cloudObject.AddComponent<SpriteRenderer>();
        cloudRenderer.sprite = cloudSprite;
        cloudRenderer.color = cloudColor;
        SpriteRenderer casterSprite = GetComponent<SpriteRenderer>();
        if (casterSprite != null)
        {
            cloudRenderer.sortingLayerID = casterSprite.sortingLayerID;
            cloudRenderer.sortingOrder = casterSprite.sortingOrder + 2;
        }
        Team casterTeam = casterHealth != null ? casterHealth.team : Team.Player;
        cloudObject.AddComponent<RainCloud>().Launch(this, casterTeam, groundBelow);
        if (casterLooks != null)
        {
            casterLooks.PlayCast();
        }
    }
}

// The storm cloud once it has been cast (made by RainCaster, never placed by hand).
public class RainCloud : MonoBehaviour
{
    RainCaster myCaster;
    Team casterTeam;
    float groundHeight;
    float rainTimer;
    float hitTimer = 0.2f;
    float dropTimer;
    float lightningTimer = 0.25f;
    float flashTimer;
    SpriteRenderer cloudRenderer;
    List<Transform> fallingDrops = new List<Transform>();
    LineRenderer boltLine;
    static Material boltMaterial;

    public void Launch(RainCaster fromCaster, Team fromTeam, float groundBelow)
    {
        myCaster = fromCaster;
        casterTeam = fromTeam;
        groundHeight = groundBelow;
        cloudRenderer = GetComponent<SpriteRenderer>();
        MakeBoltLine();
    }

    void Update()
    {
        if (myCaster == null)
        {
            Destroy(gameObject);
            return;
        }
        rainTimer += Time.deltaTime;
        flashTimer -= Time.deltaTime;
        bool stillRaining = rainTimer <= myCaster.duration;
        if (stillRaining)
        {
            MakeDrops();
            hitTimer -= Time.deltaTime;
            if (hitTimer <= 0f)
            {
                hitTimer = 0.5f;
                HitEverythingBelow();
            }
            lightningTimer -= Time.deltaTime;
            if (lightningTimer <= 0f)
            {
                lightningTimer = myCaster.lightningEvery;
                StrikeLightning();
            }
        }
        MoveDrops();
        ShowFlash();
        if (stillRaining)
        {
            return;
        }
        // the storm has stopped: fade the cloud out, then remove it
        float fadeAmount = 1f - (rainTimer - myCaster.duration) / 0.6f;
        Color fadeColor = myCaster.cloudColor;
        fadeColor.a = Mathf.Clamp01(fadeAmount);
        cloudRenderer.color = fadeColor;
        if (fadeAmount <= 0f && fallingDrops.Count == 0)
        {
            Destroy(gameObject);
        }
    }

    void MakeDrops()
    {
        if (myCaster.dropSprite == null)
        {
            return;
        }
        dropTimer += Time.deltaTime * 70f;
        while (dropTimer >= 1f)
        {
            dropTimer -= 1f;
            GameObject dropObject = new GameObject("Drop");
            dropObject.transform.position = transform.position + new Vector3(Random.Range(-myCaster.cloudWidth / 2f, myCaster.cloudWidth / 2f), -0.1f, 0f);
            SpriteRenderer dropRenderer = dropObject.AddComponent<SpriteRenderer>();
            dropRenderer.sprite = myCaster.dropSprite;
            dropRenderer.sortingLayerID = cloudRenderer.sortingLayerID;
            dropRenderer.sortingOrder = cloudRenderer.sortingOrder - 1;
            fallingDrops.Add(dropObject.transform);
        }
    }

    void MoveDrops()
    {
        for (int dropNumber = fallingDrops.Count - 1; dropNumber >= 0; dropNumber--)
        {
            Transform oneDrop = fallingDrops[dropNumber];
            oneDrop.position += new Vector3(-1.2f, -9f, 0f) * Time.deltaTime;
            if (oneDrop.position.y <= groundHeight)
            {
                Destroy(oneDrop.gameObject);
                fallingDrops.RemoveAt(dropNumber);
            }
        }
    }

    // Everything under the cloud that the caster's side can hurt.
    List<Health> TargetsBelow()
    {
        float boxHeight = Mathf.Max(0.2f, transform.position.y - groundHeight);
        Vector2 boxCenter = new Vector2(transform.position.x, groundHeight + boxHeight / 2f);
        List<Health> foundTargets = new List<Health>();
        foreach (Collider2D touchedCollider in Physics2D.OverlapBoxAll(boxCenter, new Vector2(myCaster.cloudWidth, boxHeight), 0f))
        {
            Health touchedHealth = touchedCollider.GetComponentInParent<Health>();
            if (touchedHealth != null && !foundTargets.Contains(touchedHealth))
            {
                foundTargets.Add(touchedHealth);
            }
        }
        return foundTargets;
    }

    void HitEverythingBelow()
    {
        foreach (Health oneTarget in TargetsBelow())
        {
            oneTarget.TakeDamage(myCaster.damage, DamageType.Water, casterTeam);
        }
    }

    // A bolt strikes an enemy under the cloud, or a random spot on the ground if there is none.
    void StrikeLightning()
    {
        Health struckTarget = null;
        foreach (Health oneTarget in TargetsBelow())
        {
            if (!oneTarget.IsDefeated && oneTarget.team != casterTeam && oneTarget.team != Team.Neutral)
            {
                struckTarget = oneTarget;
                break;
            }
        }
        float strikeX = transform.position.x + Random.Range(-myCaster.cloudWidth / 2f, myCaster.cloudWidth / 2f);
        float strikeBottom = groundHeight;
        if (struckTarget != null)
        {
            Bounds targetBox = struckTarget.BodyBounds();
            strikeX = targetBox.center.x;
            strikeBottom = targetBox.center.y;
            struckTarget.TakeDamage(myCaster.lightningDamage, DamageType.Physical, casterTeam);
        }
        DrawBolt(strikeX, strikeBottom);
        flashTimer = 0.15f;
    }

    void MakeBoltLine()
    {
        GameObject boltObject = new GameObject("Lightning Bolt");
        boltObject.transform.SetParent(transform, false);
        boltLine = boltObject.AddComponent<LineRenderer>();
        if (boltMaterial == null)
        {
            boltMaterial = new Material(Shader.Find("Sprites/Default"));
        }
        boltLine.material = boltMaterial;
        boltLine.startWidth = 0.07f;
        boltLine.endWidth = 0.03f;
        boltLine.startColor = myCaster.lightningColor;
        boltLine.endColor = myCaster.lightningColor;
        boltLine.useWorldSpace = true;
        boltLine.sortingLayerID = cloudRenderer.sortingLayerID;
        boltLine.sortingOrder = cloudRenderer.sortingOrder + 1;
        boltLine.enabled = false;
    }

    // A jagged line from the bottom of the cloud down to the strike point.
    void DrawBolt(float strikeX, float strikeBottom)
    {
        float boltTop = transform.position.y - 0.15f;
        int pointCount = 7;
        boltLine.positionCount = pointCount;
        for (int pointNumber = 0; pointNumber < pointCount; pointNumber++)
        {
            float pointShare = pointNumber / (float)(pointCount - 1);
            float pointX = Mathf.Lerp(transform.position.x, strikeX, pointShare);
            if (pointNumber > 0 && pointNumber < pointCount - 1)
            {
                pointX += Random.Range(-0.15f, 0.15f);
            }
            float pointY = Mathf.Lerp(boltTop, strikeBottom, pointShare);
            boltLine.SetPosition(pointNumber, new Vector3(pointX, pointY, 0f));
        }
        boltLine.enabled = true;
    }

    // The bolt and the white flash of the cloud last a moment after each strike.
    void ShowFlash()
    {
        bool flashingNow = flashTimer > 0f;
        if (boltLine != null)
        {
            boltLine.enabled = flashingNow;
        }
        if (flashingNow && rainTimer <= myCaster.duration)
        {
            cloudRenderer.color = Color.Lerp(myCaster.cloudColor, Color.white, flashTimer / 0.15f);
        }
        else if (rainTimer <= myCaster.duration)
        {
            cloudRenderer.color = myCaster.cloudColor;
        }
    }

    void OnGUI()
    {
        if (flashTimer <= 0f || myCaster == null || myCaster.screenFlash <= 0f)
        {
            return;
        }
        Color oldColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, myCaster.screenFlash * (flashTimer / 0.15f));
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = oldColor;
    }

    void OnDestroy()
    {
        foreach (Transform oneDrop in fallingDrops)
        {
            if (oneDrop != null)
            {
                Destroy(oneDrop.gameObject);
            }
        }
    }
}
