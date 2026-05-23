using UnityEngine.Events;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;

public class InventoryManager : MonoBehaviour
{
    // 单例实例（全局可访问，与TaskManager风格一致）
    public static InventoryManager Instance { get; private set; }
    // [SerializeField] private PlayerController playerController; // 玩家控制器引用（用于效果执行时传递使用者）
    // 效果处理器表
    private static readonly Dictionary<EffectType, IItemEffectHandler> handlers
        = new Dictionary<EffectType, IItemEffectHandler>();

    [Header("背包配置")]
    public int backpackCapacity = 20; // 背包最大容量（可容纳的物品堆叠数）
    [SerializeField] private List<ItemAsset> itemDatabase; // 物品数据库（所有可获取的物品）

    // 私有数据：背包存储（物品堆叠列表）、物品ID到配置的映射（提高查询效率）
    private string savePath;
    private List<ItemStack> backpackItems;
    private Dictionary<string, ItemAsset> id2ItemAsset;

    // 事件回调（供UI系统监听，更新背包界面）
    public UnityAction onInventoryChanged;

    #region 生命周期与初始化
    void Awake()
    {
        // 单例初始化（防止重复创建）
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        savePath = Path.Combine(Application.persistentDataPath, "inventory_save.json");
        InitItemDatabase();   // ① 必须先建好字典
        InitBackpack();       // ② 再建空背包
        LoadInventory();      // ③ 最后读档，依赖字典
        RegisterAllHandlers();
    }

    private void OnApplicationQuit() => SaveInventory();   // 退出时存档

    // 初始化物品数据库（转换为字典，提高查询效率）
    private void InitItemDatabase()
    {
        id2ItemAsset = new Dictionary<string, ItemAsset>();
        foreach (var itemAsset in itemDatabase)
        {
            if (!string.IsNullOrEmpty(itemAsset.itemId) && !id2ItemAsset.ContainsKey(itemAsset.itemId))
            {
                id2ItemAsset.Add(itemAsset.itemId, itemAsset);
            }
        }
    }

    // 初始化空背包
    private void InitBackpack()
    {
        backpackItems = new List<ItemStack>();
    }
    #endregion

    #region 物品获取-AddItem
    /// <summary>
    /// 获取物品（核心方法，支持堆叠、容量校验、任务进度同步）
    /// </summary>
    /// <param name="itemId">物品唯一ID</param>
    /// <param name="count">获取数量</param>
    /// <returns>是否获取成功（容量不足/物品不存在时返回false）</returns>
    public bool AddItem(string itemId, int count)
    {
        // 1. 基础校验
        if (count <= 0 || !id2ItemAsset.TryGetValue(itemId, out var targetItemAsset))
        {
            Debug.LogError($"[背包] 物品获取失败:无效物品ID {itemId} 或无效数量 {count}");
            return false;
        }
        if (targetItemAsset.isUnique && HasItem(itemId, 1))
        {
            Debug.LogWarning($"[背包] 物品获取失败:唯一物品 {targetItemAsset.itemName} 已存在");
            return false;
        }

        int remainingAmount = count;

        // 2. 优先尝试堆叠到已有物品中
        foreach (var existingStack in backpackItems)
        {
            if (remainingAmount <= 0) break;
            if (existingStack.itemAsset.itemId == itemId && existingStack.CanStack)
            {
                remainingAmount = existingStack.AddStack(remainingAmount);
            }
        }

        // 3. 剩余物品创建新堆叠（校验背包容量）
        while (remainingAmount > 0)
        {
            if (backpackItems.Count >= backpackCapacity)
            {
                Debug.LogWarning($"[背包] 物品获取失败：背包容量不足（当前{backpackItems.Count}/{backpackCapacity}）");
                return remainingAmount == count ? false : true; // 部分获取成功
            }

            // 计算新堆叠的数量（不超过物品最大堆叠数）
            int newStackCount = Mathf.Min(remainingAmount, targetItemAsset.maxStackSize);
            backpackItems.Add(new ItemStack(targetItemAsset, newStackCount));
            remainingAmount -= newStackCount;
        }

        // 4. 关键：同步任务系统进度（收集类任务）
        TaskManager.Instance?.Notify(itemId, count);

        // 5. 触发背包变更事件（更新UI）
        onInventoryChanged?.Invoke();
        Debug.Log($"[背包] 成功获取 {count} 个 {targetItemAsset.itemName}");
        return true;
    }

    /// <summary>
    /// 重载：直接通过ItemAsset获取物品（适配任务奖励发放）
    /// </summary>
    public bool AddItem(ItemAsset itemAsset, int count)
    {
        if (itemAsset == null) return false;
        return AddItem(itemAsset.itemId, count);
    }
    #endregion

