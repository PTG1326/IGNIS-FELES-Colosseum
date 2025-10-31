using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    public int healthValue = 1;
    public float lifeSpan = 15f; // How long it stays before disappearing

    void Start()
    {
        // Destroy after a while if not collected
        Destroy(gameObject, lifeSpan);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

            // Check if player exists and is NOT at full health
            if (playerHealth != null && playerHealth.currentHealth < playerHealth.maxHealth)
            {
                // Heal the player (use negative damage)
                playerHealth.TakeDamage(-healthValue);

                // Optional: Play a pickup sound effect here
                SoundManager.instance.PlaySound("HealthPickup");

                // Destroy the health coin
                Destroy(gameObject);
            }
            // If player is at full health, the coin just sits there until lifespan ends
        }
    }
}