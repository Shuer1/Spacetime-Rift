using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;
    public int level = 1;
    public int exp = 0;
    public int expToNextLevel = 20;
    public float defense = 0f;
    public bool isDead = false;

    public GameObject defeatUI;
    public Button ExitGameBtn;
    public Button BackInitSceneBtn;

    public event Action<float, float> OnHealthChanged; // 当前 / 最大
    public event Action OnDeath;

    // 击退状态锁
    private bool isBeingKnockedBack = false;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    async void Start()
    {
        await UniTask.Yield();

        var combat = GetComponent<PlayerCombatController>();
        PlayerDataManager.Instance.ApplyToPlayer(this, combat);

        // ✅ 绑定按钮
        if (ExitGameBtn != null)
            ExitGameBtn.onClick.AddListener(OnClickExitGame);

        if (BackInitSceneBtn != null)
            BackInitSceneBtn.onClick.AddListener(OnClickBackToInitScene);

        // 初始隐藏死亡UI
        if (defeatUI != null)
            defeatUI.SetActive(false);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        float finalDamage = Mathf.Max(1, damage - defense);
        currentHealth -= finalDamage;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"[Player] Took {finalDamage} damage, HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
            Die();
    }

    public void TriggerHealthChanged(float current, float max) => OnHealthChanged?.Invoke(current, max);

    void Die()
    {
        if (isDead) return;

        isDead = true;
        currentHealth = 0;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnDeath?.Invoke();
        Debug.Log("[Player] Died");

        // ✅ 显示死亡UI
        if (defeatUI != null)
            defeatUI.SetActive(true);

        PlayerControlManager.Instance.ForceUnlockAll();

        // ✅ 暂停游戏
        Time.timeScale = 0f;
    }

    // 击退
    public async UniTask ApplyKnockback(Vector3 dir, float distance, float duration)
    {
        if (isBeingKnockedBack) return;

        isBeingKnockedBack = true;

        Vector3 start = transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.001f) dir = transform.forward;

        Vector3 target = start + dir.normalized * distance;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothT = Mathf.SmoothStep(0, 1, t);

            transform.position = Vector3.Lerp(start, target, smoothT);
            await UniTask.Yield();
        }

        transform.position = target;
        isBeingKnockedBack = false;
    }

    public void AddExp(int expAmount)
    {
        exp += expAmount;

        while (exp >= expToNextLevel)
        {
            exp -= expToNextLevel;
            level++;
            expToNextLevel = Mathf.CeilToInt(expToNextLevel * 1.2f);

            Debug.Log($"[Player] Level up -> {level}");
        }

        Debug.Log($"[Player] EXP: {exp}/{expToNextLevel}");
    }

    public void Heal(int healAmount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void PlusHP(int hpAmount)
    {
        maxHealth += hpAmount;
        currentHealth += hpAmount;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void OnApplicationQuit()
    {
        var combat = GetComponent<PlayerCombatController>();
        PlayerDataManager.Instance.Save(this, combat);
    }

    // =========================
    // ✅ UI按钮逻辑
    // =========================

    void OnClickExitGame()
    {
        Debug.Log("[UI] Exit Game");

        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnClickBackToInitScene()
    {
        Debug.Log("[UI] Back To Init Scene");

        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}