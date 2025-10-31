using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    // This is a "Singleton" pattern, it allows any script to
    // access the score easily using ScoreManager.instance
    public static ScoreManager instance;

    public int currentScore = 0;

    void Awake()
    {
        // Set up the Singleton
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

    public void AddScore(int amount)
    {
        currentScore += amount;
        Debug.Log("Score: " + currentScore);
    }
}