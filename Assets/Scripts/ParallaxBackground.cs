using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [Tooltip("0 = Moves with camera (Sky). 1 = Stationary (Foreground). 0.5 = Half speed.")]
    [SerializeField] private Vector2 parallaxMultiplier;

    private Transform cameraTransform;
    private Vector3 lastCameraPosition;

    void Start()
    {
        cameraTransform = Camera.main.transform;
        lastCameraPosition = cameraTransform.position;
    }

    void LateUpdate()
    {
        // Calculate how far the camera has moved since the last frame
        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;

        // Move the background by that distance, scaled by the multiplier
        transform.position += new Vector3(deltaMovement.x * parallaxMultiplier.x, deltaMovement.y * parallaxMultiplier.y, 0);

        // Update the last camera position for the next frame
        lastCameraPosition = cameraTransform.position;
    }
}