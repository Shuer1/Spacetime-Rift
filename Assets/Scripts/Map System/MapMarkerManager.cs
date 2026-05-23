using UnityEngine;
using UnityEngine.UI;

public class MapMarkerManager : MonoBehaviour
{
    public static MapMarkerManager Instance;

    [Header("Marker")]
    public GameObject markerPrefab;
    public Button cancelMarkerBtn;

    [Header("Edge Indicator")]
    public GameObject edgeIndicatorPrefab;

    private GameObject currentMarker;
    private MapEdgeIndicator edgeIndicator;

    private Vector3 markerWorldPos;
    private Transform currentTarget; // ✅ 当前绑定目标
    private bool hasMarker = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        cancelMarkerBtn.onClick.AddListener(() => HideMarker());
    }

    void LateUpdate()
    {
        if (!hasMarker) return;

        var map = MiniMapSystem.Instance;
        if (map == null) return;

        // ✅ 如果绑定了目标 → 实时跟随
        if (currentTarget != null)
            markerWorldPos = currentTarget.position;

        RectTransform container = map.GetCurrentContainer();
        if (container == null) return;

        RectTransform viewport = container.parent as RectTransform;
        if (viewport == null) return;

        // =========================
        // 坐标转换
        // =========================
        Vector2 mapLocal = map.WorldToMap(markerWorldPos);

        float zoom = container.localScale.x;
        Vector2 viewportPos = container.anchoredPosition + mapLocal * zoom;

        float halfW = viewport.rect.width * 0.5f;
        float halfH = viewport.rect.height * 0.5f;

        bool inside =
            Mathf.Abs(viewportPos.x) <= halfW &&
            Mathf.Abs(viewportPos.y) <= halfH;

        // =========================
        // 在视野内 → 显示 Marker
        // =========================
        if (inside)
        {
            EnsureMarker(container);

            RectTransform rect = currentMarker.GetComponent<RectTransform>();

            if (rect.parent != container)
                rect.SetParent(container, false);

            rect.anchoredPosition = mapLocal;

            currentMarker.SetActive(true);

            if (edgeIndicator != null)
                edgeIndicator.gameObject.SetActive(false);
        }
        else
        {
            // =========================
            // 边缘箭头
            // =========================
            EnsureEdgeIndicator(viewport);

            RectTransform arrowRect = edgeIndicator.rect;

            arrowRect.pivot = new Vector2(0.5f, 0.5f);
            arrowRect.anchorMin = new Vector2(0.5f, 0.5f);
            arrowRect.anchorMax = new Vector2(0.5f, 0.5f);

            Vector2 size = arrowRect.rect.size;
            float radius = Mathf.Sqrt(size.x * size.x + size.y * size.y) * 0.5f;

            float padding = 16f;

            float limitX = halfW - radius - padding;
            float limitY = halfH - radius - padding;

            Vector2 dir = viewportPos.normalized;

            Vector2 edgePos = new Vector2(
                Mathf.Clamp(viewportPos.x, -limitX, limitX),
                Mathf.Clamp(viewportPos.y, -limitY, limitY)
            );

            if (Mathf.Abs(viewportPos.x) > limitX || Mathf.Abs(viewportPos.y) > limitY)
            {
                float scaleX = limitX / Mathf.Abs(viewportPos.x);
                float scaleY = limitY / Mathf.Abs(viewportPos.y);
                float scale = Mathf.Min(scaleX, scaleY);

                edgePos = viewportPos * scale;
            }

            arrowRect.anchoredPosition = Vector2.Lerp(
                arrowRect.anchoredPosition,
                edgePos,
                Time.deltaTime * 20f
            );

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float targetZ = angle - 90f;

            float z = Mathf.LerpAngle(
                arrowRect.eulerAngles.z,
                targetZ,
                Time.deltaTime * 20f
            );

            arrowRect.rotation = Quaternion.Euler(0, 0, z);

            edgeIndicator.gameObject.SetActive(true);

            if (currentMarker != null)
                currentMarker.SetActive(false);
        }
    }

    // =========================
    // 创建对象
    // =========================

    void EnsureMarker(RectTransform parent)
    {
        if (currentMarker == null)
        {
            currentMarker = Instantiate(markerPrefab, parent);
        }
    }

    void EnsureEdgeIndicator(RectTransform parent)
    {
        if (edgeIndicator == null)
        {
            var obj = Instantiate(edgeIndicatorPrefab, parent);
            edgeIndicator = obj.GetComponent<MapEdgeIndicator>();
        }
    }

    // =========================
    // API（核心：支持Toggle）
    // =========================

    public void ShowMarker(MapIcon icon)
    {
        if (icon == null || icon.target == null)
            return;

        currentTarget = icon.target;
        markerWorldPos = icon.target.position;
        hasMarker = true;
    }

    public void ShowMarkerWorld(Vector3 worldPos)
    {
        currentTarget = null;
        markerWorldPos = worldPos;
        hasMarker = true;
    }

    public void ShowMarker(Vector2 mapLocal)
    {
        if (MiniMapSystem.Instance == null) return;

        Vector3 worldPos = MiniMapSystem.Instance.MapToWorld(mapLocal);
        ShowMarkerWorld(worldPos);
    }

    public void HideMarker()
    {
        hasMarker = false;
        currentTarget = null;

        if (currentMarker != null)
            currentMarker.SetActive(false);

        if (edgeIndicator != null)
            edgeIndicator.gameObject.SetActive(false);
    }
}