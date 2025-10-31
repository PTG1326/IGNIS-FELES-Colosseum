using UnityEngine;

public class PlayerLayerManager : MonoBehaviour
{
    private SpriteRenderer[] allRenderers;
    private Transform[] allTransforms;

    void Awake()
    {
        // Get all SpriteRenderers on this object and on any of its children
        allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        
        // Get all Transforms, which includes the parent and all children
        allTransforms = GetComponentsInChildren<Transform>(true);
    }

    // This function sets the 2D sorting (visual) layer
    public void SetSorting(string layerName, int orderInLayer)
    {
        // Loop through every renderer (player, aim indicator, etc.)
        foreach (SpriteRenderer renderer in allRenderers)
        {
            renderer.sortingLayerName = layerName;
            renderer.sortingOrder = orderInLayer;
        }
    }

    // This function sets the physics layer
    public void SetPhysicsLayer(string layerName)
    {
        // Convert the layer name (string) to a layer ID (integer)
        int newLayer = LayerMask.NameToLayer(layerName);

        if (newLayer == -1)
        {
            Debug.LogError("The physics layer '" + layerName + "' does not exist. Please add it in Edit > Project Settings > Tags and Layers.");
            return;
        }

        // Loop through every transform (player, children, etc.)
        foreach (Transform trans in allTransforms)
        {
            // Set the physics layer for its GameObject
            trans.gameObject.layer = newLayer;
        }
    }
}