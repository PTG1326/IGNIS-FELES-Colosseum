using UnityEngine;

/// <summary>
/// Ensures Web3 GameObject persists across all scenes.
/// Attach this to your Web3 GameObject in the Main Menu scene.
/// CRITICAL: Without this, wallet connection will be lost when changing scenes!
/// </summary>
public class Web3Persistence : MonoBehaviour
{
    private static Web3Persistence instance;

    void Awake()
    {
        // Ensure only one instance exists
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log(" Web3 will persist across scenes");
        }
        else
        {
            // Destroy duplicate instances
            Debug.Log(" Duplicate Web3 GameObject destroyed");
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            Debug.Log("Web3 persistence ended");
        }
    }
}
