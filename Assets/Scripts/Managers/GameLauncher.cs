using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 游戏启动器 - 负责每次进入游戏时的资源初始化与热更新流程
/// </summary>
public class GameLauncher : MonoBehaviour
{
    [Header("UI 绑定")]
    [SerializeField] private GameObject launchPanel;           // 启动界面根节点
    [SerializeField] private Slider progressSlider;            // 进度条
    [SerializeField] private TextMeshProUGUI statusText;       // 主状态文本
    [SerializeField] private TextMeshProUGUI detailText;       // 详细信息（下载大小等）
    [SerializeField] private Button retryButton;               // 重试按钮
    [SerializeField] private Button cancelButton;              // 取消按钮
    [SerializeField] private Button startGameButton;           // 手动开始游戏按钮

    [Header("启动设置")]
    [SerializeField] private string initialSceneAddress = "MainScene";
    [SerializeField] private bool autoEnterAfterUpdate = true;     // 更新完是否自动进游戏
    [SerializeField] private bool skipUpdateInEditor = false;      // Editor 下跳过更新（开发加速）

    private CancellationTokenSource _cts;
    private LauncherState _currentState = LauncherState.Idle;

    private enum LauncherState
    {
        Idle, Initializing, CheckingUpdate, Downloading, 
        LoadingScene, Complete, Error
    }

    void Start()
    {
        retryButton?.onClick.AddListener(OnRetryClicked);
        cancelButton?.onClick.AddListener(OnCancelClicked);
        startGameButton?.onClick.AddListener(OnStartGameClicked);
        
        startGameButton?.gameObject.SetActive(false);
        retryButton?.gameObject.SetActive(false);
        
        // 自动开始启动流程
        StartLaunchFlow().Forget();
    }

    void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        
        retryButton?.onClick.RemoveListener(OnRetryClicked);
        cancelButton?.onClick.RemoveListener(OnCancelClicked);
        startGameButton?.onClick.RemoveListener(OnStartGameClicked);
    }

    #region 主流程

    private async UniTaskVoid StartLaunchFlow()
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            // 1. 初始化 Addressables
            await Step_Initialize(token);

            // Editor 开发模式：可跳过更新检查
#if UNITY_EDITOR
            if (skipUpdateInEditor)
            {
                UpdateUI("开发模式：跳过更新检查", 1f);
                await UniTask.Delay(500, cancellationToken: token);
                await Step_LoadInitialScene(token);
                return;
            }
