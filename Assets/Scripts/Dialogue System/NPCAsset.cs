using UnityEngine;

/// <summary>
/// NPC配置资源（轻量版）- 仅配置基本信息和Fungus Block名称
/// 对话内容、任务接取、物品奖励等完全由Fungus内部处理
/// </summary>
[CreateAssetMenu(menuName = "NPC/NPCAsset", fileName = "NewNPC")]
public class NPCAsset : ScriptableObject
{
    [Header("基础信息")]
    public string npcId;              // NPC唯一ID
    public string npcName;            // NPC显示名称
    public Sprite icon;               // NPC头像（UI显示）

    [Header("交互配置")]
    [Tooltip("玩家进入交互范围（触发器半径）")]
    public float interactRadius = 2f;
    
    [Tooltip("交互提示文本，如：按[E]对话")]
    public string interactPrompt = "按[E]对话";

    [Header("Fungus集成")]
    [Tooltip("对应的Fungus Flowchart中的Block名称")]
    public string fungusBlockName;

    /// <summary>
    /// 获取对话提示文本
    /// </summary>
    public string GetPromptText()
    {
        return interactPrompt;
    }
}