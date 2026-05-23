public class ChaseState : EnemyState
{
    public ChaseState(EnemyBase enemy)
        : base(enemy, EnemyBase.StatePriority.Chase) { }

    public override void Tick()
    {
        if (enemy.player == null) return;

        float dist = enemy.DistanceToPlayer();

        if (dist > enemy.chaseRange)
        {
            enemy.ChangeState(enemy.returnState);
            return;
        }

        if (dist <= enemy.attackRange)
        {
            enemy.ChangeState(enemy.attackState);
            return;
        }

        // ⭐ 每帧必须输入
        enemy.motor.SetMoveInput(enemy.player.position - enemy.transform.position);
    }
}