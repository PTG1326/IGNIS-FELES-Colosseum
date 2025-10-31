using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject enemyPrefab;
    private EnemyAI enemyAIScript;

    [Header("Initial Spawn")]
    public int initialSpawnCount = 5;

    [Header("Spawn Rate")]
    public float baseSpawnRate = 5f;
    public float minSpawnRate = 1f;
    public int scoreToReachMinRate = 1000;

    [Header("Spawn Location (Relative to Player)")]
    public float minSpawnRadius = 10f; // Min distance from player (should be > enemy detectionRadius)
    public float maxSpawnRadius = 20f; // Max distance from player

    [Header("Obstacle Detection")]
    public float enemyRadius = 0.5f; // How much space the enemy needs

    [Header("Player Proximity")]

    private Transform playerTransform;

    private PolygonCollider2D spawnArea;
    private Bounds spawnBounds;

    void Start()
    {
        spawnArea = GetComponent<PolygonCollider2D>();
        spawnArea.isTrigger = true; // Ensure it's a trigger so it doesn't block movement
        spawnBounds = spawnArea.bounds; // Keep for fallback or rough checks

        // Try to get the enemy script to read its detection radius automatically
        if (enemyPrefab != null)
        {
            // Try getting the specific script (adjust if using RangedEnemyAI or a base class)
            enemyAIScript = enemyPrefab.GetComponent<EnemyAI>();
            if (enemyAIScript != null)
            {
                // Set minimum spawn distance slightly outside the enemy's chase range
                minSpawnRadius = enemyAIScript.detectionRadius + 1.0f; // Add a small buffer
                Debug.Log($"{gameObject.name} setting minSpawnRadius based on {enemyPrefab.name}'s detectionRadius: {minSpawnRadius}");
            }
            else
            {
                // Fallback if script not found or doesn't match
                Debug.LogWarning($"Could not find EnemyAI script on {enemyPrefab.name}. Using default minSpawnRadius: {minSpawnRadius}. Make sure this is > enemy detection range.", this);
                // Ensure minSpawnRadius is manually set correctly in the Inspector
            }
        } else {
            Debug.LogError("Enemy Prefab not assigned to spawner!", this);
        }


        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        } else {
            Debug.LogError("Player not found! Spawner needs player with 'Player' tag.", this);
            // Disable spawner if no player?
            // enabled = false;
            // return;
        }

        // Initial spawn (still uses FindValidSpawnPoint, which now spawns around player)
        for (int i = 0; i < initialSpawnCount; i++)
        {
            SpawnEnemy(); // No longer needs the boolean
        }

        // Start the continuous spawning routine
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            float currentSpawnRate = GetCurrentSpawnRate();
            yield return new WaitForSeconds(currentSpawnRate);
            SpawnEnemy();
        }
    }

    private float GetCurrentSpawnRate()
    {
        if (ScoreManager.instance == null) return baseSpawnRate;
        float t = Mathf.InverseLerp(0, scoreToReachMinRate, ScoreManager.instance.currentScore);
        return Mathf.Lerp(baseSpawnRate, minSpawnRate, t);
    }

    private void SpawnEnemy() // Removed boolean parameter
    {
        if (enemyPrefab == null || playerTransform == null) return; // Need player

        // Find a valid point around the player, within the polygon, and clear of obstacles
        Vector2? spawnPoint = FindValidSpawnPoint(); // No longer needs the boolean

        if (spawnPoint.HasValue)
        {
            Instantiate(enemyPrefab, spawnPoint.Value, Quaternion.identity);
        }
    }

    private Vector2? FindValidSpawnPoint() // Removed boolean parameter
    {
        if (playerTransform == null) return null; // Can't spawn without player position

        int attempts = 0;
        while (attempts < 30) // Try multiple times to find a good spot
        {
            attempts++; // Increment attempt counter

            // 1. Pick a random angle and distance around the player
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad; // Angle in radians
            float randomDistance = Random.Range(minSpawnRadius, maxSpawnRadius);

            // Calculate the potential spawn point relative to the player
            Vector2 direction = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
            Vector2 potentialPoint = (Vector2)playerTransform.position + direction * randomDistance;

            // --- VALIDATION CHECKS ---

            // 2. Check if the point is inside the spawner's PolygonCollider2D area
            bool inPolygon = spawnArea.OverlapPoint(potentialPoint);
            if (!inPolygon)
            {
                continue; // Try again if outside the designated zone
            }

            // 3. Check if the point is clear of obstacles ("Prop" tag)
            bool isClear = true;
            Collider2D[] hits = Physics2D.OverlapCircleAll(potentialPoint, enemyRadius);
            foreach (Collider2D hit in hits)
            {
                // Also check if we hit another enemy or the player layer itself if necessary
                if (hit.CompareTag("Prop") || hit.CompareTag("Enemy") || hit.CompareTag("Player"))
                {
                    isClear = false;
                    break;
                }
            }

            // 4. If all checks pass, return the valid point
            if (isClear) // inPolygon check already happened
            {
                return potentialPoint;
            }

            // If checks fail, the loop continues to try another random point
        }

        // If we failed after 30 attempts, return null
        Debug.LogWarning($"Spawner {gameObject.name} failed to find valid spawn point near player after 30 attempts.");
        return null;
    }

    // ... (OnDrawGizmos function remains the same) ...
    void OnDrawGizmos()
    {
        if (spawnArea == null)
        {
            spawnArea = GetComponent<PolygonCollider2D>();
        }
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        if (spawnArea != null)
        {
            Vector2[] points = spawnArea.GetPath(0);
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 p1 = transform.TransformPoint(points[i]);
                Vector2 p2 = transform.TransformPoint(points[(i + 1) % points.Length]);
                Gizmos.DrawLine(p1, p2);
            }
        }
    }
}