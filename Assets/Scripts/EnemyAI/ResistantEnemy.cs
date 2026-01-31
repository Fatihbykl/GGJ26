using UnityEngine;

namespace EnemyAI
{
    public class ResistantEnemy : PossessableEnemy
    {
        protected override void MoveInput()
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            float drift = (Mathf.PerlinNoise(Time.time * 2f, 0f) - 0.5f) * 2f; 
        
            Vector3 intendedMove = new Vector3(h, 0, v);
            Vector3 corruptedMove = intendedMove + (transform.right * drift);
            
            animator.SetBool("Idle", false);
            animator.SetBool("WalkPossessed", true);
            
            transform.Translate(corruptedMove * 5f * Time.deltaTime, Space.World);
            
            Quaternion toRotation = Quaternion.LookRotation(corruptedMove, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, 720 * Time.deltaTime);
        }
    }
}