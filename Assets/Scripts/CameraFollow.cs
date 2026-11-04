using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;
    // Maintains the -10 Z position so the camera stays in front of the 2D scene, 
    // and adds a slight 1 unit vertical lift so it isn't staring exactly at the player's feet.
    public Vector3 offset = new Vector3(0f, 1f, -10f);

    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 desiredPosition = target.position + offset;
            // Lerp smooths the transition between the current position and the target position
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        }
    }
}