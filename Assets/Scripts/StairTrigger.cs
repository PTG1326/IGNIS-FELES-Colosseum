using UnityEngine;

public class StairTrigger : MonoBehaviour
{
    // Define the "up" direction for these stairs
    public enum StairDirection { North, South, East, West }
    public StairDirection upDirection;

    [Header("Upper Level Settings")]
    public string upperSortingLayer;
    public int upperOrderInLayer;
    public string upperPhysicsLayer;

    [Header("Lower Level Settings")]
    public string lowerSortingLayer;
    public int lowerOrderInLayer;
    public string lowerPhysicsLayer;

    // We use a small buffer to prevent accidental re-triggering
    private bool canTrigger = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only trigger if we're ready and the player enters
        if (!canTrigger || !other.CompareTag("Player"))
        {
            return;
        }

        // Get the player's components
        PlayerLayerManager layerManager = other.GetComponent<PlayerLayerManager>();
        Rigidbody2D playerRb = other.GetComponent<Rigidbody2D>();

        // We can't do anything if the player is missing these
        if (layerManager == null || playerRb == null)
        {
            return;
        }

        // --- This is the Core Logic ---
        // We check the player's velocity to see which way they're moving
        
        bool isMovingUp = false;
        bool isMovingDown = false;

        switch (upDirection)
        {
            case StairDirection.North:
                isMovingUp = playerRb.linearVelocityY > 0.1f;
                isMovingDown = playerRb.linearVelocityY < -0.1f;
                break;
            case StairDirection.South:
                isMovingUp = playerRb.linearVelocityY < -0.1f;
                isMovingDown = playerRb.linearVelocityY > 0.1f;
                break;
            case StairDirection.East:
                isMovingUp = playerRb.linearVelocityX > 0.1f;
                isMovingDown = playerRb.linearVelocityX < -0.1f;
                break;
            case StairDirection.West:
                isMovingUp = playerRb.linearVelocityX < -0.1f;
                isMovingDown = playerRb.linearVelocityX > 0.1f;
                break;
        }

        // --- Apply the Layer Changes ---

        if (isMovingUp)
        {
            // Player is moving "up" the stairs
            layerManager.SetSorting(upperSortingLayer, upperOrderInLayer);
            layerManager.SetPhysicsLayer(upperPhysicsLayer);
            StartCoroutine(TriggerCooldown());
        }
        else if (isMovingDown)
        {
            // Player is moving "down" the stairs
            layerManager.SetSorting(lowerSortingLayer, lowerOrderInLayer);
            layerManager.SetPhysicsLayer(lowerPhysicsLayer);
            StartCoroutine(TriggerCooldown());
        }
        
        // If the player isn't moving strongly in either direction (e.g., diagonally),
        // we don't trigger a change.
    }

    // This stops the player from flickering layers if they stand in the trigger
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canTrigger = true;
        }
    }

    private System.Collections.IEnumerator TriggerCooldown()
    {
        canTrigger = false;
        yield return new WaitForSeconds(0.2f); // Wait for a moment
        // We don't set canTrigger back to true here, OnTriggerExit does
    }
}