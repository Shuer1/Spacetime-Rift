public abstract class EnemyState
{
    protected EnemyBase enemy;
    public EnemyBase.StatePriority priority;

    // ⭐ 默认状态名称（如 "Patrol"/"Chase"），子类可重写，便于 UI 显示和日志调试
    public virtual string StateName => GetType().Name.Replace("State_Op", "");

    protected EnemyState(EnemyBase enemy, EnemyBase.StatePriority priority)
    {
        this.enemy = enemy;
        this.priority = priority;
    }

    public virtual void Enter() { }
    public virtual void Tick() { }
    public virtual void Exit() { }
}