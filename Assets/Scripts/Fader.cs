using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Fader : MonoBehaviour
{
    public static Fader instance; // A static instance for easy access (Singleton)

    public Image fadeImage;
    public float fadeDuration = 1f;

    void Awake()
    {
        // Singleton pattern: Ensure only one instance of the Fader exists.
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject); // Persist this object between scenes.

        fadeImage.color = new Color(0, 0, 0, 0);
    }

    // Subscribe to the sceneLoaded event when this object is enabled.
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Coroutine to fade from transparent to black (fade out)
    public IEnumerator FadeOut()
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            // Lerp the alpha from 0 to 1 over time
            fadeImage.color = new Color(0, 0, 0, Mathf.Lerp(0, 1, timer / fadeDuration));
            timer += Time.deltaTime;
            yield return null;
        }
        // Ensure it's fully black
        fadeImage.color = new Color(0, 0, 0, 1);
    }

    // Coroutine to fade from black to transparent (fade in)
    public IEnumerator FadeIn()
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            // Lerp the alpha from 1 to 0 over time
            fadeImage.color = new Color(0, 0, 0, Mathf.Lerp(1, 0, timer / fadeDuration));
            timer += Time.deltaTime;
            yield return null;
        }
        // Ensure it's fully transparent
        fadeImage.color = new Color(0, 0, 0, 0);
    }

    // This method is called every time a new scene finishes loading.
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Automatically fade in when a new scene loads.
        StartCoroutine(FadeIn());
    }
    
    // Unsubscribe when this object is disabled.
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}