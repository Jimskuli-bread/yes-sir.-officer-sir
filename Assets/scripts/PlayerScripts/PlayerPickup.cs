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

        if (aimCamera == null && Camera.main != null)
        {
            aimCamera = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (WasPickupPressed())
        {
            if (heldObject == null)
                TryPickup();
            else
            {
                NPC.TryDeliverRedChair(heldObject);
                DropObject();
            }
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
        if (Keyboard.current == null)
            return false;

        Key key = throwKey switch
        {
            KeyCode.E => Key.E,
            KeyCode.Q => Key.Q,
            KeyCode.F => Key.F,
            KeyCode.Space => Key.Space,
            _ => Key.F
        };

        return Keyboard.current[key].wasPressedThisFrame;
#else
        return Input.GetKeyDown(throwKey);
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

    private void PickupObject(GameObject obj)
    {
        heldObject = obj;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        obj.transform.SetParent(holdPoint, false);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
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

        RedChairQuest redChair = heldObject.GetComponent<RedChairQuest>();
        if (redChair != null)
        {
            redChair.MarkThrown();
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

            rb.AddForce(directionSource.forward.normalized * throwForce, ForceMode.Impulse);
        }

        heldObject = null;
    }
}
