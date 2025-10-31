using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Linq;

public class FovController : MonoBehaviour
{
    [Header("FOV Settings")]
    public float baseFovRadius = 5f;
    public float fovIncreaseAmount = 1.5f;
    public float maxFovRadius = 7.5f;
    public float fadeWidth = 3f;
    public float pixelationAmount = 50f;

    [Header("Upgrade Settings")]
    public int[] scoreThresholds = { 250, 500, 750 };
    public GameObject upgradeTextPrefab; // Assign your "FovUpgradeText" prefab
    public float darkeningDuration = 0.5f; // How long it takes to darken
    public float textFadeDuration = 0.75f; // How long text fades in/out
    public float textDisplayDuration = 1.0f; // How long text stays fully visible
    public Color darkOverlayColor = new Color(0.5f, 0.5f, 0.5f, 1f); // --- MODIFIED: Color for darkening (e.g., 50% gray, full alpha) ---
                                                                    // This is the COLOR that will multiply the scene.

    [Header("References")]
    public Camera mainCamera;
    public Image fovMaskImage;
    public RawImage lightingOverlayImage; // Assign your LightmapOverlayImage here

    private Material fovMaterial;
    private float currentFovRadius;
    private int currentFovLevel = 0;
    private bool isUpgrading = false;
    private Color initialOverlayColor; // To store original lighting overlay color (which should be white)

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (fovMaskImage == null || mainCamera == null || upgradeTextPrefab == null || lightingOverlayImage == null)
        {
            Debug.LogError("FovController missing references! Check mainCamera, fovMaskImage, upgradeTextPrefab, AND lightingOverlayImage.", this);
            enabled = false; return;
        }
        fovMaterial = fovMaskImage.material;
        if (fovMaterial == null)
        {
            Debug.LogError("FovMaskImage missing material instance!", this);
            enabled = false; return;
        }

        currentFovRadius = baseFovRadius;
        currentFovLevel = 0;
        isUpgrading = false;

        // Store the initial color of the lighting overlay (it should be white)
        // Ensure its color is set to white at start, so the lightmap works as intended.
        initialOverlayColor = Color.white; // Explicitly set to white (R:1, G:1, B:1, A:1)
        lightingOverlayImage.color = initialOverlayColor;

