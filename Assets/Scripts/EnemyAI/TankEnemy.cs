namespace EnemyAI
{
    public class TankEnemy : PossessableEnemy
    {
        protected override void Awake()
        {
            base.Awake();
            decayRate = 0.5f;
            agent.speed = 2.0f;
        }
    }
}