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

    private void Start()
    {
        Destroy(gameObject, lifeTime);
        
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    private void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
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
                //Destroy(mask.gameObject); 
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