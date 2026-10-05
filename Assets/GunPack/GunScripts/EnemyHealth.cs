using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // Register kill
        var killCounter = Object.FindFirstObjectByType<EnemyKillCounter>();
        if (killCounter != null)
            killCounter.RegisterKill();

        Destroy(gameObject);
    }
}
