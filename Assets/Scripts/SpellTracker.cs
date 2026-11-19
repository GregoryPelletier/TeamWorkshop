using UnityEngine;

// Keeps track of which spells the player owns and which one is selected.
// This is the one place a HUD or a save system would read from.
public class SpellTracker : MonoBehaviour
{
    // Each level fills these in on the Player. A spell can be unlocked by a creature (Source Creatures)
    // or by a spell orb (SpellPickup calls UnlockSpell).
    [SerializeField] private string[] spellNames = { "Magic Bolt", "Fireball", "Rock Throw" };
    [SerializeField] private string[] sourceCreatures = { "", "Fire Imp", "Stone Golem" };
    [SerializeField] private bool[] spellUnlocked = { true, false, false };
    private int selectedIndex = 0;

    // Called by a creature when it is defeated.
    public void CreatureDefeated(string creatureType)
    {
        for (int loopIndex = 0; loopIndex < sourceCreatures.Length; loopIndex++)
        {
            if (sourceCreatures[loopIndex] == creatureType)
            {
                if (spellUnlocked[loopIndex])
                {
                    Debug.Log("Duplicate ignored: " + spellNames[loopIndex] + " is already unlocked from " + creatureType + ".");
                }
                else
                {
                    spellUnlocked[loopIndex] = true;
                    Debug.Log("Spell stolen: " + spellNames[loopIndex] + " from " + creatureType + "!");
                }
            }
        }
    }

    // Called by a spell orb. Unlocks the spell with this name and selects it. Returns false if there is no such spell.
    public bool UnlockSpell(string spellName)
    {
        for (int loopIndex = 0; loopIndex < spellNames.Length; loopIndex++)
        {
            if (spellNames[loopIndex] == spellName)
            {
                spellUnlocked[loopIndex] = true;
                selectedIndex = loopIndex;
                Debug.Log("Spell unlocked: " + spellName);
                return true;
            }
        }
        return false;
    }

    // Called when the player loses a spell (health too low).
    public void LockSpell(string spellName)
    {
        for (int loopIndex = 0; loopIndex < spellNames.Length; loopIndex++)
        {
            if (spellNames[loopIndex] == spellName)
            {
                spellUnlocked[loopIndex] = false;
            }
        }
        if (HasAnyUnlocked() && !spellUnlocked[selectedIndex])
        {
            SelectNextSpell(1);
        }
    }

    public bool HasAnyUnlocked()
    {
        for (int loopIndex = 0; loopIndex < spellUnlocked.Length; loopIndex++)
        {
            if (spellUnlocked[loopIndex])
            {
                return true;
            }
        }
        return false;
    }

    // The selected spell's name, or "" if it is locked.
    public string SelectedSpellName()
    {
        if (selectedIndex < 0 || selectedIndex >= spellNames.Length || !spellUnlocked[selectedIndex])
        {
            return "";
        }
        return spellNames[selectedIndex];
    }

    // Called by keys 1 to 3. spellIndex is 0, 1 or 2.
    public void SelectSpell(int spellIndex)
    {
        if (spellIndex < 0 || spellIndex >= spellNames.Length)
        {
            return;
        }
        if (!spellUnlocked[spellIndex])
        {
            Debug.Log(spellNames[spellIndex] + " is locked.");
            return;
        }
        selectedIndex = spellIndex;
        Debug.Log("Selected spell: " + spellNames[selectedIndex]);
    }

    // Called by the scroll wheel. stepDirection is 1 for up or -1 for down.
    public void SelectNextSpell(int stepDirection)
    {
        if (!HasAnyUnlocked())
        {
            return;   // nothing to pick (this would loop forever)
        }
        int nextIndex = selectedIndex;

        do
        {
            nextIndex = nextIndex + stepDirection;

            if (nextIndex >= spellNames.Length)
            {
                nextIndex = 0;
            }
            else if (nextIndex < 0)
            {
                nextIndex = spellNames.Length - 1;
            }
        }
        while (!spellUnlocked[nextIndex]);

        selectedIndex = nextIndex;
        Debug.Log("Selected spell: " + spellNames[selectedIndex]);
    }

    private void OnGUI()
    {
        GUI.skin.label.fontSize = 28;

        for (int loopIndex = 0; loopIndex < spellNames.Length; loopIndex++)
        {
            string linePrefix = "";
            if (loopIndex == selectedIndex)
            {
                linePrefix = "> ";
            }

            string spellStatus = "(Locked)";
            if (spellUnlocked[loopIndex])
            {
                spellStatus = "(Unlocked)";
            }

            string labelText = linePrefix + spellNames[loopIndex] + " " + spellStatus;
            GUI.Label(new Rect(20, 20 + loopIndex * 35, 500, 35), labelText);
        }
    }
}
