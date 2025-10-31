using UnityEngine;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Programs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using TMPro;
using System.Linq;

/// <summary>
/// Manages on-chain leaderboard interactions
/// Auto-submits scores when game ends
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    [Header("LeaderboardData")]
    [SerializeField]
    private List<TextMeshProUGUI> names;
    [SerializeField]
    private List<TextMeshProUGUI> scores;
    

    [Header("Program Settings")]
    [SerializeField] 
    private string _programId = "7pLicminxfjyL2Z85oTnQBD7VM3CbQm4niB83fM6eESD";

    private bool isSubmitting = false;



    /// <summary>
    /// Submits a player's score to the on-chain leaderboard
    /// Returns true if successful, false otherwise
    /// </summary>
    public async Task<bool> SubmitPlayerScore(ulong score)
    {
        // Prevent double submission
        if (isSubmitting)
        {
            Debug.LogWarning(" Already submitting a score...");
            return false;
        }
        
        // Check if wallet is connected
        if (Web3.Wallet == null || Web3.Account == null)
        {
            Debug.LogWarning(" No wallet connected - score not saved");
            return false;
        }

        isSubmitting = true;
        Account playerAccount = Web3.Account;
        
        Debug.Log($" Submitting score {score} for player: {playerAccount.PublicKey}");

        try
        {
            // 2. Derive the PDA for the player's score account
            // Seeds: [b"score", player.key().as_ref()]
            PublicKey playerScorePDA = FindPlayerScorePDA(playerAccount.PublicKey);
            Debug.Log($"Player Score PDA: {playerScorePDA}");
            
            // Cache the score PDA in PlayerDataManager
            if (PlayerDataManager.instance != null)
            {
                PlayerDataManager.instance.SetScoreAccountAddress(playerScorePDA.ToString());
            }

            // 3. Get the program ID as a PublicKey
            PublicKey programId = new PublicKey(_programId);

            // 4. Create the instruction data
            // For Anchor programs: [8-byte discriminator] + [arguments]
            byte[] instructionData = CreateSubmitScoreInstructionData(score);

            // 5. Define the accounts needed for the instruction
            // Order must match your Rust program's SubmitScore struct
            List<AccountMeta> keys = new List<AccountMeta>
            {
                AccountMeta.Writable(playerScorePDA, false),       // player_score (writable, not signer)
                AccountMeta.Writable(playerAccount.PublicKey, true), // player (writable, signer)
                AccountMeta.ReadOnly(SystemProgram.ProgramIdKey, false) // system_program
            };

            // 6. Create the transaction instruction
            TransactionInstruction submitScoreInstruction = new TransactionInstruction
            {
                ProgramId = programId,
                Keys = keys,
                Data = instructionData
            };

            // 7. Get a recent blockhash
            string recentBlockHash = await Web3.BlockHash();
            if (string.IsNullOrEmpty(recentBlockHash))
            {
                Debug.LogError("Failed to get recent blockhash");
                isSubmitting = false;
                return false;
            }

            // 8. Build the transaction
            Transaction transaction = new Transaction
            {
                RecentBlockHash = recentBlockHash,
                FeePayer = playerAccount.PublicKey,
                Instructions = new List<TransactionInstruction> { submitScoreInstruction },
                Signatures = new List<SignaturePubKeyPair>()
            };

            // 9. Sign and send the transaction
            Debug.Log(" Signing and sending transaction...");
            var result = await Web3.Wallet.SignAndSendTransaction(transaction);

            if (result.WasSuccessful)
            {
                Debug.Log($" Score submitted! Signature: {result.Result}");
                Debug.Log($" View on Explorer: https://explorer.solana.com/tx/{result.Result}?cluster=devnet");
                isSubmitting = false;
                return true;
            }
            else
            {
                Debug.LogError($" Transaction failed: {result.Reason}");
                isSubmitting = false;
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($" Error submitting score: {e.Message}\n{e.StackTrace}");
            isSubmitting = false;
            return false;
        }
    }

    /// <summary>
    /// Finds the PDA for a player's score account
    /// Seeds: [b"score", player_pubkey]
    /// </summary>
    private PublicKey FindPlayerScorePDA(PublicKey playerPublicKey)
    {
        PublicKey programId = new PublicKey(_programId);
        
        // Create the seeds as specified in your Anchor program
        List<byte[]> seeds = new List<byte[]>
        {
            Encoding.UTF8.GetBytes("score"),  // b"score"
            playerPublicKey.KeyBytes           // player.key().as_ref()
        };

        // Find the PDA
        bool success = PublicKey.TryFindProgramAddress(
            seeds,
            programId,
            out PublicKey pda,
            out byte bump
        );

        if (!success)
        {
            throw new Exception("Failed to find PDA for player score account");
        }

        Debug.Log($"Found PDA with bump: {bump}");
        return pda;
    }

    /// <summary>
    /// Creates the instruction data for the submit_score instruction
    /// Format: [8-byte Anchor discriminator] + [8-byte u64 score]
    /// </summary>
    private byte[] CreateSubmitScoreInstructionData(ulong score)
    {
        // For Anchor programs, the discriminator is the first 8 bytes of:
        // SHA256("global:submit_score")
        byte[] discriminator = GetAnchorDiscriminator("global:submit_score");

        // Allocate buffer for discriminator + score
        byte[] data = new byte[16]; // 8 bytes discriminator + 8 bytes u64

        // Copy discriminator
        Array.Copy(discriminator, 0, data, 0, 8);

        // Serialize the score as little-endian u64
        byte[] scoreBytes = BitConverter.GetBytes(score);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(scoreBytes);
        }
        Array.Copy(scoreBytes, 0, data, 8, 8);

        return data;
    }

    /// <summary>
    /// Computes the Anchor instruction discriminator
    /// </summary>
    private byte[] GetAnchorDiscriminator(string instructionName)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(instructionName));
            byte[] discriminator = new byte[8];
            Array.Copy(hash, 0, discriminator, 0, 8);
            return discriminator;
        }
    }
}