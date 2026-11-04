using UnityEngine;
using UnityEngine.InputSystem;

// Goes on: Marble
// Remembers where it started, goes back there if it drops in a hole,
// shows "You Win!" in the goal cup, and R starts over.
public class MarbleBall : MonoBehaviour
{
    private Vector3 startPosition;
    private bool hasWon;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        if (transform.position.y < 1f || Keyboard.current.rKey.wasPressedThisFrame)
        {
            ResetBall();
        }
    }

    void OnTriggerEnter(Collider otherCollider)
    {
        if (otherCollider.name.StartsWith("Hole"))
        {
            ResetBall();
        }

        if (otherCollider.name == "Goal")
        {
            hasWon = true;
        }
    }

    void ResetBall()
    {
        hasWon = false;
        Rigidbody ballBody = GetComponent<Rigidbody>();
        ballBody.linearVelocity = Vector3.zero;
        ballBody.angularVelocity = Vector3.zero;
        ballBody.position = startPosition + Vector3.up;    // drop in from just above
        transform.position = startPosition + Vector3.up;
    }

    void OnGUI()
    {
        GUI.skin.label.fontSize = 48;
        if (hasWon)
        {
            GUI.Label(new Rect(20, 20, 600, 80), "You Win!  Press R");
        }
    }
}
