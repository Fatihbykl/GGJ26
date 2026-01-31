using System.Collections;
using DG.Tweening;
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
    private Animator animator;
    private Vector3 lastDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
    }
    
    private void OnEnable()
    {
        isRecovering = false;
        isGrounded = false;
        
        if(rb != null) 
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void Update()
    {
        if (!isGrounded || isRecovering) return;

        HandleInput();
    }

    private void HandleInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 inputDir = new Vector3(h, 0, v).normalized;
        
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(inputDir.x, 0, inputDir.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        transform.DORotateQuaternion(lookRotation, 0.5f);

        if (inputDir.magnitude > 0.1f)
        {
            StartJump(inputDir);
        }
    }

    private void StartJump(Vector3 direction)
    {
        isGrounded = false;
        lastDirection = direction;
        
        rb.linearVelocity = Vector3.zero; 
        rb.angularVelocity = Vector3.zero;

        animator.SetTrigger("Jump");
        animator.SetBool("Air", true);
    }


    public void Jump()
    {
        Vector3 jumpVec = (Vector3.up * upForce) + (lastDirection * forwardForce);
        rb.AddForce(jumpVec, ForceMode.Impulse);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {

            if(collision.contacts[0].normal.y > 0.5f) 
            {
                OnLand();
            }
        }
    }

    private void OnLand()
    {
        if (isGrounded) return;

        isGrounded = true;
        rb.linearVelocity = Vector3.zero;
        
        animator.SetBool("Air", false);
        
        StartCoroutine(RecoverRoutine());
    }

    private IEnumerator RecoverRoutine()
    {
        isRecovering = true;
        
        yield return new WaitForSeconds(landingRecoveryTime);

        isRecovering = false;
    }
}