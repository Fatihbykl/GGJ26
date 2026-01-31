namespace EnemyAI
{
    public class TankEnemy : PossessableEnemy
    {
        protected override void Start()
        {
            base.Start();
            decayRate = 0.5f;
            agent.speed = 2.0f;
        }
    }
}