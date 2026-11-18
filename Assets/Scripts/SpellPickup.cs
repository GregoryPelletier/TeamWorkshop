using UnityEngine;

// A glowing spell orb. Walk into it to unlock the spell with this name in the player's SpellTracker.
// A mini boss can drop it: put the orb in the level switched off, and drag it onto the boss's
// Health "Drop When Defeated". If the player's health drops too low, the spell is locked again
// and the orb comes back to where it was found.
public class SpellPickup : MonoBehaviour
{
    [Tooltip("The spell you learn, exactly as it is named in the player's SpellTracker (for example Fireball or Rain).")]
    public string spellName = "Fireball";
    [Tooltip("Lose the spell when the player's health drops to this share (0.2 = 20%). 0 = never.")]
    public float loseSpellAtHealth = 0.2f;
    public float pickupDistance = 0.45f;
    public float bobAmount = 0.08f;
    public float glowSpeed = 3f;

    Vector3 startPosition;
    SpriteRenderer orbSprite;
    Health heldBy;     // the player holding this spell, while they have it
    float nextMessageTime;

    void OnEnable()
    {
        startPosition = transform.position;
        orbSprite = GetComponent<SpriteRenderer>();
    }

    bool HealthTooLow(Health playerHealth)
    {
        return loseSpellAtHealth > 0f && playerHealth.HealthShare() <= loseSpellAtHealth;
    }

    void Update()
    {
        if (heldBy != null)
        {
            WatchHolder();
            return;
        }

        // float up and down and pulse so it is easy to spot
        float bobWave = Mathf.Sin(Time.time * glowSpeed);
        transform.position = startPosition + new Vector3(0f, bobWave * bobAmount, 0f);
        float pulseSize = 1f + bobWave * 0.1f;
        transform.localScale = new Vector3(pulseSize, pulseSize, 1f);
        if (orbSprite != null)
        {
            orbSprite.color = new Color(1f, 1f, 1f, 0.8f + bobWave * 0.2f);
        }

        foreach (Health oneHealth in Health.All)
        {
            if (oneHealth.team != Team.Player || oneHealth.IsDefeated)
            {
                continue;
            }
            SpellTracker playerSpells = oneHealth.GetComponent<SpellTracker>();
            if (playerSpells == null || Vector2.Distance(oneHealth.BodyBounds().center, transform.position) > pickupDistance)
            {
                continue;
            }
            if (HealthTooLow(oneHealth))
            {
                if (Time.time >= nextMessageTime)
                {
                    GameMessage.Show("Your health is too low to hold the spell. Heal first!");
                    nextMessageTime = Time.time + 2f;
                }
                return;
            }
            if (playerSpells.UnlockSpell(spellName))
            {
                GameMessage.Show("You learned " + spellName + "! Press B or the left mouse button to cast it.");
                heldBy = oneHealth;
                if (orbSprite != null)
                {
                    orbSprite.enabled = false;   // hidden, but still watching the player's health
                }
                return;
            }
        }
    }

    // While the player has the spell: if their health drops too low, they lose it and the orb comes back.
    void WatchHolder()
    {
        if (heldBy.IsDefeated || !HealthTooLow(heldBy))
        {
            return;
        }
        SpellTracker playerSpells = heldBy.GetComponent<SpellTracker>();
        if (playerSpells != null)
        {
            playerSpells.LockSpell(spellName);
        }
        GameMessage.Show("Your health is too low! " + spellName + " went back to where you found it.");
        heldBy = null;
        transform.position = startPosition;
        if (orbSprite != null)
        {
            orbSprite.enabled = true;
        }
    }
}
