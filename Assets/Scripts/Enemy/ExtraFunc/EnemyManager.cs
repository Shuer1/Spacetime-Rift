using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    private HashSet<string> deadEnemies = new HashSet<string>();

    private string savePath;

    [System.Serializable]
    class SaveWrapper
    {
        public List<string> deadList;
    }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        savePath = Application.persistentDataPath + "/enemy_save.json";

        Load();
    }

    public bool IsEnemyDead(string id)
    {
        return deadEnemies.Contains(id);
    }

    public void MarkEnemyDead(string id)
    {
        if (!deadEnemies.Contains(id))
        {
            deadEnemies.Add(id);
            Save();
        }
    }

    void Save()
    {
        SaveWrapper data = new SaveWrapper
        {
            deadList = new List<string>(deadEnemies)
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
    }

    void Load()
    {
        if (!File.Exists(savePath)) return;

        string json = File.ReadAllText(savePath);
        SaveWrapper data = JsonUtility.FromJson<SaveWrapper>(json);

        deadEnemies = new HashSet<string>(data.deadList);
    }
    
    void OnApplicationQuit()
    {
        Save();
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/ClearAssetsData/Enemy_Status")]
    private static void ClearData()
    {
        Instance.deadEnemies.Clear();
        Instance.Save();
    }
#endif

    public void ResetData() // API
    {
        deadEnemies.Clear();

        string path = Application.persistentDataPath + "/enemy_save.json";
        if (File.Exists(path))
            File.Delete(path);

        Debug.Log("[EnemyManager] 已重置");
    }
}