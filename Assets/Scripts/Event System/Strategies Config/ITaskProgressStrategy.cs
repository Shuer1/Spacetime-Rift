public interface ITaskProgressStrategy
{
    TaskType Type { get; }

    bool CanHandle(TaskAsset asset, GameEvent e);

    int GetProgressDelta(TaskAsset asset, GameEvent e);
}

public class KillTaskStrategy : ITaskProgressStrategy
{
    public TaskType Type => TaskType.Kill;

    public bool CanHandle(TaskAsset asset, GameEvent e)
    {
        if (e is not KillEvent ke)
            return false;

        return asset.targetId == ke.enemyId;
    }

    public int GetProgressDelta(TaskAsset asset, GameEvent e)
    {
        return (e as KillEvent).count;
    }
}

public class CollectTaskStrategy : ITaskProgressStrategy
{
    public TaskType Type => TaskType.Collect;

    public bool CanHandle(TaskAsset asset, GameEvent e)
    {
        return e is CollectEvent ce && asset.targetId == ce.itemId;
    }

    public int GetProgressDelta(TaskAsset asset, GameEvent e)
    {
        return (e as CollectEvent).count;
    }
}

public class TalkTaskStrategy : ITaskProgressStrategy
{
    public TaskType Type => TaskType.Talk;

    public bool CanHandle(TaskAsset asset, GameEvent e)
    {
        return e is TalkEvent te && asset.targetId == te.npcId;
    }

    public int GetProgressDelta(TaskAsset asset, GameEvent e)
    {
        return 1;
    }
}

public class CombineTaskStrategy : ITaskProgressStrategy
{
    public TaskType Type => TaskType.Combine;

    public bool CanHandle(TaskAsset asset, GameEvent e)
    {
        return e is CombineEvent ce && asset.targetId == ce.itemId;
    }

    public int GetProgressDelta(TaskAsset asset, GameEvent e)
    {
        return (e as CombineEvent).count;
    }
}