    #region 物品使用-UseItem
    /// <summary>
    /// 使用物品（支持消耗品、任务物品，过滤不可使用物品）
    /// </summary>
    /// <param name="itemId">物品唯一ID</param>
    /// <param name="count">使用数量</param>
    /// <returns>是否使用成功</returns>
    public bool UseItem(string itemId, int count)
    {
        // 1. 参数合法性
        if (count <= 0)
        {
            Debug.LogError($"[背包] 物品使用失败：无效数量 {count}");
            return false;
        }

        if (!id2ItemAsset.TryGetValue(itemId, out var itemAsset) || itemAsset == null)
        {
            Debug.LogError($"[背包] 物品使用失败：找不到物品 {itemId}");
            return false;
        }

        // 2. 是否允许使用
        if (!IsItemUsable(itemAsset))
        {
            Debug.LogWarning($"[背包] 物品使用失败：{itemAsset.itemName} 不可使用");
            return false;
        }

        // 3. 数量是否足够
        if (!HasItem(itemId, count))
        {
            Debug.LogWarning($"[背包] 物品使用失败：{itemAsset.itemName} 数量不足");
            return false;
        }

        // ✅ 4. 执行效果
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            for(int i = 0;i < count; i++)
            {
                ExecuteItemEffect(itemAsset, player.gameObject);
            }
        }

        // 5. 扣除物品
        int leftToRemove = count;
        for (int i = backpackItems.Count - 1; i >= 0 && leftToRemove > 0; i--)
        {
            var stack = backpackItems[i];
            if (stack.itemAsset.itemId != itemId) continue;

            int remove = Mathf.Min(leftToRemove, stack.count);
            stack.ReduceStack(remove);
            leftToRemove -= remove;

            if (stack.count <= 0)               // 堆叠已空
            {
                backpackItems.RemoveAt(i);
            }
        }

