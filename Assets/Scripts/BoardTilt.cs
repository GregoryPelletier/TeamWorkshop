using UnityEngine;
using UnityEngine.InputSystem;

// Goes on: Board
// Tilts the board with the arrow keys.
public class BoardTilt : MonoBehaviour
{
    private float maxTilt = 10f;     // degrees
    private float tiltSpeed = 15f;   // degrees per second (faster flings the marble over the walls)

    void FixedUpdate()
    {
        float tiltX = 0f;
        float tiltZ = 0f;

        if (Keyboard.current.upArrowKey.isPressed)
        {
            tiltX = maxTilt;
        }

        if (Keyboard.current.downArrowKey.isPressed)
        {
            tiltX = -maxTilt;
        }

        if (Keyboard.current.leftArrowKey.isPressed)
        {
            tiltZ = maxTilt;
        }

        if (Keyboard.current.rightArrowKey.isPressed)
        {
            tiltZ = -maxTilt;
        }

        Rigidbody boardBody = GetComponent<Rigidbody>();
        Quaternion targetTilt = Quaternion.Euler(tiltX, 0f, tiltZ);
        boardBody.MoveRotation(Quaternion.RotateTowards(boardBody.rotation, targetTilt, tiltSpeed * Time.fixedDeltaTime));
    }
}