#endif

            // 2. 检查更新
            bool hasUpdate = await Step_CheckUpdate(token);

            // 3. 下载更新
            if (hasUpdate)
            {
                bool downloadSuccess = await Step_DownloadUpdate(token);
                if (!downloadSuccess)
                {
                    EnterErrorState("更新下载失败，请检查网络后重试");
                    return;
                }
            }

            // 4. 进入游戏
            if (autoEnterAfterUpdate)
            {
                await Step_LoadInitialScene(token);
            }
            else
            {
                EnterState(LauncherState.Complete);
                UpdateUI("更新完成，请点击开始游戏", 1f);
                startGameButton?.gameObject.SetActive(true);
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[GameLauncher] 启动流程已取消");
            UpdateUI("操作已取消", 0f);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameLauncher] 启动异常: {ex}");
            EnterErrorState($"启动失败: {ex.Message}");
        }
    }

    #endregion

    #region 各步骤

    /// <summary>
    /// 步骤1：初始化
    /// </summary>
    private async UniTask Step_Initialize(CancellationToken token)
    {
        EnterState(LauncherState.Initializing);
        UpdateUI("正在初始化游戏资源...", 0f);

        bool success = await AddressablesManager.Instance.InitializeAsync(ct: token);
        if (!success)
        {
            throw new Exception("游戏资源初始化失败，请检查本地资源完整性");
        }
    }

    /// <summary>
    /// 步骤2：检查更新
    /// </summary>
    private async UniTask<bool> Step_CheckUpdate(CancellationToken token)
    {
        EnterState(LauncherState.CheckingUpdate);
        UpdateUI("正在检查版本更新...", 0.1f);

        bool hasUpdate = await AddressablesManager.Instance.CheckForUpdatesAsync(token);

        if (hasUpdate)
        {
            // 查询需要下载的大小，提示用户
            long downloadSize = await AddressablesManager.Instance.GetDownloadSizeAsync(token);
            string sizeStr = FormatBytes(downloadSize);
            
            UpdateUI($"发现新版本", 0.2f);
            detailText?.SetText($"需要下载: {sizeStr}");
            
            // 给玩家看一眼的时间
            await UniTask.Delay(800, cancellationToken: token);
        }
        else
        {
            UpdateUI("已是最新版本", 0.3f);
            detailText?.SetText("");
            await UniTask.Delay(300, cancellationToken: token);
        }

        return hasUpdate;
    }

    /// <summary>
    /// 步骤3：下载更新（带进度）
    /// </summary>
    private async UniTask<bool> Step_DownloadUpdate(CancellationToken token)
    {
        EnterState(LauncherState.Downloading);
        
        var progress = new Progress<float>(p =>
        {
            // 下载阶段映射进度条 30% ~ 90%
            float mapped = 0.3f + p * 0.6f;
            UpdateUI($"正在下载更新... {p:P0}", mapped);
        });

        bool success = await AddressablesManager.Instance.DownloadUpdatesAsync(progress, token);
        
        if (success)
        {
            UpdateUI("更新完成，准备进入游戏...", 0.9f);
            detailText?.SetText("");
        }
        
        return success;
    }

    /// <summary>
    /// 步骤4：加载初始场景
    /// </summary>
    private async UniTask Step_LoadInitialScene(CancellationToken token)
    {
        EnterState(LauncherState.LoadingScene);
        UpdateUI("正在加载游戏场景...", 0.9f);

        var progress = new Progress<float>(p =>
        {
            // 场景加载映射 90% ~ 100%
            float mapped = 0.9f + p * 0.1f;
            UpdateUI("正在进入游戏...", mapped);
        });

        bool success = await AddressablesManager.Instance.LoadSceneAsync(
            initialSceneAddress,
            activateOnLoad: true,
            onProgress: progress,
            ct: token
        );

        if (!success)
        {
            throw new Exception($"场景加载失败: {initialSceneAddress}");
        }

        // 成功进入场景后，隐藏启动界面
        launchPanel?.SetActive(false);
    }

    #endregion

    #region UI & 交互

    private void EnterState(LauncherState state)
    {
        _currentState = state;
        
        // 错误状态显示重试按钮
        retryButton?.gameObject.SetActive(state == LauncherState.Error);
        
        // 进行中的操作显示取消按钮（除了已完成/错误/空闲）
        bool showCancel = state != LauncherState.Idle 
                       && state != LauncherState.Complete 
                       && state != LauncherState.Error;
        cancelButton?.gameObject.SetActive(showCancel);
    }

    private void EnterErrorState(string message)
    {
        EnterState(LauncherState.Error);
        UpdateUI(message, 0f);
        Debug.LogError($"[GameLauncher] {message}");
    }

    private void UpdateUI(string message, float progress)
    {
        statusText?.SetText(message);
        if (progressSlider != null)
        {
            progressSlider.value = Mathf.Clamp01(progress);
        }
    }

    private void OnRetryClicked()
    {
        startGameButton?.gameObject.SetActive(false);
        StartLaunchFlow().Forget();
    }

    private void OnCancelClicked()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource(); // 重置以便重试
    }

    private void OnStartGameClicked()
    {
        startGameButton?.gameObject.SetActive(false);
        Step_LoadInitialScene(_cts.Token).Forget();
    }

    #endregion

    #region 工具

    private static string FormatBytes(long bytes)
    {
        if (bytes < 0) return "未知";
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024f:F1} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024f * 1024f):F2} MB";
        return $"{bytes / (1024f * 1024f * 1024f):F2} GB";
    }

    #endregion
}