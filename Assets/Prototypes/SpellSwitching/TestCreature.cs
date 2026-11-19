using UnityEngine;
using UnityEngine.InputSystem;

// A placeholder creature for the prototype. Press its defeat key to "defeat" it.
public class TestCreature : MonoBehaviour
{
    [SerializeField] private string creatureType = "Fire Imp";
    [SerializeField] private Key defeatKey = Key.F;
    [SerializeField] private SpellTracker spellTracker;

    private void Update()
    {
        if (Keyboard.current[defeatKey].wasPressedThisFrame)
        {
            Defeat();
        }
    }

    private void Defeat()
    {
        Debug.Log(creatureType + " was defeated.");
        spellTracker.CreatureDefeated(creatureType);
    }
}