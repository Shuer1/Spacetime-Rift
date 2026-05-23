using UnityEngine;
using UnityEngine.UI;

public class MapIcon : MonoBehaviour
{
    public Transform target;
    public RectTransform rect;
    public Image icon;
    public CanvasGroup canvasGroup;   // ⭐新增

    public MapIconType iconType;
    public bool onMiniMap = true;
    public bool onWorldMap = true;

    private void Awake()
    {
        if (rect == null)
            rect = GetComponent<RectTransform>();

        if (icon == null)
            icon = GetComponentInChildren<Image>();

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void OnEnable()
    {
        MapIconManager.Instance?.Register(this);
    }

    private void OnDisable()
    {
        MapIconManager.Instance?.Unregister(this);
    }

    // ⭐替代 SetActive
    public void SetVisible(bool visible)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
    }

    public RectTransform GetRect() => rect;

    public void SetHighlight(bool value)
    {
        // 自定义
    }

    public void RefreshParent(Transform newParent)
    {
        if (transform.parent != newParent)
            transform.SetParent(newParent, false);
    }
}

public enum MapIconType
{
    NPC,
    Enemy,
    Quest
}