using UnityEngine;

public class TaskInventory : MonoBehaviour, IBaseUI
{
    public static TaskInventory Instance { get; private set; }
    [SerializeField] private GameObject taskPanel;
    [SerializeField] private TaskSlotUI slotPrefab;
    [SerializeField] private Transform slotRoot;
    [SerializeField] private TaskInventoryDetail detailPanel;

    private string currentTaskId;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        var tm = TaskManager.Instance;
        tm.onTaskAccepted  += Refresh;
        tm.onTaskProgress  += Refresh;
        tm.onTaskCompleted += Refresh;

        tm.onTaskAssetsDeleted += DeleteAssetsEvent;
    }

    private void OnDestroy()
    {
        if (TaskManager.Instance == null)
            return;

        TaskManager.Instance.onTaskAccepted  -= Refresh;
        TaskManager.Instance.onTaskProgress  -= Refresh;
        TaskManager.Instance.onTaskCompleted -= Refresh;

        TaskManager.Instance.onTaskAssetsDeleted -= DeleteAssetsEvent;
    }

    public void Open()
    {
        taskPanel.SetActive(true);
        Refresh(null);
        PlayerControlManager.Instance.Lock(ControlLockType.QuestList);
    }

    public void Close()
    {
        taskPanel.SetActive(false);
        detailPanel.gameObject.SetActive(false);
        PlayerControlManager.Instance.Unlock(ControlLockType.QuestList);
    }

    public bool IsOpen() => taskPanel.activeSelf;

    private void Refresh(Task _)
    {
        foreach (Transform t in slotRoot) // 清空
            Destroy(t.gameObject);

        foreach (var kv in TaskManager.Instance.GetAllTasks()) // 重建
        {
            Task task = kv.Value;
            if (task.state != TaskState.Active && task.state != TaskState.Completed)
                continue;

            TaskAsset asset = TaskManager.Instance.GetTaskAsset(task.taskId);
            if (asset == null)
                continue;

            var slot = Instantiate(slotPrefab, slotRoot);
            slot.SetData(task, asset, () =>
            {
                currentTaskId = task.taskId;
                detailPanel.Render(task, asset);
            });
        }

        // --- 详情页最小刷新 ---
        if (detailPanel.gameObject.activeSelf && !string.IsNullOrEmpty(currentTaskId))
        {
            var task = TaskManager.Instance.GetAllTasks()[currentTaskId];
            var asset = TaskManager.Instance.GetTaskAsset(currentTaskId);
            detailPanel.Render(task, asset);
        }
    }

    public void DeleteAssetsEvent()
    {
        detailPanel.gameObject.SetActive(false);
    }
}
