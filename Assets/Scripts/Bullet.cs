using UnityEngine;

public class Bullet : MonoBehaviour
{
    // How long the bullet exists before being destroyed.
    public float lifetime = 3f;

    // Awake is called when the script instance is being loaded.
    void Awake()
    {
        // Destroy the bullet GameObject after 'lifetime' seconds.
        Destroy(gameObject, lifetime);
    }

    // Destroy bullet when hitting a wall
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Enemy"))
        {
            Destroy(gameObject);
        }
    }
}
