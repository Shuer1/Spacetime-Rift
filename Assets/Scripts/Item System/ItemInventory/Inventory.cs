using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Fungus;
using StarterAssets;

public class Inventory : MonoBehaviour, IBaseUI
{
    // 单例模式
    public static Inventory Instance { get; private set; }
    [Header("背包")]
    [SerializeField] private GameObject backpackPanel; // 背包主面板
    [SerializeField] private GameObject itemSlotPrefab; // 物品格子预制体
    [SerializeField] private Transform gridLayoutTransform; // 网格布局父节点
    //[SerializeField] private Button closeButton; // 关闭按钮 - 现在由UIManager统一管理
    [SerializeField] private Button organizeButton; // 整理按钮

    [Header("物品交互面板（使用/丢弃）")]
    [SerializeField] private GameObject itemActionPanel; // 交互面板
    [SerializeField] private TextMeshProUGUI actionItemName; // 交互面板物品名称
    [SerializeField] private TextMeshProUGUI actionItemDesc; // 交互面板物品描述
    [SerializeField] private Button useButton; // 使用按钮
    [SerializeField] private Button dropButton; // 丢弃按钮
    [SerializeField] private Button cancelButton; // 取消按钮
    [SerializeField] private ItemCountSelector countSelector; // 数量选择器

    private List<ItemSlotUI> allItemSlotUIs = new List<ItemSlotUI>(); // 所有物品格子UI列表
    private ItemSlotUI currentSelectedSlot; // 当前选中的物品格子

    void Awake()
    {
        // 单例模式初始化
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 绑定功能按钮事件
        BindButtonEvents();
    }

    void Start()
    {
        // 初始化物品格子（根据背包容量创建）
        InitItemSlots();

        // 监听背包变更事件（数据变更时自动刷新UI）
        InventoryManager.Instance.onInventoryChanged += RefreshBackpackUI;
        // 初始隐藏面板
        backpackPanel.SetActive(false);
        itemActionPanel.SetActive(false);
    }

    // 绑定所有按钮事件
    private void BindButtonEvents()
    {
        organizeButton.onClick.AddListener(OnOrganizeButtonClicked);
        useButton.onClick.AddListener(OnUseButtonClicked);
        dropButton.onClick.AddListener(OnDropButtonClicked);
        cancelButton.onClick.AddListener(HideItemActionPanel);
    }

    // 初始化物品格子（根据背包容量创建对应数量的格子）
    private void InitItemSlots()
    {
        // 清空原有格子（防止重复创建）
        foreach (var slotUI in allItemSlotUIs)
        {
            Destroy(slotUI.gameObject);
        }
        allItemSlotUIs.Clear();

        // 根据背包容量创建格子
        int backpackCapacity = InventoryManager.Instance.backpackCapacity;
        for (int i = 0; i < backpackCapacity; i++)
        {
            GameObject slotObj = Instantiate(itemSlotPrefab, gridLayoutTransform);
            ItemSlotUI slotUI = slotObj.GetComponent<ItemSlotUI>();
            if (slotUI != null)
            {
                slotUI.Init(this);
                allItemSlotUIs.Add(slotUI);
            }
            else
            {
                Destroy(slotObj);
            }
        }
    }

    // 刷新背包UI（核心方法：同步背包数据到UI）
    public void RefreshBackpackUI()
    {
        // 1. 获取背包当前所有物品
        List<ItemStack> backpackItems = InventoryManager.Instance.GetAllBackpackItems();

        // 2. 先重置所有格子为空白状态
        foreach (var slotUI in allItemSlotUIs)
        {
            slotUI.ResetSlot();
        }

        // 3. 渲染有物品的格子
        for (int i = 0; i < backpackItems.Count && i < allItemSlotUIs.Count; i++)
        {
            allItemSlotUIs[i].Render(backpackItems[i]);
        }

        // 4. 隐藏物品交互面板（刷新后取消选中）
        HideItemActionPanel();
    }

    // 打开背包面板
    public void Open()
    {
        backpackPanel.SetActive(true);
        RefreshBackpackUI();
        PlayerControlManager.Instance.Lock(ControlLockType.Backpack);
    }

    // 关闭背包面板
    public void Close()
    {
        backpackPanel.SetActive(false);
        HideItemActionPanel();
        PlayerControlManager.Instance.Unlock(ControlLockType.Backpack);
    }

    public bool IsOpen() => backpackPanel.activeSelf;

    // 显示物品交互面板（使用/丢弃）
    public void ShowItemActionPanel(ItemStack itemStack, ItemSlotUI selectedSlot)
    {
        // 1. 存储当前选中的格子
        currentSelectedSlot = selectedSlot;
        if (currentSelectedSlot != null) currentSelectedSlot.SetSelected(true);

        // 2. 渲染交互面板数据
        actionItemName.text = itemStack.itemAsset.itemName;
        actionItemDesc.text = itemStack.itemAsset.description;
        itemActionPanel.SetActive(true);

        countSelector.SetRange(1, itemStack.count);
        countSelector.ResetToMin();

        // 3. 控制使用/丢弃按钮的可用状态
        ItemAsset itemAsset = itemStack.itemAsset;
        useButton.interactable = itemAsset.itemType == ItemType.Consumable || itemAsset.itemType == ItemType.Quest;
        dropButton.interactable = itemAsset.itemType != ItemType.Quest && itemAsset.itemType != ItemType.Currency;
    }

    // 隐藏物品交互面板
    public void HideItemActionPanel()
    {
        // 取消选中状态
        if (currentSelectedSlot != null)
        {
            currentSelectedSlot.SetSelected(false);
            currentSelectedSlot = null;
        }
        itemActionPanel.SetActive(false);
    }

    // 整理背包按钮点击事件
    private void OnOrganizeButtonClicked()
    {
        InventoryManager.Instance.OrganizeBackpack(); // 调用背包管理器的整理方法
    }

    // 使用物品按钮点击事件
    private void OnUseButtonClicked()
    {
        if (currentSelectedSlot == null) return;
        ItemStack itemStack = currentSelectedSlot.GetCurrentItemStack();
        if (itemStack == null || !itemStack.IsValid) return;

        int toUseCount = countSelector.Current;
        InventoryManager.Instance.UseItem(itemStack.itemAsset.itemId, toUseCount);
        HideItemActionPanel();
    }

    // 丢弃物品按钮点击事件
    private void OnDropButtonClicked()
    {
        if (currentSelectedSlot == null) return;
        ItemStack itemStack = currentSelectedSlot.GetCurrentItemStack();
        if (itemStack == null || !itemStack.IsValid) return;

        int toDrop = countSelector.Current;          // 按选择器数量丢弃
        InventoryManager.Instance.DropItem(itemStack.itemAsset.itemId, toDrop);
        HideItemActionPanel();
    }

    // 防止内存泄漏，取消事件监听
    void OnDestroy()
    {
        InventoryManager.Instance.onInventoryChanged -= RefreshBackpackUI;
    }
}