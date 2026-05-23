using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Task/TaskAsset")]
public class TaskAsset : ScriptableObject
{
    public string taskId;
    public string taskName;

    [TextArea(3, 5)]
    public string description;

    public TaskType taskType;
    public string targetId;
    public int requiredAmount = 1;

    [Header("Reward")]
    public int exp;
    public int gold;
    public List<ItemInfo> items = new List<ItemInfo>();

    [Header("Chain")]
    public TaskAsset nextTask;
}

public enum TaskType
{
    Kill,
    Collect,
    Talk,
    Combine,
    Custom
}

[System.Serializable]
public struct ItemInfo
{
    public ItemAsset item;
    public int count;
}
