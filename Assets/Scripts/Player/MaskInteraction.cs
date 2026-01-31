using EnemyAI;
using UnityEngine;

namespace Player
{
    public class MaskInteraction : MonoBehaviour
    {
        public float interactionRange = 2f;
        public LayerMask enemyLayer;
        public KeyCode interactKey = KeyCode.E;

        private void Update()
        {
            if (Input.GetKeyDown(interactKey))
            {
                TryPossess();
            }
        }

        private void TryPossess()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, interactionRange, enemyLayer);
            Debug.Log(hits.Length);
            foreach (var hit in hits)
            {
                PossessableEnemy enemy = hit.GetComponent<PossessableEnemy>();
                if (enemy != null)
                {
                    PossessionManager.Instance.Possess(enemy);
                    break;
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRange);
        }
    }
}