using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 5;
    public int currentHealth;
    public bool IsDead { get; private set; } = false;
    public bool isKnockedBack { get; private set; } = false;
    private Rigidbody2D rb;

    void Start()
    {
        SetRandomSpawnPosition();

        SoundManager.instance.PlayMusic("Game");

        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
    }
    
    private void SetRandomSpawnPosition()
    {

        Random.InitState(System.DateTime.Now.Millisecond);

        // 1. Find all GameObjects tagged "SpawnPoint"
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");

        // 2. Check if any spawn points were found
        if (spawnPoints.Length > 0)
        {
            // 3. Pick a random one
            int randomIndex = Random.Range(0, spawnPoints.Length);
            Transform chosenSpawnPoint = spawnPoints[randomIndex].transform;

            // 4. Set the player's position to the chosen spawn point
            transform.position = chosenSpawnPoint.position;
            Debug.Log($"Player spawned at: {chosenSpawnPoint.name} ({chosenSpawnPoint.position})");
        }
        else
        {
            Debug.LogWarning("No GameObjects with the tag 'SpawnPoint' found. Player will spawn at original position.");
        }
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || isKnockedBack) return; 
        currentHealth -= damage;
        Debug.Log($"Player took {damage} damage! Current health: {currentHealth}");

        SoundManager.instance.PlaySound("PlayerHit");

        if (currentHealth <= 0)
        {
            SoundManager.instance.PlaySound("PlayerDeath");
            IsDead = true; 
            rb.linearVelocity = Vector2.zero; 
            Debug.Log("Player has been defeated!");
            StartCoroutine(DieAndLoadGameOver());
        }
    }

    public void ApplyKnockback(Vector2 direction, float force)
    {
        if (isKnockedBack) return; 
        StartCoroutine(KnockbackRoutine(direction, force, 0.2f)); 
    }
    
    private IEnumerator KnockbackRoutine(Vector2 direction, float force, float duration)
    {
        isKnockedBack = true;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direction * force, ForceMode2D.Impulse);
        if (CameraShake.instance != null)
        {
            CameraShake.instance.Shake(force); // Pass the knockback force to the shake method
        }
        
        yield return new WaitForSeconds(duration);
        isKnockedBack = false;
    }

    // Keep this version for compatibility if needed
    public IEnumerator ApplyKnockback(Vector2 direction, float force, float duration)
    {
        yield return StartCoroutine(KnockbackRoutine(direction, force, duration));
    }

    private IEnumerator DieAndLoadGameOver()
    {
        if (Fader.instance != null)
            yield return Fader.instance.FadeOut();
        SceneManager.LoadScene("GameOverScene");
    }
}