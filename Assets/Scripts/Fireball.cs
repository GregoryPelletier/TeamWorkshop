using UnityEngine;

// Attach to the Fireball prefab (Rigidbody2D + CircleCollider2D set to Is Trigger).
public class Fireball : MonoBehaviour
{
    public float speed = 12f;
    public float damage = 10f;
    public float lifetime = 3f;   // auto-destroy so stray fireballs never pile up

    // Called by the caster right after Instantiate.
    public void Init(Vector2 direction)
    {
        var rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = direction.normalized * speed;   // Unity 6. Older versions: rb.velocity

        // flip the sprite if the fireball is going left
        if (direction.x < 0f)
        {
            Vector3 s = transform.localScale;
            s.x = -Mathf.Abs(s.x);
            transform.localScale = s;
        }

        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger) return;                                   // ignore pickups, zones, etc.
        if (other.GetComponentInParent<FireballCaster>() != null) return; // ignore the player who cast it

        Destroy(gameObject);   // fireball is used up on impact
    }
}
