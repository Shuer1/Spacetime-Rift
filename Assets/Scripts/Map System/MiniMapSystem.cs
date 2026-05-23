using UnityEngine;
using System;

public class MiniMapSystem : MonoBehaviour
{
    public static MiniMapSystem Instance;

    [Header("Map Data")]
    public MapData mapData;

    [Header("Mini Map")]
    public GameObject miniMapRoot;
    public RectTransform miniMapContainer;
    public RectTransform miniMapRect;
    public Transform miniMapIconsRoot;

    [Header("Full Map")]
    public GameObject fullMapRoot;
    public RectTransform fullMapContainer;
    public RectTransform fullMapRect;
    public Transform fullMapIconsRoot;

    [Header("Player")]
    public Transform player;
    public RectTransform playerIcon;
    public RectTransform viewCone;
    [Header("Player Icon - WorldMap")]
    public RectTransform worldPlayerIcon;
    public RectTransform worldViewCone;

    [Header("MiniMap Follow")]
    public float followSmooth = 8f;

    [Header("Zoom")]
    public float miniMapZoom = 1f;
    public float minZoom = 0.75f;
    public float maxZoom = 3f;

    [Tooltip("滚轮每滚一格，在最小/最大缩放区间内线性推进的比例")]
    public float wheelStep = 0.12f;

    [Tooltip("缩放平滑速度，越大越跟手，越小越柔和")]
    public float zoomSmoothSpeed = 12f;

    [Tooltip("首次进入大地图时的默认缩放")]
    public float defaultWorldMapZoom = 1.5f;

    [Tooltip("再次打开大地图时，是否记住上次缩放")]
    public bool rememberWorldMapZoom = true;

    [Tooltip("滚轮缩放时，以鼠标所在位置为焦点")]
    public bool zoomToMousePosition = true;

    [Header("Drag")]
    public float dragSpeed = 1f;

    [Tooltip("鼠标移动超过该像素距离后，视为拖动，而不是点击")]
    public float dragThreshold = 10f;

    [Header("State")]
    public bool isWorldMapMode = false;
    public event Action<bool> OnMapModeChanged;

    private RectTransform currentContainer;
    private RectTransform currentMapRect;

    private float currentZoom = 1f;
    private float targetZoom = 1f;
    private float targetZoom01 = 0.5f;
    private float savedWorldMapZoom = 1.5f;

    private float mapWidth;
    private float mapHeight;

    private Vector2 dragOrigin;
    private bool isDragging;
    private bool dragUseLocalSpace = true;

    // 新增：拖动判定相关
    private bool pointerDownOnMap;
    private bool suppressClickThisPress;
    private Vector2 pointerDownScreenPos;

    // 缩放焦点缓存：用于平滑缩放期间保持鼠标下的地图点不漂移
    private Vector2 zoomFocusViewportLocal;
    private Vector2 zoomFocusMapLocal;
    private bool keepZoomFocus;

    void Awake()
    {
        Instance = this;

        savedWorldMapZoom = Mathf.Clamp(defaultWorldMapZoom, minZoom, maxZoom);
        EnterMiniMap(); // 默认小地图
    }

    void Update()
    {
        if (isWorldMapMode)
        {
            UpdateWorldMapDrag();
            UpdateWorldMapZoom();
        }
        else
        {
            UpdateMiniMapFollow();
        }

        UpdatePlayerTransformOnMap();
    }

    // =========================
    // 模式切换
    // =========================
    public void ToggleWorldMap()
    {
        isWorldMapMode = !isWorldMapMode;

        if (isWorldMapMode)
            EnterWorldMap();
        else
            EnterMiniMap();
    }

