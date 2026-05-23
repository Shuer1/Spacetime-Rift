using UnityEngine;
using System.IO;
using UnityEditor;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance;

    private string savePath;
    private PlayerData data;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        savePath = Application.persistentDataPath + "/player_data.json";

        Load();
    }

    #region === 对外接口 ===

    /// <summary>
    /// 初始化玩家（位置 + 属性 + 攻击）
    /// </summary>
    public void ApplyToPlayer(PlayerController player, PlayerCombatController combat)
    {
        if (data == null) return;

        // === 位置 ===
        player.transform.position = new Vector3(
            data.position[0],
            data.position[1],
            data.position[2]);

        player.transform.rotation = Quaternion.Euler(
            data.rotation[0],
            data.rotation[1],
            data.rotation[2]);

        // === 属性 ===
        player.maxHealth = data.maxHP;
        player.currentHealth = data.maxHP;
        player.defense = data.defense;

        player.TriggerHealthChanged(player.currentHealth, player.maxHealth);

        // === 攻击 ===
        ApplyAttackToCombat(combat, data.attack);
    }

    /// <summary>
    /// 保存玩家数据
    /// </summary>
    public void Save(PlayerController player, PlayerCombatController combat)
    {
        data = new PlayerData
        {
            position = new float[]
            {
                player.transform.position.x,
                player.transform.position.y,
                player.transform.position.z
            },

            rotation = new float[]
            {
                player.transform.eulerAngles.x,
                player.transform.eulerAngles.y,
                player.transform.eulerAngles.z
            },

            maxHP = player.maxHealth,
            defense = player.defense,
            attack = GetAttackFromCombat(combat),

        };

        WriteToDisk();
    }

    #endregion

    #region === 攻击处理（重点） ===

    // 从当前连招读取攻击值（默认取第一个作为基准）
    int GetAttackFromCombat(PlayerCombatController combat)
    {
        if (combat.comboList == null || combat.comboList.Count == 0)
            return 1;

        return combat.comboList[0].attack;
    }

    // 把攻击写回所有连招
    void ApplyAttackToCombat(PlayerCombatController combat, int attack)
    {
        if (combat.comboList == null) return;

        foreach (var cfg in combat.comboList)
        {
            cfg.attack = attack;
        }
    }

    #endregion

    #region === IO ===

    void WriteToDisk()
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
    }

    void Load()
    {
        if (!File.Exists(savePath))
        {
            data = CreateDefault();
            return;
        }

        string json = File.ReadAllText(savePath);
        data = JsonUtility.FromJson<PlayerData>(json);
    }

    PlayerData CreateDefault()
    {
        return new PlayerData
        {
            position = new float[] { 0, 1, 0 },
            rotation = new float[] { 0, 0, 0 },

            maxHP = 100,
            defense = 0,
            attack = 5,

        };
    }

    #endregion

    public void AddAttack(int value, PlayerCombatController combat) // 增加攻击
    {
        if (data == null)
            data = CreateDefault();

        // === 数据层修改 ===
        data.attack += value;

        // === 应用到战斗系统 ===
        ApplyAttackToCombat(combat, data.attack);

        // === 可选：立即存档 ===
        WriteToDisk();

        Debug.Log($"[PlayerData] 攻击力提升: +{value}，当前攻击: {data.attack}");
    }

    #if UNITY_EDITOR
    [MenuItem("Tools/ClearAssetsData/PlayerData")]
    private static void ClearData()
    {
        string path = Application.persistentDataPath + "/player_data.json";

        // === 删除文件 ===
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"[PlayerData] 本地存档已删除: {path}");
        }
        else
        {
            Debug.LogWarning($"[PlayerData] 未找到存档文件: {path}");
        }

        // === 清空运行时缓存（如果在运行）===
        if (Application.isPlaying && Instance != null)
        {
            Instance.data = Instance.CreateDefault();
            Debug.Log("[PlayerData] 运行时数据已重置为默认值");
        }

        // === 刷新Asset数据库（编辑器安全操作）===
        AssetDatabase.Refresh();
    }
    #endif

    public void ResetData() // API
    {
        data = CreateDefault();
        WriteToDisk();

        Debug.Log("[PlayerData] 已重置");
    }
}