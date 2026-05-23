using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(EnemyMotor), typeof(EnemyAnimator))]
public class EnemyBase : MonoBehaviour
{
    public enum StatePriority { Patrol, Chase, Attack, Hit, Dead }

    public Transform player;

    [Header("基础属性")]
    public string id;
    public int maxHP = 100;
    public int deadExp = 10;
    [SerializeField] int currentHP;

    // ⭐ 对外只读（UI用）
    public int CurrentHP => currentHP;

    // ⭐ UI事件（新增）
    public event Action<int, int> OnHPChanged;
    public event Action OnDead;

    [Header("出生点")]
    [HideInInspector] public Vector3 spawnPosition;

    [Header("巡逻优化")]
    public float patrolMoveTimeMin = 2f;
    public float patrolMoveTimeMax = 4f;
    public float patrolIdleTimeMin = 1f;
    public float patrolIdleTimeMax = 2.5f;

    [Header("障碍检测")]
    public float obstacleDetectDistance = 1.2f;
    public LayerMask obstacleLayer;

    public float chaseRange = 6f;
    public float attackRange = 1.6f;

    public float attackCD = 2f;
    public int damage = 10;

    public float hitInterruptCD = 2.5f;
    float lastHitTime = -999f;

    EnemyState currentState;
    public EnemyState CurrentState => currentState;

    public EnemyMotor motor;
    public EnemyAnimator anim;

    private PlayerController pc;
    private EnemyDrop drop;

    public PatrolState patrolState;
    public ChaseState chaseState;
    public AttackState attackState;
    public HitState hitState;
    public DeadState deadState;
    public ReturnState returnState;

    public float attackTimer;

    public event Action<EnemyState, EnemyState> OnStateChanged;

    void Awake()
    {
        motor = GetComponent<EnemyMotor>();
        anim = GetComponent<EnemyAnimator>();
        pc = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        drop = GetComponent<EnemyDrop>();
    }

    void Start()
    {
        spawnPosition = transform.position;
        currentHP = maxHP;

        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (EnemyManager.Instance.IsEnemyDead(id))
        {
            Destroy(gameObject);
            return;
        }

        patrolState = new PatrolState(this);
        chaseState = new ChaseState(this);
        attackState = new AttackState(this);
        hitState = new HitState(this);
        deadState = new DeadState(this);
        returnState = new ReturnState(this);

        ChangeState(patrolState, true);

        // ⭐ 初始化UI（防止血条不同步）
        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    void Update()
    {
        attackTimer += Time.deltaTime;
        currentState?.Tick();
        anim.Tick();
    }

    public void ChangeState(EnemyState newState, bool force = false)
    {
        if (currentState == newState) return;
        if (currentState is DeadState) return;

        if (force && currentState != null && newState.priority <= currentState.priority)
            return;

        var oldState = currentState;
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();

        OnStateChanged?.Invoke(oldState, newState);
    }

    public float DistanceToPlayer()
    {
        if (player == null) return Mathf.Infinity;
        return Vector3.Distance(transform.position, player.position);
    }

    public void OnAttack()
    {
        if (player == null) return;

        float dist = DistanceToPlayer();
        if (dist > attackRange) return;

        var pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.TakeDamage(damage);
        }
    }

    public void TakeDamage(int dmg)
    {
        if (currentState is DeadState) return;

        currentHP -= dmg;

        DamageTextManager.Instance.ShowDamageText(dmg, transform.position);

        // ⭐ UI刷新（核心）
        OnHPChanged?.Invoke(currentHP, maxHP);

        if (currentHP <= 0)
        {
            EnemyManager.Instance.MarkEnemyDead(id);

            ChangeState(deadState, true);
            drop.Drop();

            // ⭐ 通知UI隐藏
            OnDead?.Invoke();

            StartCoroutine(DisableAfterDelay(2f));
            return;
        }

        float now = Time.time;

        if (now - lastHitTime < hitInterruptCD)
        {
            anim.PlayHit();
            return;
        }

        lastHitTime = now;
        ChangeState(hitState, true);
    }

    IEnumerator DisableAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }

    #region 特效
    public void EPlayVFX(string key, Vector3 position, Quaternion rotation = default)
    {
        if (VFXManager.Instance == null) return;
        VFXManager.Instance.PlayEventVFX(key, position, rotation);
    }

    public void EPlayVFX(string key, Transform target)
    {
        if (VFXManager.Instance == null) return;
        VFXManager.Instance.PlayEventVFX(key, target);
    }

    public void PlayAttackVFX()
    {
        Transform spawnPoint = transform;
        EPlayVFX("EAtt1", spawnPoint);
    }

    public void PlayDeadVFX()
    {
        EPlayVFX("EDie", transform);
    }
    #endregion
}