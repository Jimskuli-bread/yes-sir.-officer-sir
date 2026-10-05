using UnityEngine;

public class AmmoCrate : MonoBehaviour
{
    [Header("Ammo Settings")]
    public int ammoAmount = 30;   // How much ammo this crate gives

    [Header("Pickup Settings")]
    public AudioSource pickupSound;   // Optional sound
    public bool destroyOnPickup = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Find the gun the player is currently holding
            Gun currentGun = other.GetComponentInChildren<Gun>();

            if (currentGun != null)
            {
                currentGun.AddReserveAmmo(ammoAmount);

                if (pickupSound != null)
                {
                    pickupSound.Play();
                    Destroy(gameObject, pickupSound.clip.length);
                }
                else
                {
                    Destroy(gameObject);
                }

            }
        }
    }
}
