using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [Tooltip("How much this layer follows the camera. 1 = moves with the camera (very far away, like the sky). 0 = stays put like the ground. Far layers use high numbers, near layers low numbers.")]
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