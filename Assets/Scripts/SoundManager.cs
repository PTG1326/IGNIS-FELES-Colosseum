using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections; // --- NEW: Needed for Coroutines ---

// Sound class (no changes)
[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 0.7f;
    [Range(0.1f, 3f)] public float pitch = 1f;
    [HideInInspector] public AudioSource source;
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    [Header("Audio Sources")]
    public AudioSource musicSource; // --- NEW: Dedicated source for BGM ---

    [Header("Audio Pool (SFX)")]
    public int poolSize = 15;
    private List<AudioSource> sourcePool = new List<AudioSource>();

    [Header("Audio Clips")]
    public Sound[] sfxClips; // --- RENAMED: For clarity ---
    public Sound[] musicTracks; // --- NEW: Array for BGM ---

    // --- NEW: Music fading variables ---
    private Coroutine musicFadeCoroutine;
    public float musicFadeDuration = 1.0f;

    void Awake()
    {
        // --- Singleton Pattern ---
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // --- IMPORTANT: Make sure this is UNCOMMENTED ---
        DontDestroyOnLoad(gameObject);
        // --- END IMPORTANT ---

        // --- Create the AudioSource Pool for SFX ---
        for (int i = 0; i < poolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0.0f;
            sourcePool.Add(source);
        }

        // --- NEW: Setup the Music Source ---
        // If you didn't add it in the Inspector, create one
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }
        musicSource.playOnAwake = false;
        musicSource.loop = true; // Music should loop
        musicSource.spatialBlend = 0.0f;
        // --- END NEW ---
    }

    // --- SFX Play Function (Renamed parameter for clarity) ---
    public void PlaySound(string soundName)
    {
        Sound s = Array.Find(sfxClips, sound => sound.name == soundName); // --- RENAMED ---
        if (s == null)
        {
            Debug.LogWarning("SoundManager: SFX '" + soundName + "' not found!");
            return;
        }

        AudioSource sourceToUse = GetAvailableSource();
        if (sourceToUse == null)
        {
            Debug.LogWarning("SoundManager: SFX Pool is full. Skipping sound: " + soundName);
            return;
        }

        sourceToUse.pitch = s.pitch;
        sourceToUse.PlayOneShot(s.clip, s.volume);
    }

    private AudioSource GetAvailableSource()
    {
        foreach (AudioSource source in sourcePool)
        {
            if (!source.isPlaying) return source;
        }
        return null;
    }

    // --- NEW: Music Control Functions ---
    public void PlayMusic(string trackName)
    {
        Sound s = Array.Find(musicTracks, track => track.name == trackName);

        if (s == null)
        {
            Debug.LogWarning("SoundManager: Music track '" + trackName + "' not found!");
            return;
        }

        // Don't restart if it's already playing
        if (musicSource.clip == s.clip && musicSource.isPlaying)
        {
            return;
        }

        // Stop any existing fade and start the new one
        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
        }
        musicFadeCoroutine = StartCoroutine(FadeMusic(s.clip, s.volume));
    }

    private IEnumerator FadeMusic(AudioClip newClip, float targetVolume)
    {
        // 1. Fade out the current track
        float startVolume = musicSource.volume;
        float timer = 0f;
        while (timer < musicFadeDuration / 2f)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, timer / (musicFadeDuration / 2f));
            yield return null;
        }
        
        musicSource.Stop();
        musicSource.volume = 0f; // Ensure it's silent

        // 2. Set new track and fade in
        musicSource.clip = newClip;
        musicSource.Play();
        
        timer = 0f;
        while (timer < musicFadeDuration / 2f)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, timer / (musicFadeDuration / 2f));
            yield return null;
        }

        musicSource.volume = targetVolume;
        musicFadeCoroutine = null;
    }
    // --- END NEW ---
}