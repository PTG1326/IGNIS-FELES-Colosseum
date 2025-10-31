using System.Collections;
using Solana.Unity.Soar.Accounts;
using UnityEngine;

public class StompEnemyAI : MonoBehaviour
{
    // --- State Machine ---
    private enum State { Idle, Chasing, Stomping }
    private State currentState;

    // --- References ---
    private Transform target;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool isKnockedBack = false;

    // --- AI Parameters ---
    [Header("Movement")]
    public float roamSpeed = 15f;
    public float chaseSpeed = 30f;
    public float detectionRadius = 10f;
    public float roamRadius = 5f;

    [Header("Stomp Attack")]
    public float stompRadius = 4f; // How close player must be to trigger stomp
    public int stompDamage = 1;
    public float stompKnockbackForce = 15f;
    public float stompWindUpTime = 0.8f; // Time for the "tell" animation before impact
    public float stompCooldown = 2.5f;   // Time after a stomp before it can stomp again
    private bool canStomp = true;
    private Coroutine stompRoutine;

    // --- Health & Hit Effects (Same as other enemies) ---
    [Header("Health & Hit Effects")]
    public int maxHealth = 3; // Maybe he's tougher
    private int currentHealth;
    public float bulletKnockbackForce = 5f;
    public float flashDuration = 0.1f;
    public Material flashMaterial;
    private Material defaultMaterial;
    private Coroutine takeHitRoutine;

    [Header("Loot Drops")]
    public GameObject coinPrefab;
    public GameObject healthCoinPrefab;
    public GameObject coinVaccuumUpgradePrefab;
    [Range(0f, 1f)]
    public float upgradeDropChance = 0.05f;
    public int minCoins = 5;
    public int maxCoins = 8;
    public float dropForce = 3f;

    [Header("Health Drop Chance")]
    [Range(0f, 1f)] // Slider in Inspector from 0% to 100%
    public float baseHealthDropChance = 0.05f; // 5% chance at score 0
    [Range(0f, 1f)]
    public float minHealthDropChance = 0.02f; // 2% chance at max score
    public int scoreToReachMinChance = 2000; // Score when max chance is reached

    [Header("Particle Effects")]
    public GameObject stompParticlePrefab;
    public Transform stompEffectPoint;
    public GameObject deathParticlePrefab;
    public Color deathParticleColor = Color.gray;

    // --- Animation ---
    private bool isFacingRight = true;

    // --- Roaming Logic ---
    private Vector2 startPosition;
    private Vector2 roamPosition;
    private float timeToNextRoam = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        defaultMaterial = spriteRenderer.material;
        currentHealth = maxHealth;
        canStomp = true;

