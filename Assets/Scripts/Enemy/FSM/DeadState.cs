public class DeadState : EnemyState
{
    public DeadState(EnemyBase enemy)
        : base(enemy, EnemyBase.StatePriority.Dead) { }

    public override void Enter()
    {
        enemy.anim.PlayDie();
    }
}