using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    // The object our camera will follow (the player).
    public Transform target;

    // The offset of the camera from the player.
    public Vector3 offset = new Vector3(0, 0, -10);

    // We still use LateUpdate to run after the player has moved.
    void LateUpdate()
    {
        // Check if a target has been assigned.
        if (target != null)
        {
            // --- THIS IS THE FIX ---
            // We NO LONGER use Lerp. We just snap the camera
            // to the target's (already interpolated) position.
            transform.position = target.position + offset;
        }
    }
}