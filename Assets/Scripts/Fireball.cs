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
        if (otherCollider.GetComponentInParent<FireballCaster>() != null)
        {
            return; // ignore the player who cast it
        }

        // Anything with Health (an enemy, a wooden gate, a cage) takes the fireball's damage as fire.
        Health hitHealth = otherCollider.GetComponentInParent<Health>();
        if (hitHealth != null)
        {
            if (hitHealth.IsDefeated || !hitHealth.CanBeHurtBy(Team.Player))
            {
                return;
            }
            hitHealth.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(damage)), DamageType.Fire, Team.Player);
            Destroy(gameObject);   // fireball is used up on impact
            return;
        }

        if (otherCollider.isTrigger)
        {
            return;                                   // ignore pickups, zones, etc.
        }
        Destroy(gameObject);   // fireball is used up on walls and the ground
    }
}
