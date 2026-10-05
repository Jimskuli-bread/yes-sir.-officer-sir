using UnityEngine;

public class BulletMovementRB : MonoBehaviour
{
    public float bulletSpeed = 2000f;
    public float knockbackForce = 5f;
    public int damage = 10;
    public float lifeTime = 3f;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        float distance = bulletSpeed * Time.deltaTime;

        // Raycast forward to detect hit BEFORE moving
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, distance))
        {
            EnemyHealth hp = hit.collider.GetComponent<EnemyHealth>();
            if (hp != null)
                hp.TakeDamage(damage);

            Rigidbody rb = hit.collider.attachedRigidbody;
            if (rb != null)
                rb.linearVelocity += transform.forward * knockbackForce;

            Destroy(gameObject);
            return;
        }

        // Move bullet forward
        transform.position += transform.forward * distance;
    }
}