        target = GameObject.FindGameObjectWithTag("Player").transform;
        startPosition = transform.position;
        SetNewRoamPosition();
        currentState = State.Idle;
    }

    void Update()
    {
        if (isKnockedBack || target == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, target.position);

        // --- State Transitions ---
        switch (currentState)
        {
            case State.Idle:
                HandleIdleState(distanceToPlayer);
                break;
            case State.Chasing:
                animator.SetBool("IsChasing", true);
                HandleChasingState(distanceToPlayer);
                break;
            case State.Stomping:
                // Do nothing here; the coroutine handles the logic
                break;
        }
    }

    void LateUpdate()
    {
        if (isKnockedBack) return; // Don't flip or animate when hit
        
        // Failsafes
        if (!spriteRenderer.enabled) spriteRenderer.enabled = true;
        Vector3 pos = transform.position;
        if (pos.z != 0) transform.position = new Vector3(pos.x, pos.y, 0);

        // Animation & Flipping (Only flip if Idle or Chasing)
        if (currentState == State.Idle || currentState == State.Chasing)
        {
            float horizontalVelocity = rb.linearVelocityX;
            if (horizontalVelocity > 0.1f && !isFacingRight) Flip();
            else if (horizontalVelocity < -0.1f && isFacingRight) Flip();
        } 
    }

    // --- State Handlers ---

    private void HandleIdleState(float distanceToPlayer)
    {
        Roam();
        if (distanceToPlayer < detectionRadius)
        {
            currentState = State.Chasing;
        }
    }

    private void HandleChasingState(float distanceToPlayer)
    {
        // If player is in range AND we are ready to stomp
        if (distanceToPlayer < stompRadius && canStomp)
        {
            // Start the stomp attack
            stompRoutine = StartCoroutine(StompRoutine());
        }
        else if (distanceToPlayer >= stompRadius) // Keep chasing if outside stomp range
        {
            Vector2 direction = (target.position - transform.position).normalized;
            rb.AddForce(direction * chaseSpeed);
        }
        else // We are in stomp range, but on cooldown, so just wait
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    // --- Actions ---

    private IEnumerator StompRoutine()
    {
        currentState = State.Stomping;
        canStomp = false; // Stomp is now on cooldown
        rb.linearVelocity = Vector2.zero; // Stop moving

        // 1. Wind-up
        animator.SetTrigger("Stomp"); // Trigger the full stomp animation (wind-up + impact)
        yield return new WaitForSeconds(stompWindUpTime);

        SoundManager.instance.PlaySound("StompImpact");

        // 2. STOMP IMPACT!
        if (stompParticlePrefab != null)
        {
            Vector3 spawnPosition = (stompEffectPoint != null) ? stompEffectPoint.position : transform.position;
            Instantiate(stompParticlePrefab, spawnPosition, Quaternion.identity);
        }

        // Check if player is still in range
        float distanceToPlayer = Vector2.Distance(transform.position, target.position);
        if (distanceToPlayer <= stompRadius && target != null)
        {
            PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
            if (playerHealth != null && !playerHealth.IsDead)
            {
                // Calculate radial direction AWAY from the enemy
                Vector2 knockbackDirection = (target.position - transform.position).normalized;
                
                // Apply damage and knockback TO THE PLAYER
                playerHealth.TakeDamage(stompDamage);
                playerHealth.ApplyKnockback(knockbackDirection, stompKnockbackForce);
            }
        }

        // 3. Cooldown
        yield return new WaitForSeconds(stompCooldown); // Wait for the cooldown period
        canStomp = true;
        stompRoutine = null;
        currentState = State.Chasing; // Go back to chasing
    }

    // --- Standard Helper Methods (Copy from other enemies) ---

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
    }

    private void Roam()
    {
        Vector2 direction = (roamPosition - (Vector2)transform.position).normalized;
        rb.AddForce(direction * roamSpeed);
        timeToNextRoam -= Time.deltaTime;
        if (Vector2.Distance(transform.position, roamPosition) < 0.2f || timeToNextRoam < 0f)
            SetNewRoamPosition();
    }

    private void SetNewRoamPosition()
    {
        roamPosition = startPosition + Random.insideUnitCircle * roamRadius;
        timeToNextRoam = Random.Range(3f, 6f);
    }

    // --- Collision & Health (Same as other enemies) ---

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            Destroy(collision.gameObject);
            Vector2 knockbackDirection = (transform.position - collision.transform.position).normalized;
            if (takeHitRoutine != null) StopCoroutine(takeHitRoutine);

            // --- NEW: Interrupt stomp if hit ---
            if (stompRoutine != null)
            {
                StopCoroutine(stompRoutine);
                stompRoutine = null;
                canStomp = true; // Reset cooldown
                currentState = State.Chasing; // Go back to chasing
            }
            // --- END NEW ---

            takeHitRoutine = StartCoroutine(TakeHitRoutine(knockbackDirection));
        }
        // This enemy doesn't do contact damage, the stomp is the damage source
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
                // Instantiate the death particles
                GameObject particles = Instantiate(deathParticlePrefab, transform.position, Quaternion.identity);

                // Get the Particle System component
                ParticleSystem ps = particles.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    // Set the start color to the one defined in the Inspector
                    var main = ps.main;
                    main.startColor = deathParticleColor;
                }
            }

            SoundManager.instance.PlaySound("EnemyDeath");

            yield return new WaitForSeconds(0.1f);
            Destroy(gameObject);
            yield break;
        }
        
        SoundManager.instance.PlaySound("EnemyHit");

        // Flash Logic

        Material currentMaterial = spriteRenderer.material;
        spriteRenderer.material = flashMaterial;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.material = defaultMaterial; // Assumes no outline

        yield return new WaitForSeconds(0.1f);
        isKnockedBack = false;
        takeHitRoutine = null;
    }

    private void DropCoins()
    {
        int coinAmount = Random.Range(minCoins, maxCoins + 1);
        for (int i = 0; i < coinAmount; i++)
        {
            Vector2 spawnOffset = Random.insideUnitCircle * 0.5f;
            Vector2 spawnPosition = (Vector2)transform.position + spawnOffset;
            GameObject coin = Instantiate(coinPrefab, spawnPosition, Quaternion.identity);
            Rigidbody2D rbCoin = coin.GetComponent<Rigidbody2D>();
            float angle = Random.Range(0f, 2f * Mathf.PI);
            Vector2 randomDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            rbCoin.AddForce(randomDirection * dropForce, ForceMode2D.Impulse);
        }
    }

    private void TryDropHealthCoin()
    {
        // Check if prefab and score manager exist
        if (healthCoinPrefab == null || ScoreManager.instance == null) return;

        // 1. Calculate the current chance based on score
        float scorePercent = Mathf.InverseLerp(0, scoreToReachMinChance, ScoreManager.instance.currentScore);
        float currentChance = Mathf.Lerp(baseHealthDropChance, minHealthDropChance, scorePercent);

        // 2. Roll the dice (generate a random number between 0.0 and 1.0)
        float dropRoll = Random.Range(0f, 1f);

        // 3. Drop if the roll is less than the calculated chance
        if (dropRoll < currentChance)
        {
            // Instantiate the health coin at the enemy's position
            GameObject healthCoin = Instantiate(healthCoinPrefab, transform.position, Quaternion.identity);

            // Optional: Add explosion force if using Rigidbody on the health coin
            Rigidbody2D rbCoin = healthCoin.GetComponent<Rigidbody2D>();
            if (rbCoin != null)
            {
                float angle = Random.Range(0f, 2f * Mathf.PI);
                Vector2 randomDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                rbCoin.AddForce(randomDirection * dropForce, ForceMode2D.Impulse);
            }
        }
    }
    
    private void TryDropUpgrade()
    {
        if (coinVaccuumUpgradePrefab == null) return;

        // Roll the dice
        if (Random.Range(0f, 1f) < upgradeDropChance)
        {
            Instantiate(coinVaccuumUpgradePrefab, transform.position, Quaternion.identity);
            // No need for explosion force, let it just sit there
        }
    }
}