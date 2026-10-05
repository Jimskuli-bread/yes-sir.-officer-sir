using UnityEngine;

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

    void Update()
    {
        // Toggle pickup/drop
        if (Input.GetKeyDown(pickupKey))
        {
            if (heldObject == null)
                TryPickup();
            else
                DropObject();
        }

        // Throw with left click
        if (heldObject != null && Input.GetMouseButtonDown(0))
        {
            ThrowObject();
        }

        // Keep object locked to hold point
        if (heldObject != null)
        {
            heldObject.transform.position = holdPoint.position;
        }
    }

    void TryPickup()
    {
        int pickableLayer = LayerMask.NameToLayer("Pickable");
        if (pickableLayer < 0) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRange, 1 << pickableLayer);
        if (hits.Length > 0)
            PickupObject(hits[0].gameObject);
    }

    void PickupObject(GameObject obj)
    {
        heldObject = obj;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        obj.transform.position = holdPoint.position;
        obj.transform.SetParent(holdPoint);
    }


    void DropObject()
    {
        if (heldObject == null) return;

        heldObject.transform.SetParent(null);

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = false;

        heldObject = null;
    }


    void ThrowObject()
    {
        heldObject.transform.SetParent(null);

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.AddForce(holdPoint.forward * throwForce, ForceMode.Impulse);
        }

        heldObject = null;
    }
}
