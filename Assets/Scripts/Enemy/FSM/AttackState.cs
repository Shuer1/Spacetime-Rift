public class AttackState : EnemyState
{
    float timer;
    int combo;

    public AttackState(EnemyBase enemy)
        : base(enemy, EnemyBase.StatePriority.Attack) { }

    public override void Enter()
    {
        timer = 0;
        combo = 0;

        enemy.anim.PlayAttack(combo);
    }

    public override void Tick()
    {
        float dist = enemy.DistanceToPlayer();

        if (dist > enemy.attackRange)
        {
            // ⭐ 判断玩家是跑出了攻击范围但仍在视野内，还是彻底跑丢了
            if (dist > enemy.chaseRange)
                enemy.ChangeState(enemy.returnState);
            else
                enemy.ChangeState(enemy.chaseState);
            return;
        }

        // ⭐ 不输入移动 = 自动Idle（由Motor处理）
        timer += UnityEngine.Time.deltaTime;

        if (timer > enemy.attackCD)
        {
            combo = (combo + 1) % 3;
            timer = 0;

            enemy.anim.PlayAttack(combo);
        }
    }

    public bool CanBeInterrupted() => true;
}