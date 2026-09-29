using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Prepares the DHopened helmet so XR controllers can pick it up.
/// Runs before XR Interaction Toolkit so the grab component registers valid colliders.
/// </summary>
[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class HelmetGrabSetup : MonoBehaviour
{
    void Awake()
    {
        RemoveEmptyMeshColliders();
        ConfigureRigidbody();
        ConfigureGrab();
        if (GetComponent<HelmetInspect>() == null)
            gameObject.AddComponent<HelmetInspect>();
        if (GetComponent<HelmetInstructionUI>() == null)
            gameObject.AddComponent<HelmetInstructionUI>();
        if (GetComponent<HelmetBoltRelease>() == null)
            gameObject.AddComponent<HelmetBoltRelease>();
        if (GetComponent<HelmetHeadWear>() == null)
            gameObject.AddComponent<HelmetHeadWear>();
        if (GetComponent<HelmetStand>() == null)
            gameObject.AddComponent<HelmetStand>();
    }

    void RemoveEmptyMeshColliders()
    {
        var meshColliders = GetComponentsInChildren<MeshCollider>(true);
        for (var i = 0; i < meshColliders.Length; i++)
        {
            var meshCollider = meshColliders[i];
            if (meshCollider.sharedMesh != null)
            {
                meshCollider.convex = true;
                meshCollider.isTrigger = false;
                continue;
            }

            DestroyImmediate(meshCollider);
        }
    }

    void ConfigureRigidbody()
    {
        var body = GetComponent<Rigidbody>();
        body.useGravity = true;
        body.isKinematic = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void ConfigureGrab()
    {
        var grab = GetComponent<XRGrabInteractable>();
        grab.addDefaultGrabTransformers = true;
        grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
        grab.useDynamicAttach = true;
        grab.matchAttachPosition = true;
        grab.matchAttachRotation = true;
        grab.snapToColliderVolume = false;
        grab.throwOnDetach = true;
        grab.retainTransformParent = false;
        grab.farAttachMode = InteractableFarAttachMode.Near;

        grab.colliders.Clear();
        var colliders = GetComponentsInChildren<Collider>(true);
        for (var i = 0; i < colliders.Length; i++)
        {
            var collider = colliders[i];
            if (collider != null && collider.enabled && !collider.isTrigger)
                grab.colliders.Add(collider);
        }
    }
}
