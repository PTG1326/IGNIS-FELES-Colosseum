using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake instance; // Singleton for easy access

    [Header("Shake Settings")]
    public float shakeDuration = 0.2f;
    public float shakeMagnitudeMultiplier = 0.05f;
    public float decreaseFactor = 1.0f;

    // We no longer need originalPos, as we are always resetting localPosition to zero relative to parent
    private Coroutine shakeCoroutine;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Ensure camera starts at its local origin relative to CameraHolder
        transform.localPosition = Vector3.zero;
    }

    public void Shake(float knockbackForce)
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            transform.localPosition = Vector3.zero; // Reset local position immediately
        }
        shakeCoroutine = StartCoroutine(ShakeRoutine(knockbackForce));
    }

    private IEnumerator ShakeRoutine(float knockbackForce)
    {
        float currentMagnitude = knockbackForce * shakeMagnitudeMultiplier;
        float currentDuration = shakeDuration;

        while (currentDuration > 0 && currentMagnitude > 0)
        {
            Vector3 randomOffset = Random.insideUnitSphere * currentMagnitude;
            randomOffset.z = 0; // Keep 2D

            // Directly modify the camera's LOCAL position
            transform.localPosition = randomOffset;

            currentMagnitude -= Time.deltaTime * decreaseFactor;
            currentDuration -= Time.deltaTime;

            yield return null;
        }

        transform.localPosition = Vector3.zero; // Ensure it ends at local origin
        shakeCoroutine = null;
    }

    public void StopShake()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            transform.localPosition = Vector3.zero;
            shakeCoroutine = null;
        }
    }
}