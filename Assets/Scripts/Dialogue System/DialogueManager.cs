using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 轻量级对话管理器 - 仅负责打开Fungus对话块
/// 任务接取、物品奖励等完全由Fungus内部处理
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("NPC配置数据库")]
    [SerializeField] private NPCAsset[] npcDatabase;

    [Header("Fungus配置(关键修复)-指定Flowchart")]
    [SerializeField] private Fungus.Flowchart targetFlowchart;
    private Dictionary<string, NPCAsset> id2NPC;
    private Fungus.Flowchart cachedFlowchart; // 缓存Flowchart引用
    private NPCAsset currentNPC;

    // 新增：对话状态事件
    public static event System.Action<bool> OnDialogueStateChanged;

    #region 生命周期
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        //DontDestroyOnLoad(gameObject);

        InitNPCDatabase();
        InitFungusReference(); // 初始化Flowchart缓存
        RegisterEvents();

        Debug.Log("[DialogueManager] 轻量级模式初始化完成");
    }
    #endregion

    #region 数据库初始化
    private void InitNPCDatabase()
    {
        id2NPC = new Dictionary<string, NPCAsset>();
        foreach (var npc in npcDatabase)
        {
            if (!string.IsNullOrEmpty(npc.npcId) && !id2NPC.ContainsKey(npc.npcId))
            {
                id2NPC.Add(npc.npcId, npc);
            }
        }
    }
    #endregion

    #region Fungus初始化(新增)
    private void InitFungusReference()
    {
        // 优先使用手动指定的Flowchart（避免查找错误）
        if (targetFlowchart != null)
        {
            cachedFlowchart = targetFlowchart;
        }
        else
        {
            // 备用：查找场景中的第一个Flowchart
            cachedFlowchart = FindFirstObjectByType<Fungus.Flowchart>();
        }

        if (cachedFlowchart == null)
        {
            Debug.LogWarning("[DialogueManager] 未找到Fungus Flowchart组件,请检查场景或手动指定targetFlowchart");
        }
    }
    #endregion

    #region 公开API
    /// <summary>
    /// 开始与NPC对话（打开对应的Fungus Block）
    /// </summary>
    public void StartDialogue(string npcId)
    {
        if (!id2NPC.TryGetValue(npcId, out var npc))
        {
            Debug.LogError($"NPC配置不存在: {npcId}");
            return;
        }

        currentNPC = npc;

        // 🔒 锁定玩家输入
        PlayerControlManager.Instance.Lock(ControlLockType.Dialogue);

        OnDialogueStateChanged?.Invoke(true);

        OpenFungusBlock(npc.fungusBlockName);
    }

    /// <summary>
    /// 打开Fungus对话块（修复查找逻辑）
    /// </summary>
    private void OpenFungusBlock(string blockName)
    {
        if (string.IsNullOrEmpty(blockName))
        {
            Debug.LogWarning($"[DialogueManager] NPC {currentNPC.npcName} 未配置Fungus Block");
            return;
        }

        // 检查缓存的Flowchart是否有效，无效则重新查找
        if (cachedFlowchart == null)
        {
            InitFungusReference();
            if (cachedFlowchart == null)
            {
                Debug.LogError("[DialogueManager] 场景中没有有效的Flowchart组件");
                return;
            }
        }

        // 查找目标Block
        var block = cachedFlowchart.FindBlock(blockName);
        if (block != null)
        {
            // 执行Block
            cachedFlowchart.ExecuteBlock(block);
            Debug.Log($"[DialogueManager] 打开Fungus Block: {blockName}（Flowchart: {cachedFlowchart.gameObject.name}）");
        }
        else
        {
            Debug.LogError($"[DialogueManager] Flowchart「{cachedFlowchart.gameObject.name}」中找不到Block: {blockName}");
        }
    }

    /// <summary>
    /// 获取当前NPC
    /// </summary>
    public NPCAsset GetCurrentNPC()
    {
        return currentNPC;
    }

    /// <summary>
    /// 结束对话（由Fungus Block调用）
    /// </summary>
    public void EndDialogue()
    {
        currentNPC = null;

        // 🔓 恢复玩家输入
        PlayerControlManager.Instance.Unlock(ControlLockType.Dialogue);

        OnDialogueStateChanged?.Invoke(false);

        Debug.Log("[DialogueManager] 对话结束");
    }
    #endregion

    #region 事件处理
    private void RegisterEvents()
    {
        GameEventBus.Subscribe<NPCInteractEvent>(OnNPCInteract);
    }

    private void OnNPCInteract(GameEvent e)
    {
        if (e is NPCInteractEvent interactEvent)
        {
            StartDialogue(interactEvent.npcId);
        }
    }
    #endregion
}