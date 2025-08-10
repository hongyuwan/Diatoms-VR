using UnityEngine;

/// <summary>
/// This script fixes sorting issues for the seal model by adjusting its material's render queue.
/// Attach this to the main seal GameObject.
/// </summary>
public class SealRenderOrder : MonoBehaviour
{
    // A larger number means it will be rendered later, thus appearing on top.
    // The default for Transparent geometry is 3000. We add 1 to render on top of other transparents.
    public int renderQueue = 3001;

    void Start()
    {
        // Get all renderers in this object and its children.
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            // We modify the material instance, not the shared material asset,
            // to avoid affecting other objects that might share this material.
            renderer.material.renderQueue = renderQueue;
        }
    }
} 