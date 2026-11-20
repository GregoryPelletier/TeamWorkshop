using UnityEngine;
using UnityEngine.InputSystem;

// Attach to the Player. Press F or left mouse button to cast.
// (In the levels SpellInput does the casting, so Listen For Keys is turned off there.)
public class FireballCaster : MonoBehaviour
{
    public GameObject fireballPrefab;
    public float cooldown = 0.6f;
    [Tooltip("Seconds after casting before the player can attack with the staff or cast again.")]
    public float castLockTime = 0.6f;
    public float manaCost = 20f;
    public float maxMana = 100f;
    public float manaRegenPerSecond = 10f;
    public Vector2 spawnOffset = new Vector2(0.6f, 0.2f);
    [Tooltip("Cast with F or the left mouse button. Turn off when SpellInput does the casting.")]
    public bool listenForKeys = true;

    // Seconds after pressing F before the fireball appears (match the moment the staff fires).
    // Set to -1 to spawn from an Animation Event instead (the event calls SpawnFireball).
    public float spawnDelay = 0.5f;

    public float Mana { get; private set; }
    float nextCastTime;
    float facingDirection = 1f;   // 1 = right, -1 = left
    Animator castAnimator;

    void Start()
    {
        Mana = maxMana;
        castAnimator = GetComponent<Animator>();
        if (castAnimator == null)
        {
            Debug.LogWarning("FireballCaster: no Animator on this object, cast animation will not play.");
        }
        if (fireballPrefab == null)
        {
            Debug.LogWarning("FireballCaster: Fireball Prefab slot is empty.");
        }
    }

    void Update()
    {
        var keyboardInput = Keyboard.current;
        var mouseInput = Mouse.current;
        if (keyboardInput == null)
        {
            return;
        }

        if (keyboardInput.leftArrowKey.isPressed || keyboardInput.aKey.isPressed)
        {
            facingDirection = -1f;
        }
        if (keyboardInput.rightArrowKey.isPressed || keyboardInput.dKey.isPressed)
        {
            facingDirection = 1f;
        }

        Mana = Mathf.Min(maxMana, Mana + manaRegenPerSecond * Time.deltaTime);

        if (!listenForKeys)
        {
            return;
        }
        bool castPressed = keyboardInput.fKey.wasPressedThisFrame ||
                           (mouseInput != null && mouseInput.leftButton.wasPressedThisFrame);
        if (castPressed)
        {
            Cast();
        }
    }

    // Public so SpellInput can cast the fireball when it is the selected spell.
    public void Cast()
    {
        if (Time.time < nextCastTime)
        {
            return;
        }
        if (Mana < manaCost)
        {
            return;
        }
        Health casterHealth = GetComponent<Health>();
        if (casterHealth != null && casterHealth.IsDefeated)
        {
            return;
        }
        // no casting in the middle of a staff attack (stops switching between the two to attack faster)
        MeleeAttack staffAttack = GetComponent<MeleeAttack>();
        if (staffAttack != null && !staffAttack.FreeToAct())
        {
            return;
        }

        Mana -= manaCost;
        nextCastTime = Time.time + cooldown;
        if (staffAttack != null)
        {
            staffAttack.BusyFor(castLockTime);
        }

        // Only use the Cast trigger if the Animator has one (CharacterLooks shows the cast pose otherwise).
        if (castAnimator != null)
        {
            foreach (AnimatorControllerParameter animatorParameter in castAnimator.parameters)
            {
                if (animatorParameter.name == "Cast")
                {
                    castAnimator.SetTrigger("Cast");   // plays AttackCastBall
                }
            }
        }
        CharacterLooks casterLooks = GetComponent<CharacterLooks>();
        if (casterLooks != null)
        {
            casterLooks.PlayCast();
        }

        if (spawnDelay >= 0f)
        {
            Invoke(nameof(SpawnFireball), spawnDelay);
        }
        // if spawnDelay is -1, the Animation Event calls SpawnFireball()
    }

    // Public so an Animation Event can call it on the exact frame.
    public void SpawnFireball()
    {
        if (fireballPrefab == null)
        {
            return;
        }
        Vector3 spawnPosition = transform.position + new Vector3(spawnOffset.x * facingDirection, spawnOffset.y, 0f);
        GameObject fireballObject = Instantiate(fireballPrefab, spawnPosition, Quaternion.identity);
        // draw the fireball on the same layer as the caster so it is not hidden behind the level
        SpriteRenderer casterSprite = GetComponent<SpriteRenderer>();
        SpriteRenderer fireballSprite = fireballObject.GetComponent<SpriteRenderer>();
        if (casterSprite != null && fireballSprite != null)
        {
            fireballSprite.sortingLayerID = casterSprite.sortingLayerID;
            fireballSprite.sortingOrder = casterSprite.sortingOrder + 1;
        }
        fireballObject.GetComponent<Fireball>().Init(new Vector2(facingDirection, 0f));
    }
}
