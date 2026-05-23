using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 数据管理器，统一持久化管理游戏的所有数据
/// 使用PlayerPrefs存储基础数值，JsonUtility存储复杂数据结构
/// </summary>
public class DataManager : MonoBehaviour
{
    #region 单例模式
    private static DataManager instance;
    public static DataManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<DataManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("DataManager");
                    instance = obj.AddComponent<DataManager>();
                }
            }
            return instance;
        }
    }
    #endregion

    #region 数据结构定义

    /// <summary>
    /// 玩家属性数据
    /// </summary>
    [Serializable]
    public class PlayerStats
    {
        public float attackPower = 10f;
        public float defense = 5f;
        public float moveSpeed = 5f;
        public float maxHealth = 100f;
    }

    /// <summary>
    /// 玩家技能数据
    /// </summary>
    [Serializable]
    public class PlayerSkill
    {
        public string skillName;
        public int skillLevel;
        public bool isUnlocked;

        public PlayerSkill(string name, int level, bool unlocked)
        {
            skillName = name;
            skillLevel = level;
            isUnlocked = unlocked;
        }
    }

    /// <summary>
    /// 物品数据
    /// </summary>
    [Serializable]
    public class ItemData
    {
        public string itemName;
        public int quantity;
        public string itemType;

        public ItemData(string name, int qty, string type)
        {
            itemName = name;
            quantity = qty;
            itemType = type;
        }
    }

    /// <summary>
    /// 游戏设置数据
    /// </summary>
    [Serializable]
    public class GameSettings
    {
        public float masterVolume = 1.0f;
        public float musicVolume = 0.8f;
        public float sfxVolume = 1.0f;
        public bool isVibrationEnabled = true;
        public bool isFullscreen = true;
        public int graphicsQuality = 2;
    }

    #endregion

    #region 基础数据键名常量

    // 玩家数据键名
    private const string KEY_PLAYER_LEVEL = "PlayerLevel";
    private const string KEY_CURRENT_EXP = "CurrentExp";
    private const string KEY_EXP_TO_NEXT_LEVEL = "ExpToNextLevel";
    private const string KEY_HIGH_SCORE = "HighScore";
    private const string KEY_CURRENT_LEVEL = "CurrentLevel";
    private const string KEY_TOTAL_ENEMIES_KILLED = "TotalEnemiesKilled";

    // 复杂数据键名
    private const string KEY_PLAYER_STATS = "PlayerStats";
    private const string KEY_PLAYER_SKILLS = "PlayerSkills";
    private const string KEY_INVENTORY = "Inventory";
    private const string KEY_GAME_SETTINGS = "GameSettings";

    #endregion

    #region 数据缓存

    // 基础数据缓存
    private int playerLevel;
    private int currentExp;
    private int expToNextLevel;
    private int highScore;
    private int currentGameLevel;
    private int totalEnemiesKilled;

    // 复杂数据缓存
    private PlayerStats playerStats;
    private List<PlayerSkill> playerSkills;
    private List<ItemData> inventory;
    private GameSettings gameSettings;

    #endregion

    #region 生命周期方法

    private void Awake()
    {
        // 确保单例模式
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeData();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region 初始化方法

    /// <summary>
    /// 初始化数据
    /// </summary>
    private void InitializeData()
    {
        // 加载基础数据
        LoadBaseData();

        // 加载复杂数据
        LoadComplexData();
    }

    /// <summary>
    /// 加载基础数据
    /// </summary>
    private void LoadBaseData()
    {
        playerLevel = PlayerPrefs.GetInt(KEY_PLAYER_LEVEL, 1);
        currentExp = PlayerPrefs.GetInt(KEY_CURRENT_EXP, 0);
        expToNextLevel = PlayerPrefs.GetInt(KEY_EXP_TO_NEXT_LEVEL, 100);
        highScore = PlayerPrefs.GetInt(KEY_HIGH_SCORE, 0);
        currentGameLevel = PlayerPrefs.GetInt(KEY_CURRENT_LEVEL, 1);
        totalEnemiesKilled = PlayerPrefs.GetInt(KEY_TOTAL_ENEMIES_KILLED, 0);
    }

    /// <summary>
    /// 加载复杂数据
    /// </summary>
    private void LoadComplexData()
    {
        // 加载玩家属性
        string playerStatsJson = PlayerPrefs.GetString(KEY_PLAYER_STATS, string.Empty);
        if (!string.IsNullOrEmpty(playerStatsJson))
        {
            playerStats = JsonUtility.FromJson<PlayerStats>(playerStatsJson);
        }
        else
        {
            playerStats = new PlayerStats();
        }

        // 加载玩家技能
        string skillsJson = PlayerPrefs.GetString(KEY_PLAYER_SKILLS, string.Empty);
        if (!string.IsNullOrEmpty(skillsJson))
        {
            SkillsWrapper skillsWrapper = JsonUtility.FromJson<SkillsWrapper>(skillsJson);
            playerSkills = skillsWrapper.skills;
        }
        else
        {
            // 初始化默认技能
            playerSkills = new List<PlayerSkill>
            {
                new PlayerSkill("Basic Attack", 1, true),
                new PlayerSkill("Fireball", 0, false),
                new PlayerSkill("Heal", 0, false)
            };
        }

        // 加载物品栏
        string inventoryJson = PlayerPrefs.GetString(KEY_INVENTORY, string.Empty);
        if (!string.IsNullOrEmpty(inventoryJson))
        {
            InventoryWrapper inventoryWrapper = JsonUtility.FromJson<InventoryWrapper>(inventoryJson);
            inventory = inventoryWrapper.items;
        }
        else
        {
            // 初始化默认物品
            inventory = new List<ItemData>
            {
                new ItemData("Health Potion", 3, "Consumable"),
                new ItemData("Mana Potion", 2, "Consumable"),
                new ItemData("Iron Sword", 1, "Weapon")
            };
        }

        // 加载游戏设置
        string settingsJson = PlayerPrefs.GetString(KEY_GAME_SETTINGS, string.Empty);
        if (!string.IsNullOrEmpty(settingsJson))
        {
            gameSettings = JsonUtility.FromJson<GameSettings>(settingsJson);
        }
        else
        {
            gameSettings = new GameSettings();
        }
    }

    #endregion

    #region 数据保存方法

    /// <summary>
    /// 保存所有数据
    /// </summary>
    public void SaveAllData()
    {
        SaveBaseData();
        SaveComplexData();
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 保存基础数据
    /// </summary>
    private void SaveBaseData()
    {
        PlayerPrefs.SetInt(KEY_PLAYER_LEVEL, playerLevel);
        PlayerPrefs.SetInt(KEY_CURRENT_EXP, currentExp);
        PlayerPrefs.SetInt(KEY_EXP_TO_NEXT_LEVEL, expToNextLevel);
        PlayerPrefs.SetInt(KEY_HIGH_SCORE, highScore);
        PlayerPrefs.SetInt(KEY_CURRENT_LEVEL, currentGameLevel);
        PlayerPrefs.SetInt(KEY_TOTAL_ENEMIES_KILLED, totalEnemiesKilled);
    }

    /// <summary>
    /// 保存复杂数据
    /// </summary>
    private void SaveComplexData()
    {
        // 保存玩家属性
        string playerStatsJson = JsonUtility.ToJson(playerStats);
        PlayerPrefs.SetString(KEY_PLAYER_STATS, playerStatsJson);

        // 保存玩家技能
        SkillsWrapper skillsWrapper = new SkillsWrapper { skills = playerSkills };
        string skillsJson = JsonUtility.ToJson(skillsWrapper);
        PlayerPrefs.SetString(KEY_PLAYER_SKILLS, skillsJson);

        // 保存物品栏
        InventoryWrapper inventoryWrapper = new InventoryWrapper { items = inventory };
        string inventoryJson = JsonUtility.ToJson(inventoryWrapper);
        PlayerPrefs.SetString(KEY_INVENTORY, inventoryJson);

        // 保存游戏设置
        string settingsJson = JsonUtility.ToJson(gameSettings);
        PlayerPrefs.SetString(KEY_GAME_SETTINGS, settingsJson);
    }

    #endregion

    #region 数据访问方法

    #region 玩家等级数据

    public int PlayerLevel
    {
        get { return playerLevel; }
        set
        {
            playerLevel = value;
            PlayerPrefs.SetInt(KEY_PLAYER_LEVEL, playerLevel);
        }
    }

    public int CurrentExp
    {
        get { return currentExp; }
        set
        {
            currentExp = value;
            PlayerPrefs.SetInt(KEY_CURRENT_EXP, currentExp);
        }
    }

    public int ExpToNextLevel
    {
        get { return expToNextLevel; }
        set
        {
            expToNextLevel = value;
            PlayerPrefs.SetInt(KEY_EXP_TO_NEXT_LEVEL, expToNextLevel);
        }
    }

    #endregion

    #region 游戏进度数据

    public int HighScore
    {
        get { return highScore; }
        set
        {
            if (value > highScore)
            {
                highScore = value;
                PlayerPrefs.SetInt(KEY_HIGH_SCORE, highScore);
            }
        }
    }

    public int CurrentGameLevel
    {
        get { return currentGameLevel; }
        set
        {
            currentGameLevel = value;
            PlayerPrefs.SetInt(KEY_CURRENT_LEVEL, currentGameLevel);
        }
    }

    public int TotalEnemiesKilled
    {
        get { return totalEnemiesKilled; }
        set
        {
            totalEnemiesKilled = value;
            PlayerPrefs.SetInt(KEY_TOTAL_ENEMIES_KILLED, totalEnemiesKilled);
        }
    }

    #endregion

    #region 玩家属性数据

    public PlayerStats GetPlayerStats()
    {
        return playerStats;
    }

    public void SavePlayerStats()
    {
        string playerStatsJson = JsonUtility.ToJson(playerStats);
        PlayerPrefs.SetString(KEY_PLAYER_STATS, playerStatsJson);
    }

    #endregion

    #region 玩家技能数据

    public List<PlayerSkill> GetPlayerSkills()
    {
        return playerSkills;
    }

    public void SavePlayerSkills()
    {
        SkillsWrapper skillsWrapper = new SkillsWrapper { skills = playerSkills };
        string skillsJson = JsonUtility.ToJson(skillsWrapper);
        PlayerPrefs.SetString(KEY_PLAYER_SKILLS, skillsJson);
    }

    public void AddPlayerSkill(string skillName, int level, bool isUnlocked = true)
    {
        playerSkills.Add(new PlayerSkill(skillName, level, isUnlocked));
        SavePlayerSkills();
    }

    public void UpgradeSkill(string skillName)
    {
        PlayerSkill skill = playerSkills.Find(s => s.skillName == skillName);
        if (skill != null)
        {
            skill.skillLevel++;
            SavePlayerSkills();
        }
    }

    #endregion

    #region 物品栏数据

    public List<ItemData> GetInventory()
    {
        return inventory;
    }

    public void SaveInventory()
    {
        InventoryWrapper inventoryWrapper = new InventoryWrapper { items = inventory };
        string inventoryJson = JsonUtility.ToJson(inventoryWrapper);
        PlayerPrefs.SetString(KEY_INVENTORY, inventoryJson);
    }

    public void AddItemToInventory(string itemName, int quantity, string itemType)
    {
        ItemData existingItem = inventory.Find(item => item.itemName == itemName);
        if (existingItem != null)
        {
            existingItem.quantity += quantity;
        }
        else
        {
            inventory.Add(new ItemData(itemName, quantity, itemType));
        }
        SaveInventory();
    }

    public bool RemoveItemFromInventory(string itemName, int quantity = 1)
    {
        ItemData item = inventory.Find(i => i.itemName == itemName);
        if (item != null)
        {
            if (item.quantity >= quantity)
            {
                item.quantity -= quantity;
                if (item.quantity <= 0)
                {
                    inventory.Remove(item);
                }
                SaveInventory();
                return true;
            }
        }
        return false;
    }

    #endregion

    #region 游戏设置数据

    public GameSettings GetGameSettings()
    {
        return gameSettings;
    }

    public void SaveGameSettings()
    {
        string settingsJson = JsonUtility.ToJson(gameSettings);
        PlayerPrefs.SetString(KEY_GAME_SETTINGS, settingsJson);
    }

    #endregion

    #endregion

    #region 数据重置方法

    /// <summary>
    /// 重置所有游戏数据
    /// </summary>
    public void ResetAllData()
    {
        // 重置基础数据
        PlayerLevel = 1;
        CurrentExp = 0;
        ExpToNextLevel = 100;
        highScore = 0;
        PlayerPrefs.SetInt(KEY_HIGH_SCORE, highScore);
        CurrentGameLevel = 1;
        TotalEnemiesKilled = 0;

        // 重置复杂数据
        playerStats = new PlayerStats();
        SavePlayerStats();

        playerSkills = new List<PlayerSkill>
        {
            new PlayerSkill("Basic Attack", 1, true),
            new PlayerSkill("Fireball", 0, false),
            new PlayerSkill("Heal", 0, false)
        };
        SavePlayerSkills();

        inventory = new List<ItemData>
        {
            new ItemData("Health Potion", 3, "Consumable"),
            new ItemData("Mana Potion", 2, "Consumable"),
            new ItemData("Iron Sword", 1, "Weapon")
        };
        SaveInventory();

        gameSettings = new GameSettings();
        SaveGameSettings();

        PlayerPrefs.Save();
    }

    #endregion

    #region 辅助类

    // 用于包装技能列表的类，因为JsonUtility不直接支持List<T>
    [Serializable]
    private class SkillsWrapper
    {
        public List<PlayerSkill> skills;
    }

    // 用于包装物品列表的类
    [Serializable]
    private class InventoryWrapper
    {
        public List<ItemData> items;
    }

    #endregion
}