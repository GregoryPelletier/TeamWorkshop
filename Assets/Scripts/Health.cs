using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// What kind of damage an attack does. A gate can be hurt only by Fire; a fire shield goes down to Water.
public enum DamageType { Physical, Fire, Water }

// Which side something is on. Attacks never hurt their own side,
// and Neutral things (gates, cages) can only be hurt by the Player side.
public enum Team { Player, Enemy, Neutral }

// A message across the middle of the screen. Any script can call GameMessage.Show("...").
// A CharacterLooks with Show Messages ticked (usually the player's) draws it.
public static class GameMessage
{
    public static string Text = "";
    public static float TimeLeft;

    public static void Show(string newText, float showFor = 4f)
    {
        if (string.IsNullOrEmpty(newText))
        {
            return;
        }
        Text = newText;
        TimeLeft = showFor;
    }
}

// Put this on anything that can be hurt: the player, an enemy, a wooden gate, a cage.
// It counts health, heals over time, decides what can hurt it, and what happens when it is defeated.
public class Health : MonoBehaviour
{
    [Header("Health")]
    public Team team = Team.Enemy;
    [Tooltip("How many 1 point hits it takes to defeat this.")]
    public int maxHealth = 6;

    [Header("Healing (Heal Amount 0 = no healing)")]
    public int healAmount = 0;
    [Tooltip("Seconds between each heal while NOT in combat.")]
    public float healDelay = 0.5f;
    [Tooltip("Seconds between each heal while in combat.")]
    public float healDelayInCombat = 3f;
    [Tooltip("Counts as in combat for this many seconds after being hurt (and while an enemy is chasing it).")]
    public float combatTime = 4f;

    [Header("What can hurt it")]
    [Tooltip("Tick to let only one kind of damage count, for example Fire for a wooden gate.")]
    public bool onlyHurtByType = false;
    public DamageType onlyHurtBy = DamageType.Fire;
    [Tooltip("A shield (like flames) that blocks every attack until damage of the type below takes it down.")]
    public bool hasShield = false;
    public DamageType shieldBrokenBy = DamageType.Water;
    [Tooltip("Seconds the shield stays down.")]
    public float shieldDownTime = 6f;
    [Tooltip("Something locked inside this (a boss in a cage): it cannot move, attack or be hurt until this is destroyed.")]
    public Health holding;

    [Header("Hit box (for 3D models and creatures without a Collider 2D)")]
    public bool makeHitBox = true;
    [Tooltip("Leave at 0, 0 to size it from the pictures automatically.")]
    public Vector2 hitBoxSize = Vector2.zero;
    public Vector2 hitBoxOffset = Vector2.zero;

    [Header("Messages (leave empty for none)")]
    public string messageWhenBlocked = "";
    public string messageWhenHurt = "";
    public string messageWhenDefeated = "";

    [Header("When defeated")]
    [Tooltip("Something to switch on where this falls, like a spell orb.")]
    public GameObject dropWhenDefeated;
    [Tooltip("Scripts to switch off, like the player's movement.")]
    public Behaviour[] turnOffWhenDefeated = new Behaviour[0];
    public bool restartLevelWhenDefeated = false;
    public float restartDelay = 3f;
    public UnityEvent whenDefeated;

    // ---- for the other building blocks ----
    public static List<Health> All = new List<Health>();
    public event Action WasHurt;
    public event Action WasBlocked;
    public event Action WasDefeated;
    public event Action WasRevived;

    public int CurrentHealth { get; private set; }
    public bool IsDefeated { get; private set; }
    public bool IsHeld { get; set; }
    public float LastHurtTime { get; private set; } = -100f;
    public Collider2D Body { get; private set; }

    float healTimer;
    Vector3 homePosition;
    float shieldDownUntil = -100f;
    float nextBlockedMessageTime;

    void OnEnable()
    {
        All.Add(this);
    }

    void OnDisable()
    {
        All.Remove(this);
    }

