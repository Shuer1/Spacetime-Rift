public class AttackStartEvent : GameEvent
{
    public int index;
    public string animationName;

    public AttackStartEvent(int index, string animationName)
    {
        this.index = index;
        this.animationName = animationName;
    }
}

public class AttackEndEvent : GameEvent { }

public class AttackInterruptedEvent : GameEvent { }

//public class DashEvent : GameEvent { }