        UpdateShaderProperties(); // Set initial shader values
    }

    void Update()
    {
        if (isUpgrading) return;

        UpdateShaderProperties();
        CheckForFovUpgrade();
    }

    void UpdateShaderProperties()
    {
        Vector2 playerViewportPos = mainCamera.WorldToViewportPoint(transform.position);
        fovMaterial.SetVector("_Center", playerViewportPos);
        fovMaterial.SetFloat("_Radius", currentFovRadius / (mainCamera.orthographicSize * 2f));
        fovMaterial.SetFloat("_FadeWidth", fadeWidth / (mainCamera.orthographicSize * 2f));
        fovMaterial.SetFloat("_PixelationAmount", pixelationAmount);
    }

    void CheckForFovUpgrade()
    {
        if (ScoreManager.instance != null && currentFovLevel < scoreThresholds.Length)
        {
            if (ScoreManager.instance.currentScore >= scoreThresholds[currentFovLevel])
            {
                StartCoroutine(UpgradeFovSequence());
            }
        }
    }

    private IEnumerator UpgradeFovSequence()
    {

        SoundManager.instance.PlaySound("BurnsBrighter");

        isUpgrading = true;
        Time.timeScale = 0f;

        // Find all active enemy AI scripts in the scene
        var allEnemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None); // Adjust if using RangedEnemyAI or a base class
        var allRangedEnemies = FindObjectsByType<RangedEnemyAI>(FindObjectsSortMode.None); // Find ranged enemies too
        var allStompEnemies = FindObjectsByType<StompEnemyAI>(FindObjectsSortMode.None); // Find stomp enemies too
        
        // Store velocities and stop them (optional but smoother)
        // Dictionary<Rigidbody2D, Vector2> enemyVelocities = new Dictionary<Rigidbody2D, Vector2>(); 
        foreach (var enemy in allEnemies) {
            enemy.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero; // Stop movement
            enemy.enabled = false; // Disable the AI script temporarily
        }
        foreach (var enemy in allRangedEnemies)
        {
            enemy.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            enemy.enabled = false;
        }
        foreach (var enemy in allStompEnemies)
        {
            enemy.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            enemy.enabled = false;
        }

        // --- 1. Darken the screen ---
        // Lerp from initialOverlayColor (white) to darkOverlayColor
        yield return StartCoroutine(FadeOverlayColor(initialOverlayColor, darkOverlayColor, darkeningDuration));

        // --- 2. Spawn and Fade in Text ---
        GameObject upgradeTextInstance = null;
        CanvasGroup textCanvasGroup = null;
        if (upgradeTextPrefab != null)
        {
            Transform parentCanvasTransform = Fader.instance?.transform;

            // Fallback to the FovCanvas if Fader doesn't exist (though it should)
            if (parentCanvasTransform == null)
            {
                parentCanvasTransform = fovMaskImage.GetComponentInParent<Canvas>().transform;
            }
            
            if (parentCanvasTransform != null)
            {
                upgradeTextInstance = Instantiate(upgradeTextPrefab, parentCanvasTransform);
                textCanvasGroup = upgradeTextInstance.GetComponent<CanvasGroup>();
                if (textCanvasGroup == null) {
                    Debug.LogWarning("FovUpgradeText prefab needs a CanvasGroup component!", upgradeTextInstance);
                } else {
                    textCanvasGroup.alpha = 0f;
                    yield return StartCoroutine(FadeCanvasGroupAlpha(textCanvasGroup, 0f, 1f, textFadeDuration));
                }
            }
        }

        // --- 3. Hold Text Visible ---
        yield return new WaitForSecondsRealtime(textDisplayDuration);

        // --- 4. Fade out Text ---
        if (textCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroupAlpha(textCanvasGroup, 1f, 0f, textFadeDuration));
        }

        // --- 5. Destroy Text ---
        if (upgradeTextInstance != null)
        {
            Destroy(upgradeTextInstance);
        }

        // --- 6. Apply FOV Upgrade ---
        currentFovLevel++;
        currentFovRadius = Mathf.Min(baseFovRadius + (currentFovLevel * fovIncreaseAmount), maxFovRadius);
        Debug.Log($"FOV Upgraded! Level: {currentFovLevel}, New Radius: {currentFovRadius}");

        // --- 7. Bring screen back to normal (fade back to white) ---
        yield return StartCoroutine(FadeOverlayColor(darkOverlayColor, initialOverlayColor, darkeningDuration));

        // --- NEW: Resume Enemies ---
        foreach (var enemy in allEnemies) {
             if (enemy != null) { // Check if enemy wasn't destroyed during pause
                enemy.enabled = true; // Re-enable the AI script
             }
        }
        foreach (var enemy in allRangedEnemies)
        {
            if (enemy != null)
            {
                enemy.enabled = true;
            }
        }
        foreach (var enemy in allStompEnemies) {
             if (enemy != null) {
                enemy.enabled = true;
             }
        }

        // --- 8. Resume game ---
        Time.timeScale = 1f;
        isUpgrading = false;
    }

    // --- NEW/MODIFIED Coroutine to fade the COLOR of the lighting overlay ---
    private IEnumerator FadeOverlayColor(Color startColor, Color endColor, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            // Lerp the entire color (RGB and A)
            lightingOverlayImage.color = Color.Lerp(startColor, endColor, timer / duration);
            yield return null;
        }
        lightingOverlayImage.color = endColor; // Ensure final color is exact
    }

    // Coroutine to fade the alpha of a CanvasGroup (remains unchanged)
    private IEnumerator FadeCanvasGroupAlpha(CanvasGroup cg, float startAlpha, float endAlpha, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            yield return null;
        }
        cg.alpha = endAlpha;
    }
}