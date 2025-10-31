using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class CooldownUI : MonoBehaviour
{
    private Slider cooldownSlider;
    private PlayerAim playerAim;
    public Image fillImage; // Assign the 'Fill' Image
    public Gradient fillColorGradient;
    public Color overheatCooldownColor = Color.black;
    public int flashCount = 3;
    public float flashDuration = 0.1f;

    private Coroutine flashRoutine = null;

    void Start()
    {
        cooldownSlider = GetComponent<Slider>();
        if (fillImage == null) fillImage = cooldownSlider.fillRect.GetComponent<Image>();

        playerAim = FindFirstObjectByType<PlayerAim>();
        if (playerAim == null)
        {
            Debug.LogError("CooldownUI could not find PlayerAim!", this);
            enabled = false;
            return;
        }

        playerAim.OnOverheatStart += StartOverheatFlash;
    }

    void OnDestroy()
    {
        if (playerAim != null)
        {
            playerAim.OnOverheatStart -= StartOverheatFlash;
        }
    }

    void Update()
    {
        if (playerAim != null)
        {
            float normalizedHeat = playerAim.GetCurrentHeatNormalized();

            // Handle visual state during overheat cooldown
            if (playerAim.IsOverheated)
            {
                if (flashRoutine != null) // --- Currently Flashing ---
                {
                    // Keep bar visually full during flash
                    cooldownSlider.value = 1f;
                    // Color is handled by the coroutine (white/red)
                }
                else // --- Cooling Down After Flash ---
                {
                    // Set slider value based on actual heat
                    cooldownSlider.value = normalizedHeat;
                    // Set color to black
                    fillImage.color = overheatCooldownColor;
                }
            }
            else // --- Not Overheated ---
            {
                cooldownSlider.value = normalizedHeat;
                fillImage.color = fillColorGradient.Evaluate(normalizedHeat); // Use gradient
            }
        }
    }

    private void StartOverheatFlash()
    {
        if (fillImage != null && flashRoutine == null) // Prevent multiple flashes
        {
            flashRoutine = StartCoroutine(FlashFillRoutine());
        }
    }

    private IEnumerator FlashFillRoutine()
    {
        // Store the color it *should* be at max heat (red)
        Color endGradientColor = fillColorGradient.Evaluate(1f);

        // Make sure the bar is visually full during the flash
        cooldownSlider.value = 1f;

        for (int i = 0; i < flashCount; i++)
        {
            fillImage.color = Color.white; // Flash white
            yield return new WaitForSeconds(flashDuration / 2f);

            // Only show red if not the very last flash (end on black)
            if (i < flashCount - 1)
            {
                fillImage.color = endGradientColor;
                yield return new WaitForSeconds(flashDuration / 2f);
            }
            else // On the last white flash, wait then switch to black
            {
                yield return new WaitForSeconds(flashDuration / 2f);
            }
        }

        // After the loop, set to the cooldown color immediately
        fillImage.color = overheatCooldownColor;
        flashRoutine = null; // Signal that flashing is done
    }
}