    void Awake()
    {
        CurrentHealth = maxHealth;
        homePosition = transform.position;
        if (makeHitBox)
        {
            Body = MakeHitBox();
        }
        else
        {
            Body = GetComponent<Collider2D>();
        }
        if (holding != null)
        {
            holding.IsHeld = true;
        }
    }

    public float HealthShare()
    {
        return maxHealth <= 0 ? 0f : CurrentHealth / (float)maxHealth;
    }

    public Bounds BodyBounds()
    {
        if (Body != null)
        {
            return Body.bounds;
        }
        return new Bounds(transform.position + Vector3.up * 0.25f, new Vector3(0.4f, 0.5f, 0.1f));
    }

    public bool ShieldUp()
    {
        return hasShield && !IsDefeated && Time.time >= shieldDownUntil;
    }

    // Seconds until the shield comes back (0 or less when it is already up).
    public float ShieldBackIn()
    {
        return shieldDownUntil - Time.time;
    }

    // True if an attack from this side is allowed to hurt this.
    public bool CanBeHurtBy(Team attackerTeam)
    {
        if (attackerTeam == team)
        {
            return false;
        }
        if (team == Team.Neutral && attackerTeam != Team.Player)
        {
            return false;
        }
        return true;
    }

    // In combat after being hurt, or while an enemy is coming after this.
    public bool InCombat()
    {
        bool combatNow = Time.time - LastHurtTime < combatTime;
        foreach (ChaseAI oneChaser in ChaseAI.Active)
        {
            if (oneChaser.IsChasing && oneChaser.Target == this)
            {
                combatNow = true;
            }
        }
        return combatNow;
    }

    // Returns true if the damage counted.
    public bool TakeDamage(int damageAmount, DamageType damageType, Team attackerTeam)
    {
        if (IsDefeated || IsHeld || !CanBeHurtBy(attackerTeam))
        {
            return false;
        }
        if (onlyHurtByType && damageType != onlyHurtBy)
        {
            Blocked();
            return false;
        }
        if (hasShield)
        {
            if (damageType == shieldBrokenBy)
            {
                shieldDownUntil = Time.time + shieldDownTime;
            }
            else if (ShieldUp())
            {
                Blocked();
                return false;
            }
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - damageAmount);
        LastHurtTime = Time.time;
        if (WasHurt != null)
        {
            WasHurt();
        }
        if (CurrentHealth <= 0)
        {
            Defeat();
        }
        else
        {
            GameMessage.Show(messageWhenHurt);
        }
        return true;
    }

    void Blocked()
    {
        if (WasBlocked != null)
        {
            WasBlocked();
        }
        if (Time.time >= nextBlockedMessageTime)
        {
            nextBlockedMessageTime = Time.time + 2f;
            GameMessage.Show(messageWhenBlocked);
        }
    }

