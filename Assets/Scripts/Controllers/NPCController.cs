using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// NPC控制器（精简优化版）
/// - 事件驱动
/// - 自动注册小地图图标
/// - 无冗余绑定
/// </summary>
public class NPCController : MonoBehaviour
{
    [Header("NPC配置")]
    [SerializeField] private NPCAsset npcAsset;

    [Header("碰撞检测")]
    [SerializeField] private TalkCharacterType playerTag;

    // ===== 事件 =====
    public UnityAction<NPCAsset> onPlayerEnter;
    public UnityAction<NPCAsset> onPlayerExit;
    public UnityAction<NPCAsset> onInteract;

    private bool isPlayerInRange = false;

    // 地图图标缓存
    private GameObject mapIconInstance;
    private MapIcon mapIcon;

    #region 生命周期
    private void Awake()
    {
        EnsureColliderSetup();
    }

    private void Start()
    {
        
    }

    void Update()
    {
        
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying) return;

        if (mapIconInstance != null)
            Destroy(mapIconInstance);
    }

    private void Reset()
    {
        playerTag = TalkCharacterType.Player;
    }
    #endregion

    #region 碰撞检测
    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;

        isPlayerInRange = true;
        onPlayerEnter?.Invoke(npcAsset);

        if (!string.IsNullOrEmpty(npcAsset?.npcId))
        {
            GameEventBus.Publish(new NPCEnterRangeEvent
            {
                npcId = npcAsset.npcId,
                npcAsset = npcAsset
            });
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;

        isPlayerInRange = false;
        onPlayerExit?.Invoke(npcAsset);

        if (!string.IsNullOrEmpty(npcAsset?.npcId))
        {
            GameEventBus.Publish(new NPCExitRangeEvent
            {
                npcId = npcAsset.npcId,
                npcAsset = npcAsset
            });
        }
    }
    #endregion

    #region 交互
    public void Interact()
    {
        if (!isPlayerInRange || npcAsset == null)
            return;

        onInteract?.Invoke(npcAsset);

        GameEventBus.Publish(new NPCInteractEvent
        {
            npcId = npcAsset.npcId,
            npcAsset = npcAsset
        });
    }
    #endregion

    #region 工具
    private bool IsPlayer(Collider other)
    {
        return other.CompareTag(playerTag.ToString());
    }

    private void EnsureColliderSetup()
    {
        var collider = GetComponent<Collider>();
        if (collider == null)
            collider = gameObject.AddComponent<SphereCollider>();

        if (collider is SphereCollider sphere)
        {
            sphere.isTrigger = true;
            sphere.radius = npcAsset?.interactRadius ?? 2f;
        }
        else if (collider is BoxCollider box)
        {
            box.isTrigger = true;
        }
    }

    public NPCAsset GetNPCAsset() => npcAsset;

    public void SetNPCAsset(NPCAsset asset)
    {
        npcAsset = asset;
        EnsureColliderSetup();
    }

    #endregion
}

public enum TalkCharacterType
{
    Player,
    NPC,
    Enemy
}