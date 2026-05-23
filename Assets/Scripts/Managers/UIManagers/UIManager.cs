using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("各UI界面的关闭按钮")]
    [SerializeField] private Button closeTaskBtn;
    [SerializeField] private Button closeBagBtn;
    [SerializeField] private Button closeMenuBtn;

    [Header("Testing Notify Buttons")]
    [SerializeField] private Button task3NotifyBtn;
    [SerializeField] private Button task4NotifyBtn;
    [SerializeField] private Button task5NotifyBtn;

    [Header("管理器引用")]
    private PlayerController Player;

    [Header("UI配置")]
    [SerializeField] private TaskSlotUI mainTask;
    public Slider playerHPBar;
    public TextMeshProUGUI playerHPTMP;
    //public Slider playerEXPBar;

    public TextMeshProUGUI tipOfPickItem;

    // 当前追踪任务
    private string currentTrackedTaskId = null;

    // 按钮回调缓存
    private UnityAction closeTaskAction;
    private UnityAction closeBagAction;
    private UnityAction closeMenuAction;

    #region 生命周期

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(gameObject);

        InitiateClickEvents();
    }

    void Start()
    {
        // 测试按钮
        task3NotifyBtn.onClick.AddListener(() => GameEventBus.Publish(new CollectEvent("cat", 1)));
        task4NotifyBtn.onClick.AddListener(() => GameEventBus.Publish(new KillEvent("pig")));
        task5NotifyBtn.onClick.AddListener(() => GameEventBus.Publish(new KillEvent("gfmonster")));

        BindTaskEvents();
        UpdateTaskReminder();

        InitPlayerAsync().Forget(); // ✅ 异步安全初始化
    }

    private void OnDestroy()
    {
        closeTaskBtn.onClick.RemoveListener(closeTaskAction);
        closeBagBtn.onClick.RemoveListener(closeBagAction);
        closeMenuBtn.onClick.RemoveListener(closeMenuAction);

        UnbindTaskEvents();
        UnbindPlayerEvents();
    }

    #endregion

    #region Player 初始化（关键优化）

    private async UniTaskVoid InitPlayerAsync()
    {
        while (Player == null)
        {
            Player = FindFirstObjectByType<PlayerController>();
            await UniTask.Yield();
        }

        BindPlayerEvents();
        InitPlayerUI();
    }

    #endregion

    #region 按钮事件

    private void InitiateClickEvents()
    {
        closeTaskAction = () => TaskInventory.Instance.Close();
        closeBagAction = () => Inventory.Instance.Close();
        closeMenuAction = () => MenuUI.Instance.Close();

        closeTaskBtn.onClick.AddListener(closeTaskAction);
        closeBagBtn.onClick.AddListener(closeBagAction);
        closeMenuBtn.onClick.AddListener(closeMenuAction);
    }

    #endregion

    #region Player UI（事件驱动）

    private void BindPlayerEvents()
    {
        if (Player == null) return;

        Player.OnHealthChanged += OnPlayerHealthChanged;
        Player.OnDeath += OnPlayerDeath;
    }

    private void UnbindPlayerEvents()
    {
        if (Player != null)
        {
            Player.OnHealthChanged -= OnPlayerHealthChanged;
            Player.OnDeath -= OnPlayerDeath;
        }
    }

    private void InitPlayerUI()
    {
        if (Player == null) return;

        playerHPBar.maxValue = Player.maxHealth;
        playerHPBar.value = Player.currentHealth;

        playerHPTMP.text = $"{Mathf.CeilToInt(Player.currentHealth)}/{Mathf.CeilToInt(Player.maxHealth)}";
    }

    private void OnPlayerHealthChanged(float current, float max)
    {
        playerHPBar.maxValue = max;

        // 平滑过渡（更高级UI表现）
        playerHPBar.DOValue(current, 0.25f);

        playerHPTMP.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }

    private void OnPlayerDeath()
    {
        Debug.Log("UI: Player Dead");
        playerHPBar.DOValue(0, 0.3f);
    }

    #endregion

    #region 任务追踪系统（核心）

    private void BindTaskEvents()
    {
        var tm = TaskManager.Instance;
        if (tm == null) return;

        tm.onTaskProgress += OnTaskChanged;
        tm.onTaskCompleted += OnTaskChanged;
        tm.onTaskAccepted += OnTaskChanged;
    }

    private void UnbindTaskEvents()
    {
        if (TaskManager.Instance == null) return;

        TaskManager.Instance.onTaskProgress -= OnTaskChanged;
        TaskManager.Instance.onTaskCompleted -= OnTaskChanged;
        TaskManager.Instance.onTaskAccepted -= OnTaskChanged;
    }

    private void OnTaskChanged(Task _)
    {
        UpdateTaskReminder();
    }

    private void UpdateTaskReminder()
    {
        if (TaskManager.Instance == null) return;

        Task firstIncomplete = null;

        foreach (var kv in TaskManager.Instance.GetAllTasks())
        {
            var task = kv.Value;
            if (task.state == TaskState.Active)
            {
                firstIncomplete = task;
                break;
            }
        }

        if (firstIncomplete != null)
        {
            if (currentTrackedTaskId == firstIncomplete.taskId)
                return;

            currentTrackedTaskId = firstIncomplete.taskId;

            var asset = TaskManager.Instance.GetTaskAsset(firstIncomplete.taskId);
            if (asset == null) return;

            mainTask.gameObject.SetActive(true);
            mainTask.SetData(firstIncomplete, asset, () =>
            {
                TaskInventory.Instance.Open();
            });

            // 动画优化（防抖）
            mainTask.transform.DOComplete();
            mainTask.transform.localScale = Vector3.one * 0.8f;
            mainTask.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
        }
        // ❗没有未完成任务：保持当前UI（不隐藏）
    }

    #endregion

    #region 拾取提示
    public void ShowInteractTip(string msg)
    {
        if (tipOfPickItem == null) return;

        tipOfPickItem.gameObject.SetActive(true);
        tipOfPickItem.text = msg;

        // 可选：简单动画
        tipOfPickItem.DOFade(1, 0.2f).From(0);
    }

    public void HideInteractTip()
    {
        if (tipOfPickItem == null) return;

        tipOfPickItem.gameObject.SetActive(false);
    }
    #endregion

    
}