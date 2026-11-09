using UnityEngine;

// Keeps track of which spells the player owns and which one is selected.
// This is the one place a HUD or a save system would read from.
public class SpellTracker : MonoBehaviour
{
    // Slot 0 is unlocked from the start. Slots 1 and 2 are stolen from creatures.
    // Rename the spells to match the pitch if they differ.
    private string[] spellNames = { "Magic Bolt", "Fireball", "Rock Throw" };
    private string[] sourceCreatures = { "", "Fire Imp", "Stone Golem" };
    private bool[] spellUnlocked = { true, false, false };
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

    // Called by keys 1 to 3. spellIndex is 0, 1 or 2.
    public void SelectSpell(int spellIndex)
    {
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