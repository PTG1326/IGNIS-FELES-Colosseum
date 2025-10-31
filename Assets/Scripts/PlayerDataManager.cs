using UnityEngine;
using Solana.Unity.SDK;

/// <summary>
/// Manages player data that persists across scenes
/// Stores player name per wallet address (one name per wallet)
/// Uses PlayerPrefs with wallet address as key for WebGL compatibility
/// </summary>
public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager instance;
    
    [Header("Player Info")]
    public string playerName = "";
    public string walletAddress = ""; // Current player's wallet address
    public string scoreAccountAddress = ""; // Player's score PDA address
    
    void Awake()
    {
        // Singleton pattern
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Loads the player name for the current connected wallet
    /// Call this after wallet connects!
    /// </summary>
    public void LoadPlayerNameForWallet()
    {
        if (Web3.Account == null)
        {
            Debug.LogWarning(" No wallet connected - cannot load player name");
            playerName = "";
            walletAddress = "";
            scoreAccountAddress = "";
            return;
        }
        
        // Store wallet address
        walletAddress = Web3.Account.PublicKey.ToString();
        
        // Load saved name for this wallet
        string key = GetStorageKey(walletAddress);
        playerName = PlayerPrefs.GetString(key, "");
        
        if (!string.IsNullOrEmpty(playerName))
        {
            Debug.Log($" Loaded name '{playerName}' for wallet {ShortAddress(walletAddress)}");
        }
        else
        {
            Debug.Log($" No name found for wallet {ShortAddress(walletAddress)}");
        }
    }
    
    /// <summary>
    /// Sets the player's name for the current wallet and saves it
    /// </summary>
    public void SetPlayerName(string name)
    {
        if (Web3.Account == null)
        {
            Debug.LogError(" No wallet connected - cannot set player name");
            return;
        }
        
        playerName = name.Trim();
        walletAddress = Web3.Account.PublicKey.ToString();
        
        string key = GetStorageKey(walletAddress);
        PlayerPrefs.SetString(key, playerName);
        PlayerPrefs.Save();
        
        Debug.Log($" Name '{playerName}' saved for wallet {ShortAddress(walletAddress)}");
    }
    
    /// <summary>
    /// Sets the player's score account PDA address
    /// Call this after deriving the PDA in LeaderboardManager
    /// </summary>
    public void SetScoreAccountAddress(string pdaAddress)
    {
        scoreAccountAddress = pdaAddress;
        Debug.Log($" Score PDA set: {ShortAddress(scoreAccountAddress)}");
    }
    
    /// <summary>
    /// Gets the wallet address
    /// </summary>
    public string GetWalletAddress()
    {
        return walletAddress;
    }
    
    /// <summary>
    /// Gets the score account PDA address
    /// </summary>
    public string GetScoreAccountAddress()
    {
        return scoreAccountAddress;
    }
    
    /// <summary>
    /// Gets the player's name (or wallet address if not set)
    /// </summary>
    public string GetPlayerName()
    {
        if (string.IsNullOrEmpty(playerName))
        {
            // Return shortened wallet address as fallback
            if (!string.IsNullOrEmpty(walletAddress))
            {
                return ShortAddress(walletAddress);
            }
            return "Player";
        }
        return playerName;
    }
    
    /// <summary>
    /// Checks if current wallet has a name set
    /// </summary>
    public bool HasPlayerName()
    {
        return !string.IsNullOrEmpty(playerName);
    }
    
    /// <summary>
    /// Gets the storage key for a wallet address
    /// Format: "PlayerName_<wallet_address>"
    /// </summary>
    private string GetStorageKey(string walletAddress)
    {
        return $"PlayerName_{walletAddress}";
    }
    
    /// <summary>
    /// Shortens wallet address for display
    /// </summary>
    private string ShortAddress(string address)
    {
        if (string.IsNullOrEmpty(address) || address.Length < 8)
            return address;
        
        return $"{address.Substring(0, 4)}...{address.Substring(address.Length - 4)}";
    }
    
    /// <summary>
    /// Clears player name for current wallet (for testing)
    /// </summary>
    public void ClearPlayerData()
    {
        if (string.IsNullOrEmpty(walletAddress))
        {
            Debug.LogWarning(" No wallet address to clear");
            return;
        }
        
        string key = GetStorageKey(walletAddress);
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        
        playerName = "";
        scoreAccountAddress = "";
        Debug.Log($" Player name cleared for wallet {ShortAddress(walletAddress)}");
    }
    
    /// <summary>
    /// Clears ALL player names (for testing)
    /// </summary>
    public void ClearAllPlayerData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        
        playerName = "";
        walletAddress = "";
        scoreAccountAddress = "";
        
        Debug.Log(" All player data cleared");
    }
}
