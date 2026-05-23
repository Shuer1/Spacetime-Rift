using UnityEngine;
using System.Collections.Generic;
using StarterAssets;

public class PlayerCombatController : MonoBehaviour
{
    public List<AttackConfig> comboList;
    private HashSet<EnemyBase> _hitEnemies = new HashSet<EnemyBase>();
    public Transform weaponPoint; // ⭐武器挂点 
    // Attack SFX Keys
    private const string att1SFX = "Att1";
    private const string att2SFX = "Att2";
    private const string att4SFX = "Att4";

    [Header("Interrupt Settings")]
    public float moveInterruptThreshold = 0.5f; // 移动输入强度阈值，用于中断攻击

    [Header("Attack Rotation")]
    public float attackTurnSpeed = 360f; // 旋转速度，度/秒
    public float attackFaceCameraThreshold = 5f; // 旋转结束阈值（度）

    private int _currentComboIndex = -1;
    private float _attackTimer;

    private bool _hasQueuedInput;
    private float _lastAttackInputTime;

    private float _inputBufferTime = 0.25f;
    private float _maxAttackTime = 2f;

    private PlayerActionState _state = PlayerActionState.Idle;

    private ThirdPersonController _move;
    private Transform _camera;

    private Vector3 _attackForward;

    private void Start()
    {
        _move = GetComponent<ThirdPersonController>();
        _camera = Camera.main.transform;
    }

    // ===== 输入 =====
    public void OnAttackInput()
    {
        if (_state == PlayerActionState.Idle)
        {
            StartAttack(0);
            return;
        }

        if (_state == PlayerActionState.Attacking)
        {
            _hasQueuedInput = true;
            _lastAttackInputTime = Time.time;
        }
    }

    public void OnDashInput()
    {
        if (_state == PlayerActionState.Attacking)
        {
            var attack = comboList[_currentComboIndex];
            if (!attack.canInterrupt) return;

            InterruptAttack();
        }

        StartDash();
    }

    // ===== 攻击开始 =====
    private void StartAttack(int index)
    {
        _hitEnemies.Clear();

        _currentComboIndex = index;
        _attackTimer = 0f;
        _state = PlayerActionState.Attacking;

        _hasQueuedInput = false;
        _lastAttackInputTime = -999f;

        _move.AllowMovement = false;

        if (_move.Grounded)
        {
            _move.AllowGravity = false;
            _move.FreezeVerticalVelocity();
        }
        else
        {
            _move.AllowGravity = true;
        }

        Vector3 camForward = _camera.forward;
        camForward.y = 0;
        _attackForward = camForward.normalized;


        var attack = comboList[index];

        GameEventBus.Publish(new AttackStartEvent(index, attack.animationName));
    }

    private void StartDash()
    {
        _state = PlayerActionState.Dodging;

        _move.AllowMovement = false;

        if (_move.Grounded)
        {
            _move.AllowGravity = false;
            _move.FreezeVerticalVelocity();
        }
        else
        {
            _move.AllowGravity = true;
        }

        //GameEventBus.Publish(new DashEvent());

        ResetState();
    }

    private void InterruptAttack()
    {
        _state = PlayerActionState.Interrupted;

        GameEventBus.Publish(new AttackInterruptedEvent());

        ResetState();
    }

    public void TryInterruptAttackByMovement(Vector2 moveInput)
    {
        if (_state != PlayerActionState.Attacking)
            return;

        if (moveInput.magnitude > moveInterruptThreshold)
        {
            InterruptAttack();
        }
    }

    private void Update()
    {
        if (_state != PlayerActionState.Attacking)
            return;

        // 检查移动输入是否超过阈值，中断攻击
        if (_move.GetMoveInput().magnitude > moveInterruptThreshold)
        {
            InterruptAttack();
            return;
        }

        // ⭐ 持续转向过渡，攻击时逐帧接近摄像机朝向
        Quaternion targetRotation = Quaternion.LookRotation(_attackForward);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation,
            attackTurnSpeed * Time.deltaTime);

        var attack = comboList[_currentComboIndex];

        // 可选：当接近目标朝向时，触发转身动画结束标记
        float angleDiff = Quaternion.Angle(transform.rotation, targetRotation);
        if (_move != null && _move.AllowMovement == false)
        {
            // 这里可以在 Animator 里用 similar parameter 触发“完成转身”分支（如果存在）
            if (angleDiff <= attackFaceCameraThreshold && _move.Grounded)
            {
                // example: _animator.SetBool("TurnComplete", true); // 需在 Animator 里配置
            }
        }

        _attackTimer += Time.deltaTime;

        if (_attackTimer > _maxAttackTime)
        {
            ResetState();
            return;
        }

        bool hasBufferedInput = false;

        if (_hasQueuedInput)
            hasBufferedInput = true;
        else if (_lastAttackInputTime > 0 && Time.time - _lastAttackInputTime <= _inputBufferTime)
            hasBufferedInput = true;

        if (_attackTimer >= attack.duration)
        {
            if (hasBufferedInput && attack.nextAttackIndex >= 0)
            {
                StartAttack(attack.nextAttackIndex);
            }
            else
            {
                EndAttack();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_state != PlayerActionState.Attacking) return;
        
        if (other.CompareTag("Enemy"))
        {
            if (other.TryGetComponent<EnemyBase>(out var enemy))
            {
                if (_hitEnemies.Contains(enemy)) return;
                _hitEnemies.Add(enemy);

                var attack = comboList[_currentComboIndex];

                // ⭐伤害
                enemy.TakeDamage(attack.attack);
            }
        }
    }

    private void EndAttack()
    {
        GameEventBus.Publish(new AttackEndEvent());
        ResetState();
    }

    private void ResetState()
    {
        _state = PlayerActionState.Idle;
        _currentComboIndex = -1;
        _hasQueuedInput = false;
        _lastAttackInputTime = -999f;

        _move.AllowMovement = true;
        _move.AllowGravity = true;

        GameEventBus.Publish(new AttackEndEvent());
    }

    public void PlayAttackVFX()
    {
        var attack = comboList[_currentComboIndex];

        if (!string.IsNullOrEmpty(attack.vFXKey))
        {
            Transform spawnPoint = weaponPoint != null ? weaponPoint : transform;

            VFXManager.Instance?.PlayEventVFX(
                attack.vFXKey,
                spawnPoint.position,
                spawnPoint.rotation
            );
        }
    }

    public void PlayAttackSFX(int index)
    {
        switch (index)
        {
            case 0: // Att1
                SFXManager.Instance?.PlayEventSFX(att1SFX);
                break;
            case 1: // Att2
                SFXManager.Instance?.PlayEventSFX(att2SFX);
                break;
            case 3: // Att4（注意索引）
                SFXManager.Instance?.PlayEventSFX(att4SFX);
                break;
        }
    }
}