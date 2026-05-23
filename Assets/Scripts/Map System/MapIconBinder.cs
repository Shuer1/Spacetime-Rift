using UnityEngine;

/// <summary>
/// 通用地图图标绑定器（NPC / Enemy / Boss 全通用）
/// </summary>
public class MapIconBinder : MonoBehaviour
{
    [Header("图标配置")]
    public MapIconType iconType = MapIconType.NPC;

    [Tooltip("为空则自动从 Resources 加载")]
    public GameObject iconPrefab;

    private GameObject iconInstance;
    private MapIcon mapIcon;

    void Start()
    {
        SetupIcon();
    }

    void Update()
    {
        RefreshParent();
    }

    void OnDestroy()
    {
        if (!Application.isPlaying) return;

        if (iconInstance != null)
            Destroy(iconInstance);
    }

    void SetupIcon()
    {
        var map = MiniMapSystem.Instance;
        if (map == null) return;

        Transform root = map.GetCurrentIconsRoot();
        if (root == null) return;

        // ⭐自动加载默认图标
        if (iconPrefab == null)
        {
            string path = GetDefaultPath(iconType);
            iconPrefab = Resources.Load<GameObject>(path);

            if (iconPrefab == null)
            {
                Debug.LogError($"[MapIconBinder] 未找到图标: Resources/{path}");
                return;
            }
        }

        iconInstance = Instantiate(iconPrefab, root);

        mapIcon = iconInstance.GetComponent<MapIcon>();
        if (mapIcon == null)
        {
            Debug.LogError("[MapIconBinder] iconPrefab 缺少 MapIcon 组件");
            return;
        }

        mapIcon.target = transform;
        mapIcon.iconType = iconType;
    }

    string GetDefaultPath(MapIconType type)
    {
        switch (type)
        {
            case MapIconType.NPC: return "Prefabs/Icons/NPCIcon";
            case MapIconType.Enemy: return "Prefabs/Icons/EnemyIcon";
            case MapIconType.Quest: return "Prefabs/Icons/QuestIcon";
            default: return "Prefabs/Icons/DefaultIcon";
        }
    }

    void RefreshParent()
    {
        if (iconInstance == null || mapIcon == null) return;

        var map = MiniMapSystem.Instance;
        if (map == null) return;

        Transform root = map.GetCurrentIconsRoot();
        if (root == null) return;

        if (iconInstance.transform.parent != root)
        {
            RectTransform rect = iconInstance.GetComponent<RectTransform>();

            Vector2 mapPos = map.WorldToMap(transform.position);

            iconInstance.transform.SetParent(root, false);

            if (rect != null)
                rect.anchoredPosition = mapPos;

            iconInstance.SetActive(true);
        }
    }
}