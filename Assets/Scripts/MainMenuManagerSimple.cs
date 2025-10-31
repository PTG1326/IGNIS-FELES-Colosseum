using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using TMPro;

/// <summary>
/// SIMPLE Main Menu Manager - Uses IN-GAME WALLET for quick testing
/// Works in Unity Editor! No WebGL build needed.
/// Replace with MainMenuManager.cs when ready for production
/// </summary>
public class MainMenuManagerSimple : MonoBehaviour
{
    [Header("Panels")]
    public GameObject howToPlayPanel;
    
    [Header("Name Input UI")]
    public TMP_InputField nameInputField; // Where player enters name
    public GameObject playButton; // Play button (also submits name)
    
    [Header("Wallet UI (Optional)")]
    public TextMeshProUGUI connectButtonText; // Shows connection status
    public TextMeshProUGUI errorText; // Shows errors
    
    [Header("Testing - Multiple Wallets")]
    [Tooltip("Select which test wallet to use (0-9). Each number = different wallet for leaderboard testing!")]
    [Range(0, 9)]
    public int testWalletIndex = 0; // 0-9 gives you 10 different test wallets!
    
    private bool isWalletConnected = false;

    void Start()
    {
        SoundManager.instance.PlayMusic("Menu");

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(false);
        }
        
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
        
        // Check if already connected from previous session
        CheckExistingConnection();
        
