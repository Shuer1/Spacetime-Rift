using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;
using System.IO;          
using Newtonsoft.Json;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance { get; private set; }

    [Header("Task Database")]
    [SerializeField] private List<TaskAsset> taskDatabase;

    private Dictionary<string, TaskAsset> id2Asset;
    private Dictionary<string, Task> id2Task;
    private Dictionary<TaskType, ITaskProgressStrategy> strategies;
    public UnityAction<Task> onTaskAccepted;
    public UnityAction<Task> onTaskProgress;
    public UnityAction<Task> onTaskCompleted;

    public UnityAction onTaskAssetsDeleted;
    private string savePath;

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        savePath = Path.Combine(Application.persistentDataPath, "task_save.json");

        Init();
    }

    private void OnApplicationQuit() => SaveTasks();
    #endregion

    #region Init / Load / Save
    private void Init()
    {
        id2Asset = taskDatabase.ToDictionary(a => a.taskId);
        LoadTasks();

        strategies = new Dictionary<TaskType, ITaskProgressStrategy>
        {
            { TaskType.Collect, new CollectTaskStrategy() },
            { TaskType.Kill, new KillTaskStrategy() },
            { TaskType.Talk, new TalkTaskStrategy() },
            { TaskType.Combine, new CombineTaskStrategy() }
        };

        RegisterEvents();
    }

    public void SaveTasks()
    {
        TaskSaveData saveData = new TaskSaveData
        {
            tasks = id2Task.Values.ToList()
        };

        // 使用Newtonsoft.Json进行序列化
        string json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
        File.WriteAllText(savePath, json);
    }

    public void LoadTasks()
    {
        id2Task = new Dictionary<string, Task>();

        if (!File.Exists(savePath))
            return;

        // 使用Newtonsoft.Json进行反序列化
        string json = File.ReadAllText(savePath);

        // 添加异常处理
        try 
        {
            TaskSaveData saveData = JsonConvert.DeserializeObject<TaskSaveData>(json);
            
            if (saveData == null || saveData.tasks == null)
                return;

            foreach (var task in saveData.tasks)
            {
                if (!id2Task.ContainsKey(task.taskId))
                    id2Task.Add(task.taskId, task);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Task] Failed to load tasks from JSON: {ex.Message}");
            // 初始化一个空字典以防出错
            id2Task = new Dictionary<string, Task>();
        }
    }
    #endregion

    #region Public API
    public IReadOnlyDictionary<string, Task> GetAllTasks() => id2Task;

    public TaskAsset GetTaskAsset(string taskId)
    {
        id2Asset.TryGetValue(taskId, out var asset);
        return asset;
    }

    public void AcceptTask(string taskId)
    {
        if (id2Task.ContainsKey(taskId))
            return;

        if (!id2Asset.TryGetValue(taskId, out var asset))
        {
            Debug.LogError($"[Task] TaskAsset not found: {taskId}");
            return;
        }

        Task task = new Task
        {
            taskId = taskId,
            state = TaskState.Active,
            currentAmount = 0
        };

        id2Task.Add(taskId, task);
        onTaskAccepted?.Invoke(task);
        SaveTasks();

        Debug.Log($"[Task] Accepted: {asset.taskName}");
    }

    /// <summary>
    /// 供 Fungus 外部查询任务状态，无需知道 Task 内部结构。
    /// 返回：0=不存在(=未接)  1=Active  2=Completed
    /// </summary>
    public int GetTaskStateCode(string taskId)
    {
        if (!id2Task.TryGetValue(taskId, out var t))
            return 0;               // 未接
        return t.state == TaskState.Completed ? 2 : 1;
    }

    public void Notify(string targetId, int delta)
    {
        foreach (var task in id2Task.Values)
        {
            if (task.state != TaskState.Active)
                continue;

            var asset = GetTaskAsset(task.taskId);
            if (asset == null || asset.targetId != targetId)
                continue;

            task.currentAmount = Mathf.Min(
                task.currentAmount + delta,
                asset.requiredAmount
            );

            onTaskProgress?.Invoke(task);

            if (task.currentAmount >= asset.requiredAmount) // 任务完成调用
            {
                CompleteTask(task.taskId);
                break;
            }
        }
    }

    public void CompleteTask(string taskId)
    {
        if (!id2Task.TryGetValue(taskId, out var task))
            return;

        if (task.state == TaskState.Completed)
            return;

        var asset = GetTaskAsset(taskId);
        if (asset == null)
            return;

        task.state = TaskState.Completed;
        task.currentAmount = asset.requiredAmount;

        onTaskCompleted?.Invoke(task);
        GiveReward(asset);

        SaveTasks();

        Debug.Log($"[Task] Completed: {asset.taskName}");

        if (asset.nextTask != null)
            AcceptTask(asset.nextTask.taskId);
    }
    #endregion

    #region Reward
    private void GiveReward(TaskAsset asset)
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[Task] InventoryManager 未就绪，跳过物品奖励发放。");
            return;
        }

        Debug.Log($"[Task Reward] Exp: {asset.exp}");

        if (InventoryManager.Instance == null) return;

        var goldAsset = InventoryManager.Instance.GetItemAsset("Gold");
        if (goldAsset != null)
        {
            InventoryManager.Instance.AddItem(goldAsset, asset.gold);
        }

        foreach (var item in asset.items)
        {
            if (item.item != null && item.count > 0)
                InventoryManager.Instance.AddItem(item.item, item.count);
        }
    }
    #endregion

    #region Register Events
    private void RegisterEvents()
    {
        GameEventBus.Subscribe<KillEvent>(OnGameEvent);
        GameEventBus.Subscribe<CollectEvent>(OnGameEvent);
        GameEventBus.Subscribe<TalkEvent>(OnGameEvent);
        GameEventBus.Subscribe<CombineEvent>(OnGameEvent);
    }

    private void OnGameEvent(GameEvent e)
    {
        foreach (var task in id2Task.Values)
        {
            if (task.state != TaskState.Active)
                continue;

            TaskAsset asset = GetTaskAsset(task.taskId);
            if (asset == null)
                continue;

            if (!strategies.TryGetValue(asset.taskType, out var strategy))
                continue;

            if (!strategy.CanHandle(asset, e))
                continue;

            int delta = strategy.GetProgressDelta(asset, e);
            if (delta <= 0)
                continue;

            task.currentAmount = Mathf.Min(task.currentAmount + delta, asset.requiredAmount);

            onTaskProgress?.Invoke(task);

            if (task.currentAmount >= asset.requiredAmount) CompleteTask(task.taskId);

            SaveTasks();
            break;
        }
    }
    #endregion

    [ContextMenu("Delete TaskAssets Data File")]
    public void DeleteSaveFile()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("[Task] Save file deleted.");
            onTaskAssetsDeleted?.Invoke();
        }
        id2Task.Clear();
    }
}