    void Update()
    {
        if (GameMessage.TimeLeft > 0f && team == Team.Player)
        {
            GameMessage.TimeLeft -= Time.deltaTime;
        }
        if (IsDefeated || healAmount <= 0)
        {
            return;
        }
        if (CurrentHealth >= maxHealth)
        {
            healTimer = 0f;
            return;
        }
        healTimer += Time.deltaTime;
        float waitForHealth = InCombat() ? healDelayInCombat : healDelay;
        if (healTimer >= waitForHealth)
        {
            healTimer = 0f;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + healAmount);
        }
    }

    // Brings this back at full health where it started, for example a mini boss
    // that has to be beaten again when the player loses its spell.
    public void Revive()
    {
        if (!IsDefeated)
        {
            return;
        }
        transform.position = homePosition;
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        IsDefeated = false;
        CurrentHealth = maxHealth;
        healTimer = 0f;
        shieldDownUntil = -100f;
        if (turnOffWhenDefeated != null)
        {
            foreach (Behaviour oneBehaviour in turnOffWhenDefeated)
            {
                if (oneBehaviour != null)
                {
                    oneBehaviour.enabled = true;
                }
            }
        }
        if (makeHitBox && Body != null)
        {
            Body.enabled = true;
        }
        if (WasRevived != null)
        {
            WasRevived();
        }
    }

    void Defeat()
    {
        IsDefeated = true;
        // older saved objects can have no list at all, which would stop the defeat here
        if (turnOffWhenDefeated != null)
        {
            foreach (Behaviour oneBehaviour in turnOffWhenDefeated)
            {
                if (oneBehaviour != null)
                {
                    oneBehaviour.enabled = false;
                }
            }
        }
        if (makeHitBox && Body != null)
        {
            Body.enabled = false;
        }
        if (holding != null)
        {
            holding.IsHeld = false;
        }
        if (dropWhenDefeated != null)
        {
            Vector3 dropPoint = BodyBounds().center;
            dropWhenDefeated.transform.position = new Vector3(dropPoint.x, dropPoint.y, 0f);
            dropWhenDefeated.SetActive(true);
        }
        GameMessage.Show(messageWhenDefeated);
        if (whenDefeated != null)
        {
            whenDefeated.Invoke();
        }
        if (WasDefeated != null)
        {
            WasDefeated();
        }
        if (restartLevelWhenDefeated)
        {
            StartCoroutine(RestartLevel());
        }
    }

    IEnumerator RestartLevel()
    {
        yield return new WaitForSeconds(restartDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // A flat 2D trigger box around the pictures, on its own child so it stays flat when a 3D model turns.
    Collider2D MakeHitBox()
    {
        Bounds worldBounds = new Bounds(transform.position, Vector3.zero);
        bool foundRenderer = false;
        foreach (Renderer partRenderer in GetComponentsInChildren<Renderer>())
        {
            if (partRenderer is ParticleSystemRenderer)
            {
                continue;
            }
            if (!foundRenderer)
            {
                worldBounds = partRenderer.bounds;
                foundRenderer = true;
            }
            else
            {
                worldBounds.Encapsulate(partRenderer.bounds);
            }
        }
        float shrinkWidth = 0.8f;
        float shrinkHeight = 0.9f;
        if (!foundRenderer)
        {
            worldBounds = new Bounds(transform.position + Vector3.up * 0.25f, new Vector3(0.4f, 0.5f, 0.1f));
        }
        if (hitBoxSize.x > 0f && hitBoxSize.y > 0f)
        {
            worldBounds = new Bounds(transform.position + (Vector3)hitBoxOffset, new Vector3(hitBoxSize.x, hitBoxSize.y, 0.1f));
            shrinkWidth = 1f;
            shrinkHeight = 1f;
        }

        GameObject boxObject = new GameObject("Hit Box");
        boxObject.layer = gameObject.layer;
        boxObject.transform.SetParent(transform, false);
        boxObject.transform.position = new Vector3(worldBounds.center.x, worldBounds.center.y, transform.position.z);
        boxObject.transform.rotation = Quaternion.identity;
        boxObject.AddComponent<HitBoxKeeper>();

        BoxCollider2D hitBox = boxObject.AddComponent<BoxCollider2D>();
        hitBox.isTrigger = true;   // the player walks through enemies instead of standing on their heads
        Vector3 worldScale = boxObject.transform.lossyScale;
        float boxWidth = Mathf.Max(0.15f, worldBounds.size.x * shrinkWidth);
        float boxHeight = Mathf.Max(0.15f, worldBounds.size.y * shrinkHeight);
        hitBox.size = new Vector2(boxWidth / Mathf.Abs(worldScale.x), boxHeight / Mathf.Abs(worldScale.y));

        Rigidbody2D hitBoxBody = boxObject.AddComponent<Rigidbody2D>();
        hitBoxBody.bodyType = RigidbodyType2D.Kinematic;
        return hitBox;
    }
}

// Keeps a hit box flat when the 3D model it belongs to turns around.
public class HitBoxKeeper : MonoBehaviour
{
    void LateUpdate()
    {
        transform.rotation = Quaternion.identity;
    }
}
