using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// A close up attack (a staff, a sword, claws, a bite) for any character.
// The player uses the keys; an enemy's ChaseAI calls TryAttack when it is close enough.
// It hurts everything on the other side inside a box just in front of the character.
public class MeleeAttack : MonoBehaviour
{
    [Header("The hit")]
    public int damage = 1;
    public DamageType damageType = DamageType.Physical;
    [Tooltip("How far in front of the body the attack reaches.")]
    public float reach = 0.45f;
    [Tooltip("Seconds between attacks.")]
    public float cooldown = 0.45f;
    [Tooltip("Seconds into the swing when the hit lands.")]
    public float hitDelay = 0.15f;
    [Tooltip("Seconds the character stands still after the hit lands.")]
    public float pauseAfterHit = 0f;

    [Header("Player only")]
    [Tooltip("Attack with V or the left mouse button.")]
    public bool useKeyboard = false;

    [Tooltip("Hit low, along the ground in front of the feet (the player sets this while crouching).")]
    public bool attackLow = false;

    public bool IsAttacking { get; private set; }

    Health myHealth;
    CharacterLooks myLooks;
    float nextAttackTime;

    void Awake()
    {
        myHealth = GetComponent<Health>();
        myLooks = GetComponent<CharacterLooks>();
    }

    void Update()
    {
        if (!useKeyboard || (myHealth != null && myHealth.IsDefeated))
        {
            return;
        }
        Keyboard keyboardInput = Keyboard.current;
        Mouse mouseInput = Mouse.current;
        bool attackPressed = (keyboardInput != null && keyboardInput.vKey.wasPressedThisFrame) ||
                             (mouseInput != null && mouseInput.leftButton.wasPressedThisFrame);
        if (attackPressed)
        {
            TryAttack();
        }
    }

    // Starts an attack if the cooldown is over. Returns true if it started.
    public bool TryAttack()
    {
        if (Time.time < nextAttackTime || IsAttacking)
        {
            return false;
        }
        if (myHealth != null && (myHealth.IsDefeated || myHealth.IsHeld))
        {
            return false;
        }
        nextAttackTime = Time.time + cooldown;
        if (myLooks != null)
        {
            myLooks.PlayAttack();
        }
        StartCoroutine(SwingLands());
        return true;
    }

    float FacingDirection()
    {
        if (myLooks != null)
        {
            return myLooks.FacingDirection();
        }
        return transform.localScale.x < 0f ? -1f : 1f;
    }

    IEnumerator SwingLands()
    {
        IsAttacking = pauseAfterHit > 0f;
        yield return new WaitForSeconds(hitDelay);
        if (myHealth == null || !myHealth.IsDefeated)
        {
            Team myTeam = myHealth != null ? myHealth.team : Team.Neutral;
            Bounds myBox = myHealth != null ? myHealth.BodyBounds() : new Bounds(transform.position, Vector3.one * 0.5f);
            float facingDirection = FacingDirection();
            Vector2 hitCenter = new Vector2(myBox.center.x + facingDirection * (myBox.extents.x + reach / 2f), myBox.center.y);
            Vector2 hitSize = new Vector2(reach, myBox.size.y);
            if (attackLow)
            {
                // a low sweep: from just below the feet to a little above them, reaching a bit further
                float sweepReach = reach * 1.3f;
                hitCenter = new Vector2(myBox.center.x + facingDirection * (myBox.extents.x + sweepReach / 2f), myBox.min.y + 0.1f);
                hitSize = new Vector2(sweepReach, 0.35f);
            }
            HashSet<Health> alreadyHit = new HashSet<Health>();
            foreach (Collider2D touchedCollider in Physics2D.OverlapBoxAll(hitCenter, hitSize, 0f))
            {
                Health touchedHealth = touchedCollider.GetComponentInParent<Health>();
                if (touchedHealth != null && touchedHealth != myHealth && alreadyHit.Add(touchedHealth))
                {
                    touchedHealth.TakeDamage(damage, damageType, myTeam);
                }
            }
        }
        if (pauseAfterHit > 0f)
        {
            yield return new WaitForSeconds(pauseAfterHit);
        }
        IsAttacking = false;
    }
}
