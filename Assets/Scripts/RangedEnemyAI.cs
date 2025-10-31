using System.Collections;
using Solana.Unity.Soar.Accounts;
using UnityEngine;

public class RangedEnemyAI : MonoBehaviour
{
    // --- State Machine ---
    private enum State { Idle, Chasing, Attacking }
    private State currentState;

    // --- References ---
    private Transform target; // The player
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private PlayerHealth playerHealth;
    private bool isKnockedBack = false;

    // --- AI Parameters ---
    [Header("Movement")]
    public float roamSpeed = 15f;
    public float chaseSpeed = 30f; // Slower than melee chase
    public float detectionRadius = 12f;
    public float shootingRadius = 7f; // Must be less than detectionRadius
    public float roamRadius = 5f;

    [Header("Attacking")]
    public GameObject enemyBulletPrefab;
    public Transform firePoint; // An empty child object where bullets spawn
    public float bulletSpeed = 8f;
    public float fireRate = 1.5f; // Time between shots
    private float nextFireTime = 0f;

    [Header("Health & Hit Effects")]
    public int maxHealth = 3;
    private int currentHealth;
    public float bulletKnockbackForce = 5f;
    public float flashDuration = 0.1f;
    public Material flashMaterial;
    private Material defaultMaterial;
    private Coroutine takeHitRoutine;

    [Header("Loot Drops")]
    public GameObject coinPrefab;
    public GameObject healthCoinPrefab;
    public GameObject speedBoostUpgradePrefab;
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
        playerHealth = target.GetComponent<PlayerHealth>();

        // --- Roaming Logic ---
        startPosition = transform.position;
        SetNewRoamPosition();
        currentState = State.Idle;

