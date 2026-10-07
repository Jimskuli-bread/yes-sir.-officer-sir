using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BananaPickup : MonoBehaviour
{
    public static bool HasBanana { get; private set; }
    public static BananaPickup FirstBanana { get; private set; }
    public static event Action OnBananaPickedUp;
    public bool IsDetectorTarget { get; private set; }
    public bool IsDetectorTargetRevealed { get; private set; }
    private bool returningToOffice;

    [Header("Pickup Settings")]
    public string pickupMessage = "You picked up the banana.";

    public void MarkAsDetectorTarget()
    {
        IsDetectorTarget = true;
    }

    public void RevealDetectorTarget()
    {
        IsDetectorTargetRevealed = true;
    }

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
        if (IsDetectorTarget)
        {
            if (IsDetectorTargetRevealed)
                TryCollectDetectorTarget();
            return;
        }

        HasBanana = true;
        if (FirstBanana == null)
        {
            FirstBanana = this;
            Debug.Log(pickupMessage);
            OnBananaPickedUp?.Invoke();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (IsDetectorTarget)
        {
            TryCollectDetectorTarget();
            return;
        }

        if (HasBanana)
            return;

        bool keepInHand = NPC.IsTaskActive(0) || NPC.IsTaskActive(6);
        if (keepInHand)
        {
            PlayerPickup playerPickup = other.GetComponentInParent<PlayerPickup>();
            bool keepPlayerAcrossScenes = NPC.IsTaskActive(0);
            if (playerPickup == null || !playerPickup.TryPickupObject(gameObject, keepPlayerAcrossScenes))
            {
                Debug.LogWarning("The banana could not be picked up. Add PlayerPickup to the player and assign its hold point.");
                return;
            }
        }

        RegisterPickup();
        if (!keepInHand)
            Destroy(gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        if (IsDetectorTarget && other.CompareTag("Player"))
        {
            TryCollectDetectorTarget();
        }
    }

    private void TryCollectDetectorTarget()
    {
        if (!IsDetectorTargetRevealed || returningToOffice)
            return;

        returningToOffice = true;
        NPC.CompleteTaskInLoadedScenes(8);
        SceneReturnTracker.LoadScene("Office");
    }

    public static void ResetBanana()
    {
        HasBanana = false;
        FirstBanana = null;
    }
}
