using UnityEngine;
using UnityEngine.Tilemaps;

// The Bone Spear spell flying through the air. PlayerSpellbook makes one each time you cast.
// It flies straight, hits the first enemy it touches, and breaks on walls and trunks.
public class BoneSpear : MonoBehaviour
{
    public float flySpeed = 9f;
    public float lifetime = 1.5f;
    public Vector2 spearSize = new Vector2(0.45f, 0.12f);

    float flyDirection = 1f;

    // Called by PlayerSpellbook right after the spear is made.
    public void Launch(float launchDirection)
    {
        flyDirection = launchDirection < 0f ? -1f : 1f;
        SpriteRenderer spearSprite = GetComponent<SpriteRenderer>();
        if (spearSprite != null)
        {
            spearSprite.flipX = flyDirection < 0f;
        }
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += new Vector3(flyDirection * flySpeed * Time.deltaTime, 0f, 0f);

        Collider2D[] touchedColliders = Physics2D.OverlapBoxAll(transform.position, spearSize, 0f);

        // a cage in the way takes the spear first
        foreach (Collider2D touchedCollider in touchedColliders)
        {
            BossCage touchedCage = touchedCollider.GetComponentInParent<BossCage>();
            if (touchedCage != null && !touchedCage.IsDefeated)
            {
                touchedCage.TakeSpellHit();
                Destroy(gameObject);
                return;
            }
        }

        foreach (Collider2D touchedCollider in touchedColliders)
        {
            EnemyFighter enemyFighter = touchedCollider.GetComponentInParent<EnemyFighter>();
            if (enemyFighter != null && !enemyFighter.IsDefeated && !enemyFighter.caged)
            {
                enemyFighter.TakeSpellHit();
                Destroy(gameObject);
                return;
            }
            if (touchedCollider.GetComponentInParent<Tilemap>() != null)
            {
                Destroy(gameObject);   // broke on a trunk or the ground
                return;
            }
        }
    }
}
