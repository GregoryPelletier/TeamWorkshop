using UnityEngine;

// The glowing spell orb the mini boss drops. Walk into it to learn the Bone Spear.
// It starts switched off; the mini boss's EnemyFighter switches it on when the mini boss is defeated.
// If the player later loses the spell (health at 20%), the orb comes back to where it was picked up.
// It cannot be picked up while the player's health is that low.
public class SpellPickup : MonoBehaviour
{
    public float pickupDistance = 0.45f;
    public float bobAmount = 0.08f;
    public float glowSpeed = 3f;

    Vector3 startPosition;
    SpriteRenderer orbSprite;
    PlayerSpellbook playerSpellbook;
    Vector3 foundAt;
    float nextMessageTime;

    void OnEnable()
    {
        startPosition = transform.position;
        orbSprite = GetComponent<SpriteRenderer>();
        playerSpellbook = FindAnyObjectByType<PlayerSpellbook>();
    }

    // Called when the player loses the spell: the orb appears again where it was picked up.
    public void ReturnToWhereFound()
    {
        transform.position = foundAt;
        transform.localScale = Vector3.one;
        gameObject.SetActive(true);
    }

    void Update()
    {
        // float up and down and pulse so it is easy to spot
        float bobWave = Mathf.Sin(Time.time * glowSpeed);
        transform.position = startPosition + new Vector3(0f, bobWave * bobAmount, 0f);
        float pulseSize = 1f + bobWave * 0.1f;
        transform.localScale = new Vector3(pulseSize, pulseSize, 1f);
        if (orbSprite != null)
        {
            orbSprite.color = new Color(1f, 1f, 1f, 0.8f + bobWave * 0.2f);
        }

        if (playerSpellbook == null)
        {
            return;
        }
        Collider2D playerCollider = playerSpellbook.GetComponent<Collider2D>();
        Vector3 playerCenter = playerCollider != null ? playerCollider.bounds.center : playerSpellbook.transform.position;
        if (Vector2.Distance(playerCenter, transform.position) <= pickupDistance)
        {
            if (playerSpellbook.HealthTooLow())
            {
                if (Time.time >= nextMessageTime)
                {
                    playerSpellbook.ShowMessage("Your health is too low to hold the spell. Heal first!");
                    nextMessageTime = Time.time + 2f;
                }
                return;
            }
            foundAt = startPosition;
            playerSpellbook.LearnBoneSpear(this);
            gameObject.SetActive(false);
        }
    }
}
