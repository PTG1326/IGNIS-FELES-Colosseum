using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    // --- Public References (assign in Inspector) ---
    public PlayerHealth playerHealth;
    public Sprite fullMaskSprite;
    public Sprite brokenMaskSprite;
    public GameObject maskPrefab;

    // --- Private Variables ---
    private List<Image> maskImages = new List<Image>();

    void Start()
    {
        // Find the player's health script if not assigned
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }

        // Create the mask icons based on the player's max health
        SetupMasks();
    }

    void Update()
    {
        // Update the visual state of the masks each frame
        UpdateMasks();
    }

    private void SetupMasks()
    {
        // Create a mask icon for each point of max health
        for (int i = 0; i < playerHealth.maxHealth; i++)
        {
            // Instantiate the prefab and set its parent to this container
            GameObject newMask = Instantiate(maskPrefab, transform);
            // Add the Image component of the new mask to our list
            maskImages.Add(newMask.GetComponent<Image>());
        }
    }

    private void UpdateMasks()
    {
        // Loop through all the mask images we've created
        for (int i = 0; i < maskImages.Count; i++)
        {
            // If the index is less than the player's current health,
            // the mask should be full. Otherwise, it should be broken.
            if (i < playerHealth.currentHealth)
            {
                maskImages[i].sprite = fullMaskSprite;
            }
            else
            {
                maskImages[i].sprite = brokenMaskSprite;
            }
        }
    }
}