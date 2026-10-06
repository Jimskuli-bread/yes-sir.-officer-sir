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
    }

    private void Update()
    {
        if (WasPickupPressed())
        {
            if (heldObject == null)
                TryPickup();
            else
                DropObject();
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
        heldObject.transform.SetParent(null, true);

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.AddForce(holdPoint.forward * throwForce, ForceMode.Impulse);
        }

        heldObject = null;
    }
}
