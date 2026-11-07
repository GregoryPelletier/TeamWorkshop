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
        if (castAction.action.WasPressedThisFrame())
        {
            Debug.Log("Cast pressed");
        }

        if (spellOneAction.action.WasPressedThisFrame())
        {
            Debug.Log("Spell one selected");
        }

        if (spellTwoAction.action.WasPressedThisFrame())
        {
            Debug.Log("Spell two selected");
        }

        if (spellThreeAction.action.WasPressedThisFrame())
        {
            Debug.Log("Spell three selected");
        }

        float scrollValue = scrollAction.action.ReadValue<float>();
        if (scrollValue > 0)
        {
            Debug.Log("Scroll up");
        }
        else if (scrollValue < 0)
        {
            Debug.Log("Scroll down");
        }
    }
}
