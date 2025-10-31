using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // For UI Images

// Define an enum for different upgrade types for clarity
public enum UpgradeType
{
    None,
    MultiShot,
    SpeedBoost,
    CoinVaccuum
}

public class UpgradeManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform upgradeIconContainer; // The "box" (Horizontal Layout Group)
    public GameObject upgradeIconPrefab; // The prefab for the UI icon

    // Dictionary to hold active upgrade coroutines and their UI elements
    private Dictionary<UpgradeType, UpgradeState> activeUpgrades = new Dictionary<UpgradeType, UpgradeState>();

    // --- Public Properties to Check Active Upgrades ---
    // Other scripts will check these bools
    public bool isMultiShotActive => activeUpgrades.ContainsKey(UpgradeType.MultiShot);
    public bool isSpeedBoostActive => activeUpgrades.ContainsKey(UpgradeType.SpeedBoost);
    public bool isCoinVaccuumActive => activeUpgrades.ContainsKey(UpgradeType.CoinVaccuum);
    // --- End Public Properties ---

    // Internal class to hold state for each active upgrade
    private class UpgradeState
    {
        public Coroutine coroutine;
        public GameObject iconInstance;
        public float remainingTime; // Could be used for a timer display on the icon
    }

    // --- Generic Activation Method ---
    // This is the primary method called by any UpgradePickup
    public void ActivateUpgrade(UpgradeType type, float duration, Sprite icon)
    {
        if (type == UpgradeType.None) return;

        if (activeUpgrades.ContainsKey(type))
        {
            // Upgrade of this type is ALREADY ACTIVE
            // Stop the old coroutine to reset its timer
            StopCoroutine(activeUpgrades[type].coroutine);
            Debug.Log($"Refreshed {type} upgrade. New duration: {duration}s");
            // Optionally, update the remainingTime or UI elements if you had a countdown
        }
        else
        {
            // Upgrade is NOT ACTIVE, create new UI icon
            Debug.Log($"Activated {type} upgrade for {duration}s");
            activeUpgrades[type] = new UpgradeState();
            
            if (upgradeIconContainer != null && upgradeIconPrefab != null)
            {
                activeUpgrades[type].iconInstance = Instantiate(upgradeIconPrefab, upgradeIconContainer);
                activeUpgrades[type].iconInstance.GetComponent<Image>().sprite = icon;
                // Could also set its name or add a tooltip for the type
            }
        }

        // Start (or restart) the coroutine
        activeUpgrades[type].coroutine = StartCoroutine(UpgradeTimerCoroutine(type, duration));
    }

    private IEnumerator UpgradeTimerCoroutine(UpgradeType type, float duration)
    {
        // Store remaining time (useful if you want to display it)
        activeUpgrades[type].remainingTime = duration;

        while (activeUpgrades[type].remainingTime > 0)
        {
            activeUpgrades[type].remainingTime -= Time.deltaTime;
            // You could update a text field on the icon here to show remaining time
            yield return null;
        }

        // --- Upgrade Ended ---
        Debug.Log($"{type} upgrade ended.");
        
        // Remove UI icon if it exists
        if (activeUpgrades[type].iconInstance != null)
        {
            Destroy(activeUpgrades[type].iconInstance);
        }
        
        // Remove from active upgrades dictionary
        activeUpgrades.Remove(type);
    }
}