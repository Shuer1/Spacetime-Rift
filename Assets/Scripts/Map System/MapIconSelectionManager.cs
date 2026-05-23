using UnityEngine;
using UnityEngine.AI;

public class MapIconSelectionManager : MonoBehaviour
{
    public static MapIconSelectionManager Instance;

    private MapIcon currentSelected;

    [Header("Navigation")]
    public NavMeshAgent playerAgent;

    void Awake()
    {
        Instance = this;
    }

    // ⭐ 选中图标
    public void Select(MapIcon icon)
    {
        if (icon == null || icon.target == null)
            return;

        // 取消旧选中
        if (currentSelected != null)
            currentSelected.SetHighlight(false);

        currentSelected = icon;

        // 高亮
        currentSelected.SetHighlight(true);

        // 自动寻路
        Vector3 targetPos = icon.target.position;
        playerAgent.SetDestination(targetPos);

        // 统一交给 MarkerManager
        if (MapMarkerManager.Instance != null)
            MapMarkerManager.Instance.ShowMarker(icon);
    }

    public void ClearSelection()
    {
        if (currentSelected != null)
        {
            currentSelected.SetHighlight(false);
            currentSelected = null;
        }
    }
}