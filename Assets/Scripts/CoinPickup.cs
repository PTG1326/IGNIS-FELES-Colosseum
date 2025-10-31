using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    public int coinValue = 1;
    public float lifeSpan = 10f;

    [Header("Absorption Settings")]
    public float absorptionRadius = 3f; // The normal pickup radius
    public float upgradedAbsorptionRadius = 10f; // The vacuum upgrade radius
    public float absorptionSpeed = 8f;
    
    private Transform playerTransform;
    private UpgradeManager playerUpgradeManager; 
    private bool isAbsorbing = false;
    private Rigidbody2D rb;
    private Collider2D col;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            // Get the manager from the player
            playerUpgradeManager = player.GetComponent<UpgradeManager>(); 
        }

        Destroy(gameObject, lifeSpan);
    }

    void Update()
    {
        if (playerTransform == null) return;

        // If not yet absorbing, check if player is within the correct radius
        if (!isAbsorbing)
        {
            // --- MODIFIED RADIUS CHECK ---
            float currentAbsorptionRadius = absorptionRadius; // Start with base radius

            // Check if the manager exists and the upgrade is active
            if (playerUpgradeManager != null && playerUpgradeManager.isCoinVaccuumActive)
            {
                currentAbsorptionRadius = upgradedAbsorptionRadius; // Use the bigger radius!
            }
            // --- END MODIFICATION ---

            // Check distance against the chosen radius
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer < currentAbsorptionRadius)
            {
                StartAbsorption();
            }
        }
        else // If already absorbing, move towards player
        {
            transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, absorptionSpeed * Time.deltaTime);

            // Collect if we get very close
            if (Vector2.Distance(transform.position, playerTransform.position) < 0.5f)
            {
                CollectCoin();
            }
        }
    }

    private void StartAbsorption()
    {
        isAbsorbing = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic; // Stop physics from interfering
        }
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            SoundManager.instance.PlaySound("CoinPickup");
            CollectCoin();
        }
    }

    private void CollectCoin()
    {
        if (ScoreManager.instance != null)
        {
            ScoreManager.instance.AddScore(coinValue);
        }
        Destroy(gameObject);
    }
}