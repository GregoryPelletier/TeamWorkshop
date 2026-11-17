using System.Collections;
using UnityEngine;

// A cage that locks an enemy (the boss) inside.
// While the cage stands, the boss stays put, does not attack, and cannot be hurt.
// Only the Bone Spear spell can break the cage. Fireballs and the staff just bounce off.
public class BossCage : MonoBehaviour, IHittable
{
    [Tooltip("The enemy locked inside this cage.")]
    public EnemyFighter lockedEnemy;
    [Tooltip("How many Bone Spears it takes to break the cage.")]
    public int spearsToBreak = 3;
    [Tooltip("The picture of the cage after it breaks (bars lying on the ground).")]
    public Sprite brokenSprite;
    [Tooltip("Seconds the broken bars stay on the ground before they fade away.")]
    public float brokenStaysFor = 2f;

    public bool IsDefeated { get; private set; }   // true once the cage is broken
    public int SpearsLeft { get; private set; }

    SpriteRenderer cageSprite;
    Collider2D cageCollider;
    PlayerSpellbook playerSpellbook;
    Vector3 startPosition;
    float shakeTimer;
    float flashTimer;
    float nextMessageTime;

    void Awake()
    {
        SpearsLeft = spearsToBreak;
        cageSprite = GetComponent<SpriteRenderer>();
        cageCollider = GetComponent<Collider2D>();
        startPosition = transform.position;
        if (lockedEnemy != null)
        {
            lockedEnemy.caged = true;
        }
    }

    void Start()
    {
        playerSpellbook = FindAnyObjectByType<PlayerSpellbook>();
    }

    void Update()
    {
        if (IsDefeated)
        {
            return;
        }
        // shake and flash a moment after each Bone Spear
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float shakeOffset = Mathf.Sin(Time.time * 70f) * 0.03f * (shakeTimer / 0.3f);
            transform.position = startPosition + new Vector3(shakeOffset, 0f, 0f);
        }
        else
        {
            transform.position = startPosition;
        }
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
        }
        if (cageSprite != null)
        {
            cageSprite.color = flashTimer > 0f ? new Color(1f, 0.75f, 0.5f) : Color.white;
        }
    }

    // Fireballs and the staff call this. They cannot break the cage.
    public void TakeHit()
    {
        if (IsDefeated)
        {
            return;
        }
        if (Time.time < nextMessageTime)
        {
            return;   // do not repeat the message on every fireball
        }
        nextMessageTime = Time.time + 2f;
        if (playerSpellbook == null)
        {
            return;
        }
        if (playerSpellbook.knowsBoneSpear)
        {
            playerSpellbook.ShowMessage("Only the Bone Spear can break this cage. Press B!");
        }
        else
        {
            playerSpellbook.ShowMessage("The boss is locked in a cage. You need a spell to break it.");
        }
    }

    // The Bone Spear calls this.
    public void TakeSpellHit()
    {
        if (IsDefeated)
        {
            return;
        }
        SpearsLeft = SpearsLeft - 1;
        shakeTimer = 0.3f;
        flashTimer = 0.15f;
        if (SpearsLeft <= 0)
        {
            StartCoroutine(Break());
            return;
        }
        if (playerSpellbook != null)
        {
            playerSpellbook.ShowMessage("The cage cracks! " + SpearsLeft + (SpearsLeft == 1 ? " more Bone Spear." : " more Bone Spears."));
        }
    }

    IEnumerator Break()
    {
        IsDefeated = true;
        transform.position = startPosition;
        if (cageCollider != null)
        {
            cageCollider.enabled = false;
        }
        if (lockedEnemy != null)
        {
            lockedEnemy.caged = false;
        }
        if (playerSpellbook != null)
        {
            playerSpellbook.ShowMessage("The cage breaks! The boss is free!");
        }
        if (cageSprite != null)
        {
            cageSprite.color = Color.white;
            if (brokenSprite != null)
            {
                cageSprite.sprite = brokenSprite;
            }
        }
        yield return new WaitForSeconds(brokenStaysFor);
        // fade the broken bars away
        for (float fadeAmount = 1f; fadeAmount > 0f; fadeAmount -= Time.deltaTime)
        {
            if (cageSprite != null)
            {
                cageSprite.color = new Color(1f, 1f, 1f, fadeAmount);
            }
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
