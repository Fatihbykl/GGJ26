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

            transform.Translate(corruptedMove * 5f * Time.deltaTime, Space.World);
        }
    }
}