using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Attach to the Player, next to FireballCaster. SpellInput calls Cast() when Rain is the selected spell.
// A raincloud appears in front of the player and rains for a few seconds. Every half second,
// everything under it takes the damage as Water, which also puts out a fire boss's flames.
public class RainCaster : MonoBehaviour
{
    public Sprite cloudSprite;
    public Sprite dropSprite;
    public float cooldown = 4f;
    [Tooltip("Seconds it rains.")]
    public float duration = 2.5f;
    public int damage = 1;
    [Tooltip("How far in front of the player the cloud appears.")]
    public float cloudDistance = 1.1f;
    [Tooltip("How high above the player's feet the cloud floats.")]
    public float cloudHeight = 1.5f;
    public float cloudWidth = 1.4f;

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
        nextCastTime = Time.time + cooldown;

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

        GameObject cloudObject = new GameObject("Rain Cloud");
        cloudObject.transform.position = cloudPoint;
        SpriteRenderer cloudRenderer = cloudObject.AddComponent<SpriteRenderer>();
        cloudRenderer.sprite = cloudSprite;
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

// The cloud once it has been cast (made by RainCaster, never placed by hand).
public class RainCloud : MonoBehaviour
{
    RainCaster myCaster;
    Team casterTeam;
    float groundHeight;
    float rainTimer;
    float hitTimer = 0.2f;
    float dropTimer;
    SpriteRenderer cloudRenderer;
    List<Transform> fallingDrops = new List<Transform>();

    public void Launch(RainCaster fromCaster, Team fromTeam, float groundBelow)
    {
        myCaster = fromCaster;
        casterTeam = fromTeam;
        groundHeight = groundBelow;
        cloudRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (myCaster == null)
        {
            Destroy(gameObject);
            return;
        }
        rainTimer += Time.deltaTime;
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
        }
        MoveDrops();
        if (stillRaining)
        {
            return;
        }
        // the rain has stopped: fade the cloud out, then remove it
        float fadeAmount = 1f - (rainTimer - myCaster.duration) / 0.6f;
        cloudRenderer.color = new Color(1f, 1f, 1f, Mathf.Clamp01(fadeAmount));
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
        dropTimer += Time.deltaTime * 45f;
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
            oneDrop.position += Vector3.down * 7f * Time.deltaTime;
            if (oneDrop.position.y <= groundHeight)
            {
                Destroy(oneDrop.gameObject);
                fallingDrops.RemoveAt(dropNumber);
            }
        }
    }

    void HitEverythingBelow()
    {
        float boxHeight = Mathf.Max(0.2f, transform.position.y - groundHeight);
        Vector2 boxCenter = new Vector2(transform.position.x, groundHeight + boxHeight / 2f);
        HashSet<Health> alreadyHit = new HashSet<Health>();
        foreach (Collider2D touchedCollider in Physics2D.OverlapBoxAll(boxCenter, new Vector2(myCaster.cloudWidth, boxHeight), 0f))
        {
            Health touchedHealth = touchedCollider.GetComponentInParent<Health>();
            if (touchedHealth != null && alreadyHit.Add(touchedHealth))
            {
                touchedHealth.TakeDamage(myCaster.damage, DamageType.Water, casterTeam);
            }
        }
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
