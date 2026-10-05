using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI")]
    public Slider healthSlider;
    public GameObject deathUI;
    public KeyCode respawnKey = KeyCode.R;

    [Header("Camera Freeze")]
    public MonoBehaviour cameraScript;   // your FPS camera script

    [Header("Game Freeze Flag")]
    public static bool isGameFrozen = false;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;

        if (healthSlider != null)
            healthSlider.value = currentHealth / maxHealth;

        if (deathUI != null)
            deathUI.SetActive(false);
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (healthSlider != null)
            healthSlider.value = currentHealth / maxHealth;

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        isDead = true;
        isGameFrozen = true;

        if (cameraScript != null)
            cameraScript.enabled = false;

        if (deathUI != null)
            deathUI.SetActive(true);
    }

    void Update()
    {
        if (isDead && Input.GetKeyDown(respawnKey))
        {
            reloadScene();
        }
    }

    void reloadScene()
    {
        isDead = false;
        isGameFrozen = false;
        if (cameraScript != null)
            cameraScript.enabled = true;
        if (deathUI != null)
            deathUI.SetActive(false);
        currentHealth = maxHealth;
        if (healthSlider != null)
            healthSlider.value = currentHealth / maxHealth;
    }

    void OnDestroy()
    {
        isGameFrozen = false;
    }
}
