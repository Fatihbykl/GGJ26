using UnityEngine;

namespace EnemyAI
{
    public abstract class PossessableEnemy : EnemyBase
    {
        [Header("Host Stats")]
        public float maxEnlightenmentTime = 10f;
        protected float currentEnlightenmentTime;
        public float decayRate = 1f;

        public virtual void OnPossess()
        {
            currentState = EnemyState.Possessed;
            agent.enabled = false;
            currentEnlightenmentTime = 0;
        
            Debug.Log("Bedene girildi: " + gameObject.name);
        }

        public virtual void OnDepossess(bool fullEnlightenment)
        {
            agent.enabled = true;
        
            if (fullEnlightenment)
            {
                Die();
            }
            else
            {
                currentState = EnemyState.Chase; 
                currentEnlightenmentTime = 0;
                Debug.Log("Erken çıkıldı, düşman öfkeli!");
            }
        }

        public void HandlePossessedUpdate()
        {
            currentEnlightenmentTime += Time.deltaTime * decayRate;
        
            // UI Bar Update'i buraya event olarak atılabilir.

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
            Vector3 move = new Vector3(h, 0, v) * 5f * Time.deltaTime;
            transform.Translate(move, Space.World);
        }
    
        private void Die()
        {
            Destroy(gameObject);
        }
    }
}