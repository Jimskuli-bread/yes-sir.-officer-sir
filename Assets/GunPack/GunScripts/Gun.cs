using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class Gun : MonoBehaviour
{
    [Header("Gun Settings")]
    public float fireRate = 0.1f;        // Time between shots
    public int maxAmmo = 30;             // Magazine size
    public int startReserveAmmo = 90;    // Ammo you start with in storage
    public float reloadTime = 1.5f;
    public int damage = 10;

    [Header("Bullet Settings")]
    public float bulletSpeed = 20f;
    public float bulletKnockbackForce = 10f;

    [Header("Movement Slowdown")]
    public FPSMovement movementScript;   // Drag your movement script here
    public float normalSpeed = 10f;
    public float reducedAimSpeed = 5f;

    [Header("Recoil")]
    public float recoilAmount = 5f;          // Upward kick
    public float recoilReturnSpeed = 10f;    // How fast it resets
    public float aimRecoilMultiplier = 0.5f; // Reduced recoil when aiming
    public float maxRecoil = 10f;            // Prevent propeller mode
    public Quaternion originalRotation;

    private float currentRecoil = 0f;
    private float targetRecoil = 0f;



    [Header("Aiming")]
    public Transform hipPosition;     // Default gun position
    public Transform aimPosition;     // Position in front of camera
    public float aimSpeed = 10f;

    [Header("Zoom")]
    public Camera playerCamera;
    public float normalFOV = 60f;
    public float zoomFOV = 40f;
    public float zoomSpeed = 10f;


    [Header("Hitmarker")]
    public Image[] crosshairParts;   // Any amount of crosshair images
    public Color normalColor = Color.white;
    public Color enemyColor = Color.red;
    public float hitCheckDistance = 100f;   // How far the raycast checks


    [Header("References")]
    public Transform firePoint;          // Where bullets spawn
    public GameObject bulletPrefab;      // Your bullet object
    public TextMeshProUGUI ammoText;     // UI text for ammo
    public ParticleSystem muzzleFlash;   // Optional

    public AudioSource gunSound;         // Shooting sound
    public AudioSource reloadSound;      // Reload sound (NEW)

    private int currentAmmo;             // Ammo in magazine
    private int reserveAmmo;             // Ammo in storage
    private float nextFireTime = 0f;
    private bool isReloading = false;

    private ParticleSystem.EmissionModule emission;

    private void Start()
    {
        currentAmmo = maxAmmo;
        reserveAmmo = startReserveAmmo;

        originalRotation = transform.localRotation;

        if (muzzleFlash == null)
            muzzleFlash = GetComponentInChildren<ParticleSystem>();

        if (muzzleFlash != null)
            emission = muzzleFlash.emission;

        UpdateAmmoUI();

        if (PlayerHealth.isGameFrozen)
            return;
    }

    void OnEnable()
    {
        // Snap weapon to hip rotation
        transform.localRotation = hipPosition.localRotation;
        transform.localRotation = Quaternion.identity;
   
        if (muzzleFlash != null)
            emission = muzzleFlash.emission;

        // Update UI for this weapon
        UpdateAmmoUI();
    }



    private void Update()
    {

        // Aiming (right-click hold)
        if (Input.GetMouseButton(1))
        {
            // Move gun toward aim position
            transform.position = Vector3.Lerp(transform.position, aimPosition.position, Time.deltaTime * aimSpeed);
        }
        else
        {
            // Move gun back to hip position
            transform.position = Vector3.Lerp(transform.position, hipPosition.position, Time.deltaTime * aimSpeed);
        }


        if (Input.GetMouseButton(1))
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, aimPosition.localPosition, Time.deltaTime * aimSpeed);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, aimPosition.localRotation, Time.deltaTime * aimSpeed);

        }
        else
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, hipPosition.localPosition, Time.deltaTime * aimSpeed);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, hipPosition.localRotation, Time.deltaTime * aimSpeed);
        }



        if (isReloading)
            return;

        // Reload
        if (Input.GetKeyDown(KeyCode.R))
        {
            StartCoroutine(Reload());
            return;
        }

        // Automatic fire when holding LMB
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            Shoot();
        }

        if (!Input.GetMouseButton(0) && muzzleFlash != null)
            muzzleFlash.Stop();


        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, hitCheckDistance))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                foreach (Image part in crosshairParts)
                    part.color = enemyColor;
            }
            else
            {
                foreach (Image part in crosshairParts)
                    part.color = normalColor;
            }
        }
        else
        {
            foreach (Image part in crosshairParts)
                part.color = normalColor;
        }


        if (Input.GetMouseButton(1))
        {
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, zoomFOV, Time.deltaTime * zoomSpeed);
        }
        else
        {
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, normalFOV, Time.deltaTime * zoomSpeed);
        }

        if (Input.GetMouseButton(1))
        {
            movementScript.moveSpeed = reducedAimSpeed;
        }
        else
        {
            movementScript.moveSpeed = normalSpeed;
        }

        // Smoothly move current recoil toward target recoil
        currentRecoil = Mathf.Lerp(currentRecoil, targetRecoil, Time.deltaTime * recoilReturnSpeed);

        // Apply recoil as upward tilt
        transform.localRotation = Quaternion.Euler(
            hipPosition.localRotation.eulerAngles.x - currentRecoil,
            hipPosition.localRotation.eulerAngles.y,
            hipPosition.localRotation.eulerAngles.z
        );

        // Once recoil is applied, begin returning to zero
        if (Mathf.Abs(targetRecoil - currentRecoil) < 0.1f)
        {
            targetRecoil = 0f;
        }      
            UpdateAmmoUI();
       
    }


    void Shoot()
    {

        


        if (currentAmmo <= 0)
            return;

        nextFireTime = Time.time + fireRate;
        currentAmmo--;

        float finalRecoil = Input.GetMouseButton(1)
            ? recoilAmount * aimRecoilMultiplier
            : recoilAmount;

        targetRecoil = Mathf.Clamp(targetRecoil + finalRecoil, 0f, maxRecoil);

        GameObject b = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        BulletMovementRB bullet = b.GetComponent<BulletMovementRB>();

        bullet.damage = damage;
        bullet.bulletSpeed = bulletSpeed;                 // from gun
        bullet.knockbackForce = bulletKnockbackForce;     // from gun


        if (muzzleFlash != null)
        {
            muzzleFlash.Play();   // single burst
        }

        if (gunSound != null)
            gunSound.Play();

        UpdateAmmoUI();
    }

    void BulletKnockBack(Rigidbody targetRb)
    {
        if (targetRb != null)
        {
            Vector3 knockbackDirection = firePoint.forward;
            targetRb.AddForce(knockbackDirection * bulletKnockbackForce, ForceMode.VelocityChange);
        }
    }


    System.Collections.IEnumerator Reload()
    {
        if (reserveAmmo <= 0 || currentAmmo == maxAmmo)
            yield break;

        isReloading = true;
        ammoText.text = "Reloading...";

        // Play reload sound (NEW)
        if (reloadSound != null)
            reloadSound.Play();

        yield return new WaitForSeconds(reloadTime);

        int neededAmmo = maxAmmo - currentAmmo;

        if (reserveAmmo >= neededAmmo)
        {
            currentAmmo = maxAmmo;
            reserveAmmo -= neededAmmo;
        }
        else
        {
            currentAmmo += reserveAmmo;
            reserveAmmo = 0;
        }

        UpdateAmmoUI();
        isReloading = false;
    }

    void UpdateAmmoUI()
    {
        ammoText.text = currentAmmo + " / " + reserveAmmo;
    }

    public void AddReserveAmmo(int amount)
    {
        reserveAmmo += amount;
        UpdateAmmoUI();
    }


}
