using UnityEngine;
using System.Collections.Generic;

public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance { get; private set; }

    [Header("Damage Text Settings")]
    [SerializeField] private DamageText damageTextPrefab;
    [SerializeField] private Canvas canvas;
    [SerializeField] private int initialPoolSize = 3;
    [SerializeField] private int maxActiveTexts = 4;

    private Queue<DamageText> pool = new Queue<DamageText>();
    private List<DamageText> activeTexts = new List<DamageText>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializePool();
        EnsureCanvasBound();
    }

    /// <summary>
    /// 确保 Canvas 引用有效
    /// </summary>
    private void EnsureCanvasBound()
    {
        if (canvas != null) return;

        // 优先通过 Tag 查找（推荐在你的主 Canvas 上添加 Tag：MainUI）
        GameObject canvasObj = GameObject.FindWithTag("MainUI");
        if (canvasObj != null)
        {
            canvas = canvasObj.GetComponent<Canvas>();
        }

        // 如果没找到，通过类型查找（防止 Tag 缺失）
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            Debug.LogWarning("[DamageTextManager] ⚠️ 未找到 Canvas");
        }
    }

    /// <summary>
    /// 初始化对象池
    /// </summary>
    private void InitializePool()
    {
        if (damageTextPrefab == null)
        {
            Debug.LogError("❌ DamageTextManager 缺少 DamageTextPrefab 引用！");
            return;
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            var text = CreateNewDamageText();
            text.gameObject.SetActive(false);
            pool.Enqueue(text);
        }
    }

    private DamageText CreateNewDamageText()
    {
        if (canvas == null)
            EnsureCanvasBound();

        if (canvas == null)
        {
            Debug.LogWarning("[DamageTextManager] ⚠️ 未找到 Canvas，无法创建 DamageText 实例。");
            return null;
        }

        DamageText dt = Instantiate(damageTextPrefab, canvas.transform);
        dt.gameObject.name = "DamageText_Pooled";
        return dt;
    }

    /// <summary>
    /// 显示伤害数字
    /// </summary>
    public void ShowDamageText(int damage, Vector3 worldPos)
    {
        // ✅ 确保 Canvas 始终有效
        if (canvas == null)
            EnsureCanvasBound();

        if (canvas == null || damageTextPrefab == null)
        {
            Debug.LogWarning("DamageTextManager: 缺少引用。");
            return;
        }

        if (activeTexts.Count >= maxActiveTexts) //新增
        {
            // 移除最旧的文本
            DamageText oldestText = activeTexts[0];
            activeTexts.RemoveAt(0);

            oldestText.gameObject.SetActive(false);
            pool.Enqueue(oldestText);
        }

        DamageText dt = GetFromPool();
        if (dt == null) return;

        dt.gameObject.SetActive(true);
        dt.Initialize(damage, worldPos);

        activeTexts.Add(dt);
    }

    /// <summary>
    /// 从对象池取出
    /// </summary>
    private DamageText GetFromPool()
    {
        if (pool.Count > 0)
            return pool.Dequeue();
        else
            return CreateNewDamageText();
    }

    /// <summary>
    /// 回收至对象池
    /// </summary>
    public void ReturnToPool(DamageText dt)
    {
        if (dt == null) return;

        activeTexts.Remove(dt);
        pool.Enqueue(dt);
    }
}