        // Update UI based on wallet/name state
        UpdatePlayButtonState();
    }
    
    private void CheckExistingConnection()
    {
        if (Web3.Instance != null && Web3.Wallet != null && Web3.Account != null)
        {
            isWalletConnected = true;
            Debug.Log($"✅ Wallet already connected: {Web3.Account.PublicKey}");
        }
    }

    void OnDestroy()
    {
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

    #region Wallet Connection (In-Game Wallet)
    
    /// <summary>
    /// Connect wallet - called by button OR auto on start
    /// Uses in-game wallet with fixed password for testing
    /// </summary>
    public void ConnectWallet()
    {
        ConnectWalletAsync();
    }
    
    private async void ConnectWalletAsync()
    {
        if (isWalletConnected)
        {
            Debug.Log("⚠️ Wallet already connected! Disconnect first to switch wallets.");
            ShowError("wallet already connected !");
            return;
        }
        
        HideError();
        
        string walletPassword = GetWalletPassword();
        string walletMnemonic = GetWalletMnemonic();
        
        Debug.Log($"🔄 Connecting Test Wallet #{testWalletIndex}...");
        
        try
        {
            // Try to login with existing wallet
            var account = await Web3.Instance.LoginInGameWallet(walletPassword);
            
            // If no wallet exists, create one with specific mnemonic
            if (account == null)
            {
                Debug.Log($"📝 Creating Test Wallet #{testWalletIndex}...");
                account = await Web3.Instance.CreateAccount(walletMnemonic, walletPassword);
                
                if (account != null)
                {
                    Debug.Log($"✅ Test Wallet #{testWalletIndex} created!");
                    Debug.Log($"📍 Address: {account.PublicKey}");
                }
            }
            else
            {
                Debug.Log($"✅ Test Wallet #{testWalletIndex} loaded!");
            }
            
            if (account != null)
            {
                isWalletConnected = true;
                Debug.Log($"✅ Connected: {account.PublicKey}");
                
                // Load player name for this wallet
                if (PlayerDataManager.instance != null)
                {
                    PlayerDataManager.instance.LoadPlayerNameForWallet();
                }
                
                UpdateUI();
                UpdatePlayButtonState();
            }
            else
            {
                ShowError("Failed to create/load wallet");
            }
        }
        catch (System.Exception e)
        {
            ShowError($"Connection error: {e.Message}");
            Debug.LogError($"❌ Wallet error: {e}");
        }
    }
    
    /// <summary>
    /// Gets unique password for each test wallet
    /// </summary>
    private string GetWalletPassword()
    {
        return $"test-wallet-{testWalletIndex}";
    }
    
    /// <summary>
    /// Gets unique mnemonic for each test wallet
    /// Different mnemonics = different wallet addresses
    /// All mnemonics are valid BIP39 test mnemonics
    /// </summary>
    private string GetWalletMnemonic()
    {
        // Valid BIP39 mnemonics for consistent test wallets
        string[] testMnemonics = new string[]
        {
            // Wallet 0
            "pill tomorrow foster begin walnut pen idea crop memory mixed manage vintage",
            // Wallet 1
            "quality vacuum heart guard buzz spike sight swarm shove special gym robust",
            // Wallet 2
            "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about",
            // Wallet 3
            "test walk hurt river rhythm soft duck penalty window suit ensure mixed",
            // Wallet 4
            "echo echo echo echo echo echo echo echo echo echo echo nominee",
            // Wallet 5
            "lucky toy fortune desert wave nature patrol market wisdom typical venture napkin",
            // Wallet 6
            "game build develop remove energy nation field entry simple radio victory avoid",
            // Wallet 7
            "outer ride share memory bike place coast quiz bottom family season adjust",
            // Wallet 8
            "priority fetch prison avocado float top ceiling business ritual seminar sunset prize",
            // Wallet 9
            "humble city wave glory crush convince latin group earth circle quantum liberty"
        };
        
        return testMnemonics[testWalletIndex];
    }
    
    /// <summary>
    /// Disconnect current wallet - use this to switch to a different test wallet
    /// </summary>
    public void DisconnectWallet()
    {
        if (!isWalletConnected)
        {
            Debug.Log("No wallet connected");
            return;
        }
        
        Debug.Log("🔌 Disconnecting wallet...");
        Web3.Instance.Logout();
        isWalletConnected = false;
        UpdateUI();
        Debug.Log("✅ Disconnected. You can now connect a different test wallet.");
    }
    
    private void OnWalletConnected(Account account)
    {
        Debug.Log($"✅ Wallet connected event: {account.PublicKey}");
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
            Debug.Log("⚠️ Starting game without wallet");
            StartGame();
            return;
        }
        
        // Wallet connected - check if has name saved
        if (PlayerDataManager.instance != null && PlayerDataManager.instance.HasPlayerName())
        {
            // Already has name, start game
            Debug.Log($"✅ Starting game for: {PlayerDataManager.instance.GetPlayerName()}");
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
                Debug.Log($"✅ Name saved and starting game for: {playerName}");
                
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
            Debug.Log("⚠️ Starting without wallet - scores won't be saved");
        }
        else
        {
            Debug.Log($"✅ Starting with wallet: {Web3.Account.PublicKey}");
        }
        
        Debug.Log($"🎮 Starting game for player: {PlayerDataManager.instance?.GetPlayerName() ?? "Unknown"}");
        
        StartCoroutine(LoadGameScene());
    }

    public void ShowHowToPlay()
    {
        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(true);
        }
    }

    public void HideHowToPlay()
    {
        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(false);
        }
    }

    private IEnumerator LoadGameScene()
    {
        if (Fader.instance != null)
        {
            yield return Fader.instance.FadeOut();
        }
        else
        {
            yield return null;
        }
        
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
            
            Debug.Log("🎮 Play button: ENABLED (no wallet)");
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
            
            Debug.Log($"🎮 Play button: {(hasInputText ? "ENABLED" : "DISABLED")} (input text: '{nameInputField?.text}') | Input: VISIBLE");
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
            
            Debug.Log($"🎮 Play button: ENABLED (has name: {PlayerDataManager.instance.GetPlayerName()})");
        }
    }
    
    private void UpdateUI()
    {
        if (connectButtonText != null)
        {
            if (isWalletConnected && Web3.Account != null)
            {
                string address = Web3.Account.PublicKey.ToString();
                connectButtonText.text = $"✓ Wallet #{testWalletIndex}\n{ShortAddress(address)}";
            }
            else
            {
                connectButtonText.text = $"Connect Wallet #{testWalletIndex}";
            }
        }
    }
    
    private void ShowError(string message)
    {
        Debug.LogWarning($"[Error] {message}");
        
        if (errorText != null)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(true);
            
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
    
    #region Debug/Testing
    
    [ContextMenu("1. Connect Current Wallet")]
    private void QuickConnect()
    {
        ConnectWallet();
    }
    
    [ContextMenu("2. Disconnect Wallet")]
    private void QuickDisconnect()
    {
        DisconnectWallet();
    }
    
    [ContextMenu("3. Get Devnet Airdrop")]
    private async void RequestAirdrop()
    {
        if (Web3.Wallet == null)
        {
            Debug.LogError("Wallet not connected!");
            return;
        }
        
        Debug.Log("🪂 Requesting devnet airdrop...");
        
        try
        {
            var result = await Web3.Wallet.RequestAirdrop(
                amount: 1_000_000_000, // 1 SOL
                commitment: Solana.Unity.Rpc.Types.Commitment.Confirmed
            );
            
            if (result.WasSuccessful)
            {
                Debug.Log("✅ Airdrop successful! Got 1 SOL");
                await System.Threading.Tasks.Task.Delay(2000);
                
                double balance = await Web3.Wallet.GetBalance();
                Debug.Log($"💰 Balance: {balance:F4} SOL");
            }
            else
            {
                Debug.LogError($"❌ Airdrop failed: {result.Reason}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Airdrop error: {e.Message}");
        }
    }
    
    [ContextMenu("4. Check Balance")]
    private async void CheckBalance()
    {
        if (Web3.Wallet == null)
        {
            Debug.LogError("Wallet not connected!");
            return;
        }
        
        double balance = await Web3.Wallet.GetBalance();
        Debug.Log($"💰 Balance: {balance:F4} SOL");
    }
    
    [ContextMenu("5. Print Wallet Info")]
    private void PrintWalletInfo()
    {
        if (Web3.Wallet != null && Web3.Account != null)
        {
            Debug.Log("=== Wallet Info ===");
            Debug.Log($"Wallet Index: {testWalletIndex}");
            Debug.Log($"Address: {Web3.Account.PublicKey}");
            Debug.Log($"Connected: {isWalletConnected}");
        }
        else
        {
            Debug.Log($"No wallet connected (Test Wallet #{testWalletIndex} ready to connect)");
        }
    }
    
    [ContextMenu("6. Clear Player Name for Current Wallet")]
    private void ClearPlayerName()
    {
        if (PlayerDataManager.instance != null)
        {
            PlayerDataManager.instance.ClearPlayerData();
            Debug.Log("✅ Player name cleared for current wallet");
        }
        else
        {
            Debug.LogWarning("PlayerDataManager not found");
        }
    }
    
    [ContextMenu("6b. Clear ALL Player Names (All Wallets)")]
    private void ClearAllPlayerNames()
    {
        if (PlayerDataManager.instance != null)
        {
            PlayerDataManager.instance.ClearAllPlayerData();
            Debug.Log("✅ All player names cleared");
        }
        else
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("✅ All PlayerPrefs cleared");
        }
    }
    
    [ContextMenu("7. Print Player Info")]
    private void PrintPlayerInfo()
    {
        if (PlayerDataManager.instance != null)
        {
            Debug.Log("=== Player Info ===");
            Debug.Log($"Name: {PlayerDataManager.instance.GetPlayerName()}");
            Debug.Log($"Has Name: {PlayerDataManager.instance.HasPlayerName()}");
            Debug.Log($"Wallet Address: {PlayerDataManager.instance.GetWalletAddress()}");
            Debug.Log($"Score PDA: {PlayerDataManager.instance.GetScoreAccountAddress()}");
        }
        else
        {
            Debug.Log("PlayerDataManager not initialized");
        }
    }
    
    #endregion
}

