using UnityEngine;

public class PatrolState : EnemyState
{
    Vector3 target;

    public PatrolState(EnemyBase enemy)
        : base(enemy, EnemyBase.StatePriority.Patrol) { }

    public override void Enter()
    {
        Pick();
    }

    public override void Tick()
    {
        float dist = enemy.DistanceToPlayer();

        if (dist <= enemy.chaseRange)
        {
            enemy.ChangeState(enemy.chaseState);
            return;
        }

        Vector3 dir = target - enemy.transform.position;

        if (dir.magnitude < 0.3f)
        {
            Pick();
            return;
        }

        Vector3 moveDir = dir.normalized;

        // 实时障碍物规避
        if (Physics.Raycast(enemy.transform.position, moveDir, out RaycastHit hit,
                            enemy.obstacleDetectDistance, enemy.obstacleLayer))
        {
            Vector3 avoidDir = Vector3.Cross(Vector3.up, hit.normal).normalized;
            if (Random.value > 0.5f) avoidDir = -avoidDir;

            target = enemy.transform.position + avoidDir * 2f + moveDir * 0.5f;
            dir = target - enemy.transform.position;
        }

        enemy.motor.SetMoveInput(dir, 0.6f);
    }

    void Pick()
    {
        // ⭐ 围绕出生点 spawnPosition 随机选点，而非当前位置
        for (int i = 0; i < 10; i++)
        {
            Vector2 r = Random.insideUnitCircle * 4f;
            Vector3 candidate = enemy.spawnPosition + new Vector3(r.x, 0, r.y);

            Vector3 toTarget = candidate - enemy.transform.position;
            float dist = toTarget.magnitude;
            if (dist < 0.01f) continue;

            bool pathBlocked = Physics.Raycast(
                enemy.transform.position,
                toTarget.normalized,
                dist,
                enemy.obstacleLayer
            );

            bool pointInsideObstacle = Physics.CheckSphere(candidate, 0.3f, enemy.obstacleLayer);

            if (!pathBlocked && !pointInsideObstacle)
            {
                target = candidate;
                return;
            }
        }

        // 回退：在出生点附近短距离移动，防止完全卡住
        Vector2 fallback = Random.insideUnitCircle * 1.5f;
        target = enemy.spawnPosition + new Vector3(fallback.x, 0, fallback.y);
    }
}