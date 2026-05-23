using System.Collections.Generic;
using UnityEngine;

public class MapIconManager : MonoBehaviour
{
    public static MapIconManager Instance;

    private readonly List<MapIcon> icons = new();

    void Awake()
    {
        Instance = this;
    }

    void LateUpdate()
    {
        UpdateIcons();
    }

    public void Register(MapIcon icon)
    {
        if (icon == null) return;

        if (!icons.Contains(icon))
            icons.Add(icon);
    }

    public void Unregister(MapIcon icon)
    {
        if (icon == null) return;

        if (icons.Contains(icon))
            icons.Remove(icon);
    }

    void UpdateIcons()
    {
        var map = MiniMapSystem.Instance;
        if (map == null) return;

        Transform currentRoot = map.GetCurrentIconsRoot();
        if (currentRoot == null) return;

        for (int i = icons.Count - 1; i >= 0; i--)
        {
            var icon = icons[i];

            if (icon == null)
            {
                icons.RemoveAt(i);
                continue;
            }

            if (icon.target == null || icon.rect == null)
                continue;

            // ⭐地图类型过滤
            if (!map.isWorldMapMode && !icon.onMiniMap)
            {
                icon.SetVisible(false);
                continue;
            }

            if (map.isWorldMapMode && !icon.onWorldMap)
            {
                icon.SetVisible(false);
                continue;
            }

            // ⭐自动切换父节点（关键）
            icon.RefreshParent(currentRoot);

            // ⭐更新位置
            Vector2 mapLocal = map.WorldToMap(icon.target.position);
            icon.rect.anchoredPosition = mapLocal;

            // ⭐可见性判断
            bool isVisible = map.isWorldMapMode || map.IsMapLocalVisible(mapLocal);

            icon.SetVisible(isVisible);

            HandleIconType(icon);
        }
    }

    void HandleIconType(MapIcon icon)
    {
        switch (icon.iconType)
        {
            case MapIconType.NPC:
                break;
            case MapIconType.Enemy:
                break;
            case MapIconType.Quest:
                break;
        }
    }
}