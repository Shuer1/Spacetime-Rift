// 实现均为构造方法：创建一个类继承 GameEvent，并传入参数
public class KillEvent : GameEvent
{
    public string enemyId;
    public int count;

    public KillEvent(string enemyId, int count = 1)
    {
        this.enemyId = enemyId;
        this.count = count;
    }
}

public class CollectEvent : GameEvent
{
    public string itemId;
    public int count;

    public CollectEvent(string itemId, int count)
    {
        this.itemId = itemId;
        this.count = count;
    }
}

public class TalkEvent : GameEvent
{
    public string npcId;

    public TalkEvent(string npcId)
    {
        this.npcId = npcId;
    }
}

public class CombineEvent : GameEvent // 合成事件
{
    public string itemId;
    public int count;

    public CombineEvent(string itemId, int count)
    {
        this.itemId = itemId;
        this.count = count;
    }
}

public class NPCEnterRangeEvent : GameEvent
{
    public string npcId;
    public NPCAsset npcAsset;
}

public class NPCExitRangeEvent : GameEvent
{
    public string npcId;
    public NPCAsset npcAsset;
}

public class NPCInteractEvent : GameEvent
{
    public string npcId;
    public NPCAsset npcAsset;
}

public class ItemEnterRangeEvent : GameEvent
{
    public ItemPrefab item;

    public ItemEnterRangeEvent(ItemPrefab item)
    {
        this.item = item;
    }
}

public class ItemExitRangeEvent : GameEvent
{
    public ItemPrefab item;

    public ItemExitRangeEvent(ItemPrefab item)
    {
        this.item = item;
    }
}

public class PortalEnterRangeEvent : GameEvent
{
    public PortalController portal;

    public PortalEnterRangeEvent(PortalController portal)
    {
        this.portal = portal;
    }
}

public class PortalExitRangeEvent : GameEvent
{
    public PortalController portal;

    public PortalExitRangeEvent(PortalController portal)
    {
        this.portal = portal;
    }
}