using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.AI;

public class MapClickHandler : MonoBehaviour, IPointerClickHandler
{
    public NavMeshAgent agent;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!MiniMapSystem.Instance.isWorldMapMode)
            return;

        // 如果这一按压过程发生过拖动，则不触发点击寻路
        if (MiniMapSystem.Instance.ShouldSuppressClickThisPress())
        {
            MiniMapSystem.Instance.ConsumeSuppressClickFlag(); 
            return;
        }

        Vector2 mapLocal;
        if (!MiniMapSystem.Instance.TryScreenPointToMapLocal(
                eventData.position,
                eventData.pressEventCamera,
                out mapLocal))
            return;

        Vector3 worldPos = MiniMapSystem.Instance.MapToWorld(mapLocal);

        // 统一交给 MarkerManager
        if (MapMarkerManager.Instance != null)
            MapMarkerManager.Instance.ShowMarker(mapLocal);

        agent.SetDestination(worldPos);

        // 地图点击后取消 icon 选中
        MapIconSelectionManager.Instance?.ClearSelection();
    }
}