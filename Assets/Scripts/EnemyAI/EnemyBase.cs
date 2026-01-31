using System;
using Player;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyState
{
    Idle,
    Chase,
    Attack,
    Possessed,
    Stunned
}

[RequireComponent(typeof(NavMeshAgent))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Base Stats")] public float detectionRadius = 10f;
    public float attackRange = 5f;
    public float attackCooldown = 2f;
    protected float lastAttackTime;

    [Header("Combat")] public GameObject projectilePrefab;
    public GameObject possessedProjectilePrefab;
    public Transform firePoint;
    public ParticleSystem echoEffect;

    protected NavMeshAgent agent;
    protected Transform playerTransform;
    protected Animator animator;
    public EnemyState currentState = EnemyState.Idle;


    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        PossessionManager.Instance.OnPossessChanged += OnPossessChanged;
    }

    private void OnPossessChanged(Transform host)
    {
        playerTransform = host;
    }

    protected virtual void Update()
    {
        if (currentState == EnemyState.Possessed) return;

        RunStateMachine();
    }

    public bool IsPlayingAttack =>
        animator.GetCurrentAnimatorStateInfo(0).IsName("Attack") ||
        animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Attack") ||
        animator.GetNextAnimatorStateInfo(0).IsName("Attack") ||
        animator.GetNextAnimatorStateInfo(0).IsName("Base Layer.Attack");

    protected virtual void RunStateMachine()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (IsPlayingAttack)
        {
            agent.isStopped = true;
            return;
        }

        switch (currentState)
        {
            case EnemyState.Idle:
                animator.SetBool("Walk", false);
                animator.SetBool("Idle", true);
                animator.SetBool("WalkPossessed", false);
                if (distanceToPlayer < detectionRadius) currentState = EnemyState.Chase;
                break;

            case EnemyState.Chase:
                animator.SetBool("Walk", true);
                animator.SetBool("Idle", false);
                animator.SetBool("WalkPossessed", false);
                agent.isStopped = false;
                agent.SetDestination(playerTransform.position);

                if (distanceToPlayer <= attackRange && Time.time > lastAttackTime + attackCooldown)
                    currentState = EnemyState.Attack;
                if (distanceToPlayer > detectionRadius * 1.5f) currentState = EnemyState.Idle;
                break;

            case EnemyState.Attack:
                agent.isStopped = true;
                FaceTarget();
                animator.SetTrigger("Attack");
                //Attack();
                lastAttackTime = Time.time;

                if (distanceToPlayer > attackRange) currentState = EnemyState.Chase;
                break;
        }
    }

    public void PlayEchoEffect()
    {
        echoEffect.Play();
    }

    public virtual void Attack()
    {
        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogWarning("Mermi veya FirePoint eksik!");
            return;
        }

        var prefab = currentState == EnemyState.Possessed ? possessedProjectilePrefab : projectilePrefab;

        GameObject projectile = Instantiate(prefab, firePoint.position, firePoint.rotation);

        SoundWaveProjectile swp = projectile.GetComponent<SoundWaveProjectile>();
        if (swp != null)
        {
            swp.targetTag = currentState == EnemyState.Possessed ? "Enemy" : "Player";
        }

        Debug.Log(gameObject.name + " saldırdı!");
    }

    public virtual void TakeDamage(float amount)
    {
    }

    private void FaceTarget()
    {
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

    private void OnDestroy()
    {
        PossessionManager.Instance.OnPossessChanged -= OnPossessChanged;
    }
}