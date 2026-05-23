using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 玩家交互控制器（零 Update 依赖）
/// </summary>
public class PlayerInteractionController : MonoBehaviour
{
    [Header("交互配置")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private NPCController currentNearbyNPC;
    private PortalController currentNearbyPortal;
    private ItemPrefab currentNearbyItem;

    private void Awake() => RegisterEvents();
    private void OnDestroy() => UnregisterEvents();

    private void RegisterEvents()
    {
        GameEventBus.Subscribe<NPCEnterRangeEvent>(OnNPCEnterRange);
        GameEventBus.Subscribe<NPCExitRangeEvent>(OnNPCExitRange);

        GameEventBus.Subscribe<ItemEnterRangeEvent>(OnItemEnterRange);
        GameEventBus.Subscribe<ItemExitRangeEvent>(OnItemExitRange);

        GameEventBus.Subscribe<PortalEnterRangeEvent>(OnPortalEnterRange);
        GameEventBus.Subscribe<PortalExitRangeEvent>(OnPortalExitRange);
    }

    private void UnregisterEvents()
    {
        GameEventBus.Unsubscribe<NPCEnterRangeEvent>(OnNPCEnterRange);
        GameEventBus.Unsubscribe<NPCExitRangeEvent>(OnNPCExitRange);

        GameEventBus.Unsubscribe<ItemEnterRangeEvent>(OnItemEnterRange);
        GameEventBus.Unsubscribe<ItemExitRangeEvent>(OnItemExitRange);

        GameEventBus.Unsubscribe<PortalEnterRangeEvent>(OnPortalEnterRange);
        GameEventBus.Unsubscribe<PortalExitRangeEvent>(OnPortalExitRange);
    }

    private void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            HandleInteractionAsync().Forget();
        }
    }

    /// <summary>
    /// 异步处理交互：物品 -> 传送门 -> NPC
    /// </summary>
    private async UniTaskVoid HandleInteractionAsync()
    {
        // 物品
        if (currentNearbyItem != null)
        {
            currentNearbyItem.TryPickup();
            return;
        }

        // 传送门
        if (currentNearbyPortal != null)
        {
            await currentNearbyPortal.Teleport(transform);
            return;
        }

        // NPC
        if (currentNearbyNPC != null)
        {
            currentNearbyNPC.Interact();
        }
    }

    #region NPC事件
    private void OnNPCEnterRange(GameEvent e)
    {
        if (e is NPCEnterRangeEvent enterEvent)
        {
            var npcs = FindObjectsByType<NPCController>(FindObjectsSortMode.None);
            foreach (var npc in npcs)
            {
                if (npc.GetNPCAsset()?.npcId == enterEvent.npcId)
                {
                    currentNearbyNPC = npc;
                    break;
                }
            }
        }
    }

    private void OnNPCExitRange(GameEvent e)
    {
        if (e is NPCExitRangeEvent exitEvent)
        {
            if (currentNearbyNPC?.GetNPCAsset()?.npcId == exitEvent.npcId)
                currentNearbyNPC = null;
        }
    }
    #endregion

    #region 物品事件
    private void OnItemEnterRange(GameEvent e)
    {
        if (e is ItemEnterRangeEvent enterEvent)
        {
            currentNearbyItem = enterEvent.item;
            UIManager.Instance.ShowInteractTip(
                $"按 [E] 拾取 {enterEvent.item.itemAsset.itemName}"
            );
        }
    }

    private void OnItemExitRange(GameEvent e)
    {
        if (e is ItemExitRangeEvent exitEvent)
        {
            if (currentNearbyItem == exitEvent.item)
            {
                currentNearbyItem = null;
                UIManager.Instance.HideInteractTip();
            }
        }
    }
    #endregion

    #region 传送门事件
    private void OnPortalEnterRange(GameEvent e)
    {
        if (e is PortalEnterRangeEvent enterEvent)
            currentNearbyPortal = enterEvent.portal;
    }

    private void OnPortalExitRange(GameEvent e)
    {
        if (e is PortalExitRangeEvent exitEvent)
        {
            if (currentNearbyPortal == exitEvent.portal)
                currentNearbyPortal = null;
        }
    }
    #endregion
}