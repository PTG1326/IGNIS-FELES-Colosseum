using UnityEngine;

public class UpgradePickup : MonoBehaviour
{
    public UpgradeType upgradeType = UpgradeType.MultiShot; // --- NEW: Specify type in Inspector ---
    public float duration = 10f; // How long the upgrade lasts
    public Sprite upgradeIcon; // The sprite to show in the UI

    void Start()
    {
        Destroy(gameObject, 15f); // Auto-destroy if not picked up
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            UpgradeManager manager = other.GetComponent<UpgradeManager>();

            SoundManager.instance.PlaySound("UpgradePickup");

            if (manager != null)
            {
                // --- MODIFIED: Call generic ActivateUpgrade method ---
                manager.ActivateUpgrade(upgradeType, duration, upgradeIcon);
                // --- END MODIFIED ---

                Destroy(gameObject); // Destroy the pickup
            }
        }
    }
}