using UnityEngine;
// 为 XRI 3.0.8 使用正确的、精确的命名空间


/// <summary>
/// This script is responsible for making a target object interactable for XR.
/// This version is specifically for XR Interaction Toolkit 3.0.8.
/// It dynamically adds and configures an XRGrabInteractable to allow
/// for two-handed rotation and scaling.
/// </summary>
public class ObjectInteraction : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _currentInteractable;
    private Vector3 _initialScale;
    private Quaternion _initialRotation;

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            if (child != null) SetLayerRecursively(child.gameObject, layer);
        }
    }

    /// <summary>
    /// Configures the target object to be grabbable and manipulable by the user in XR.
    /// </summary>
    /// <param name="newTarget">The transform of the GameObject to make interactable.</param>
    public void SetTarget(Transform newTarget)
    {
        if (newTarget == null)
        {
            Debug.LogError("ObjectInteraction: SetTarget was called with a null target.", this);
            return;
        }

        // Put object on a raycastable Physics Layer.
        // NOTE: We are commenting this out. We assume the bubble prefab (and thus this contained object)
        // is already on a layer that the XR Ray Interactor can hit. Forcing it to 'Default'
        // can cause issues if the interactor's raycast mask doesn't include the 'Default' layer.
        // var defaultLayer = LayerMask.NameToLayer("Default");
        // if (defaultLayer >= 0)
        //     SetLayerRecursively(newTarget.gameObject, defaultLayer);

        // Store initial transform state to reset it later
        _initialScale = newTarget.localScale;
        _initialRotation = newTarget.localRotation;
        
        // Get or add the XRGrabInteractable component
        _currentInteractable = newTarget.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (_currentInteractable == null)
        {
            _currentInteractable = newTarget.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        }

        // Ensure there is at least one collider for ray selection
        var anyCollider = _currentInteractable.GetComponent<Collider>();
        if (anyCollider == null)
        {
            // Approximate collider from renderers' bounds
            var renderers = newTarget.GetComponentsInChildren<Renderer>();
            if (renderers != null && renderers.Length > 0)
            {
                Bounds worldBounds = new Bounds(renderers[0].bounds.center, Vector3.zero);
                foreach (var r in renderers) worldBounds.Encapsulate(r.bounds);
                var box = _currentInteractable.gameObject.AddComponent<BoxCollider>();
                box.center = newTarget.InverseTransformPoint(worldBounds.center);
                // Convert world size to local size by compensating lossyScale
                Vector3 ls = newTarget.lossyScale;
                ls.x = Mathf.Approximately(ls.x, 0f) ? 1f : ls.x;
                ls.y = Mathf.Approximately(ls.y, 0f) ? 1f : ls.y;
                ls.z = Mathf.Approximately(ls.z, 0f) ? 1f : ls.z;
                box.size = new Vector3(worldBounds.size.x / ls.x, worldBounds.size.y / ls.y, worldBounds.size.z / ls.z) * 1.1f; // slightly inflate
                box.isTrigger = false;
                anyCollider = box;
            }
            else
            {
                anyCollider = _currentInteractable.gameObject.AddComponent<BoxCollider>();
                ((BoxCollider)anyCollider).size = Vector3.one * 0.1f;
                anyCollider.isTrigger = false;
            }
        }
        else
        {
            anyCollider.enabled = true;
            anyCollider.isTrigger = false;
        }

        // Explicitly register colliders with Grab Interactable
        var cols = _currentInteractable.GetComponentsInChildren<Collider>(true);
        _currentInteractable.colliders.Clear();
        foreach (var c in cols)
        {
            if (c.enabled) _currentInteractable.colliders.Add(c);
        }

        // --- Configure the Interactable for XR (XRI 3.0.8 API) ---
        _currentInteractable.useDynamicAttach = true;
        _currentInteractable.movementType = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.Instantaneous;
        _currentInteractable.trackPosition = true;
        _currentInteractable.trackRotation = true;
        _currentInteractable.trackScale = true;

        // Allow two controllers to select at the same time for two-handed rotate/scale
        _currentInteractable.selectMode = UnityEngine.XR.Interaction.Toolkit.Interactables.InteractableSelectMode.Multiple;
        _currentInteractable.addDefaultGrabTransformers = true; // ensure default transformers are active
        // Include all interaction layers
        _currentInteractable.interactionLayers = (UnityEngine.XR.Interaction.Toolkit.InteractionLayerMask)(-1);

        // Ensure general grab transformer exists and is configured for two-handed features
        var general = _currentInteractable.GetComponent<UnityEngine.XR.Interaction.Toolkit.Transformers.XRGeneralGrabTransformer>();
        if (general == null)
        {
            general = _currentInteractable.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Transformers.XRGeneralGrabTransformer>();
        }
        general.allowTwoHandedScaling = true;
        general.allowTwoHandedRotation = UnityEngine.XR.Interaction.Toolkit.Transformers.XRGeneralGrabTransformer.TwoHandedRotationMode.TwoHandedAverage;

        // 确保存在刚体并禁用重力，防止物体在被抓取前下落
        var rb = _currentInteractable.GetComponent<UnityEngine.Rigidbody>() ?? _currentInteractable.gameObject.AddComponent<UnityEngine.Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Finally, enable the component so the user can interact with it
        _currentInteractable.enabled = true;

        Debug.Log($"[ObjectInteraction] XRGrabInteractable ready. selectMode={_currentInteractable.selectMode}, addDefaultGrabTransformers={_currentInteractable.addDefaultGrabTransformers}, colliders={_currentInteractable.colliders.Count}", _currentInteractable);
    }

    /// <summary>
    /// Resets the object to its initial state and disables XR interaction.
    /// </summary>
    public void ResetAndDisable()
    {
        if (_currentInteractable != null)
        {
            _currentInteractable.enabled = false;
            
            _currentInteractable.transform.localScale = _initialScale;
            _currentInteractable.transform.localRotation = _initialRotation;
        }

        _currentInteractable = null;
    }
}
