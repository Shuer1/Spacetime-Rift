using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class RuntimeDataCleaner : MonoBehaviour
{
    public void ClearAllData()
    {
        Debug.Log("========== 运行时清除所有数据 ==========");

        ClearPlayerData();
        ClearEnemyData();
        ClearFirstTalkData();
        ClearPlayerPrefs();

        Debug.Log("========== 清除完成 ==========");
    }

    /* ============================= */
    /* PlayerData */
    /* ============================= */
    void ClearPlayerData()
    {
        string path = Path.Combine(Application.persistentDataPath, "player_data.json");

        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("[Clear] PlayerData 文件删除");
        }

        if (PlayerDataManager.Instance != null)
        {
            // ⚠️ 推荐你给 PlayerDataManager 加一个 Reset 方法（见下文）
            PlayerDataManager.Instance.ResetData();
        }
    }

    /* ============================= */
    /* Enemy */
    /* ============================= */
    void ClearEnemyData()
    {
        string path = Path.Combine(Application.persistentDataPath, "enemy_save.json");

        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("[Clear] EnemyData 文件删除");
        }

        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.ResetData();
        }
    }

    /* ============================= */
    /* FirstTalk */
    /* ============================= */
    void ClearFirstTalkData()
    {
        string path = Path.Combine(Application.persistentDataPath, "FirstTalk.json");

        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("[Clear] FirstTalk 文件删除");
        }

        FirstTalkRepo.ResetData();
    }

    /* ============================= */
    /* PlayerPrefs */
    /* ============================= */
    void ClearPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Debug.Log("[Clear] PlayerPrefs 已清空");
    }
}