using UnityEngine;
using System;

[Serializable]
public class ItemStack
{
    public ItemAsset itemAsset;    // 关联的物品配置
    public int count;              // 当前堆叠数量

    // 验证物品是否有效
    public bool IsValid => itemAsset != null && count > 0 && count <= itemAsset.maxStackSize;
    // 判断是否可以继续堆叠
    public bool CanStack => IsValid && count < itemAsset.maxStackSize && !itemAsset.isUnique;

    // 构造函数（创建物品堆叠）
    public ItemStack(ItemAsset asset, int stackCount = 1)
    {
        itemAsset = asset;
        // 限制数量在有效范围内（1 ~ 最大堆叠数）
        count = Mathf.Clamp(stackCount, 1, asset?.maxStackSize ?? 1);
    }

    // 尝试添加堆叠数量（返回未堆叠成功的剩余数量）
    public int AddStack(int amount)
    {
        if (!CanStack || amount <= 0) return amount;

        // 计算可堆叠的最大数量
        int availableSpace = itemAsset.maxStackSize - count;
        int addAmount = Mathf.Min(amount, availableSpace);
        
        // 更新堆叠数量
        count += addAmount;
        // 返回剩余未堆叠的数量
        return amount - addAmount;
    }

    // 尝试减少堆叠数量（返回是否减少成功）
    public bool ReduceStack(int amount)
    {
        if (!IsValid || amount <= 0 || count < amount) return false;

        count -= amount;
        return true;
    }
}