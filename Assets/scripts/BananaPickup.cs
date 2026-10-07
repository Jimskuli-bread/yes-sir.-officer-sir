using System;
using UnityEngine;

public class BananaPickup : MonoBehaviour
{
    public static bool HasBanana { get; private set; }
    public static BananaPickup FirstBanana { get; private set; }
    public static event Action OnBananaPickedUp;

    [Header("Pickup Settings")]
    public string pickupMessage = "You picked up the banana.";

    private void Awake()
    {
        if (GetComponent<InspectaBanana>() == null)
            gameObject.AddComponent<InspectaBanana>();
    }

    public static bool SpawnIntoHand(GameObject prefab, PlayerPickup playerPickup)
    {
        if (prefab == null || playerPickup == null)
            return false;

        if (playerPickup.HeldObject != null && playerPickup.HeldObject.GetComponent<BananaPickup>() != null)
        {
            BananaPickup heldBanana = playerPickup.HeldObject.GetComponent<BananaPickup>();
            heldBanana.RegisterPickup();
            return true;
        }

        GameObject bananaObject = Instantiate(prefab);
        BananaPickup banana = bananaObject.GetComponent<BananaPickup>();
        if (banana == null)
        {
            banana = bananaObject.AddComponent<BananaPickup>();
        }

        if (!playerPickup.ForcePickupObject(bananaObject, true))
        {
            Destroy(bananaObject);
            return false;
        }

        return true;
    }

    public void RegisterPickup()
    {
        HasBanana = true;
        if (FirstBanana == null)
        {
            FirstBanana = this;
            Debug.Log(pickupMessage);
            OnBananaPickedUp?.Invoke();
        }
    }

    public static void ResetBanana()
    {
        HasBanana = false;
        FirstBanana = null;
    }
}
