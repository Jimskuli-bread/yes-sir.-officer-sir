using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    public KeyCode pickupKey = KeyCode.E;
    public float pickupRange = 3f;

    [Header("Hold Position")]
    public Transform holdPoint;

    [Header("Throw Settings")]
    public float throwForce = 10f;

    private GameObject heldObject;
    private int pickableLayer;

    private void Awake()
    {
        pickableLayer = LayerMask.NameToLayer("Pickable");
        if (pickableLayer < 0)
        {
            Debug.LogError("Create a layer named 'Pickable' and assign it to objects the player can pick up.");
        }

        if (holdPoint == null)
        {
            Transform cameraTransform = GetComponentInChildren<Camera>()?.transform;
            Transform holdParent = cameraTransform != null ? cameraTransform : transform;
            GameObject holdPointObject = new GameObject("HoldPoint");
            holdPointObject.transform.SetParent(holdParent, false);
            holdPointObject.transform.localPosition = cameraTransform != null
                ? new Vector3(0.35f, -0.25f, 1f)
                : new Vector3(0f, 1.4f, 1f);
            holdPoint = holdPointObject.transform;
        }

        EnsureChairComponents();
    }

    private void EnsureChairComponents()
    {
        Transform[] sceneObjects = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (Transform candidate in sceneObjects)
        {
            if (candidate.IsChildOf(transform) ||
                candidate.name.IndexOf("chair", System.StringComparison.OrdinalIgnoreCase) < 0 ||
                candidate.GetComponentInParent<REDCHAIR>() != null)
            {
                continue;
            }

            Transform chairRoot = candidate;
            while (chairRoot.parent != null &&
                   chairRoot.parent.name.IndexOf("chair", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                chairRoot = chairRoot.parent;
            }

            if (chairRoot.GetComponentInChildren<REDCHAIR>() == null)
                chairRoot.gameObject.AddComponent<REDCHAIR>();
        }
    }

    private void Update()
    {
        REDCHAIR nearbyChair = FindNearbyChair();
        if (nearbyChair != null)
            nearbyChair.UpdatePlayerProximity(transform, IsHoldingChair(nearbyChair));

        if (WasApologyPressed() && nearbyChair != null && nearbyChair.HasBeenThrown)
            nearbyChair.Apologize(transform);

        if (WasPickupPressed())
        {
            if (heldObject != null)
                ThrowObject();
            else if (nearbyChair != null)
            {
                if (nearbyChair.CanBePickedUp)
                {
                    bool completedChairTask = nearbyChair.HasCompletedApologies;
                    string nextSceneName = nearbyChair.nextSceneName;
                    PickupObject(nearbyChair.gameObject);
                    if (completedChairTask)
                        SceneReturnTracker.LoadScene(nextSceneName);
                }
                else
                    nearbyChair.RefusePickup(transform);
            }
            else
                TryPickup();
        }

        if (heldObject != null && WasThrowPressed())
        {
            ThrowObject();
        }
    }

    private bool WasPickupPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
            return false;

        Key key = pickupKey switch
        {
            KeyCode.E => Key.E,
            KeyCode.Q => Key.Q,
            KeyCode.F => Key.F,
            KeyCode.Space => Key.Space,
            _ => Key.E
        };

        return Keyboard.current[key].wasPressedThisFrame;
#else
        return Input.GetKeyDown(pickupKey);
#endif
    }

    private bool WasThrowPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

        private bool WasApologyPressed()
        {
    #if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
    #else
        return Input.GetKeyDown(KeyCode.Q);
    #endif
        }

    private void TryPickup()
    {
        if (holdPoint == null || pickableLayer < 0)
        {
            Debug.LogWarning("Assign a hold point and a valid Pickable layer before picking up objects.");
            return;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRange, 1 << pickableLayer);
        Collider closestHit = null;
        float closestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            float distance = (hit.transform.position - transform.position).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestHit = hit;
            }
        }

        if (closestHit != null)
        {
            Rigidbody body = closestHit.attachedRigidbody;
            PickupObject(body != null ? body.gameObject : closestHit.gameObject);
        }
    }

    private REDCHAIR FindNearbyChair()
    {
        REDCHAIR[] chairs = FindObjectsByType<REDCHAIR>(FindObjectsSortMode.None);
        REDCHAIR closestChair = null;
        float closestDistance = float.MaxValue;

        foreach (REDCHAIR chair in chairs)
        {
            float distance = (chair.transform.position - transform.position).sqrMagnitude;
            if (distance <= chair.interactionRange * chair.interactionRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestChair = chair;
            }
        }

        return closestChair;
    }

    private bool IsHoldingChair(REDCHAIR chair)
    {
        if (heldObject == null)
            return false;

        Transform heldTransform = heldObject.transform;
        Transform chairTransform = chair.transform;
        return heldTransform == chairTransform ||
               heldTransform.IsChildOf(chairTransform) ||
               chairTransform.IsChildOf(heldTransform);
    }

    private void PickupObject(GameObject obj)
    {
        heldObject = obj;

        REDCHAIR redChair = obj.GetComponent<REDCHAIR>() ??
                            obj.GetComponentInChildren<REDCHAIR>() ??
                            obj.GetComponentInParent<REDCHAIR>();
        if (redChair != null)
            redChair.MarkPickedUp(transform);

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        obj.transform.SetParent(holdPoint, false);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
    }

    private void DropObject()
    {
        if (heldObject == null) return;

        heldObject.transform.SetParent(null, true);

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = false;

        heldObject = null;
    }

    private void ThrowObject()
    {
        REDCHAIR redChair = heldObject.GetComponent<REDCHAIR>() ??
                            heldObject.GetComponentInChildren<REDCHAIR>() ??
                            heldObject.GetComponentInParent<REDCHAIR>();

        float appliedThrowForce = throwForce;
        if (redChair != null)
        {
            redChair.MarkThrown(transform);
            appliedThrowForce = redChair.GetThrowForce();
        }

        heldObject.transform.SetParent(null, true);

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.AddForce(holdPoint.forward * appliedThrowForce, ForceMode.Impulse);
        }

        heldObject = null;
    }
}