    void EnterMiniMap()
    {
        miniMapRoot.SetActive(true);
        fullMapRoot.SetActive(false);

        currentContainer = miniMapContainer;
        currentMapRect = miniMapRect;

        mapWidth = currentMapRect.rect.width;
        mapHeight = currentMapRect.rect.height;

        if (playerIcon != null)
            playerIcon.gameObject.SetActive(true);

        if (worldPlayerIcon != null)
            worldPlayerIcon.gameObject.SetActive(false);

        SetZoomInstant(miniMapZoom);

        if (PlayerControlManager.Instance != null)
            PlayerControlManager.Instance.Unlock(ControlLockType.Map);

        ResetPointerDragState();
        OnMapModeChanged?.Invoke(false);
    }

    void EnterWorldMap()
    {
        miniMapRoot.SetActive(false);
        fullMapRoot.SetActive(true);

        currentContainer = fullMapContainer;
        currentMapRect = fullMapRect;

        mapWidth = currentMapRect.rect.width;
        mapHeight = currentMapRect.rect.height;

        if (playerIcon != null)
            playerIcon.gameObject.SetActive(false);

        if (worldPlayerIcon != null)
            worldPlayerIcon.gameObject.SetActive(true);

        float startZoom = rememberWorldMapZoom ? savedWorldMapZoom : defaultWorldMapZoom;
        SetZoomInstant(startZoom);

        // 打开大地图时，确保位置合法
        ClampCurrentContainerPosition();

        if (PlayerControlManager.Instance != null)
            PlayerControlManager.Instance.Lock(ControlLockType.Map);

        ResetPointerDragState();
        OnMapModeChanged?.Invoke(true);
    }

    void SetZoomInstant(float zoom)
    {
        currentZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
        targetZoom = currentZoom;
        targetZoom01 = Mathf.InverseLerp(minZoom, maxZoom, currentZoom);
        keepZoomFocus = false;

        if (currentContainer != null)
            currentContainer.localScale = Vector3.one * currentZoom;
    }

    // =========================
    // 坐标转换（统一）
    // =========================
    public Vector2 WorldToMap(Vector3 worldPos)
    {
        float x = Mathf.InverseLerp(mapData.worldMin.x, mapData.worldMax.x, worldPos.x);
        float y = Mathf.InverseLerp(mapData.worldMin.y, mapData.worldMax.y, worldPos.z);

        return new Vector2(
            (x - 0.5f) * mapWidth,
            (y - 0.5f) * mapHeight
        );
    }

    public Vector3 MapToWorld(Vector2 mapPos)
    {
        float x = (mapPos.x / mapWidth) + 0.5f;
        float y = (mapPos.y / mapHeight) + 0.5f;

        float worldX = Mathf.Lerp(mapData.worldMin.x, mapData.worldMax.x, x);
        float worldZ = Mathf.Lerp(mapData.worldMin.y, mapData.worldMax.y, y);

        return new Vector3(worldX, 0f, worldZ);
    }

    // =========================
    // 小地图跟随
    // =========================
    void UpdateMiniMapFollow()
    {
        if (player == null || currentContainer == null)
            return;

        Vector2 playerMapPos = WorldToMap(player.position);
        Vector2 target = -playerMapPos * currentZoom;

        currentContainer.anchoredPosition = Vector2.Lerp(
            currentContainer.anchoredPosition,
            target,
            Time.deltaTime * followSmooth
        );
    }

    void UpdatePlayerTransformOnMap()
    {
        if (player == null)
            return;

        Vector2 mapPos = WorldToMap(player.position);

        // ===== 小地图（永远居中）=====
        if (playerIcon != null && playerIcon.gameObject.activeSelf)
        {
            playerIcon.anchoredPosition = Vector2.zero;

            float rot = -player.eulerAngles.y;
            playerIcon.rotation = Quaternion.Euler(0f, 0f, rot);
        }

        // ===== 大地图（真实位置）=====
        if (worldPlayerIcon != null && worldPlayerIcon.gameObject.activeSelf)
        {
            worldPlayerIcon.anchoredPosition = mapPos;

            float rot = -player.eulerAngles.y;
            worldPlayerIcon.rotation = Quaternion.Euler(0f, 0f, rot);
        }
    }

