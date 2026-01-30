using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MaskController : MonoBehaviour
{
    [Header("Movement Stats")]
    public float upForce = 5f;
    public float forwardForce = 8f;
    public float landingRecoveryTime = 1.0f;

    // State
    private bool isGrounded;
    private bool isRecovering; 
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // Havadaysak, dinleniyorsak veya input yoksa işlem yapma
        if (!isGrounded || isRecovering) return;

        HandleInput();
    }

    private void HandleInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 inputDir = new Vector3(h, 0, v).normalized;

        if (inputDir.magnitude > 0.1f)
        {
            Jump(inputDir);
        }
    }

    private void Jump(Vector3 direction)
    {
        isGrounded = false; // Manuel olarak "havadayım" de
        
        // Hızı sıfırla (Tutarlılık için şart)
        rb.linearVelocity = Vector3.zero; 
        rb.angularVelocity = Vector3.zero;

        // Kuvvet uygula
        Vector3 jumpVec = (Vector3.up * upForce) + (direction * forwardForce);
        rb.AddForce(jumpVec, ForceMode.Impulse);
    }

    // --- EN ÖNEMLİ KISIM: COLLISION EVENTLERİ ---

    // Bir şeye çarptığımız an çalışır
    private void OnCollisionEnter(Collision collision)
    {
        // Sadece "Zemin" ile çarpışınca çalışsın
        // NOT: Yere 'Ground' Tag'i vermeyi unutma!
        if (collision.gameObject.CompareTag("Ground"))
        {
            // Sadece yukarıdan aşağı düşerken (yere inince) çalışsın
            // Yan duvara çarpınca yere indim sanmasın
            if(collision.contacts[0].normal.y > 0.5f) 
            {
                OnLand();
            }
        }
    }

    private void OnLand()
    {
        // Zaten yerdeysek tekrar tetikleme
        if (isGrounded) return;

        Debug.Log("Yere İndi!");
        isGrounded = true;
        rb.linearVelocity = Vector3.zero; // Kaymayı engellemek için anlık durdur
        
        StartCoroutine(RecoverRoutine());
    }

    private IEnumerator RecoverRoutine()
    {
        isRecovering = true;
        
        // Bekleme süresi
        yield return new WaitForSeconds(landingRecoveryTime);

        isRecovering = false;
        Debug.Log("Tekrar zıplamaya hazır.");
    }
}