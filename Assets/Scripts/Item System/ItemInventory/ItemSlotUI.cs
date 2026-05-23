using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 单个物品格子UI
public class ItemSlotUI : MonoBehaviour
{
    [SerializeField] private Image itemIcon;       // 物品图标
    [SerializeField] private TextMeshProUGUI countText; // 堆叠数量文本
    [SerializeField] private Image background;     // 格子背景
    [SerializeField] private Button slotButton;    // 交互按钮

    private ItemStack currentItemStack; // 绑定的物品堆叠数据
    private Inventory parentInventory; // 父背包UI引用
    private Color normalBgColor = Color.gray; // 正常背景色
    private Color selectedBgColor = Color.blue; // 选中背景色

    // 初始化格子
    public void Init(Inventory parentUI)
    {
        parentInventory = parentUI;
        ResetSlot(); // 重置格子为空白状态
        // 绑定按钮点击事件（触发物品交互面板）
        slotButton.onClick.AddListener(OnSlotClicked);
    }

    // 渲染物品格子（绑定物品数据）
    public void Render(ItemStack itemStack)
    {
        // 1. 存储当前物品数据
        currentItemStack = itemStack;

        // 2. 校验物品有效性，无效则重置格子
        if (!itemStack.IsValid)
        {
            ResetSlot();
            return;
        }

        // 3. 渲染图标
        itemIcon.gameObject.SetActive(true);
        itemIcon.sprite = itemStack.itemAsset.icon;
        itemIcon.preserveAspect = true; // 保持图标宽高比

        // 4. 渲染堆叠数量（仅数量>1时显示）
        countText.gameObject.SetActive(itemStack.count > 1);
        if (itemStack.count > 1)
        {
            countText.text = itemStack.count.ToString();
        }

        // 5. 重置背景色
        background.color = normalBgColor;
    }

    // 重置格子为空白状态
    public void ResetSlot()
    {
        currentItemStack = null;
        itemIcon.gameObject.SetActive(false);
        countText.gameObject.SetActive(false);
        background.color = normalBgColor;
    }

    // 格子被点击（触发物品交互：使用/丢弃）
    private void OnSlotClicked()
    {
        if (currentItemStack == null || !currentItemStack.IsValid) return;
        
        // 通知父背包UI，显示物品交互面板
        parentInventory.ShowItemActionPanel(currentItemStack, this);
    }

    // 选中/取消选中格子（高亮显示）
    public void SetSelected(bool isSelected)
    {
        background.color = isSelected ? selectedBgColor : normalBgColor;
    }

    // 获取当前绑定的物品
    public ItemStack GetCurrentItemStack()
    {
        return currentItemStack;
    }
}