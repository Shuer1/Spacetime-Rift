using System;

[Serializable]
public class Task
{
    public string taskId;
    public TaskState state;
    public int currentAmount;

    public bool IsFinished =>
        state == TaskState.Completed || state == TaskState.Failed;
}

public enum TaskState
{
    Inactive,
    Active,
    Completed,
    Failed
}
