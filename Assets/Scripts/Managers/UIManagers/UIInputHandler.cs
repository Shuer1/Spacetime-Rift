using UnityEngine;
using StarterAssets;

public class UIInputHandler : MonoBehaviour
{
    public StarterAssetsInputs input;

    public Inventory inventoryUI;
    public TaskInventory questUI;
    public MenuUI menuUI;
    public MiniMapSystem miniMap;

    void Update()
    {
        HandleBackpack();
        HandleTaskList();
        HandleMenuUI();
        HandleMap();
    }

    private void HandleBackpack()
    {
        if (input.openBackpack)
        {
            input.openBackpack = false;
            ToggleUI(inventoryUI);
        }
    }

    private void HandleTaskList()
    {
        if (input.openTaskList)
        {
            input.openTaskList = false;
            ToggleUI(questUI);
        }
    }
    
    private void HandleMenuUI()
    {
        if (input.openMenuUI)
        {
            input.openMenuUI = false;
            ToggleUI(menuUI);
        }
    }
    
    private void HandleMap()
    {
        if (input.openMap)
        {
            input.openMap = false;

            miniMap.ToggleWorldMap(); // 地图UI的打开/关闭逻辑在MiniMapSystem内部处理，这里只负责触发事件 -- 特殊处理
            input.ClearInputs();
        }
    }

    // ✅ 通用UI切换逻辑（核心复用点）
    private void ToggleUI(IBaseUI ui)
    {
        if (ui.IsOpen())
            ui.Close();
        else
            ui.Open();
    }
}