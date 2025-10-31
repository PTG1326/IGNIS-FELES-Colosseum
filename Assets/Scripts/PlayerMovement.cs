using UnityEngine;

[RequireComponent(typeof(PlayerHealth))] // Ensures we have the health script
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 75f;  // Acceleration force
    public float maxSpeed = 10f;   // Top speed
    public float brakeFactor = 5f; // How fast you stop

    private Rigidbody2D rb;
    private Animator animator;
    public Vector2 movementInput;
    private float baseMaxSpeed;
    private float baseMoveSpeed;

    private PlayerHealth playerHealth;
    private UpgradeManager upgradeManager;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerHealth = GetComponent<PlayerHealth>();
        upgradeManager = GetComponent<UpgradeManager>();
        baseMaxSpeed = maxSpeed;
        baseMoveSpeed = moveSpeed;
    }

    void Update()
    {
        // Don't get input if dead
        if (playerHealth.IsDead)
        {
            movementInput = Vector2.zero;
            return;
        }

        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");

        // Set animations
        animator.SetFloat("BlendH", movementInput.x);
        animator.SetFloat("BlendV", movementInput.y);
        animator.SetBool("IsMoving", movementInput.magnitude > 0.01);
    }

    void FixedUpdate()
    {
        // --- Knockback Override ---
        if (playerHealth.isKnockedBack)
        {
            return; // Let knockback control the Rigidbody
        }

        float currentMaxSpeed = baseMaxSpeed;
        float currentMoveSpeed = baseMoveSpeed;

        if (upgradeManager != null && upgradeManager.isSpeedBoostActive)
        {
            currentMaxSpeed = baseMaxSpeed * 1.5f;
            currentMoveSpeed = baseMoveSpeed * 1.2f;
        }

        if (movementInput.magnitude > 0.01f)
        {
            // --- Player is Moving ---

            // Only accelerate if we are below max speed
            if (rb.linearVelocity.magnitude < currentMaxSpeed)
            {
                rb.AddForce(movementInput.normalized * currentMoveSpeed);
            }
            // If we are AT max speed, AddForce does very little, preventing jitter.
            // We can add a tiny clamp just to be safe, but it's often not needed.
            if (rb.linearVelocity.magnitude > currentMaxSpeed)
            {
                 rb.linearVelocity = rb.linearVelocity.normalized * currentMaxSpeed; 
            }
        }
        else
        {
            // --- Player is Stopping: Apply Brakes ---
            rb.AddForce(-rb.linearVelocity * brakeFactor);
        }
    }
}