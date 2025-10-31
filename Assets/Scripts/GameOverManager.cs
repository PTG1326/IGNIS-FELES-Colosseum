using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using Solana.Unity.SDK;

public class GameOverManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI statusText; // Shows "Saving score..." / "Score saved!"
    public GameObject retryButton; // Shows when submission fails
    
    [Header("References")]
    public LeaderboardManager leaderboardManager; // Blockchain submission
    
    [Header("Google Sheets Leaderboard (Optional)")]
    [Tooltip("Your Google Sheets Web App URL - Leave empty to skip online leaderboard")]
    public string googleSheetsUrl = "";
    public Transform leaderboardContainer; // Container to spawn leaderboard entries
    public GameObject leaderboardEntryPrefab; // Prefab for each entry
    public TextMeshProUGUI loadingText; // Shows "Loading leaderboard..." while fetching
    
    private int finalScore;

    void Start()
    {
        SoundManager.instance.PlayMusic("Menu");

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        // Get and display the score
        if (ScoreManager.instance != null)
        {
            finalScore = ScoreManager.instance.currentScore;
            if (scoreText != null) scoreText.text = $"{finalScore}";
        }
        else
        {
            finalScore = 0;
            if (scoreText != null) scoreText.text = "000";
        }
        
        // Hide retry button initially
        if (retryButton != null)
        {
            retryButton.SetActive(false);
        }
        
        // Show loading text immediately if leaderboard is configured
        if (!string.IsNullOrEmpty(googleSheetsUrl) && leaderboardContainer != null)
        {
            ShowLoadingText(true);
        }
        
        // Auto-submit score if wallet is connected
        AutoSubmitScore();
        
        // Also fetch leaderboard on scene load (after a delay to avoid immediate rate limit)
        if (!string.IsNullOrEmpty(googleSheetsUrl) && leaderboardContainer != null)
        {
            StartCoroutine(DelayedLeaderboardFetch());
        }
    }
    
    /// <summary>
    /// Fetches leaderboard after a delay (used on scene load)
    /// Waits for submission to complete so leaderboard includes your new score
    /// </summary>
    private IEnumerator DelayedLeaderboardFetch()
    {
        // Wait 6 seconds for blockchain + Google Sheets submission to complete
        yield return new WaitForSeconds(6f);
        
        Debug.Log("Fetching leaderboard with your new score...");
        yield return GetLeaderboardCoroutine();
    }
    
    /// <summary>
    /// Automatically submits the score to blockchain if wallet is connected
    /// </summary>
    private async void AutoSubmitScore()
    {
        // Hide retry button while attempting
        if (retryButton != null)
        {
            retryButton.SetActive(false);
        }
        
        // Check if wallet is connected
        if (Web3.Wallet == null || Web3.Account == null)
        {
            Debug.Log("No wallet connected - score not saved to blockchain");
            UpdateStatus("Score not saved (no wallet)");
            return;
        }
        
        // Check if LeaderboardManager is assigned
        if (leaderboardManager == null)
        {
            Debug.LogWarning("LeaderboardManager not assigned in Inspector!");
            UpdateStatus("Leaderboard unavailable");
            return;
        }
        
        // Submit the score
        UpdateStatus("Saving score...");
        Debug.Log($"Auto-submitting score: {finalScore}");
        
        bool success = await leaderboardManager.SubmitPlayerScore((ulong)finalScore);
        
        // Update UI based on result
        if (success)
        {
            UpdateStatus("✅ Score saved!");
            
            // Submit to Google Sheets if URL is provided
            if (!string.IsNullOrEmpty(googleSheetsUrl))
            {
                SubmitToGoogleSheets();
            }
            
            // Hide retry button on success
            if (retryButton != null)
            {
                retryButton.SetActive(false);
            }
        }
        else
        {
            UpdateStatus("Failed to save score");
            // Show retry button on failure
            if (retryButton != null)
            {
                retryButton.SetActive(true);
            }
        }
    }
    
    /// <summary>
    /// Submit score to Google Sheets leaderboard
    /// </summary>
    private void SubmitToGoogleSheets()
    {
        string walletAddress = Web3.Account.PublicKey.ToString();
        string playerName = PlayerDataManager.instance?.GetPlayerName() ?? "Anonymous";
        
        // Clean the player name (remove special characters that might break URL)
        playerName = CleanPlayerName(playerName);
        
        StartCoroutine(SubmitToGoogleSheetsCoroutine(walletAddress, playerName, finalScore));
    }
    
    private IEnumerator SubmitToGoogleSheetsCoroutine(string wallet, string name, int score)
    {
        // Double-check encoding
        string encodedWallet = UnityWebRequest.EscapeURL(wallet);
        string encodedName = UnityWebRequest.EscapeURL(name);
        
        string url = $"{googleSheetsUrl}?action=submit&wallet={encodedWallet}&name={encodedName}&score={score}";
        
        Debug.Log($"Submitting to Google Sheets:");
        Debug.Log($"   Name: {name}");
        Debug.Log($"   Wallet: {wallet}");
        Debug.Log($"   Score: {score}");
        Debug.Log($"   URL: {url}");
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 15;
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                Debug.Log($" Submitted to Google Sheets!");
                Debug.Log($"   Response: {response}");
                Debug.Log($"   Leaderboard will auto-refresh in a moment...");
                
                // Don't fetch here - let the DelayedLeaderboardFetch handle it
                // This avoids duplicate fetches and rate limits
            }
            else
            {
                Debug.LogError($" Google Sheets submission failed!");
                Debug.LogError($"   Error: {request.error}");
                Debug.LogError($"   HTTP Code: {request.responseCode}");
                Debug.LogError($"   Response: {request.downloadHandler?.text}");
                Debug.LogError($"   URL was: {url}");
            }
        }
    }
    
    /// <summary>
    /// Clean player name to avoid URL encoding issues
    /// </summary>
    private string CleanPlayerName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "Anonymous";
        
        // Remove problematic characters
        name = name.Trim();
        
        // Replace common problematic characters
        name = name.Replace("&", "and");
        name = name.Replace("=", "-");
        name = name.Replace("?", "");
        name = name.Replace("#", "");
        name = name.Replace("%", "");
        
        // If name becomes empty after cleaning, use Anonymous
        if (string.IsNullOrEmpty(name))
            return "Anonymous";
        
        return name;
    }
    
    /// <summary>
    /// Get top 10 from Google Sheets and display
    /// </summary>
    public void RefreshLeaderboard()
    {
        if (!string.IsNullOrEmpty(googleSheetsUrl) && leaderboardContainer != null)
        {
            ShowLoadingText(true);
            StartCoroutine(GetLeaderboardCoroutine());
        }
    }
    
    private IEnumerator GetLeaderboardCoroutine()
    {
        string url = $"{googleSheetsUrl}?action=get&count=10";
        
        Debug.Log($" Fetching leaderboard from: {url}");
        ShowLoadingText(true);
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 15;
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                Debug.Log($" Leaderboard received");
                Debug.Log($"   Raw response: {response}");
                
                try
                {
                    // Parse JSON response
                    LeaderboardResponse data = JsonUtility.FromJson<LeaderboardResponse>(response);
                    
                    if (data.entries != null && data.entries.Count > 0)
                    {
                        Debug.Log($" Found {data.entries.Count} entries");
                        ShowLoadingText(false);
                        DisplayLeaderboard(data.entries);
                    }
                    else
                    {
                        Debug.LogWarning(" No entries in leaderboard");
                        ShowLoadingText(false, "No scores yet");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($" Failed to parse leaderboard: {e.Message}");
                    Debug.LogError($"   Response was: {response}");
                    ShowLoadingText(false, "Error loading leaderboard");
                }
            }
            else
            {
                Debug.LogWarning($" Failed to fetch leaderboard: {request.error}");
                Debug.LogWarning($"   HTTP Code: {request.responseCode}");
                Debug.LogWarning($"   Response: {request.downloadHandler?.text}");
                
                if (request.responseCode == 429)
                {
                    Debug.LogWarning(" Rate limit hit! Wait a few seconds and try RefreshLeaderboard()");
                    ShowLoadingText(false, "Rate limit - try again in a moment");
                }
                else
                {
                    ShowLoadingText(false, "Failed to load leaderboard");
                }
            }
        }
    }
    
    private void DisplayLeaderboard(List<LeaderboardEntry> entries)
    {
        Debug.Log($" DisplayLeaderboard called with {entries.Count} entries");
        
        if (leaderboardContainer == null)
        {
            Debug.LogError(" Leaderboard Container is NULL! Assign it in Inspector.");
            return;
        }
        
        Debug.Log($"   Container found: {leaderboardContainer.name}");
        
        // Clear existing entries
        int childCount = leaderboardContainer.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            Destroy(leaderboardContainer.GetChild(i).gameObject);
        }
        Debug.Log($"   Cleared {childCount} old entries");
        
        // Create entry for each player
        foreach (var entry in entries)
        {
            Debug.Log($"   Creating entry: #{entry.rank} {entry.player_name} - {entry.score}");
            
            GameObject entryObj;
            
            if (leaderboardEntryPrefab != null)
            {
                entryObj = Instantiate(leaderboardEntryPrefab, leaderboardContainer);
                
                // Populate prefab with data
                TextMeshProUGUI[] texts = entryObj.GetComponentsInChildren<TextMeshProUGUI>();
                Debug.Log($"      Found {texts.Length} text components in prefab");
                
                if (texts.Length >= 3)
                {
                    texts[0].text = $"#{entry.rank}";
                    texts[1].text = $"{entry.player_name} _ {ShortAddress(entry.wallet_address)}";
                    texts[2].text = entry.score.ToString();
                }
                else
                {
                    Debug.LogWarning($"      Prefab needs 3 TextMeshProUGUI components, found {texts.Length}");
                }
            }
            else
            {
                // Simple text if no prefab
                Debug.Log($"      No prefab assigned, creating simple text entry");
                entryObj = new GameObject($"Entry_{entry.rank}");
                entryObj.transform.SetParent(leaderboardContainer);
                var text = entryObj.AddComponent<TextMeshProUGUI>();
                text.text = $"#{entry.rank}  {entry.player_name}  {entry.score}";
                text.fontSize = 24;
                text.color = Color.white;
            }
        }
        
        Debug.Log($" Displayed {entries.Count} leaderboard entries successfully!");
    }
    
    private string ShortAddress(string address)
    {
        if (string.IsNullOrEmpty(address) || address.Length <= 8)
            return address;
        
        return $"{address.Substring(0, 4)}...{address.Substring(address.Length - 4)}";
    }
    
    /// <summary>
    /// Show or hide the loading text
    /// </summary>
    private void ShowLoadingText(bool show, string customMessage = "Loading leaderboard...")
    {
        if (loadingText != null)
        {
            loadingText.gameObject.SetActive(show);
            loadingText.text = customMessage;
        }
    }
    
    /// <summary>
    /// PUBLIC method for Retry button - Retries score submission
    /// Wire this to your Retry Button's OnClick() event
    /// </summary>
    public void RetrySubmitScore()
    {
        Debug.Log(" Retrying score submission...");
        AutoSubmitScore();
    }
    
    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    // --- Button Functions (These start the coroutines) ---

    public void StartRestartSequence()
    {
        StartCoroutine(RestartRoutine());
    }

    public void StartMainMenuSequence()
    {
        StartCoroutine(MainMenuRoutine());
    }

    // --- Coroutines (Handle fade, destroy ScoreManager, load scene) ---

    private IEnumerator RestartRoutine()
    {
        // Optional: Fade out first
        if (Fader.instance != null)
            yield return Fader.instance.FadeOut();

        // --- Destroy ScoreManager HERE ---
        if (ScoreManager.instance != null)
        {
            Destroy(ScoreManager.instance.gameObject);
        }
        // --- End Destroy ---

        // Load the game scene
        SceneManager.LoadScene("GameScene"); // Replace "GameScene" with your actual scene name
    }

    private IEnumerator MainMenuRoutine()
    {
        // Optional: Fade out first
        if (Fader.instance != null)
            yield return Fader.instance.FadeOut();

        // --- Destroy ScoreManager HERE ---
        if (ScoreManager.instance != null)
        {
            Destroy(ScoreManager.instance.gameObject);
        }
        // --- End Destroy ---

        // Load the main menu scene
        SceneManager.LoadScene("MainMenuScene"); // Replace "MainMenuScene" with your actual scene name
    }
}

// JSON classes for Google Sheets API
[Serializable]
public class LeaderboardResponse
{
    public bool success;
    public List<LeaderboardEntry> entries;
}

[Serializable]
public class LeaderboardEntry
{
    public int rank;
    public string wallet_address;
    public string player_name;
    public int score;
}