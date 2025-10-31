using System.Collections;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    // --- State Machine ---
    private enum State { Idle, Alert, Chasing }
    private State currentState;

    // --- References ---
    private Transform target;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool isKnockedBack = false;

    // --- AI Parameters ---
    [Header("Movement")]
    public float roamSpeed = 15f; // Adjusted for AddForce
    public float chaseSpeed = 40f; // Adjusted for AddForce
    public float detectionRadius = 8f;
    public float roamRadius = 5f;

    [Header("Behavior")]
    public int scoreValue = 10;
    public float alertAnimationTime = 0.5f;
    public int damage = 1;
    public float knockbackForce = 10f;

    [Header("On Hit Effects")]
    public float bulletKnockbackForce = 5f;
    public float flashDuration = 0.1f;

    [Header("Health")]
    public int maxHealth = 2;
    private int currentHealth;

    [Header("Materials")]
    public Material flashMaterial;
    private Material defaultMaterial;
    private Coroutine takeHitRoutine;

    [Header("Loot Drops")]
    public GameObject coinPrefab;
    public GameObject healthCoinPrefab;
    public GameObject multiShotUpgradePrefab;
    [Range(0f, 1f)]
    public float upgradeDropChance = 0.02f;
    public int minCoins = 2;
    public int maxCoins = 4;
    public float dropForce = 3f;

    [Header("Health Drop Chance")]
    [Range(0f, 1f)] // Slider in Inspector from 0% to 100%
    public float baseHealthDropChance = 0.05f; // 5% chance at score 0
    [Range(0f, 1f)]
    public float minHealthDropChance = 0.02f; // 2% chance at max score
    public int scoreToReachMinChance = 2000; // Score when max chance is reached

    [Header("Particle Effects")]
    public GameObject deathParticlePrefab;
    public Color deathParticleColor = Color.gray;


    // --- Roaming Logic ---
    private Vector2 startPosition;
    private Vector2 roamPosition;
    private float timeToNextRoam = 0f;

    // --- Animation ---
    private bool isFacingRight = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        defaultMaterial = spriteRenderer.material;
        
        currentHealth = maxHealth;
        target = GameObject.FindGameObjectWithTag("Player").transform;
        startPosition = transform.position;
        SetNewRoamPosition();
        currentState = State.Idle;
    }

    void Update()
    {
        if (isKnockedBack) return;

        switch (currentState)
        {
            case State.Idle:
                HandleIdleState();
                break;
            case State.Alert:
                // since now the enemy will start chasing
                animator.SetBool("IsChasing", true);
                break;
            case State.Chasing:
                HandleChasingState();
                break;
        }
    }

    void LateUpdate()
    {
        // --- Defensive Checks (Good Practice) ---
        if (!spriteRenderer.enabled)
        {
            spriteRenderer.enabled = true;
        }
        Vector3 pos = transform.position;
        if (pos.z != 0)
        {
            transform.position = new Vector3(pos.x, pos.y, 0);
        }
        
        // --- Animation Logic ---
        float horizontalVelocity = rb.linearVelocityX; // --- CORRECTED TYPO ---

        if (rb.linearVelocity.magnitude > 0.1f)
        {
            animator.SetBool("IsMoving", true);
        }
        else
        {
            animator.SetBool("IsMoving", false);
        }

        if (horizontalVelocity > 0.1f && !isFacingRight) Flip();
        else if (horizontalVelocity < -0.1f && isFacingRight) Flip();
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
    }

    private void HandleIdleState()
    {
        Roam();
        if (Vector2.Distance(transform.position, target.position) < detectionRadius)
        {
            currentState = State.Alert;
            StartCoroutine(AlertRoutine());
        }
    }

    private void HandleChasingState()
    {
        Vector2 direction = (target.position - transform.position).normalized;
        rb.AddForce(direction * chaseSpeed);
    }

    private void Roam()
    {
        Vector2 direction = (roamPosition - (Vector2)transform.position).normalized;
        rb.AddForce(direction * roamSpeed);
        timeToNextRoam -= Time.deltaTime;
        if (Vector2.Distance(transform.position, roamPosition) < 0.2f || timeToNextRoam < 0f)
        {
            SetNewRoamPosition();
        }
    }

    private void SetNewRoamPosition()
    {
        roamPosition = startPosition + Random.insideUnitCircle * roamRadius;
        timeToNextRoam = Random.Range(3f, 6f);
    }

    private IEnumerator AlertRoutine()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        animator.SetBool("IsMoving", false);
        yield return new WaitForSeconds(alertAnimationTime);

        currentState = State.Chasing;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (currentState == State.Chasing && collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                Vector2 knockbackDirection = (collision.transform.position - transform.position).normalized;
                playerHealth.ApplyKnockback(knockbackDirection, knockbackForce);
            }
            Destroy(gameObject);
        }
        else if (collision.gameObject.CompareTag("Bullet"))
        {
            Destroy(collision.gameObject);
            Vector2 knockbackDirection = (transform.position - collision.transform.position).normalized;
            if (takeHitRoutine != null)
            {
                StopCoroutine(takeHitRoutine);
            }
            takeHitRoutine = StartCoroutine(TakeHitRoutine(knockbackDirection));
        }
    }

    private IEnumerator TakeHitRoutine(Vector2 knockbackDirection)
    {
        isKnockedBack = true;
        rb.AddForce(knockbackDirection * bulletKnockbackForce, ForceMode2D.Impulse);

        currentHealth -= 1;
        if (currentHealth <= 0)
        {
            DropCoins();
            TryDropHealthCoin();
            TryDropUpgrade();

            if (deathParticlePrefab != null)
            {
                // Instantiate the particles
                GameObject particles = Instantiate(deathParticlePrefab, transform.position, Quaternion.identity);

                // Get the Particle System component from the instantiated object
                ParticleSystem ps = particles.GetComponent<ParticleSystem>();

                if (ps != null)
                {
                    // Get the main module to change the start color
                    var main = ps.main;
                    main.startColor = deathParticleColor; // Set the color from the enemy's variable
                }
            }

            SoundManager.instance.PlaySound("EnemyDeath");

            yield return new WaitForSeconds(0.1f);
            Destroy(gameObject);
            yield break;
        }

        SoundManager.instance.PlaySound("EnemyHit");

        // 1. Swap to the flash material assigned in the Inspector
        spriteRenderer.material = flashMaterial;

        // 2. Wait
        yield return new WaitForSeconds(flashDuration);

        // 3. Swap back to the original material we saved in Start()
        spriteRenderer.material = defaultMaterial;

        yield return new WaitForSeconds(0.1f);
        isKnockedBack = false;
        takeHitRoutine = null;
    }

    private void DropCoins()
    {
        // Determine how many coins to drop
        int coinAmount = Random.Range(minCoins, maxCoins + 1);

        for (int i = 0; i < coinAmount; i++)
        {
            // Add a small random offset to the spawn position 
            // This prevents all coins from spawning on top of each other and colliding.
            Vector2 spawnOffset = Random.insideUnitCircle * 0.3f; // Spawns within a 0.5 unit radius circle
            Vector2 spawnPosition = (Vector2)transform.position + spawnOffset;

            GameObject coin = Instantiate(coinPrefab, spawnPosition, Quaternion.identity);

            // Get its Rigidbody
            Rigidbody2D rb = coin.GetComponent<Rigidbody2D>();

            // Use a more robust random direction for the force 
            // This guarantees a perfectly random direction.
            float angle = Random.Range(0f, 2f * Mathf.PI); // Get a random angle in radians
            Vector2 randomDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            // Apply the force to make it fly out
            rb.AddForce(randomDirection * dropForce, ForceMode2D.Impulse);
        }
    }

    private void TryDropHealthCoin()
    {
        if (healthCoinPrefab == null || ScoreManager.instance == null) return;

        // 1. Calculate the current chance based on score
        float scorePercent = Mathf.InverseLerp(0, scoreToReachMinChance, ScoreManager.instance.currentScore);
        float currentChance = Mathf.Lerp(baseHealthDropChance, minHealthDropChance, scorePercent);

        // 2. Roll the dice
        float dropRoll = Random.Range(0f, 1f);

        // 3. Drop if the roll is less than the chance
        if (dropRoll < currentChance)
        {
            // Instantiate the health coin at the enemy's position
            GameObject healthCoin = Instantiate(healthCoinPrefab, transform.position, Quaternion.identity);

            // Optional: Add explosion force if using Rigidbody on the coin
            Rigidbody2D rbCoin = healthCoin.GetComponent<Rigidbody2D>();
            if (rbCoin != null)
            {
                float angle = Random.Range(0f, 2f * Mathf.PI);
                Vector2 randomDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                rbCoin.AddForce(randomDirection * dropForce, ForceMode2D.Impulse); // Use same force as gold?
            }
        }
    }
    
    private void TryDropUpgrade()
    {
        if (multiShotUpgradePrefab == null) return;

        // Roll the dice
        if (Random.Range(0f, 1f) < upgradeDropChance)
        {
            Instantiate(multiShotUpgradePrefab, transform.position, Quaternion.identity);
            // No need for explosion force, let it just sit there
        }
    }
}