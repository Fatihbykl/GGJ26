using Microlight.MicroBar;
using Player;
using UnityEngine;

namespace EnemyAI
{
    public class PossessableEnemy : EnemyBase 
    {
        [Header("Enlightenment Stats")]
        public float maxStability = 100f;
        public float decayRate = 5f;
        public float damagePenalty = 10f;

        protected float currentStability;
        protected float effectiveMaxStability; 
        private Renderer[] renderers;
        private bool isDead = false;
        private Material defaultMat;
        
        public float CurrentStability => currentStability;
        public float EffectiveMaxStability => effectiveMaxStability;

        protected override void Start()
        {
            base.Start();
            renderers = GetComponentsInChildren<Renderer>();
            if(renderers.Length > 0) defaultMat = renderers[0].material;
        
            effectiveMaxStability = maxStability;
            currentStability = maxStability;
        }

        protected override void Update()
        {
            base.Update();

            if (currentState == EnemyState.Possessed)
            {
                HandlePossessedUpdate();
            }
        }
    
        public virtual void OnPossess(Material m)
        {
            currentState = EnemyState.Possessed;
            agent.enabled = false;
        
            gameObject.tag = "Player"; 
            gameObject.layer = LayerMask.NameToLayer("Player");

            ChangeMaterial(m);

            currentStability = effectiveMaxStability;
        }

        public override void TakeDamage(float amount)
        {
            if (currentState == EnemyState.Possessed)
            {
                TakeStabilityDamage(amount);
            }
            else
            {
                effectiveMaxStability -= amount;
                if (effectiveMaxStability < 10f) effectiveMaxStability = 10f;
                Debug.Log($"{gameObject.name} max stability reduced to {effectiveMaxStability}");
            }
            
            base.TakeDamage(amount);
        }

        public virtual void OnDepossess(bool isEnlightened)
        {
            gameObject.tag = "Enemy";
            gameObject.layer = LayerMask.NameToLayer("Enemy");

            ChangeMaterial(defaultMat);

            if (isEnlightened)
            {
                Die();
            }
            else
            {
                currentState = EnemyState.Chase; 
                agent.enabled = true;
                currentStability = maxStability;
            }
        }

        protected virtual void HandlePossessedUpdate()
        {
            currentStability -= Time.deltaTime * decayRate;

            if (currentStability <= 0)
            {
                PossessionManager.Instance.Eject(); 
                return;
            }

            if (!IsPlayingAttack)
            {
                animator.SetBool("Walk", false);
                MoveInput();
            }

            if (Input.GetButtonDown("Fire1"))
            {
                animator.SetTrigger("Attack");
            }
        }

        protected virtual void MoveInput()
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            Vector3 moveDir = new Vector3(h, 0, v).normalized;

            if (moveDir.magnitude >= 0.1f)
            {
                animator.SetBool("Idle", false);
                animator.SetBool("WalkPossessed", true);
                transform.Translate(moveDir * agent.speed * Time.deltaTime, Space.World);

                Quaternion toRotation = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, 720 * Time.deltaTime);
            }
            else
            {
                animator.SetBool("Idle", true);
                animator.SetBool("WalkPossessed", false);
            }
        }

        public void TakeStabilityDamage(float amount)
        {
            if (currentState == EnemyState.Possessed)
            {
                currentStability -= amount;
            }
        }

        public bool IsEnlightened()
        {
            return (currentStability / maxStability) <= 0.2f;
        }

        private void Die()
        {
            if (isDead) return;
            
            isDead = true;
            PossessionManager.Instance.Eject();
            Debug.Log(gameObject.name + " is dead");
            Destroy(gameObject);
        }

        private void ChangeMaterial(Material m)
        {
            renderers[0].material = m;
        }
    }
}