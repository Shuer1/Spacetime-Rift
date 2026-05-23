public class HitState : EnemyState
{
    float timer;

    public HitState(EnemyBase enemy)
        : base(enemy, EnemyBase.StatePriority.Hit) { }

    public override void Enter()
    {
        timer = 0;
        enemy.anim.PlayHit();
    }

    public override void Tick()
    {
        timer += UnityEngine.Time.deltaTime;

        if (timer > 0.4f)
        {
            if(enemy.DistanceToPlayer() > enemy.chaseRange)
                enemy.ChangeState(enemy.returnState);
            else
                enemy.ChangeState(enemy.chaseState);
        }
    }
}