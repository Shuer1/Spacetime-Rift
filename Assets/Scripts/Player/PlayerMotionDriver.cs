using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerMotionDriver : MonoBehaviour
{
    private CharacterController _controller;
    private Animator _animator;

    private float _verticalVelocity;
    private float _gravity = -20f;

    private Vector3 _externalForce;

    public bool UseRootMotion { get; set; }

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();

        _animator.applyRootMotion = false; // ⭐核心
    }

    public void Tick(Vector3 inputMove)
    {
        Vector3 motion = Vector3.zero;

        if (UseRootMotion)
            motion += GetRootMotion();
        else
            motion += inputMove;

        motion += ApplyGravity();
        motion += ApplyExternalForce();

        _controller.Move(motion);
    }

    private Vector3 GetRootMotion()
    {
        Vector3 delta = _animator.deltaPosition;
        return new Vector3(delta.x, 0, delta.z);
    }

    private Vector3 ApplyGravity()
    {
        if (_controller.isGrounded)
        {
            if (_verticalVelocity < 0)
                _verticalVelocity = -2f;
        }
        else
        {
            _verticalVelocity += _gravity * Time.deltaTime;
        }

        return Vector3.up * _verticalVelocity * Time.deltaTime;
    }

    private Vector3 ApplyExternalForce()
    {
        Vector3 force = _externalForce;
        _externalForce = Vector3.Lerp(_externalForce, Vector3.zero, 10f * Time.deltaTime);
        return force * Time.deltaTime;
    }

    public void AddForce(Vector3 force)
    {
        _externalForce += force;
    }
}