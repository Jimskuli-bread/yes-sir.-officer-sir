using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    public KeyCode pickupKey = KeyCode.E;
    public float pickupRange = 3f;

    [Header("Hold Position")]
    public Transform holdPoint;

    [Header("Throw Aim")]
    [SerializeField] private Transform aimCamera;

    [Header("Throw Settings")]
    public KeyCode throwKey = KeyCode.F;
    public float throwForce = 10f;

    private GameObject heldObject;
    private int pickableLayer;
    private static PlayerPickup persistentPlayer;

    public GameObject HeldObject => heldObject;

    private void Awake()
    {
        if (persistentPlayer != null && persistentPlayer != this)
        {
            Destroy(gameObject);
            return;
        }

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

        if (aimCamera == null && Camera.main != null)
            aimCamera = Camera.main.transform;

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
            {
                REDCHAIR heldChair = heldObject.GetComponent<REDCHAIR>() ??
                                     heldObject.GetComponentInChildren<REDCHAIR>() ??
                                     heldObject.GetComponentInParent<REDCHAIR>();
                if (heldChair != null)
                    ThrowHeldObject();
                else
                {
                    NPC.TryDeliverRedChair(heldObject);
                    DropObject();
                }
            }
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
            ThrowHeldObject();
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
        Key key = throwKey switch
        {
            KeyCode.E => Key.E,
            KeyCode.Q => Key.Q,
            KeyCode.F => Key.F,
            KeyCode.Space => Key.Space,
            _ => Key.F
        };

        return (Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame) ||
               (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
#else
        return Input.GetKeyDown(throwKey) || Input.GetMouseButtonDown(0);
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
            GameObject pickupObject = body != null ? body.gameObject : closestHit.gameObject;
            Transform gripPoint = pickupObject.GetComponent<BananaDetector>() != null
                ? closestHit.transform
                : null;
            PickupObject(pickupObject, gripPoint);
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

    private void PickupObject(GameObject obj, Transform gripPoint = null)
    {
        heldObject = obj;

        REDCHAIR redChair = obj.GetComponent<REDCHAIR>() ??
                            obj.GetComponentInChildren<REDCHAIR>() ??
                            obj.GetComponentInParent<REDCHAIR>();
        if (redChair != null)
            redChair.MarkPickedUp(transform);

        Vector3 gripLocalPosition = Vector3.zero;
        Quaternion gripLocalRotation = Quaternion.identity;
        if (gripPoint != null)
        {
            gripLocalPosition = obj.transform.InverseTransformPoint(gripPoint.position);
            gripLocalRotation = Quaternion.Inverse(obj.transform.rotation) * gripPoint.rotation;
        }

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        obj.transform.SetParent(holdPoint, false);
        obj.transform.localRotation = Quaternion.Inverse(gripLocalRotation);
        obj.transform.localPosition = -(obj.transform.localRotation *
            Vector3.Scale(gripLocalPosition, obj.transform.localScale));
    }

    public bool TryPickupObject(GameObject obj, bool keepPlayerAcrossScenes = false)
    {
        if (obj == null || heldObject != null || holdPoint == null)
            return false;

        if (pickableLayer >= 0)
        {
            obj.layer = pickableLayer;
        }

        PickupObject(obj);
        if (keepPlayerAcrossScenes)
        {
            persistentPlayer = this;
            DontDestroyOnLoad(transform.root.gameObject);
        }

        return true;
    }

    public bool ForcePickupObject(GameObject obj, bool keepPlayerAcrossScenes = false)
    {
        if (obj == null || holdPoint == null)
            return false;

        if (heldObject != null && heldObject != obj)
        {
            DropObject();
        }

        return TryPickupObject(obj, keepPlayerAcrossScenes);
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

    public void ReleaseBananaForQuestReset()
    {
        if (heldObject != null && heldObject.GetComponent<BananaPickup>() != null)
        {
            DropObject();
        }
    }

    public void ThrowHeldObject()
    {
        if (heldObject == null || holdPoint == null)
            return;

        RedChairQuest legacyChair = heldObject.GetComponent<RedChairQuest>();
        if (legacyChair != null)
            legacyChair.MarkThrown();

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
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;

            PlayerMovement playerMovement = GetComponent<PlayerMovement>();
            Transform directionSource = playerMovement != null && playerMovement.cameraTransform != null
                ? playerMovement.cameraTransform
                : aimCamera != null ? aimCamera : Camera.main != null ? Camera.main.transform : holdPoint;

            rb.AddForce(directionSource.forward.normalized * appliedThrowForce, ForceMode.Impulse);
        }

        heldObject = null;
    }
}
