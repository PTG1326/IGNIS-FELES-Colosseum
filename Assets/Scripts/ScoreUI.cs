using UnityEngine;
using TMPro; // You MUST add this line to use TextMeshPro

[RequireComponent(typeof(TextMeshProUGUI))]
public class ScoreUI : MonoBehaviour
{
    private TextMeshProUGUI scoreText;

    void Awake()
    {
        // Get the TextMeshPro component that is on this same GameObject
        scoreText = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        // Check if the ScoreManager instance exists
        if (ScoreManager.instance != null)
        {
            // Update the text to show the current score
            // The $ sign lets us inject a variable directly into the string
            scoreText.text = $"{ScoreManager.instance.currentScore}";
        }
        else
        {
            // Failsafe in case the ScoreManager hasn't loaded yet
            scoreText.text = "000";
        }
    }
}