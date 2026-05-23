using UnityEngine;

public class ReturnState : EnemyState
{
    public ReturnState(EnemyBase enemy)
        : base(enemy, EnemyBase.StatePriority.Chase) { }

    public override void Tick()
    {
        if (enemy.player == null)
        {
            // 玩家已销毁/消失，直接回家
            MoveToHome();
            return;
        }

        float distToPlayer = enemy.DistanceToPlayer();

        // ⭐ 玩家重新进入视野，立即恢复追击
        if (distToPlayer <= enemy.chaseRange)
        {
            enemy.ChangeState(enemy.chaseState);
            return;
        }

        MoveToHome();
    }

    void MoveToHome()
    {
        Vector3 toHome = enemy.spawnPosition - enemy.transform.position;
        float distToHome = toHome.magnitude;

        // ⭐ 到达出生点附近，切回巡逻
        if (distToHome < 0.5f)
        {
            enemy.ChangeState(enemy.patrolState);
            return;
        }

        Vector3 moveDir = toHome.normalized;

        // ⭐ 障碍物规避（与PatrolState逻辑保持一致）
        if (Physics.Raycast(enemy.transform.position, moveDir, out RaycastHit hit,
                            enemy.obstacleDetectDistance, enemy.obstacleLayer))
        {
            Vector3 avoidDir = Vector3.Cross(Vector3.up, hit.normal).normalized;
            if (Random.value > 0.5f) avoidDir = -avoidDir;
            
            moveDir = (moveDir + avoidDir).normalized;
        }

        // 返回速度略低于全力追击，体现"警戒撤退"
        enemy.motor.SetMoveInput(moveDir, 0.75f);
    }
}