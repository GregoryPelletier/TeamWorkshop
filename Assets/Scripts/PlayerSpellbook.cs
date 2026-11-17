using UnityEngine;
using UnityEngine.InputSystem;

// Put this on the Player. It remembers which spells the player has learned and casts them.
//   B = Bone Spear (only after picking up the spell from the mini boss).
// Once you have the spell, it shows in the top left corner of the screen.
// If your health drops to 20% you lose the spell, and the orb goes back to where you found it.
public class PlayerSpellbook : MonoBehaviour
{
    [Header("Bone Spear")]
    public bool knowsBoneSpear = false;
    public Sprite boneSpearSprite;
    public float boneSpearCooldown = 0.8f;
    public Vector2 castOffset = new Vector2(0.4f, 0.05f);
    [Tooltip("Lose the spell when health drops to this share of full health (0.2 = 20%).")]
    public float loseSpellAtHealth = 0.2f;

    float nextCastTime;
    string messageText = "";
    float messageTimer;
    PlayerFighter playerFighter;
    SpellPickup learnedFrom;   // the orb the spell came from, so it can go back there
    Collider2D playerCollider;

    void Awake()
    {
        playerFighter = GetComponent<PlayerFighter>();
        playerCollider = GetComponent<Collider2D>();
    }

    // Called by the spell orb when the player walks into it.
    public void LearnBoneSpear(SpellPickup fromOrb = null)
    {
        learnedFrom = fromOrb;
        knowsBoneSpear = true;
        ShowMessage("You learned Bone Spear! Press B to cast it.");
    }

    // True when health is too low to hold a spell.
    public bool HealthTooLow()
    {
        if (playerFighter == null)
        {
            return false;
        }
        return playerFighter.HitsLeft <= playerFighter.hitsToKill * loseSpellAtHealth;
    }

    void LoseBoneSpear()
    {
        knowsBoneSpear = false;
        if (learnedFrom != null)
        {
            learnedFrom.ReturnToWhereFound();
            ShowMessage("Your health is too low! The Bone Spear went back to the spell room.");
        }
        else
        {
            ShowMessage("Your health is too low! You lost the Bone Spear.");
        }
    }

    public void ShowMessage(string newMessage)
    {
        messageText = newMessage;
        messageTimer = 4f;
    }

    void Update()
    {
        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
        }
        if (playerFighter != null && playerFighter.IsDefeated)
        {
            return;
        }
        if (knowsBoneSpear && HealthTooLow())
        {
            LoseBoneSpear();
        }

        Keyboard keyboardInput = Keyboard.current;
        bool castPressed = keyboardInput != null && keyboardInput.bKey.wasPressedThisFrame;
        if (!castPressed)
        {
            return;
        }

        if (!knowsBoneSpear)
        {
            ShowMessage("You have not learned a spell yet. Defeat the mini boss to get one.");
            return;
        }
        if (Time.time < nextCastTime)
        {
            return;
        }
        nextCastTime = Time.time + boneSpearCooldown;
        CastBoneSpear();
    }

    void CastBoneSpear()
    {
        float facingDirection = transform.localScale.x < 0f ? -1f : 1f;
        Vector3 startPoint = playerCollider != null ? playerCollider.bounds.center : transform.position;
        startPoint += new Vector3(castOffset.x * facingDirection, castOffset.y, 0f);

        GameObject spearObject = new GameObject("Bone Spear");
        spearObject.transform.position = startPoint;
        SpriteRenderer spearSprite = spearObject.AddComponent<SpriteRenderer>();
        spearSprite.sprite = boneSpearSprite;
        SpriteRenderer playerSprite = GetComponent<SpriteRenderer>();
        if (playerSprite != null)
        {
            spearSprite.sortingLayerID = playerSprite.sortingLayerID;
            spearSprite.sortingOrder = playerSprite.sortingOrder + 1;
        }
        spearObject.AddComponent<BoneSpear>().Launch(facingDirection);

        if (playerFighter != null)
        {
            playerFighter.PlayFightAnimation(playerFighter.castAnimation);
        }
    }

    void DrawSpellIcon()
    {
        Rect boxRect = new Rect(16f, 16f, 150f, 74f);
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(boxRect, Texture2D.whiteTexture);

        // the spear picture, dimmed while it is cooling down
        bool spellReady = Time.time >= nextCastTime;
        GUI.color = spellReady ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        if (boneSpearSprite != null)
        {
            Texture2D spearTexture = boneSpearSprite.texture;
            Rect pictureArea = boneSpearSprite.textureRect;
            Rect texCoords = new Rect(pictureArea.x / spearTexture.width, pictureArea.y / spearTexture.height,
                                      pictureArea.width / spearTexture.width, pictureArea.height / spearTexture.height);
            float drawWidth = boxRect.width - 20f;
            float drawHeight = drawWidth * pictureArea.height / pictureArea.width;
            GUI.DrawTextureWithTexCoords(new Rect(boxRect.x + 10f, boxRect.y + 8f, drawWidth, drawHeight), spearTexture, texCoords);
        }

        GUI.color = Color.white;
        GUIStyle nameText = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.LowerCenter };
        GUI.Label(new Rect(boxRect.x, boxRect.y, boxRect.width, boxRect.height - 4f), "Bone Spear  [B]", nameText);
    }

    void OnGUI()
    {
        // The spell in the top left corner, only once you have it.
        if (knowsBoneSpear)
        {
            DrawSpellIcon();
        }

        if (messageTimer > 0f)
        {
            GUIStyle bigText = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(0f, 100f, Screen.width, 40f), messageText, bigText);
        }
    }
}
