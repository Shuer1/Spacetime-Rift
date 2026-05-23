using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public enum ControlLockType
{
    Dialogue,
    UI,
    Backpack,
    QuestList,
    Loading,
    Map,
    Menu,
}

public class PlayerControlManager : MonoBehaviour
{
    public static PlayerControlManager Instance { get; private set; }

    private HashSet<ControlLockType> activeLocks = new HashSet<ControlLockType>();
    
    [SerializeField] private PlayerInput playerInput;
    
    // 使用完全限定名或确保命名空间正确
    private StarterAssets.StarterAssetsInputs _inputs;
    private StarterAssets.ThirdPersonController _controller;

    public static event Action<bool> OnControlStateChanged;
    public bool IsControlLocked => activeLocks.Count > 0;

    private const string GAMEPLAY_MAP = "Player";
    private const string UI_MAP = "UI";

    private bool forceUnlocked = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(gameObject);
        
        if (playerInput == null)
            playerInput = FindFirstObjectByType<PlayerInput>();
            
        _inputs = FindFirstObjectByType<StarterAssets.StarterAssetsInputs>();
        _controller = FindFirstObjectByType<StarterAssets.ThirdPersonController>();
    }

    public void Lock(ControlLockType type)
    {
        // ✅ 如果处于强制解锁状态，直接忽略所有加锁
        if (forceUnlocked)
            return;

        bool wasLocked = IsControlLocked;
        if (activeLocks.Add(type))
        {
            Debug.Log($"[Control] 加锁: {type} | 总锁数: {activeLocks.Count}");
            if (!wasLocked)
                ApplyLockState();
        }
    }

    public void Unlock(ControlLockType type)
    {
        bool wasLocked = IsControlLocked;
        if (activeLocks.Remove(type))
        {
            Debug.Log($"[Control] 解锁: {type} | 剩余锁数: {activeLocks.Count}");
            if (wasLocked && !IsControlLocked && !forceUnlocked)
                ApplyUnlockState();
        }
    }

    private void ApplyLockState()
    {
        // 切换到UI Action Map
        if (playerInput != null)
            playerInput.SwitchCurrentActionMap(UI_MAP);
        
        // 解锁鼠标
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // 清空输入并冻结控制器
        _inputs?.ClearInputs();
        _controller?.SetControlLock(true);
        
        OnControlStateChanged?.Invoke(true);
        Debug.Log("[Control] 已切换到UI模式（鼠标解锁）");
    }

    private void ApplyUnlockState()
    {
        // 切换回Gameplay Action Map
        if (playerInput != null)
            playerInput.SwitchCurrentActionMap(GAMEPLAY_MAP);
        
        // 锁定鼠标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // 恢复控制
        _controller?.SetControlLock(false);
        
        OnControlStateChanged?.Invoke(false);
        Debug.Log("[Control] 已切换到Gameplay模式（鼠标锁定）");
    }

    public void ForceUnlockAll()
    {
        Debug.Log("[Control] 强制解锁全部控制（用于死亡/UI等）");

        forceUnlocked = true;

        // ✅ 清空所有锁
        activeLocks.Clear();

        // ✅ 切换到UI输入
        if (playerInput != null)
            playerInput.SwitchCurrentActionMap(UI_MAP);

        // ✅ 鼠标释放
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // ✅ 停止角色控制
        _inputs?.ClearInputs();
        _controller?.SetControlLock(true);

        OnControlStateChanged?.Invoke(true);
    }

    public void RestoreControlSystem()
    {
        Debug.Log("[Control] 恢复控制系统");

        forceUnlocked = false;

        ApplyUnlockState();
    }
}