    // =========================
    // 大地图拖动
    // =========================
    void UpdateWorldMapDrag()
    {
        if (currentContainer == null)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            pointerDownOnMap = true;
            suppressClickThisPress = false;
            pointerDownScreenPos = Input.mousePosition;

            isDragging = false;
            keepZoomFocus = false;

            dragUseLocalSpace = TryGetPointerLocalInViewport(out dragOrigin);
            if (!dragUseLocalSpace)
                dragOrigin = Input.mousePosition;
        }

        if (Input.GetMouseButton(0) && pointerDownOnMap)
        {
            // 先判断是否超过拖动阈值
            if (!isDragging)
            {
                float dist = Vector2.Distance(pointerDownScreenPos, (Vector2)Input.mousePosition);
                if (dist >= dragThreshold)
                {
                    isDragging = true;
                    suppressClickThisPress = true;
                }
            }

            if (isDragging)
            {
                Vector2 pointerPos;

                if (dragUseLocalSpace)
                {
                    if (!TryGetPointerLocalInViewport(out pointerPos))
                        pointerPos = Input.mousePosition;
                }
                else
                {
                    pointerPos = Input.mousePosition;
                }

                Vector2 delta = pointerPos - dragOrigin;

                // 注意：这里不乘 Time.deltaTime，鼠标位移本身就是帧间增量
                currentContainer.anchoredPosition += delta * dragSpeed;
                ClampCurrentContainerPosition();

                dragOrigin = pointerPos;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            pointerDownOnMap = false;
            isDragging = false;
        }
    }

    // =========================
    // 大地图缩放（线性输入 + 平滑显示）
    // =========================
    void UpdateWorldMapZoom()
    {
        if (currentContainer == null)
            return;

        float scroll = Input.mouseScrollDelta.y;

        // 1. 滚轮输入：在线性区间 0~1 内推进
        if (Mathf.Abs(scroll) > 0.001f)
        {
            CacheZoomFocusPoint();

            targetZoom01 = Mathf.Clamp01(targetZoom01 + scroll * wheelStep);
            targetZoom = Mathf.Lerp(minZoom, maxZoom, targetZoom01);

            if (rememberWorldMapZoom)
                savedWorldMapZoom = targetZoom;
        }

        // 2. 平滑追踪目标缩放
        if (Mathf.Abs(currentZoom - targetZoom) > 0.0001f)
        {
            float t = 1f - Mathf.Exp(-zoomSmoothSpeed * Time.unscaledDeltaTime);
            currentZoom = Mathf.Lerp(currentZoom, targetZoom, t);

            if (Mathf.Abs(currentZoom - targetZoom) < 0.0001f)
                currentZoom = targetZoom;

            currentContainer.localScale = Vector3.one * currentZoom;

            // 3. 保持鼠标下的地图点稳定
            if (keepZoomFocus)
            {
                currentContainer.anchoredPosition =
                    zoomFocusViewportLocal - zoomFocusMapLocal * currentZoom;
            }

            ClampCurrentContainerPosition();
        }
        else
        {
            keepZoomFocus = false;
        }
    }

    void CacheZoomFocusPoint()
    {
        if (currentContainer == null)
            return;

        Vector2 focusLocal = Vector2.zero;

        if (zoomToMousePosition)
        {
            if (!TryGetPointerLocalInViewport(out focusLocal))
                focusLocal = Vector2.zero;
        }

        zoomFocusViewportLocal = focusLocal;
        zoomFocusMapLocal = (zoomFocusViewportLocal - currentContainer.anchoredPosition) / Mathf.Max(currentZoom, 0.0001f);
        keepZoomFocus = true;
    }

    // =========================
    // 视口 / UI 工具
    // =========================
    RectTransform GetCurrentViewport()
    {
        if (currentContainer == null)
            return null;

        return currentContainer.parent as RectTransform;
    }

