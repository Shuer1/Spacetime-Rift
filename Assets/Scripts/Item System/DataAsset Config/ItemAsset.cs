using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(menuName = "Item/ItemAsset", fileName = "New Item")]
public class ItemAsset : ScriptableObject
{
    [Header("基础标识")]
    public string itemId;          // 物品唯一ID（与任务系统targetId、奖励配置对应）
    public string itemName;        // 物品显示名称

    [Header("显示配置")]
    [TextArea] public string description;  // 物品描述
    public Sprite icon;            // 物品图标（用于背包UI渲染）
    [Header("掉落配置")]
    public GameObject worldPrefab; // 物品世界实例

    [Header("功能配置")]
    public ItemType itemType;      // 物品类型
    public int maxStackSize = 99;  // 最大堆叠数量（材料/消耗品默认99，装备/唯一物品默认1）
    public bool isUnique = false;  // 是否唯一物品（不可堆叠、不可重复获取）
    public int sellPrice;          // 出售价格

    [Header("效果配置（仅消耗品/任务物品生效）")]
    public List<ItemEffect> itemEffects = new List<ItemEffect>(); // 物品效果列表（可配置多个效果）
    void OnValidate()
    {
        // 自动修正：唯一物品/装备的最大堆叠数强制为1
        if (isUnique || itemType == ItemType.Equipment)
        {
            maxStackSize = 1;
        }
        // 自动填充itemId（若未手动设置，使用文件名）
        if (string.IsNullOrEmpty(itemId) && !string.IsNullOrEmpty(name))
        {
            itemId = name;
        }
    }
}

// 物品类型枚举（覆盖核心使用场景）
public enum ItemType
{
    Consumable,   // 消耗品（药水、食物等，使用后消失）
    Equipment,    // 装备（武器、防具等，不可消耗，可穿戴）
    Material,     // 材料（矿石、布料等，用于合成/任务提交）
    Quest,        // 任务物品（仅用于任务，不可出售/丢弃）
    Currency      // 货币（金币、钻石等，特殊处理）
}

[Serializable]
public class ItemEffect
{
    public EffectType type;
    public int intParam;   // 数值/任务ID
    public float duration;   // 持续秒数（瞬时类可填0）
}

// 待完善
public enum EffectType
{
    Heal,        // 立即回血
    SpeedUp,     // 加速buff
    AttackUp,
    DefendUp,
    Purchase,
}