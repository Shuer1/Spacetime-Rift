using UnityEngine;
using Fungus;

[CommandInfo("Dialogue", "First Talk Jump", "根据是否首次对话跳转到指定 Block")]
[AddComponentMenu("")]
public class CmdFirstTalkJump : Command
{
    [Tooltip("NPC 唯一 ID")]
    [SerializeField] private string npcId;

    [Tooltip("首次对话时跳转的 Block 名称")]
    [SerializeField] private string firstBlock;

    [Tooltip("非首次对话时跳转的 Block 名称")]
    [SerializeField] private string normalBlock;

    private Flowchart flow;
    private bool hasJumped;

    public override void OnEnter()
    {
        if (string.IsNullOrEmpty(npcId))
        {
            Debug.LogWarning("[CmdFirstTalkJump] npcId 为空，直接继续");
            Continue(); return;
        }

        // 首次？标记并跳转
        bool isFirst = FirstTalkRepo.IsFirst(npcId);
        string targetBlock = isFirst ? firstBlock : normalBlock;

        if (isFirst) FirstTalkRepo.Finish(npcId);   // 先标记，防止重入

        if (!string.IsNullOrEmpty(targetBlock))
        {
            var block = GetFlowchart().FindBlock(targetBlock);
            if (block != null)
            {
                StopParentBlock();     // 结束当前命令所在 Block
                block.StartExecution();// 跳到目标 Block
                return;
            }
        }

        // 无目标或找不到 Block 就继续往下走
        Continue();
    }

    public override string GetSummary() =>
        string.IsNullOrEmpty(npcId) ? "未指定 NPC" : $"NPC [{npcId}] 首次分流";

    public override Color GetButtonColor() => new Color(0.8f, 0.9f, 0.5f);
}