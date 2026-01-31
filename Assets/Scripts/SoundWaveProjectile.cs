using EnemyAI;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class SoundWaveProjectile : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 15f;
    public float damage = 15f;
    public float lifeTime = 3f;
    public string targetTag = "Player"; // Default to hitting player

    private void Start()
    {
        Destroy(gameObject, lifeTime);
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if(rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    private void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if we hit the target
        if (other.CompareTag(targetTag))
        {
            // If hitting player/possessed enemy
            if (targetTag == "Player")
            {
                PossessableEnemy host = other.GetComponent<PossessableEnemy>();
                if (host != null)
                {
                    host.TakeStabilityDamage(damage);
                    Debug.Log("Bedene hasar verildi! Stability düştü.");
                    HitEffect();
                    return;
                }

                MaskController mask = other.GetComponent<MaskController>();
                if (mask != null)
                {
                    Debug.Log("Maske vuruldu! GAME OVER.");
                    // GameManager.Instance.RestartGame();
                    // Destroy(mask.gameObject); 
                    HitEffect();
                    return;
                }
            }
            // If hitting enemy (while possessed)
            else if (targetTag == "Enemy")
            {
                EnemyBase enemy = other.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                    HitEffect();
                    return;
                }
            }
        }
        
        // Also check generic EnemyBase if we are targeting Enemy, in case tag is missing or different
        if (targetTag == "Enemy")
        {
             EnemyBase enemy = other.GetComponent<EnemyBase>();
             if (enemy != null && !other.CompareTag("Player")) // Ensure we don't hit ourselves if we are Player
             {
                 enemy.TakeDamage(damage);
                 HitEffect();
                 return;
             }
        }

        
        if (other.CompareTag("Ground") || other.CompareTag("Wall"))
        {
            HitEffect();
        }
    }

    private void HitEffect()
    {
        // Instantiate(vfxPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}