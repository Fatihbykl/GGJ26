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
        private Renderer[] renderers;
        private Color originalColor;
        private bool isDead = false;

        protected override void Start()
        {
            base.Start();
            renderers = GetComponentsInChildren<Renderer>();
            if(renderers.Length > 0) originalColor = renderers[0].material.color;
        
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
    
        public virtual void OnPossess()
        {
            currentState = EnemyState.Possessed;
            agent.enabled = false;
        
            gameObject.tag = "Player"; 
            gameObject.layer = LayerMask.NameToLayer("Player");

            ChangeColor(Color.cyan);

            currentStability = maxStability;
        }

        public virtual void OnDepossess(bool isEnlightened)
        {
            gameObject.tag = "Enemy";
            gameObject.layer = LayerMask.NameToLayer("Enemy");

            ChangeColor(originalColor);

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
            Debug.Log(currentStability);

            if (currentStability <= 0)
            {
                PossessionManager.Instance.Eject(); 
                return;
            }

            MoveInput();

            if (Input.GetButtonDown("Fire1"))
            {
                Attack(); 
            }
        }

        protected virtual void MoveInput()
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            Vector3 moveDir = new Vector3(h, 0, v).normalized;

            if (moveDir.magnitude >= 0.1f)
            {
                transform.Translate(moveDir * agent.speed * Time.deltaTime, Space.World);

                Quaternion toRotation = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, 720 * Time.deltaTime);
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

        private void ChangeColor(Color c)
        {
            foreach(var r in renderers) r.material.color = c;
        }
    }
}