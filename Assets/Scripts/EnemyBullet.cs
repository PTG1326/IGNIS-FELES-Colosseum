using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float lifetime = 5f;
    public int damage = 1; // How much damage it does to the player
    public float knockbackForce = 6f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if we hit the player
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                Vector2 knockbackDirection = (other.transform.position - transform.position).normalized;
                playerHealth.ApplyKnockback(knockbackDirection, knockbackForce);
            }
            Destroy(gameObject); // Destroy the bullet on impact
        }
        // Destroy if it hits a wall/prop
        else if (other.CompareTag("Prop"))
        {
            Destroy(gameObject);
        }
    }
}