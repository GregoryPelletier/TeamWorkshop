using UnityEngine;

// Attach to the Fireball prefab (Rigidbody2D + CircleCollider2D set to Is Trigger).
public class Fireball : MonoBehaviour
{
    public float speed = 12f;
    public float damage = 10f;
    public float lifetime = 3f;   // auto-destroy so stray fireballs never pile up

    // Called by the caster right after Instantiate.
    public void Init(Vector2 flyDirection)
    {
        var fireballBody = GetComponent<Rigidbody2D>();
        fireballBody.linearVelocity = flyDirection.normalized * speed;   // Unity 6. Older versions: fireballBody.velocity

        // flip the sprite if the fireball is going left
        if (flyDirection.x < 0f)
        {
            Vector3 fireballScale = transform.localScale;
            fireballScale.x = -Mathf.Abs(fireballScale.x);
            transform.localScale = fireballScale;
        }

        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D otherCollider)
    {
        if (otherCollider.isTrigger)
        {
            return;                                   // ignore pickups, zones, etc.
        }
        if (otherCollider.GetComponentInParent<FireballCaster>() != null)
        {
            return; // ignore the player who cast it
        }

        // Anything that can be hit (an enemy) takes one hit.
        IHittable hitTarget = otherCollider.GetComponentInParent<IHittable>();
        if (hitTarget != null && !hitTarget.IsDefeated)
        {
            hitTarget.TakeHit();
        }

        Destroy(gameObject);   // fireball is used up on impact
    }
}
