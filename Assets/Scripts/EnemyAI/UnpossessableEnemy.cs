using UnityEngine;

namespace EnemyAI
{
    public class UnpossessableEnemy : EnemyBase
    {
        public float health = 100f;
        
        protected override void Start()
        {
            base.Start();
        }

        protected override void Update()
        {
            base.Update();
        }

        public override void TakeDamage(float amount)
        {
            health -= amount;
            Debug.Log($"{gameObject.name} took {amount} damage. Current health: {health}");
            
            if (health <= 0)
            {
                Die();
            }
            
            base.TakeDamage(amount);
        }

        private void Die()
        {
            Debug.Log($"{gameObject.name} died.");
            Destroy(gameObject);
        }

        public override void Attack()
        {
            base.Attack();
        }
    }
}
