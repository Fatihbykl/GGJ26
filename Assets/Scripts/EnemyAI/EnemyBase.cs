using UnityEngine;
using UnityEngine.AI;

public enum EnemyState { Idle, Chase, Attack, Possessed, Stunned }

[RequireComponent(typeof(NavMeshAgent))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Base Stats")]
    public float detectionRadius = 10f;
    public float attackRange = 5f;
    public float attackCooldown = 2f;
    protected float lastAttackTime;

    protected NavMeshAgent agent;
    protected Transform playerTransform;
    public EnemyState currentState = EnemyState.Idle;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform; 
    }

    protected virtual void Update()
    {
        if (currentState == EnemyState.Possessed) return;

        RunStateMachine();
    }

    protected virtual void RunStateMachine()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        switch (currentState)
        {
            case EnemyState.Idle:
                if (distanceToPlayer < detectionRadius) currentState = EnemyState.Chase;
                break;

            case EnemyState.Chase:
                agent.isStopped = false;
                agent.SetDestination(playerTransform.position);
                
                if (distanceToPlayer <= attackRange) currentState = EnemyState.Attack;
                if (distanceToPlayer > detectionRadius * 1.5f) currentState = EnemyState.Idle;
                break;

            case EnemyState.Attack:
                agent.isStopped = true;
                FaceTarget();
                if (Time.time > lastAttackTime + attackCooldown)
                {
                    Attack();
                    lastAttackTime = Time.time;
                }
                
                if (distanceToPlayer > attackRange) currentState = EnemyState.Chase;
                break;
        }
    }

    protected virtual void Attack()
    {
        Debug.Log(gameObject.name + " ses dalgası attı: 'Uyum Sağla!'");
        // Buraya Projectile Instantiate kodu gelecek
    }

    private void FaceTarget()
    {
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }
}