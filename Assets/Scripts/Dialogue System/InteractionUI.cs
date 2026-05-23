using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 交互提示UI - 显示"按[E]对话"等提示
/// 通过事件驱动，零Update依赖
/// </summary>
public class InteractionUI : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Canvas promptCanvas;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private Image promptIcon;

    [Header("显示配置")]
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float promptOffsetY = 2f;     // 提示框在NPC上方的偏移

    private NPCController currentNPC;
    private CanvasGroup canvasGroup;

    #region 生命周期
    private void Awake()
    {
        canvasGroup = promptCanvas.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = promptCanvas.gameObject.AddComponent<CanvasGroup>();

        // 初始隐藏
        HidePrompt();
    }

    private void OnDestroy()
    {
        UnregisterEvents();
    }
    #endregion

    #region 事件注册
    private void Start()
    {
        RegisterEvents();
    }

    private void RegisterEvents()
    {
        // 监听所有NPC的范围进入/离开事件
        var npcs = FindObjectsByType<NPCController>(FindObjectsSortMode.None);
        foreach (var npc in npcs)
        {
            npc.onPlayerEnter += OnPlayerEnterNPC;
            npc.onPlayerExit += OnPlayerExitNPC;
        }

        // 监听事件总线（支持动态创建的NPC）
        GameEventBus.Subscribe<NPCEnterRangeEvent>(OnNPCEnterRangeEvent);
        GameEventBus.Subscribe<NPCExitRangeEvent>(OnNPCExitRangeEvent);
    }

    private void UnregisterEvents()
    {
        var npcs = FindObjectsByType<NPCController>(FindObjectsSortMode.None);
        foreach (var npc in npcs)
        {
            npc.onPlayerEnter -= OnPlayerEnterNPC;
            npc.onPlayerExit -= OnPlayerExitNPC;
        }

        GameEventBus.Unsubscribe<NPCEnterRangeEvent>(OnNPCEnterRangeEvent);
        GameEventBus.Unsubscribe<NPCExitRangeEvent>(OnNPCExitRangeEvent);
    }
    #endregion

    #region 事件处理
    private void OnPlayerEnterNPC(NPCAsset npcAsset)
    {
        ShowPrompt(npcAsset);
    }

    private void OnPlayerExitNPC(NPCAsset npcAsset)
    {
        if (currentNPC?.GetNPCAsset() == npcAsset)
        {
            HidePrompt();
        }
    }

    private void OnNPCEnterRangeEvent(GameEvent e)
    {
        if (e is NPCEnterRangeEvent enterEvent)
        {
            // 找到对应的NPCController
            var npcs = FindObjectsByType<NPCController>(FindObjectsSortMode.None);
            foreach (var npc in npcs)
            {
                if (npc.GetNPCAsset()?.npcId == enterEvent.npcId)
                {
                    currentNPC = npc;
                    ShowPrompt(enterEvent.npcAsset);
                    break;
                }
            }
        }
    }

    private void OnNPCExitRangeEvent(GameEvent e)
    {
        if (e is NPCExitRangeEvent exitEvent)
        {
            HidePrompt();
            currentNPC = null;
        }
    }
    #endregion

    #region UI显示控制
    /// <summary>
    /// 显示提示
    /// </summary>
    private void ShowPrompt(NPCAsset npcAsset)
    {
        if (npcAsset == null)
            return;

        currentNPC = FindNPCByAsset(npcAsset);

        // 设置提示文本
        if (promptText != null)
            promptText.text = npcAsset.GetPromptText();

        // 设置提示图标（NPC头像）
        if (promptIcon != null && npcAsset.icon != null)
            promptIcon.sprite = npcAsset.icon;

        // 显示并淡入
        promptCanvas.enabled = true;
        StartCoroutine(FadeIn());
    }

    /// <summary>
    /// 隐藏提示
    /// </summary>
    private void HidePrompt()
    {
        StartCoroutine(FadeOut());
    }

    private System.Collections.IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    private System.Collections.IEnumerator FadeOut()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        promptCanvas.enabled = false;
    }
    #endregion

    #region 辅助方法
    private NPCController FindNPCByAsset(NPCAsset asset)
    {
        var npcs = FindObjectsByType<NPCController>(FindObjectsSortMode.None);
        foreach (var npc in npcs)
        {
            if (npc.GetNPCAsset() == asset)
                return npc;
        }
        return null;
    }
    #endregion

    #region 位置跟随（可选）
    private void Update()
    {
        // 注意：这里仅用于UI位置跟随，不影响事件驱动架构
        // 如果完全避免Update，可以使用LateUpdate或协程
        if (currentNPC != null && promptCanvas.enabled)
        {
            Vector3 worldPos = currentNPC.transform.position + Vector3.up * promptOffsetY;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            
            // 转换为世界坐标（Canvas设为Overlay时不需要转换）
            RectTransform rectTransform = promptCanvas.GetComponent<RectTransform>();
            if (rectTransform != null && promptCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                rectTransform.position = screenPos;
            }
        }
    }
    #endregion
}