        // 6. 通知 & 日志
        onInventoryChanged?.Invoke();
        Debug.Log($"[背包] 成功使用 {count} 个 {itemAsset.itemName}");
        return true;
    }

    // 判断物品是否可使用
    private bool IsItemUsable(ItemAsset asset)
    {
        return asset.itemType == ItemType.Consumable ||
            asset.itemType == ItemType.Quest;
    }

    /* ======== 原来调用 UseItem 的地方统一走这里 ======== */
    private void ExecuteItemEffect(ItemAsset item, GameObject user)
    {
        if (item.itemEffects == null || item.itemEffects.Count == 0) return;

        foreach (var eff in item.itemEffects)
        {
            if (handlers.TryGetValue(eff.type, out var h))
                h.Execute(user, eff);
            else
                Debug.LogWarning($"[Item] 未注册的效果处理器 {eff.type}");
        }
    }
    #endregion

    #region 物品丢弃-DropItem
    /// <summary>
    /// 丢弃物品（支持部分丢弃，过滤不可丢弃物品）
    /// </summary>
    /// <param name="itemId">物品唯一ID</param>
    /// <param name="count">丢弃数量</param>
    /// <returns>是否丢弃成功</returns>
    public bool DropItem(string itemId, int count)
    {
        // 1. 基础校验
        if (count <= 0 || !id2ItemAsset.TryGetValue(itemId, out var targetItemAsset))
        {
            Debug.LogError($"[背包] 物品丢弃失败:无效物品ID {itemId} 或无效数量 {count}");
            return false;
        }
        if (!HasItem(itemId, count))
        {
            Debug.LogWarning($"[背包] 物品丢弃失败:{targetItemAsset.itemName} 数量不足");
            return false;
        }
        if (targetItemAsset.itemType == ItemType.Quest || targetItemAsset.itemType == ItemType.Currency)
        {
            Debug.LogWarning($"[背包] 物品丢弃失败:{targetItemAsset.itemName} 不可丢弃");
            return false;
        }

        int remainingDropAmount = count;

        // 2. 遍历背包，减少对应物品数量
        for (int i = backpackItems.Count - 1; i >= 0 && remainingDropAmount > 0; i--)
        {
            var currentStack = backpackItems[i];
            if (currentStack.itemAsset.itemId != itemId) continue;

            // 尝试减少当前堆叠数量
            int reduceAmount = Mathf.Min(remainingDropAmount, currentStack.count);
            bool reduceSuccess = currentStack.ReduceStack(reduceAmount);

            if (reduceSuccess)
            {
                remainingDropAmount -= reduceAmount;

                // 3. 堆叠数量为0时，移除该堆叠
                if (currentStack.count <= 0)
                {
                    backpackItems.RemoveAt(i);
                }
            }
        }

        // 4. 触发背包变更事件（更新UI）
        onInventoryChanged?.Invoke();
        Debug.Log($"[背包] 成功丢弃 {count - remainingDropAmount} 个 {targetItemAsset.itemName}");
        return remainingDropAmount <= 0;
    }
    #endregion

    #region 背包整理-OrganizeBackpack
    /// <summary>
    /// 整理背包（合并可堆叠物品、按物品类型排序）
    /// </summary>
    public void OrganizeBackpack()
    {
        // 1. 分组：按物品ID分组，合并所有可堆叠物品的数量
        var itemGroups = new Dictionary<string, int>();
        foreach (var itemStack in backpackItems)
        {
            if (!itemStack.IsValid) continue;

            string itemId = itemStack.itemAsset.itemId;
            if (itemGroups.ContainsKey(itemId))
            {
                itemGroups[itemId] += itemStack.count;
            }
            else
            {
                itemGroups.Add(itemId, itemStack.count);
            }
        }

        // 2. 清空原有背包，重新创建合并后的物品堆叠
        backpackItems.Clear();
        foreach (var group in itemGroups)
        {
            string itemId = group.Key;
            int totalCount = group.Value;
            if (!id2ItemAsset.TryGetValue(itemId, out var itemAsset)) continue;

            // 拆分为多个最大堆叠（如物品最大堆叠99，总数量200则拆分为2个99+1个2）
            while (totalCount > 0)
            {
                int stackCount = Mathf.Min(totalCount, itemAsset.maxStackSize);
                backpackItems.Add(new ItemStack(itemAsset, stackCount));
                totalCount -= stackCount;
            }
        }

        // 3. 按物品类型排序（优化UI展示）
        backpackItems = backpackItems.OrderBy(stack => stack.itemAsset.itemType)
                                     .ThenBy(stack => stack.itemAsset.itemName)
                                     .ToList();

        // 4. 触发背包变更事件（更新UI）
        onInventoryChanged?.Invoke();
        Debug.Log("[背包] 背包整理完成");
    }
    #endregion

    #region 辅助查询方法（供外部系统调用）
    /// <summary>
    /// 检查是否拥有指定数量的物品
    /// </summary>
    public bool HasItem(string itemId, int requiredCount = 1)
    {
        if (requiredCount <= 0) return true;

        int totalCount = 0;
        foreach (var itemStack in backpackItems)
        {
            if (itemStack.itemAsset?.itemId == itemId)
            {
                totalCount += itemStack.count;
                if (totalCount >= requiredCount) return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 获取背包中所有物品堆叠
    /// </summary>
    public List<ItemStack> GetAllBackpackItems()
    {
        return new List<ItemStack>(backpackItems); // 返回副本，防止外部修改原数据
    }

    /// <summary>
    /// 获取物品配置（与TaskManager的GetTaskAsset风格一致）
    /// </summary>
    public ItemAsset GetItemAsset(string itemId)
    {
        if (Instance == null)
        {
            var mgr = FindFirstObjectByType<InventoryManager>();
            if (mgr != null) Instance = mgr;
            else
            {
                Debug.LogError("[InventoryManager] 单例未初始化，且场景里找不到 InventoryManager");
                return null;
            }
        }

        id2ItemAsset.TryGetValue(itemId, out var itemAsset);
        return itemAsset;
    }
    #endregion

    // 注册所有物品效果处理器
    private void RegisterAllHandlers()
    {
        var types = System.AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .Where(t => !t.IsAbstract && typeof(IItemEffectHandler).IsAssignableFrom(t));

        foreach (var t in types)
        {
            var h = (IItemEffectHandler)System.Activator.CreateInstance(t);
            handlers[h.EffectType] = h;
        }
    }

    #region 持久化 Save / Load / Delete
    private void SaveInventory()
    {
        var data = new InventorySaveData
        {
            stacks = backpackItems.Select(s => new ItemStackData
            {
                itemId = s.itemAsset.itemId,
                count = s.count
            }).ToList()
        };

        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(savePath, json);
        // Debug.Log("[Inventory] Saved.");
    }

    private void LoadInventory()
    {
        if (!File.Exists(savePath)) return;

        try
        {
            string json = File.ReadAllText(savePath);
            InventorySaveData data = JsonConvert.DeserializeObject<InventorySaveData>(json);
            if (data?.stacks == null) return;

            backpackItems.Clear();
            foreach (var s in data.stacks)
            {
                if (id2ItemAsset.TryGetValue(s.itemId, out var asset))
                    backpackItems.Add(new ItemStack(asset, s.count));
            }
            // Debug.Log("[Inventory] Loaded.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Inventory] Failed to load: {ex.Message}");
            InitBackpack();   // 读档失败回到空背包
        }
    }

    public void DeleteSaveFile()
    {
        // 1. 删磁盘文件
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("[Item] Save file deleted.");
        }

        // 2. 清空运行时背包
        backpackItems.Clear();

        // 3. 通知所有监听者（UI）刷新
        onInventoryChanged?.Invoke();
    }
    #endregion
}