using System.Collections.Generic;
using UnityEngine;

public class MapIconPool : MonoBehaviour
{
    public static MapIconPool Instance;

    [System.Serializable]
    public class PoolItem
    {
        public MapIconType type;
        public GameObject prefab;
        public int initialCount = 10;
    }

    public List<PoolItem> poolItems;

    private Dictionary<MapIconType, Queue<MapIcon>> poolDict = new();

    void Awake()
    {
        Instance = this;

        foreach (var item in poolItems)
        {
            Queue<MapIcon> queue = new Queue<MapIcon>();

            for (int i = 0; i < item.initialCount; i++)
            {
                var obj = Instantiate(item.prefab, transform);
                obj.SetActive(false);

                var icon = obj.GetComponent<MapIcon>();
                queue.Enqueue(icon);
            }

            poolDict[item.type] = queue;
        }
    }

    public MapIcon Get(MapIconType type, Transform parent)
    {
        if (!poolDict.ContainsKey(type))
        {
            Debug.LogError($"No pool for type: {type}");
            return null;
        }

        MapIcon icon;

        if (poolDict[type].Count > 0)
        {
            icon = poolDict[type].Dequeue();
        }
        else
        {
            var prefab = poolItems.Find(p => p.type == type).prefab;
            icon = Instantiate(prefab).GetComponent<MapIcon>();
        }

        icon.transform.SetParent(parent, false);
        icon.gameObject.SetActive(true);

        return icon;
    }

    public void Release(MapIcon icon)
    {
        if (icon == null) return;

        icon.gameObject.SetActive(false);
        icon.transform.SetParent(transform);

        if (!poolDict.ContainsKey(icon.iconType))
            poolDict[icon.iconType] = new Queue<MapIcon>();

        poolDict[icon.iconType].Enqueue(icon);
    }
}