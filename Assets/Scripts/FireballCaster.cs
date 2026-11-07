using UnityEngine;
using UnityEngine.InputSystem;

// Attach to the Player. Press F or left mouse button to cast.
public class FireballCaster : MonoBehaviour
{
    public GameObject fireballPrefab;
    public float cooldown = 0.6f;
    public float manaCost = 20f;
    public float maxMana = 100f;
    public float manaRegenPerSecond = 10f;
    public Vector2 spawnOffset = new Vector2(0.6f, 0.2f);

    // Seconds after pressing F before the fireball appears (match the moment the staff fires).
    // Set to -1 to spawn from an Animation Event instead (the event calls SpawnFireball).
    public float spawnDelay = 0.5f;

    public float Mana { get; private set; }
    float nextCastTime;
    float facing = 1f;   // 1 = right, -1 = left
    Animator animator;

    void Start()
    {
        Mana = maxMana;
        animator = GetComponent<Animator>();
        if (animator == null) Debug.LogWarning("FireballCaster: no Animator on this object, cast animation will not play.");
        if (fireballPrefab == null) Debug.LogWarning("FireballCaster: Fireball Prefab slot is empty.");
    }

    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null) return;

        if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) facing = -1f;
        if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) facing = 1f;

        Mana = Mathf.Min(maxMana, Mana + manaRegenPerSecond * Time.deltaTime);

        bool castPressed = kb.fKey.wasPressedThisFrame ||
                           (mouse != null && mouse.leftButton.wasPressedThisFrame);
        if (castPressed) Cast();
    }

    void Cast()
    {
        if (Time.time < nextCastTime) return;
        if (Mana < manaCost) return;

        Mana -= manaCost;
        nextCastTime = Time.time + cooldown;

        if (animator != null) animator.SetTrigger("Cast");   // plays AttackCastBall

        if (spawnDelay >= 0f) Invoke(nameof(SpawnFireball), spawnDelay);
        // if spawnDelay is -1, the Animation Event calls SpawnFireball()
    }

    // Public so an Animation Event can call it on the exact frame.
    public void SpawnFireball()
    {
        if (fireballPrefab == null) return;
        Vector3 pos = transform.position + new Vector3(spawnOffset.x * facing, spawnOffset.y, 0f);
        GameObject go = Instantiate(fireballPrefab, pos, Quaternion.identity);
        go.GetComponent<Fireball>().Init(new Vector2(facing, 0f));
    }
}