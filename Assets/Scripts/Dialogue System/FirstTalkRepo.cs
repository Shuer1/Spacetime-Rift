using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public static class FirstTalkRepo
{
    private static readonly string JsonPath =
        Path.Combine(Application.persistentDataPath, "FirstTalk.json");

    private static Dictionary<string, bool> s_map;

    /* ---------- 生命周期 ---------- */
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Load()
    {
        s_map = File.Exists(JsonPath)
            ? JsonConvert.DeserializeObject<Dictionary<string, bool>>(File.ReadAllText(JsonPath))
            : new Dictionary<string, bool>();
    }

    /* ---------- 对外接口 ---------- */
    public static bool IsFirst(string npcId) =>
        !string.IsNullOrEmpty(npcId) && !s_map.GetValueOrDefault(npcId, false);

    public static void Finish(string npcId)
    {
        if (string.IsNullOrEmpty(npcId)) return;
        if (s_map.TryGetValue(npcId, out var v) && v) return;

        s_map[npcId] = true;
        Save();
    }

    /* ---------- 本地持久化 ---------- */
    private static void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(JsonPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(JsonPath, JsonConvert.SerializeObject(s_map, Formatting.Indented));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FirstTalkRepo] 写盘失败: {e.Message}");
        }
    }

    /* ---------- 调试 ---------- */
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/ClearAssetsData/FirstTalk_Status")]
    private static void ClearAll()
    {
        s_map?.Clear();
        if (File.Exists(JsonPath)) File.Delete(JsonPath);
        Debug.Log("[FirstTalkRepo] 已清空");
    }
#endif

    public static void ResetData() // API
    {
        s_map?.Clear();

        if (File.Exists(JsonPath))
            File.Delete(JsonPath);

        Debug.Log("[FirstTalkRepo] 已重置");
    }
}