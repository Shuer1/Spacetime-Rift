using TMPro;
using UnityEngine;

/// <summary>
/// 物品数量选择器
/// </summary>
public class ItemCountSelector : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI countText;
    private int min = 1;
    private int max = 99;   // 运行时会被 SetRange 覆盖

    private int current;

    public int Current => current;

    void OnEnable() => ResetToMin();

    /// <summary>
    /// 外部调用：动态设置上下限
    /// </summary>
    public void SetRange(int minVal, int maxVal)
    {
        min = minVal;
        max = maxVal;
    }

    public void ResetToMin()
    {
        current = min;
        RefreshUI();
    }

    public void OnAdd() // 按钮事件调用：add
    {
        current = Mathf.Min(current + 1, max);
        RefreshUI();
    }

    public void OnSub() // 按钮事件调用：sub
    {
        current = Mathf.Max(current - 1, min);
        RefreshUI();
    }

    void RefreshUI() => countText.SetText(current.ToString());
}