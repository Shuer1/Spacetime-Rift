using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class EnemyHPBar : MonoBehaviour
{
    [Header("绑定")]
    public EnemyBase enemy;
    public Image fillImage;

    [Header("表现")]
    public Vector3 offset = new Vector3(0, 2f, 0);
    public float smoothTime = 0.2f;

    Camera cam;
    bool isDead = false;

    void Awake()
    {
        cam = Camera.main;
    }

    void OnEnable()
    {
        if (enemy == null)
        {
            Debug.LogError("EnemyHPBar 未绑定 EnemyBase");
            enabled = false;
            return;
        }

        enemy.OnHPChanged += UpdateHP;
        enemy.OnDead += OnDead;

        // ⭐ 初始化（关键）
        UpdateHP(enemy.CurrentHP, enemy.maxHP);

        // ⭐ 始终显示
        gameObject.SetActive(true);
    }

    void LateUpdate()
    {
        if (enemy == null || isDead) return;

        // 跟随位置
        transform.position = enemy.transform.position + offset;

        // 朝向摄像机
        if (cam != null)
            transform.forward = cam.transform.forward;
    }

    void UpdateHP(int current, int max)
    {
        float target = Mathf.Clamp01((float)current / max);

        // 平滑血量变化
        fillImage.DOFillAmount(target, smoothTime);
    }

    void OnDead()
    {
        isDead = true;

        // 死亡隐藏（也可以不隐藏，看你需求）
        gameObject.SetActive(false);
    }

    void OnDisable()
    {
        if (enemy != null)
        {
            enemy.OnHPChanged -= UpdateHP;
            enemy.OnDead -= OnDead;
        }
    }
}