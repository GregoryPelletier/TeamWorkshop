using UnityEngine;

// Attach to the MagicBolt prefab (Rigidbody2D + small trigger collider).
public class MagicBolt : MonoBehaviour
{
    public float speed = 20f;     // fast, like a Mega Buster pellet
    public float damage = 5f;
    public float lifetime = 0.8f; // short life so the shot only travels a screen or so

    // Tick if the art points right (head on the right). Untick if the bolt looks backwards.
    public bool spriteFacesRight = true;

    MagicBoltCaster owner;

    public void Init(Vector2 direction, MagicBoltCaster shooter)
    {
        owner = shooter;
        GetComponent<Rigidbody2D>().linearVelocity = direction.normalized * speed;   // older Unity: .velocity

        // mirror the sprite so it always points the way it is travelling
        bool goingRight = direction.x >= 0f;
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (goingRight == spriteFacesRight ? 1f : -1f);
        transform.localScale = s;

        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger) return;
        if (other.GetComponentInParent<MagicBoltCaster>() != null) return;   // ignore the shooter

        // TODO: damage enemies, for example other.GetComponent<Health>()?.TakeDamage(damage);

        Destroy(gameObject);
    }

    void OnDestroy()
    {
        // tell the shooter this bolt is gone so it can fire another
        if (owner != null) owner.BoltGone();
    }
}
