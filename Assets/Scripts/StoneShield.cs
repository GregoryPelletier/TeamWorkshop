using UnityEngine;
using UnityEngine.InputSystem;

// Attach to the Player. Press the cast key (Q by default) to raise a stone shield
// that blocks ALL damage for a few seconds. No art needed: it draws its own bubble
// unless you assign your own visual.
public class StoneShield : MonoBehaviour
{
    public Key castKey = Key.Q;
    public float duration = 3f;     // how long the shield lasts
    public float cooldown = 8f;     // seconds before it can be cast again (counted from the cast)

    [Header("Visual")]
    public GameObject shieldVisual;                       // optional: your own child object with a sprite
    public Vector2 visualOffset = new Vector2(-0.09f, -0.1f);  // raise this so the bubble sits around the body
    public float visualSize = 0.91f;                        // bubble diameter in world units
    public Color shieldColor = new Color(0.55f, 0.65f, 0.8f, 0.6f);
    public int sortingOrder = 50;   // draw order; raise it if the bubble is hidden behind the player or background

    public bool IsActive { get; private set; }
    public float CooldownRemaining { get { return Mathf.Max(0f, nextCastTime - Time.time); } }

    float endTime;
    float nextCastTime;
    float flashUntil;
    SpriteRenderer visualRenderer;

    void Start()
    {
        if (shieldVisual == null) shieldVisual = BuildDefaultVisual();
        visualRenderer = shieldVisual.GetComponentInChildren<SpriteRenderer>();
        shieldVisual.SetActive(false);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb[castKey].wasPressedThisFrame) Activate();

        if (!IsActive) return;

        float timeLeft = endTime - Time.time;
        if (timeLeft <= 0f)
        {
            Deactivate();
            return;
        }

        if (visualRenderer != null)
        {
            Color c = shieldColor;
            if (Time.time < flashUntil) c = Color.white;                                   // flash when a hit is blocked
            else if (timeLeft < 0.6f) c.a *= (Mathf.Sin(Time.time * 30f) > 0f ? 1f : 0.3f); // blink as it runs out
            visualRenderer.color = c;
        }
    }

    void Activate()
    {
        if (IsActive) return;
        if (Time.time < nextCastTime) return;   // still on cooldown

        IsActive = true;
        endTime = Time.time + duration;
        nextCastTime = Time.time + cooldown;
        shieldVisual.SetActive(true);
    }

    void Deactivate()
    {
        IsActive = false;
        shieldVisual.SetActive(false);
    }

    // The player's health script calls this when it would take damage.
    // Returns the damage that should actually be applied (0 while shielded).
    public float FilterDamage(float amount)
    {
        if (!IsActive) return amount;
        flashUntil = Time.time + 0.12f;
        return 0f;
    }

    GameObject BuildDefaultVisual()
    {
        // draw a soft circle with a solid rim, so no art is required
        int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float r = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                float a = d > 1f ? 0f : (d > 0.9f ? 1f : 0.35f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();

        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);

        var go = new GameObject("StoneShieldVisual");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(visualOffset.x, visualOffset.y, 0f);
        go.transform.localScale = Vector3.one * visualSize;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = shieldColor;
        sr.sortingOrder = sortingOrder;

        // draw on the same sorting layer as the player so the bubble is not hidden behind other layers
        var playerSprite = GetComponentInChildren<SpriteRenderer>();
        if (playerSprite != null) sr.sortingLayerID = playerSprite.sortingLayerID;
        return go;
    }
}