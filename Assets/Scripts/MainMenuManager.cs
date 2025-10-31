using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Needed for loading scenes
using System.Collections; // Needed for Coroutines if using Fader
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using TMPro; // Use UnityEngine.UI.Text if not using TextMeshPro

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject howToPlayPanel;
    
    [Header("Name Input UI")]
    public TMP_InputField nameInputField; // Where player enters name
    public GameObject playButton; // Play button (also submits name)
    
    [Header("Wallet UI")]
    public TextMeshProUGUI errorText; // Shows errors
    public TextMeshProUGUI connectButtonText; // Text inside Connect Wallet button
    
    private bool isWalletConnected = false;

    void Start()
    {
        // Play menu music if SoundManager exists
        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlayMusic("Menu");
        }
        
        // Ensure cursor is visible and unlocked when the main menu loads
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        // Ensure the HowToPlay panel starts hidden
        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(false);
        }
        
        // Hide error text initially
        if (errorText != null)
        {
            errorText.gameObject.SetActive(false);
        }
        
        // Subscribe to wallet events
        Web3.OnLogin += OnWalletConnected;
        Web3.OnLogout += OnWalletDisconnected;
        
        // Subscribe to name input changes
        if (nameInputField != null)
        {
            nameInputField.onValueChanged.AddListener(OnNameInputChanged);
        }
        
        // Check if wallet is already connected (from previous session)
        CheckWalletConnection();
        
        // Initial UI update
        UpdateUI();
        UpdatePlayButtonState();
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        Web3.OnLogin -= OnWalletConnected;
        Web3.OnLogout -= OnWalletDisconnected;
        
        if (nameInputField != null)
        {
            nameInputField.onValueChanged.RemoveListener(OnNameInputChanged);
        }
    }
    
    /// <summary>
    /// Called when player types in the name input field
    /// Enables Play button if text is valid
    /// </summary>
    private void OnNameInputChanged(string text)
    {
        // Only relevant when wallet is connected and no name saved
        if (isWalletConnected && PlayerDataManager.instance != null && !PlayerDataManager.instance.HasPlayerName())
        {
            UpdatePlayButtonState();
        }
    }
    
    private void CheckWalletConnection()
    {
        // Check if Web3 instance exists and wallet is connected
        if (Web3.Instance != null && Web3.Wallet != null && Web3.Account != null)
        {
            isWalletConnected = true;
            Debug.Log($" Wallet already connected: {Web3.Account.PublicKey}");
            
            // Load player name for this wallet
            if (PlayerDataManager.instance != null)
            {
                PlayerDataManager.instance.LoadPlayerNameForWallet();
            }
        }
        else
        {
            isWalletConnected = false;
        }
    }

    #region Wallet Connection
    
    /// <summary>
    /// PUBLIC wrapper for Unity Button - Connect Wallet
    /// </summary>
    public void ConnectWallet()
    {
        ConnectWalletAsync();
    }
    
    /// <summary>
    /// Async implementation of wallet connection
    /// Uses Wallet Adapter for browser wallets (Phantom, Solflare, etc.)
    /// </summary>
    private async void ConnectWalletAsync()
    {
        if (isWalletConnected)
        {
            Debug.Log(" Wallet already connected!");
            ShowError("Wallet already connected!");
            return;
        }
        
        // Hide any previous errors
        HideError();
        
        try
        {
            // Connect to Solana wallet (Phantom, Solflare, etc.)
            var account = await Web3.Instance.LoginWalletAdapter();
            
            // Alternative: Web3Auth for social login
            // var account = await Web3.Instance.LoginWeb3Auth(Provider.GOOGLE);
            
            if (account != null)
            {
                isWalletConnected = true;
                Debug.Log($" Wallet connected: {account.PublicKey}");
                
                // Load player name for this wallet
                if (PlayerDataManager.instance != null)
                {
                    PlayerDataManager.instance.LoadPlayerNameForWallet();
                }
                
                // Don't show success message, just update UI silently
            }
            else
            {
                ShowError("Failed to connect wallet. Please try again.");
            }
        }
        catch (System.Exception e)
        {
            ShowError($"Connection error: {e.Message}");
            Debug.LogError($"Wallet connection error: {e}");
        }
        finally
        {
            UpdateUI();
            UpdatePlayButtonState();
        }
    }
    
    private void OnWalletConnected(Account account)
    {
        Debug.Log($" Wallet connected event: {account.PublicKey}");
        isWalletConnected = true;
        
        // Load player name for this wallet
        if (PlayerDataManager.instance != null)
        {
            PlayerDataManager.instance.LoadPlayerNameForWallet();
        }
        
        UpdateUI();
        UpdatePlayButtonState();
    }
    
    private void OnWalletDisconnected()
    {
        Debug.Log("Wallet disconnected");
        isWalletConnected = false;
        UpdateUI();
        UpdatePlayButtonState();
    }
    
    #endregion
    
    #region Game Flow
    
    /// <summary>
    /// Called when Play button is clicked
    /// Saves name if needed, then starts game
    /// </summary>
    public void PlayGame()
    {
        // If no wallet connected, allow playing without name
        if (!isWalletConnected)
        {
            Debug.Log(" Starting game without wallet");
            StartGame();
            return;
        }
        
        // Wallet connected - check if has name saved
        if (PlayerDataManager.instance != null && PlayerDataManager.instance.HasPlayerName())
        {
            // Already has name, start game
            Debug.Log($" Starting game for: {PlayerDataManager.instance.GetPlayerName()}");
            StartGame();
        }
        else
        {
            // No saved name - get from input field and save it
            if (nameInputField != null && !string.IsNullOrEmpty(nameInputField.text.Trim()))
            {
                string playerName = nameInputField.text.Trim();
                
                // Validate name
                if (playerName.Length < 2)
                {
                    ShowError("Name must be at least 2 characters!");
                    return;
                }
                
                if (playerName.Length > 20)
                {
                    ShowError("Name must be 20 characters or less!");
                    return;
                }
                
                // Save name
                if (PlayerDataManager.instance == null)
                {
                    GameObject playerDataObj = new GameObject("PlayerDataManager");
                    playerDataObj.AddComponent<PlayerDataManager>();
                }
                
                PlayerDataManager.instance.SetPlayerName(playerName);
                Debug.Log($" Name saved and starting game for: {playerName}");
                
                // Start game
                StartGame();
            }
            else
            {
                // This shouldn't happen (button should be disabled)
                ShowError("Please enter your name!");
            }
        }
    }
    
    /// <summary>
    /// Starts the game (loads game scene)
    /// </summary>
    private void StartGame()
    {
        if (!isWalletConnected)
        {
            Debug.Log(" Starting without wallet - scores won't be saved");
        }
        else
        {
            Debug.Log($" Starting with wallet: {Web3.Account.PublicKey}");
        }
        
        Debug.Log($" Starting game for player: {PlayerDataManager.instance?.GetPlayerName() ?? "Unknown"}");
        
        StartCoroutine(LoadGameScene());
    }

    // Called by the How to Play button
    public void ShowHowToPlay()
    {
        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(true);
        }
    }

    // Called by the Close button on the How to Play panel
    public void HideHowToPlay()
    {
        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(false);
        }
    }

    // Coroutine to handle potential fade before loading
    private IEnumerator LoadGameScene()
    {
        // Check if your Fader exists before trying to use it
        if (Fader.instance != null)
        {
            yield return Fader.instance.FadeOut();
        }
        else
        {
            yield return null; // Wait a frame even without fader
        }
        
        // Load your main game scene (replace "GameScene" if needed)
        SceneManager.LoadScene("GameScene");
    }
    
    #endregion
    
    #region UI Updates
    
    /// <summary>
    /// Updates Play button and Name Input state based on wallet connection and name
    /// </summary>
    private void UpdatePlayButtonState()
    {
        bool hasWallet = isWalletConnected;
        bool hasName = PlayerDataManager.instance != null && PlayerDataManager.instance.HasPlayerName();
        bool hasInputText = nameInputField != null && !string.IsNullOrEmpty(nameInputField.text.Trim());
        
        // Logic:
        // No wallet: Play enabled, input hidden
        // Wallet + no name + no input: Play disabled, input visible
        // Wallet + no name + has input: Play enabled, input visible
        // Wallet + has name: Play enabled, input hidden
        
        if (!hasWallet)
        {
            // No wallet connected - can play without wallet
            if (playButton != null)
            {
                var button = playButton.GetComponent<UnityEngine.UI.Button>();
                if (button != null) button.interactable = true;
            }
            
            // Hide name input
            if (nameInputField != null)
            {
                nameInputField.gameObject.SetActive(false);
            }
            
            Debug.Log("Play button: ENABLED (no wallet)");
        }
        else if (hasWallet && !hasName)
        {
            // Wallet connected but no name saved
            // Show input, enable Play only if input has text
            if (playButton != null)
            {
                var button = playButton.GetComponent<UnityEngine.UI.Button>();
                if (button != null) button.interactable = hasInputText;
            }
            
            // Show name input
            if (nameInputField != null)
            {
                if (!nameInputField.gameObject.activeSelf)
                {
                    nameInputField.gameObject.SetActive(true);
                    nameInputField.interactable = true;
                    nameInputField.text = "";
                    nameInputField.Select();
                    nameInputField.ActivateInputField();
                }
            }
            
            Debug.Log($"Play button: {(hasInputText ? "ENABLED" : "DISABLED")} (input text: '{nameInputField?.text}') | Input: VISIBLE");
        }
        else // hasWallet && hasName
        {
            // Wallet connected and has name - enable play, hide input
            if (playButton != null)
            {
                var button = playButton.GetComponent<UnityEngine.UI.Button>();
                if (button != null) button.interactable = true;
            }
            
            // Hide name input
            if (nameInputField != null)
            {
                nameInputField.gameObject.SetActive(false);
            }
            
            Debug.Log($"Play button: ENABLED (has name: {PlayerDataManager.instance.GetPlayerName()})");
        }
    }
    
    private void UpdateUI()
    {
        // Update connect button text to show connection status
        if (connectButtonText != null)
        {
            if (isWalletConnected && Web3.Account != null)
            {
                string address = Web3.Account.PublicKey.ToString();
                connectButtonText.text = $"✓ {ShortAddress(address)}";
            }
            else
            {
                connectButtonText.text = "Connect Wallet";
            }
        }
    }
    
    private void ShowError(string message)
    {
        Debug.LogWarning($"[Main Menu Error] {message}");
        
        if (errorText != null)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(true);
            
            // Auto-hide error after 5 seconds
            CancelInvoke(nameof(HideError));
            Invoke(nameof(HideError), 5f);
        }
    }
    
    private void HideError()
    {
        if (errorText != null)
        {
            errorText.gameObject.SetActive(false);
        }
    }
    
    private string ShortAddress(string address)
    {
        if (string.IsNullOrEmpty(address) || address.Length < 8)
            return address;
        
        return $"{address.Substring(0, 4)}...{address.Substring(address.Length - 4)}";
    }
    
    #endregion
}