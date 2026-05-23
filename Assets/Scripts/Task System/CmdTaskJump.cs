using UnityEngine;
using Fungus;
using System.Collections.Generic;

[CommandInfo("Task", "Task State Detection", "根据任务状态跳转到指定 Block")]
[AddComponentMenu("")]
public class CmdTaskJump : Command
{
    [Tooltip("要检查的任务 ID")] [SerializeField] protected string taskId;

    [Tooltip("未接取时跳转的目标 Block 名字")] [SerializeField] protected string notAcceptedTarget;
    [Tooltip("已接取但尚未完成时跳转的目标 Block 名字")] [SerializeField] protected string activeTarget;
    [Tooltip("已完成时跳转的目标 Block 名字")] [SerializeField] protected string completedTarget;

    private Dictionary<string, Block> _blockMap;
    private Flowchart _flow;

    private Flowchart Flow => _flow ?? (_flow = GetFlowchart());

    public override void OnEnter()
    {
        if (TaskManager.Instance == null)
        {
            Debug.LogError("[CmdTaskJump] TaskManager 未就绪");
            Continue(); return;
        }

        int code = TaskManager.Instance.GetTaskStateCode(taskId);
        Block dest = code switch
        {
            0 => FindBlock(notAcceptedTarget),
            1 => FindBlock(activeTarget),
            2 => FindBlock(completedTarget),
            _ => null
        };

        if (dest != null)
        {
            StopParentBlock();     // ① 先结束自己
            dest.StartExecution(); // ② 再跳过去
        }
        else
        {
            Continue();           // 无目标就正常往下走
        }
    }

    private void CacheBlocks()
    {
        if (_blockMap != null) return;
        _blockMap = new Dictionary<string, Block>();
        foreach (var b in Flow.GetComponentsInChildren<Block>())
            _blockMap[b.BlockName] = b;
    }

    private Block FindBlock(string blockName)
    {
        if (string.IsNullOrEmpty(blockName)) return null;
        CacheBlocks();
        return _blockMap.TryGetValue(blockName, out var block) ? block : null;
    }

    public override string GetSummary() => $"Task [{taskId}] state jump";
    public override Color GetButtonColor() => new(0.9f, 0.9f, 0.35f);
}