    Camera GetUICamera()
    {
        if (currentContainer == null)
            return null;

        Canvas canvas = currentContainer.GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    bool TryGetPointerLocalInViewport(out Vector2 localPoint)
    {
        RectTransform viewport = GetCurrentViewport();
        if (viewport == null)
        {
            localPoint = Vector2.zero;
            return false;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewport,
            Input.mousePosition,
            GetUICamera(),
            out localPoint
        );
    }

    /// <summary>
    /// 尝试将屏幕坐标转换为当前地图容器的本地坐标，返回是否成功（失败可能是因为没有有效的视口或容器）
    /// </summary>
    public bool TryScreenPointToMapLocal(Vector2 screenPoint, Camera eventCamera, out Vector2 mapLocal)
    {
        mapLocal = Vector2.zero;

        if (currentContainer == null)
            return false;

        RectTransform viewport = GetCurrentViewport();
        if (viewport == null)
            return false;

        Vector2 viewportLocalPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                viewport,
                screenPoint,
                eventCamera != null ? eventCamera : GetUICamera(),
                out viewportLocalPoint))
        {
            return false;
        }

        float scale = currentContainer.localScale.x;
        if (Mathf.Abs(scale) < 0.0001f)
            return false;

        mapLocal = (viewportLocalPoint - currentContainer.anchoredPosition) / scale;
        return true;
    }

    // =========================
    // 边界约束（防止拖出遮罩外露底）
    // =========================
    void ClampCurrentContainerPosition()
    {
        if (currentContainer == null)
            return;

        RectTransform viewport = GetCurrentViewport();
        if (viewport == null)
            return;

        float viewportWidth = viewport.rect.width;
        float viewportHeight = viewport.rect.height;

        float scaledMapWidth = mapWidth * currentZoom;
        float scaledMapHeight = mapHeight * currentZoom;

        Vector2 pos = currentContainer.anchoredPosition;

        float maxOffsetX = Mathf.Max(0f, (scaledMapWidth - viewportWidth) * 0.5f);
        float maxOffsetY = Mathf.Max(0f, (scaledMapHeight - viewportHeight) * 0.5f);

        pos.x = Mathf.Clamp(pos.x, -maxOffsetX, maxOffsetX);
        pos.y = Mathf.Clamp(pos.y, -maxOffsetY, maxOffsetY);

        // 如果地图比视口还小，则自动居中
        if (scaledMapWidth <= viewportWidth)
            pos.x = 0f;

        if (scaledMapHeight <= viewportHeight)
            pos.y = 0f;

        currentContainer.anchoredPosition = pos;
    }

    // =========================
    // 提供给NPC用
    // =========================
    public RectTransform GetCurrentMapRect()
    {
        return isWorldMapMode ? fullMapRect : miniMapRect;
    }

    public RectTransform GetCurrentContainer()
    {
        return currentContainer;
    }

    public Transform GetCurrentIconsRoot()
    {
        return isWorldMapMode ? fullMapIconsRoot : miniMapIconsRoot;
    }

    public bool IsMapLocalVisible(Vector2 mapLocal)
    {
        if (currentContainer == null)
            return false;

        RectTransform viewport = GetCurrentViewport();
        if (viewport == null)
            return false;

        Vector2 viewportPos = currentContainer.anchoredPosition + mapLocal * currentZoom;

        float halfWidth = viewport.rect.width * 0.5f;
        float halfHeight = viewport.rect.height * 0.5f;

        return Mathf.Abs(viewportPos.x) <= halfWidth &&
               Mathf.Abs(viewportPos.y) <= halfHeight;
    }

    // =========================
    // 新增：给点击系统查询本次按压是否应屏蔽点击
    // =========================
    public bool ShouldSuppressClickThisPress()
    {
        return suppressClickThisPress;
    }

    public void ConsumeSuppressClickFlag()
    {
        suppressClickThisPress = false;
    }

    void ResetPointerDragState()
    {
        pointerDownOnMap = false;
        suppressClickThisPress = false;
        isDragging = false;
    }
}