        // --- Error Check ---
        if (shootingRadius >= detectionRadius)
        {
            Debug.LogError("Shooting Radius must be less than Detection Radius on " + gameObject.name);
        }
        if (firePoint == null)
        {
             Debug.LogError("Fire Point transform is not assigned on " + gameObject.name);
        }
    }

    void Update()
    {
        if (isKnockedBack || target == null) return; // Stop if knocked back or no target

        float distanceToPlayer = Vector2.Distance(transform.position, target.position);

        // --- State Transitions ---
        switch (currentState)
        {
            case State.Idle:
                animator.SetBool("IsChasing", false);
                animator.SetBool("IsAttacking", false);
                HandleIdleState(distanceToPlayer);
                break;
            case State.Chasing:
                animator.SetBool("IsChasing", true);
                animator.SetBool("IsAttacking", false);
                HandleChasingState(distanceToPlayer);
                break;
            case State.Attacking:
                animator.SetBool("IsAttacking", true);
                HandleAttackingState(distanceToPlayer);
                break;
        }

        // --- Material/Visuals (Can use tint or icon instead) ---
        // Optional: Could change color/material based on state
        // if (currentState == State.Chasing || currentState == State.Attacking)
        //     spriteRenderer.color = Color.yellow; // Example tint
        // else
        //     spriteRenderer.color = defaultColor; // Assuming you store defaultColor in Start()
    }

    void LateUpdate()
    {
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
        } else {
             // Optional: Face the player when attacking
             FacePlayer();
        }
    }
    
    // --- State Handlers ---

    private void HandleIdleState(float distanceToPlayer)
    {
        Roam();
        // If player enters detection radius, start chasing
        if (distanceToPlayer < detectionRadius)
        {
            currentState = State.Chasing;
        }
    }

    private void HandleChasingState(float distanceToPlayer)
    {
        // If player gets too far, go back to idle
        if (distanceToPlayer >= detectionRadius)
        {
            currentState = State.Idle;
            return;
        }
        // If player enters shooting radius, start attacking
        if (distanceToPlayer < shootingRadius)
        {
            currentState = State.Attacking;
            rb.linearVelocity = Vector2.zero; // Stop moving
            return;
        }

        // Move towards the player
        Vector2 direction = (target.position - transform.position).normalized;
        rb.AddForce(direction * chaseSpeed);
    }

     private void HandleAttackingState(float distanceToPlayer)
    {
        // Stop moving
        rb.linearVelocity = Vector2.zero;

        // If player moves outside shooting radius, chase again
        if (distanceToPlayer >= shootingRadius)
        {
            currentState = State.Chasing;
            return;
        }

        // Optional: Face the player while attacking
        FacePlayer();

        // Check if it's time to fire
        if (Time.time >= nextFireTime)
        {
            Shoot();
            // Set the time for the next shot
            nextFireTime = Time.time + fireRate;
        }
    }

    // --- Actions ---

    private void Shoot()
    {
        if (enemyBulletPrefab == null || firePoint == null) return;

        // Calculate direction to player from the fire point
        Vector2 direction = (target.position - firePoint.position).normalized;

        // Calculate rotation
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion bulletRotation = Quaternion.Euler(0, 0, angle); // Assuming bullet sprite faces right

        // Instantiate the bullet
        GameObject bullet = Instantiate(enemyBulletPrefab, firePoint.position, bulletRotation);
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();

        // Apply velocity
        bulletRb.linearVelocity = direction * bulletSpeed;
    }

    private void FacePlayer()
    {
         Vector2 directionToPlayer = target.position - transform.position;
         if (directionToPlayer.x > 0 && !isFacingRight)
         {
             Flip();
         }
         else if (directionToPlayer.x < 0 && isFacingRight)
         {
             Flip();
         }
    }

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
        {
            SetNewRoamPosition();
        }
    }

    private void SetNewRoamPosition()
    {
        roamPosition = startPosition + Random.insideUnitCircle * roamRadius;
        timeToNextRoam = Random.Range(3f, 6f);
    }

    // --- Collision & Health --- (Mostly copied from Melee Enemy)

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Enemy bullets should be triggers, so we check OnTriggerEnter2D for them
        // This only handles collision with player or player bullets

        if (collision.gameObject.CompareTag("Player"))
        {
            // Calculate knockback direction AWAY from the player
            Vector2 knockbackDirection = (transform.position - collision.transform.position).normalized;

            // Stop any existing hit routine to prevent conflicts
            if (takeHitRoutine != null) StopCoroutine(takeHitRoutine);

            // Start the routine that handles knockback, flashing, AND health reduction
            // Pass a specific knockback force for player collision if desired,
            // otherwise, use the existing bulletKnockbackForce or a new variable.
            // Let's use bulletKnockbackForce for simplicity here.
            takeHitRoutine = StartCoroutine(TakeHitRoutine(knockbackDirection));

            // Apply a small knockback force TO the player as well
            if (playerHealth != null)
            {
                // Use a smaller force for bumping into the enemy
                playerHealth.ApplyKnockback(-knockbackDirection, bulletKnockbackForce * 0.7f); // Knock player back
            }
        
        }
        else if (collision.gameObject.CompareTag("Bullet")) // Check your player bullet tag
        {
            Destroy(collision.gameObject);
            Vector2 knockbackDirection = (transform.position - collision.transform.position).normalized;
            if (takeHitRoutine != null) StopCoroutine(takeHitRoutine);
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
            DropCoins(); // Call coin drop function
            TryDropHealthCoin(); // Call health coin drop function
            TryDropUpgrade(); // Call upgrade drop function

            // Particles

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

        // Flash Logic (Using defaultMaterial and flashMaterial)
        Material currentMaterial = spriteRenderer.material; // Store whatever material we have
        spriteRenderer.material = flashMaterial;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.material = currentMaterial; // Restore it

        yield return new WaitForSeconds(0.1f);
        isKnockedBack = false;
        takeHitRoutine = null;
    }

    // --- Loot Drop --- (Copied from Melee Enemy)
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
        if (speedBoostUpgradePrefab == null) return;

        // Roll the dice
        if (Random.Range(0f, 1f) < upgradeDropChance)
        {
            Instantiate(speedBoostUpgradePrefab, transform.position, Quaternion.identity);
            // No need for explosion force, let it just sit there
        }
    }
}