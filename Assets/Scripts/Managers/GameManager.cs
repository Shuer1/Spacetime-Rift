using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏管理器，统一管理游戏状态和事件通知
/// </summary>
public class GameManager : MonoBehaviour
{
    #region 单例模式
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("GameManager");
                    instance = obj.AddComponent<GameManager>();
                }
            }
            return instance;
        }
    }
    #endregion

    #region 游戏状态枚举
    /// <summary>
    /// 游戏状态枚举
    /// </summary>
    public enum GameState
    {
        Menu,       // 菜单状态
        Playing,    // 游戏中状态
        Paused,     // 暂停状态
        GameOver    // 游戏结束状态
    }
    #endregion

    #region 事件定义
    /// <summary>
    /// 游戏状态变化事件
    /// </summary>
    public delegate void GameStateChangedDelegate(GameState newState, GameState oldState);
    public static event GameStateChangedDelegate OnGameStateChanged;

    /// <summary>
    /// 玩家死亡事件
    /// </summary>
    public delegate void PlayerDeathDelegate();
    public static event PlayerDeathDelegate OnPlayerDeath;

    /// <summary>
    /// 玩家受到伤害事件
    /// </summary>
    /// <param name="damage">伤害值</param>
    public delegate void PlayerDamagedDelegate(float damage);
    public static event PlayerDamagedDelegate OnPlayerDamaged;

    /// <summary>
    /// 玩家治疗事件
    /// </summary>
    /// <param name="healAmount">治疗量</param>
    public delegate void PlayerHealedDelegate(float healAmount);
    public static event PlayerHealedDelegate OnPlayerHealed;

    /// <summary>
    /// 敌人死亡事件
    /// </summary>
    /// <param name="enemy">死亡的敌人</param>
    public delegate void EnemyDeathDelegate(GameObject enemy);
    public static event EnemyDeathDelegate OnEnemyDeath;

    /// <summary>
    /// 分数变化事件
    /// </summary>
    /// <param name="newScore">新分数</param>
    /// <param name="oldScore">旧分数</param>
    public delegate void ScoreChangedDelegate(int newScore, int oldScore);
    public static event ScoreChangedDelegate OnScoreChanged;

    /// <summary>
    /// 关卡完成事件
    /// </summary>
    public delegate void LevelCompletedDelegate();
    public static event LevelCompletedDelegate OnLevelCompleted;

    /// <summary>
    /// 经验值变化事件
    /// </summary>
    /// <param name="currentExp">当前经验值</param>
    /// <param name="expToNextLevel">升级所需经验值</param>
    public delegate void ExpChangedDelegate(int currentExp, int expToNextLevel);
    public static event ExpChangedDelegate OnExpChanged;

    /// <summary>
    /// 等级提升事件
    /// </summary>
    /// <param name="newLevel">新等级</param>
    public delegate void LevelUpDelegate(int newLevel);
    public static event LevelUpDelegate OnLevelUp;
    #endregion

    #region 游戏状态
    [Header("游戏状态")]
    [SerializeField] private GameState currentGameState = GameState.Menu; // 当前游戏状态
    #endregion

    #region 游戏数据
    [Header("游戏数据")]
    [SerializeField] private int playerScore = 0; // 玩家分数
    [SerializeField] private int currentLevel = 1; // 当前关卡
    [SerializeField] private int enemiesAlive = 0; // 当前存活敌人数量
    #endregion

    #region 私有变量
    private GameState previousGameState = GameState.Menu; // 上一个游戏状态
    #endregion

    /// <summary>
    /// 初始化
    /// </summary>
    private void Awake()
    {
        // 确保单例模式
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 初始化游戏状态
        ChangeGameState(currentGameState);
    }

    /// <summary>
    /// 更新逻辑
    /// </summary>
    private void Update()
    {
        // 处理暂停输入
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentGameState == GameState.Playing)
            {
                PauseGame();
            }
            else if (currentGameState == GameState.Paused)
            {
                ResumeGame();
            }
        }

        // 处理游戏结束条件
        if (currentGameState == GameState.Playing && enemiesAlive <= 0)
        {
            CompleteLevel();
        }
    }

    #region 游戏状态管理
    /// <summary>
    /// 开始游戏
    /// </summary>
    public void StartGame()
    {
        ChangeGameState(GameState.Playing);
        ResetGameData();
    }

    /// <summary>
    /// 暂停游戏
    /// </summary>
    public void PauseGame()
    {
        ChangeGameState(GameState.Paused);
        Time.timeScale = 0f;
    }

    /// <summary>
    /// 恢复游戏
    /// </summary>
    public void ResumeGame()
    {
        ChangeGameState(GameState.Playing);
        Time.timeScale = 1f;
    }

    /// <summary>
    /// 游戏结束
    /// </summary>
    public void GameOver()
    {
        ChangeGameState(GameState.GameOver);
        Time.timeScale = 0f;
        
        // 触发玩家死亡事件
        if (OnPlayerDeath != null)
        {
            OnPlayerDeath();
        }
    }

    /// <summary>
    /// 完成关卡
    /// </summary>
    public void CompleteLevel()
    {
        ChangeGameState(GameState.Menu);
        
        // 触发关卡完成事件
        if (OnLevelCompleted != null)
        {
            OnLevelCompleted();
        }
        
        // 加载下一关
        LoadNextLevel();
    }

    /// <summary>
    /// 改变游戏状态
    /// </summary>
    /// <param name="newState">新的游戏状态</param>
    private void ChangeGameState(GameState newState)
    {
        if (newState == currentGameState) return;
        
        previousGameState = currentGameState;
        currentGameState = newState;
        
        // 触发游戏状态变化事件
        if (OnGameStateChanged != null)
        {
            OnGameStateChanged(newState, previousGameState);
        }
        
        Debug.Log(string.Format("游戏状态变化: {0} -> {1}", previousGameState, currentGameState));
    }
    #endregion

    #region 游戏数据管理
    /// <summary>
    /// 重置游戏数据
    /// </summary>
    private void ResetGameData()
    {
        playerScore = 0;
        enemiesAlive = 0;
        Time.timeScale = 1f;
        // 经验值和等级通过DataManager管理，不需要在这里重置
    }

    /// <summary>
    /// 增加分数
    /// </summary>
    /// <param name="amount">增加的分数</param>
    public void AddScore(int amount)
    {
        int oldScore = playerScore;
        playerScore += amount;
        
        // 触发分数变化事件
        if (OnScoreChanged != null)
        {
            OnScoreChanged(playerScore, oldScore);
        }
    }

    /// <summary>
    /// 获取当前分数
    /// </summary>
    /// <returns>当前分数</returns>
    public int GetScore()
    {
        return playerScore;
    }

    /// <summary>
    /// 注册敌人
    /// </summary>
    public void RegisterEnemy()
    {
        enemiesAlive++;
    }

    /// <summary>
    /// 注销敌人
    /// </summary>
    public void UnregisterEnemy()
    {
        enemiesAlive--;
        enemiesAlive = Mathf.Max(0, enemiesAlive);
        
        // 增加分数
        AddScore(100);
    }

    /// <summary>
    /// 获取当前存活敌人数量
    /// </summary>
    /// <returns>存活敌人数量</returns>
    public int GetEnemiesAlive()
    {
        return enemiesAlive;
    }
    #endregion

    #region 场景管理
    /// <summary>
    /// 加载关卡
    /// </summary>
    /// <param name="levelIndex">关卡索引</param>
    public void LoadLevel(int levelIndex)
    {
        SceneManager.LoadScene(levelIndex);
        currentLevel = levelIndex;
        ResetGameData();
        ChangeGameState(GameState.Playing);
    }

    /// <summary>
    /// 加载下一关
    /// </summary>
    public void LoadNextLevel()
    {
        int nextLevelIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextLevelIndex < SceneManager.sceneCountInBuildSettings)
        {
            LoadLevel(nextLevelIndex);
        }
        else
        {
            // 所有关卡完成，返回主菜单
            LoadMainMenu();
        }
    }

    /// <summary>
    /// 加载主菜单
    /// </summary>
    public void LoadMainMenu()
    {
        SceneManager.LoadScene(0);
        ChangeGameState(GameState.Menu);
    }

    /// <summary>
    /// 重新加载当前关卡
    /// </summary>
    public void ReloadCurrentLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        ResetGameData();
        ChangeGameState(GameState.Playing);
    }
    #endregion

    #region 事件触发方法
    
    /// <summary>
    /// 触发玩家受到伤害事件
    /// </summary>
    /// <param name="damage">伤害值</param>
    public void TriggerPlayerDamaged(float damage)
    {
        if (OnPlayerDamaged != null)
        {
            OnPlayerDamaged(damage);
        }
    }

    /// <summary>
    /// 触发玩家治疗事件
    /// </summary>
    /// <param name="healAmount">治疗量</param>
    public void TriggerPlayerHealed(float healAmount)
    {
        if (OnPlayerHealed != null)
        {
            OnPlayerHealed(healAmount);
        }
    }

    /// <summary>
    /// 触发敌人死亡事件
    /// </summary>
    /// <param name="enemy">死亡的敌人</param>
    public void TriggerEnemyDeath(GameObject enemy)
    {
        if (OnEnemyDeath != null)
        {
            OnEnemyDeath(enemy);
        }
    }

    /// <summary>
    /// 触发经验值变化事件
    /// </summary>
    /// <param name="currentExp">当前经验值</param>
    /// <param name="expToNextLevel">升级所需经验值</param>
    public void TriggerExpChanged(int currentExp, int expToNextLevel)
    {
        // 更新DataManager中的数据
        DataManager.Instance.CurrentExp = currentExp;
        DataManager.Instance.ExpToNextLevel = expToNextLevel;
        
        if (OnExpChanged != null)
        {
            OnExpChanged(currentExp, expToNextLevel);
        }
    }

    /// <summary>
    /// 触发等级提升事件
    /// </summary>
    /// <param name="newLevel">新等级</param>
    public void TriggerLevelUp(int newLevel)
    {
        // 更新DataManager中的数据
        DataManager.Instance.PlayerLevel = newLevel;
        
        if (OnLevelUp != null)
        {
            OnLevelUp(newLevel);
        }
    }
    
    /// <summary>
    /// 获取玩家当前等级
    /// </summary>
    /// <returns>玩家等级</returns>
    public int GetPlayerLevel()
    {
        return DataManager.Instance.PlayerLevel;
    }
    
    /// <summary>
    /// 获取当前经验值
    /// </summary>
    /// <returns>当前经验值</returns>
    public int GetCurrentExp()
    {
        return DataManager.Instance.CurrentExp;
    }
    
    /// <summary>
    /// 获取升级所需经验值
    /// </summary>
    /// <returns>升级所需经验值</returns>
    public int GetExpToNextLevel()
    {
        return DataManager.Instance.ExpToNextLevel;
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 获取当前游戏状态
    /// </summary>
    /// <returns>当前游戏状态</returns>
    public GameState GetCurrentGameState()
    {
        return currentGameState;
    }

    /// <summary>
    /// 获取当前关卡
    /// </summary>
    /// <returns>当前关卡</returns>
    public int GetCurrentLevel()
    {
        return currentLevel;
    }
    #endregion
}
