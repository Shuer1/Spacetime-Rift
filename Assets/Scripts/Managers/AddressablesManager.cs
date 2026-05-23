using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

/// <summary>
/// Addressables 资源管理器
/// 支持：初始化、热更新、资源加载/释放、场景加载、批量预加载、进度报告
/// </summary>
public class AddressablesManager : MonoBehaviour
{
    public static AddressablesManager Instance { get; private set; }

    // 已加载资源的句柄缓存，用于自动/手动释放
    private readonly Dictionary<string, AsyncOperationHandle> _loadedAssets = new();
    private readonly Dictionary<string, AsyncOperationHandle> _loadedScenes = new();
    private readonly HashSet<string> _updatingCatalogs = new();

    // 状态标记
    public bool IsInitialized { get; private set; }
    public bool IsUpdating { get; private set; }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            ReleaseAll();
        }
    }

    #region 初始化

    /// <summary>
    /// 初始化 Addressables（自动重试机制）
    /// </summary>
    public async UniTask<bool> InitializeAsync(int maxRetry = 3, CancellationToken ct = default)
    {
        if (IsInitialized) return true;

        for (int i = 0; i < maxRetry; i++)
        {
            try
            {
                await Addressables.InitializeAsync().Task.AsUniTask().AttachExternalCancellation(ct);
                IsInitialized = true;
                Debug.Log("[AddressablesManager] 初始化成功");
                return true;
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[AddressablesManager] 初始化已取消");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressablesManager] 初始化失败 ({i + 1}/{maxRetry}): {ex.Message}");
                if (i < maxRetry - 1) await UniTask.Delay(1000, cancellationToken: ct);
            }
        }
        return false;
    }

    #endregion

    #region 热更新

    /// <summary>
    /// 检查是否有 Catalog 更新
    /// </summary>
    public async UniTask<bool> CheckForUpdatesAsync(CancellationToken ct = default)
    {
        EnsureInitialized();

        try
        {
            var catalogs = await Addressables.CheckForCatalogUpdates(false)
                .Task.AsUniTask().AttachExternalCancellation(ct);

            bool hasUpdate = catalogs != null && catalogs.Count > 0;
            Debug.Log($"[AddressablesManager] 更新检查完成，需要更新: {hasUpdate}");
            return hasUpdate;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AddressablesManager] 检查更新失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 下载更新（包含 Catalog 更新 + 资源下载），支持进度报告
    /// </summary>
    /// <param name="onProgress">进度回调 0~1</param>
    public async UniTask<bool> DownloadUpdatesAsync(
        IProgress<float> onProgress = null, 
        CancellationToken ct = default)
    {
        EnsureInitialized();
        if (IsUpdating)
        {
            Debug.LogWarning("[AddressablesManager] 更新正在进行中...");
            return false;
        }

        IsUpdating = true;
        try
        {
            // 1. 检查需要更新的 Catalogs
            List<string> catalogsToUpdate = await Addressables.CheckForCatalogUpdates(false)
                .Task.AsUniTask().AttachExternalCancellation(ct);

            if (catalogsToUpdate == null || catalogsToUpdate.Count == 0)
            {
                Debug.Log("[AddressablesManager] 没有可用更新");
                onProgress?.Report(1f);
                return true;
            }

            Debug.Log($"[AddressablesManager] 发现 {catalogsToUpdate.Count} 个 Catalog 需要更新");

            // 2. 更新 Catalogs
            List<IResourceLocator> updatedLocators = await Addressables.UpdateCatalogs(catalogsToUpdate, false)
                .Task.AsUniTask().AttachExternalCancellation(ct);

            Debug.Log($"[AddressablesManager] Catalog 更新完成，开始下载资源...");

            // 3. 下载所有依赖资源（带进度）
            var downloadHandle = Addressables.DownloadDependenciesAsync(updatedLocators, true);

            // 进度轮询
            using (var progressCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                var progressTask = ReportProgressAsync(downloadHandle, onProgress, progressCts.Token);
                
                await downloadHandle.Task.AsUniTask().AttachExternalCancellation(ct);
                progressCts.Cancel(); // 停止进度报告
                try { await progressTask; } catch { /* ignore */ }
            }

            if (downloadHandle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log("[AddressablesManager] 所有更新下载完成");
                return true;
            }
            else
            {
                Debug.LogError("[AddressablesManager] 资源下载失败");
                return false;
            }
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning("[AddressablesManager] 更新下载已取消");
            throw;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AddressablesManager] 更新下载异常: {ex.Message}");
            return false;
        }
        finally
        {
            IsUpdating = false;
        }
    }

    /// <summary>
    /// 获取需要下载的总大小（字节）
    /// </summary>
    public async UniTask<long> GetDownloadSizeAsync(CancellationToken ct = default)
    {
        EnsureInitialized();

        try
        {
            var catalogs = await Addressables.CheckForCatalogUpdates(false)
                .Task.AsUniTask().AttachExternalCancellation(ct);

            if (catalogs == null || catalogs.Count == 0) return 0;

            var locators = await Addressables.UpdateCatalogs(catalogs, false)
                .Task.AsUniTask().AttachExternalCancellation(ct);

            long totalSize = await Addressables.GetDownloadSizeAsync(locators)
                .Task.AsUniTask().AttachExternalCancellation(ct);

            return totalSize;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AddressablesManager] 获取下载大小失败: {ex.Message}");
            return -1;
        }
    }

    #endregion

    #region 资源加载

    /// <summary>
    /// 加载单个资源（自动缓存句柄）
    /// </summary>
    public async UniTask<T> LoadAssetAsync<T>(
        string address, 
        CancellationToken ct = default) where T : UnityEngine.Object
    {
        EnsureInitialized();

        if (string.IsNullOrEmpty(address))
        {
            Debug.LogError("[AddressablesManager] 资源地址为空");
            return null;
        }

        // 如果已缓存，直接返回（注意：这里假设同一地址资源类型一致）
        if (_loadedAssets.TryGetValue(address, out var cachedHandle) && cachedHandle.IsValid())
        {
            if (cachedHandle.Result is T result) return result;
        }

        try
        {
            var handle = Addressables.LoadAssetAsync<T>(address);
            T asset = await handle.Task.AsUniTask().AttachExternalCancellation(ct);

            if (handle.Status == AsyncOperationStatus.Succeeded && asset != null)
            {
                _loadedAssets[address] = handle;
                return asset;
            }
            else
            {
                Debug.LogError($"[AddressablesManager] 加载资源失败: {address}");
                Addressables.Release(handle);
                return null;
            }
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning($"[AddressablesManager] 加载资源已取消: {address}");
            throw;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AddressablesManager] 加载资源异常 [{address}]: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 批量加载资源，支持总进度报告
    /// </summary>
    public async UniTask<List<T>> LoadAssetsAsync<T>(
        IEnumerable<string> addresses, 
        IProgress<float> onProgress = null,
        CancellationToken ct = default) where T : UnityEngine.Object
    {
        var results = new List<T>();
        var addressList = new List<string>(addresses);
        
        if (addressList.Count == 0) return results;

        float total = addressList.Count;
        for (int i = 0; i < addressList.Count; i++)
        {
            var asset = await LoadAssetAsync<T>(addressList[i], ct);
            if (asset != null) results.Add(asset);
            onProgress?.Report((i + 1) / total);
        }

        return results;
    }

    /// <summary>
    /// 预加载资源（加载到内存但不返回，用于提前缓存）
    /// </summary>
    public async UniTask PreloadAssetsAsync<T>(
        IEnumerable<string> addresses, 
        IProgress<float> onProgress = null,
        CancellationToken ct = default) where T : UnityEngine.Object
    {
        await LoadAssetsAsync<T>(addresses, onProgress, ct);
        Debug.Log("[AddressablesManager] 预加载完成");
    }

    #endregion

    #region 场景加载

    /// <summary>
    /// 加载场景（Addressables 方式）
    /// </summary>
    public async UniTask<bool> LoadSceneAsync(
        string sceneAddress, 
        LoadSceneMode mode = LoadSceneMode.Single,
        bool activateOnLoad = true,
        IProgress<float> onProgress = null,
        CancellationToken ct = default)
    {
        EnsureInitialized();

        try
        {
            // 如果已加载同名场景，先卸载
            if (_loadedScenes.TryGetValue(sceneAddress, out var oldHandle) && oldHandle.IsValid())
            {
                await Addressables.UnloadSceneAsync(oldHandle).Task.AsUniTask().AttachExternalCancellation(ct);
            }

            var handle = Addressables.LoadSceneAsync(sceneAddress, mode, activateOnLoad);
            _loadedScenes[sceneAddress] = handle;

            // 进度报告
            using (var progressCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                var progressTask = ReportProgressAsync(handle, onProgress, progressCts.Token);
                
                var sceneInstance = await handle.Task.AsUniTask().AttachExternalCancellation(ct);
                progressCts.Cancel();
                try { await progressTask; } catch { /* ignore */ }

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    Debug.Log($"[AddressablesManager] 场景加载成功: {sceneAddress}");
                    return true;
                }
                else
                {
                    Debug.LogError($"[AddressablesManager] 场景加载失败: {sceneAddress}");
                    return false;
                }
            }
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning($"[AddressablesManager] 场景加载已取消: {sceneAddress}");
            throw;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AddressablesManager] 场景加载异常 [{sceneAddress}]: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 卸载 Addressables 场景
    /// </summary>
    public async UniTask UnloadSceneAsync(string sceneAddress, CancellationToken ct = default)
    {
        if (_loadedScenes.TryGetValue(sceneAddress, out var handle) && handle.IsValid())
        {
            await Addressables.UnloadSceneAsync(handle).Task.AsUniTask().AttachExternalCancellation(ct);
            _loadedScenes.Remove(sceneAddress);
            Debug.Log($"[AddressablesManager] 场景已卸载: {sceneAddress}");
        }
    }

    #endregion

    #region 资源释放

    /// <summary>
    /// 释放指定资源
    /// </summary>
    public void ReleaseAsset(string address)
    {
        if (_loadedAssets.TryGetValue(address, out var handle) && handle.IsValid())
        {
            Addressables.Release(handle);
            _loadedAssets.Remove(address);
            Debug.Log($"[AddressablesManager] 资源已释放: {address}");
        }
    }

    /// <summary>
    /// 释放指定类型的所有资源
    /// </summary>
    public void ReleaseAssets<T>() where T : UnityEngine.Object
    {
        var toRemove = new List<string>();
        foreach (var kvp in _loadedAssets)
        {
            if (kvp.Value.IsValid() && kvp.Value.Result is T)
            {
                Addressables.Release(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var key in toRemove) _loadedAssets.Remove(key);
    }

    /// <summary>
    /// 释放所有管理的资源（场景 + 资源）
    /// </summary>
    public void ReleaseAll()
    {
        foreach (var kvp in _loadedAssets)
        {
            if (kvp.Value.IsValid()) Addressables.Release(kvp.Value);
        }
        _loadedAssets.Clear();

        foreach (var kvp in _loadedScenes)
        {
            if (kvp.Value.IsValid()) Addressables.UnloadSceneAsync(kvp.Value);
        }
        _loadedScenes.Clear();

        Debug.Log("[AddressablesManager] 所有资源已释放");
    }

    #endregion

    #region 工具方法

    private void EnsureInitialized()
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("[AddressablesManager] Addressables 尚未初始化，请先调用 InitializeAsync()");
        }
    }

    /// <summary>
    /// 进度报告协程
    /// </summary>
    private async UniTask ReportProgressAsync(
        AsyncOperationHandle handle, 
        IProgress<float> progress, 
        CancellationToken ct)
    {
        if (progress == null) return;

        while (!handle.IsDone && !ct.IsCancellationRequested)
        {
            progress.Report(handle.PercentComplete);
            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, ct);
        }

        if (!ct.IsCancellationRequested && handle.Status == AsyncOperationStatus.Succeeded)
        {
            progress.Report(1f);
        }
    }

    /// <summary>
    /// 检查资源是否存在
    /// </summary>
    public async UniTask<bool> IsAssetExists(string address, CancellationToken ct = default)
    {
        var locations = await Addressables.LoadResourceLocationsAsync(address).Task.AsUniTask().AttachExternalCancellation(ct);
        return locations != null && locations.Count > 0;
    }

    /// <summary>
    /// 获取已加载资源的句柄（用于高级操作）
    /// </summary>
    public AsyncOperationHandle? GetHandle(string address)
    {
        if (_loadedAssets.TryGetValue(address, out var handle) && handle.IsValid())
            return handle;
        return null;
    }

    #endregion
}