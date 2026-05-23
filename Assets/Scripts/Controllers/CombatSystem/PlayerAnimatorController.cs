using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]
public class PlayerAnimatorController : MonoBehaviour
{
    private Animator _animator;
    private CharacterController _controller;

    private bool _useRootMotion = false;

    // Optimization
    public Vector3 RootMotionDelta { get; private set; }
    public bool UseRootMotion => _useRootMotion;
    public float MoveZ { get; private set; }

    [Header("Root Motion")]
    public float rootMotionMultiplier = 1.0f;

    // ⭐ 重力系统
    private float _verticalVelocity;
    private float _gravity = -20f;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _controller = GetComponent<CharacterController>();

        _animator.applyRootMotion = true;
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<AttackStartEvent>(OnAttackStart);
        GameEventBus.Subscribe<AttackEndEvent>(OnAttackEnd);
        GameEventBus.Subscribe<AttackInterruptedEvent>(OnAttackInterrupted);
        //GameEventBus.Subscribe<DashEvent>(OnDash);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<AttackStartEvent>(OnAttackStart);
        GameEventBus.Unsubscribe<AttackEndEvent>(OnAttackEnd);
        GameEventBus.Unsubscribe<AttackInterruptedEvent>(OnAttackInterrupted);
        //GameEventBus.Unsubscribe<DashEvent>(OnDash);
    }

    private void OnAttackStart(AttackStartEvent e)
    {
        _useRootMotion = true;
        _animator.CrossFade(e.animationName, 0.08f);
    }

    private void OnAttackEnd(AttackEndEvent e)
    {
        _useRootMotion = false;
        _animator.CrossFade("Idle", 0.1f);
    }

    private void OnAttackInterrupted(AttackInterruptedEvent e)
    {
        _useRootMotion = false;
        _animator.CrossFade("Idle", 0.05f); // 更快的过渡以确保流畅
    }

#if ENABLE_INPUT_SYSTEM
    /*
    public void OnDash(InputValue value)
    {
        if (!value.isPressed) return;

        // 兼容输入系统事件回调和游戏事件总线
        OnDash(new DashEvent());
    }
    */
#endif

    /*
    private void OnDash(DashEvent e)
    {
        _useRootMotion = true;
        _animator.CrossFade("Dash", 0.05f);
    }
    */

    public void FreezeVerticalVelocity()
    {
        _verticalVelocity = 0f;
    }

    private void OnAnimatorMove()
    {
        if (!_useRootMotion)
        {
            RootMotionDelta = Vector3.zero;
            return;
        }

        // === 动画位移 ===
        Vector3 delta = _animator.deltaPosition * rootMotionMultiplier;
        Vector3 horizontal = new Vector3(delta.x, 0f, delta.z);

        // === 重力 ===
        if (_controller.isGrounded)
        {
            if (_verticalVelocity < 0f)
                _verticalVelocity = -2f;
        }
        else
        {
            _verticalVelocity += _gravity * Time.deltaTime;
        }

        Vector3 vertical = new Vector3(0f, _verticalVelocity, 0f);

        // === 最终位移 ===
        Vector3 finalMove = horizontal + vertical * Time.deltaTime;

        _controller.Move(finalMove);

        // === 调试 / 外部使用 ===
        RootMotionDelta = horizontal;
        MoveZ = _animator.GetFloat("MoveZ");

        // === Dodge 结束检测 ===
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName("Dodge") && stateInfo.normalizedTime >= 1f)
        {
            _useRootMotion = false;
            _animator.CrossFade("Idle", 0.1f);
        }
    }
}