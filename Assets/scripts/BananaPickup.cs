using System;
using UnityEngine;

public class BananaPickup : MonoBehaviour
{
    public static bool HasBanana { get; private set; }
    public static event Action OnBananaPickedUp;

    [Header("Pickup Settings")]
    public string pickupMessage = "You picked up the banana.";

    private void Start()
    {
        HasBanana = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (HasBanana)
            return;

        HasBanana = true;
        Debug.Log(pickupMessage);
        OnBananaPickedUp?.Invoke();

        Destroy(gameObject);
    }

    public static void ResetBanana()
    {
        HasBanana = false;
    }
}
