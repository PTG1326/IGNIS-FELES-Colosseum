using System;
using UnityEngine;

public class PlayerAim : MonoBehaviour
{
    [Header("Custom Cursor")]
    public Texture2D defaultCursorTexture; // Assign your custom cursor texture here
    public Vector2 hotSpot = Vector2.zero; // Hotspot of the cursor (e.g., Vector2.zero for top-left, or new Vector2(16,16) for center of 32x32)

    [Header("Aiming")]
    public Transform aimIndicatorTransform;
    public float aimOffsetDistance = 0.8f;
    public Vector2 orbitCenterOffset = new Vector2(0f, 0.5f);

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 20f;
    public float bulletSpawnDistance = 1.2f;

    [Header("Cooldown / Overheat")]
    public float maxHeat = 100f; // The maximum heat capacity
    public float heatPerShot = 25f; // Heat added per bullet fired
    public float coolDownRate = 50f; // Heat lost per second when not shooting
    public float overheatCoolDownRate = 30f; // Heat lost per second when overheated (slower)
    public float currentHeat = 0f; // Current heat level

    [Header("Upgrade")]
    public float spreadAngle = 15f;

    public event Action OnOverheatStart;
    private bool isOverheated = false;

    private Camera mainCam;
    private PlayerHealth playerHealth; // Reference for IsDead check
    private Animator headFireAnimator;
    private UpgradeManager upgradeManager;

    void Start()
    {
        mainCam = Camera.main;
        playerHealth = GetComponent<PlayerHealth>();
        upgradeManager = GetComponent<UpgradeManager>();

        Transform fireChild = transform.Find("Fire"); // Case-sensitive!

        // 2. Check if we found the child
        if (fireChild == null)
        {
            Debug.LogError("Could not find a child object named 'fire'!", this);
        }
        else
        {
            // 3. Get the Animator component from that specific child
            headFireAnimator = fireChild.GetComponent<Animator>();

            // 4. Check if the Animator component exists on that child
            if (headFireAnimator == null)
            {
                Debug.LogError("Found the 'fire' child, but it doesn't have an Animator component!", fireChild);
            }
        }

        // Ensure heat starts at 0
        currentHeat = 0f;
        isOverheated = false;

        headFireAnimator.SetBool("IsOverheated", false);

        // Confine cursor (Keep existing cursor logic)
        SetCustomCursor(defaultCursorTexture); // Apply your default custom cursor
        Cursor.lockState = CursorLockMode.Confined; // Confine to game window
        Cursor.visible = true;
    }

    // Call this method whenever you want to change the cursor texture
    public void SetCustomCursor(Texture2D cursorTexture)
    {
        if (cursorTexture != null)
        {
            Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
        }
        else
        {
            // If no custom texture is provided, revert to default system cursor
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }

    void Update()
    {
        // Don't do anything if player is dead
        if (playerHealth != null && playerHealth.IsDead)
        {
            return;
        }

        // --- Handle Cursor Locking ---
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Toggle pause state
            // Example: if (isPaused) { UnpauseGame(); } else { PauseGame(); }
            // For demonstration:
            if (Cursor.lockState == CursorLockMode.Confined) {
                Cursor.lockState = CursorLockMode.None; // Release confinement
                SetCustomCursor(null); // Revert to system cursor or a menu-specific custom cursor
                Cursor.visible = true; // Ensure visible
            } else {
                Cursor.lockState = CursorLockMode.Confined; // Confine again
                SetCustomCursor(defaultCursorTexture); // Set your custom game cursor
                Cursor.visible = true; // Ensure visible
            }
        }

        // --- Handle Shooting Input & Cooldown ---
        bool shootInput = Input.GetMouseButtonDown(0); // Use GetMouseButton for continuous fire check

        if (shootInput && !isOverheated)
        {
            // Re-confine cursor if player clicks back into the game.
            if (Cursor.lockState != CursorLockMode.Confined)
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = false;
            }
            Shoot();
        }
        else
        {
            // If not shooting OR if overheated, cool down
            if (isOverheated)
            {
                currentHeat -= overheatCoolDownRate * Time.deltaTime;
                if (currentHeat <= 0)
                {
                    currentHeat = 0;
                    isOverheated = false; // Cooldown finished
                    headFireAnimator.SetBool("IsOverheated", false);
                    SoundManager.instance.PlaySound("FireIgnite");

                    Debug.Log("Overheat cooldown finished!");
                }
            }
            else
            {
                // Normal cooldown when not firing
                currentHeat -= coolDownRate * Time.deltaTime;
            }
        }

        // Clamp heat to min/max values
        currentHeat = Mathf.Clamp(currentHeat, 0, maxHeat);
    }

    // Aiming logic (no changes needed)
    void LateUpdate()
    {
        if (playerHealth != null && playerHealth.IsDead) return;
        if (Cursor.lockState != CursorLockMode.Confined) return;

        Vector3 mouseWorldPosition = mainCam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 orbitCenter = (Vector2)transform.position + orbitCenterOffset;
        Vector2 direction = (Vector2)mouseWorldPosition - orbitCenter;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        aimIndicatorTransform.rotation = Quaternion.Euler(0, 0, angle);
        aimIndicatorTransform.localPosition = orbitCenterOffset + (direction.normalized * aimOffsetDistance);
    }

    // This is the main function called by Update
    void Shoot()
    {
        // 1. Get the main angle towards the mouse
        Vector2 orbitCenter = (Vector2)transform.position + orbitCenterOffset;
        Vector2 shootDirection = (Vector2)mainCam.ScreenToWorldPoint(Input.mousePosition) - orbitCenter;
        float centerAngle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;

        // 2. Check if the upgrade is active
        if (upgradeManager != null && upgradeManager.isMultiShotActive)
        {
            // Fire 3 bullets
            FireBulletAtAngle(centerAngle); // Center
            FireBulletAtAngle(centerAngle + spreadAngle); // Right
            FireBulletAtAngle(centerAngle - spreadAngle); // Left
        }
        else
        {
            // Fire 1 bullet (original logic)
            FireBulletAtAngle(centerAngle);
        }

        SoundManager.instance.PlaySound("PlayerShoot");

        // 3. Apply heat (this logic is unchanged)
        currentHeat += heatPerShot;
        if (currentHeat >= maxHeat)
        {
            isOverheated = true;
            OnOverheatStart?.Invoke();
            headFireAnimator.SetBool("IsOverheated", true);

            SoundManager.instance.PlaySound("FireExtinguish");
        }
    }

    // This is our new helper function for spawning bullets
    private void FireBulletAtAngle(float angle)
    {
        // 1. Calculate direction vector from angle
        Vector2 shootDirection = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

        // 2. Calculate spawn point
        Vector2 orbitCenter = (Vector2)transform.position + orbitCenterOffset;
        Vector2 spawnPoint = orbitCenter + (shootDirection.normalized * bulletSpawnDistance);

        // 3. Calculate rotation
        Quaternion bulletRotation = Quaternion.Euler(0, 0, angle);

        // 4. Spawn bullet and set velocity
        GameObject bullet = Instantiate(bulletPrefab, spawnPoint, bulletRotation);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        rb.linearVelocity = shootDirection.normalized * bulletSpeed;
    }

    // Public getter for the UI script to read the heat level
    public float GetCurrentHeatNormalized()
    {
        return currentHeat / maxHeat;
    }

    public bool IsOverheated => isOverheated;
}
