using UnityEngine;
using UnityEngine.InputSystem;

public class SpellInput : MonoBehaviour
{
    [SerializeField] 
    private InputActionReference castAction;
    [SerializeField]
    private InputActionReference spellOneAction;
    [SerializeField]
    private InputActionReference spellTwoAction;
    [SerializeField]
    private InputActionReference spellThreeAction;
    [SerializeField]
    private InputActionReference scrollAction;
    [SerializeField]
    private SpellTracker spellTracker;
    // The casters for each spell (leave empty for spells this level does not have).
    [SerializeField]
    private FireballCaster fireballCaster;
    [SerializeField]
    private RainCaster rainCaster;
    private void OnEnable()
    {
        castAction.action.Enable();
        spellOneAction.action.Enable();
        spellTwoAction.action.Enable();
        spellThreeAction.action.Enable();
        scrollAction.action.Enable();
    }

    private void OnDisable()
    {
        castAction.action.Disable();
        spellOneAction.action.Disable();
        spellTwoAction.action.Disable();
        spellThreeAction.action.Disable();
        scrollAction.action.Disable();

    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    //void Start()
    //{
        
    //}

    // Update is called once per frame
    private void Update()
    {
        // Right mouse button: cast the spell picked with the mouse wheel (or keys 1 to 3)
        if (castAction.action.WasPressedThisFrame())
        {
            CastSelectedSpell();
        }

        // B, N and M each cast their own spell: the Level 1, Level 2 and Level 3 spell
        Keyboard keyboardInput = Keyboard.current;
        if (keyboardInput != null)
        {
            if (keyboardInput.bKey.wasPressedThisFrame)
            {
                CastSpellInSlot(0);
            }
            if (keyboardInput.nKey.wasPressedThisFrame)
            {
                CastSpellInSlot(1);
            }
            if (keyboardInput.mKey.wasPressedThisFrame)
            {
                CastSpellInSlot(2);
            }
        }

        if (spellOneAction.action.WasPressedThisFrame())
        {
            Debug.Log("Spell one selected");
            spellTracker.SelectSpell(0);
        }

        if (spellTwoAction.action.WasPressedThisFrame())
        {
            Debug.Log("Spell two selected");
            spellTracker.SelectSpell(1);
        }

        if (spellThreeAction.action.WasPressedThisFrame())
        {
            Debug.Log("Spell three selected");
            spellTracker.SelectSpell(2);
        }

        float scrollValue = scrollAction.action.ReadValue<float>();
        if (scrollValue > 0)
        {
            spellTracker.SelectNextSpell(1);
            Debug.Log("Scroll up");
        }
        else if (scrollValue < 0)
        {
            spellTracker.SelectNextSpell(-1);
            Debug.Log("Scroll down");
        }
    }

    // Casts whichever spell the SpellTracker has selected.
    private void CastSelectedSpell()
    {
        string selectedSpell = spellTracker.SelectedSpellName();
        if (selectedSpell == "")
        {
            GameMessage.Show("You have not learned a spell yet. Defeat the mini boss to get one.");
            return;
        }
        CastNamedSpell(selectedSpell);
    }

    // Casts the spell in this slot (0 = Level 1 spell, 1 = Level 2 spell, 2 = Level 3 spell) if it is learned.
    private void CastSpellInSlot(int spellSlot)
    {
        string slotSpell = spellTracker.SpellNameIfUnlocked(spellSlot);
        if (slotSpell == "")
        {
            GameMessage.Show("You have not learned that spell yet.");
            return;
        }
        CastNamedSpell(slotSpell);
    }

    private void CastNamedSpell(string spellName)
    {
        if (spellName == "Fireball" && fireballCaster != null)
        {
            fireballCaster.Cast();
        }
        else if (spellName == "Rain" && rainCaster != null)
        {
            rainCaster.Cast();
        }
    }
}
