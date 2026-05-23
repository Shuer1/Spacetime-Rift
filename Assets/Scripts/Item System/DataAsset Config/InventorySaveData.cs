using System.Collections.Generic;

[System.Serializable]
public class InventorySaveData
{
    public List<ItemStackData> stacks;
}

[System.Serializable]
public class ItemStackData
{
    public string itemId;
    public int count;
}