using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyMotor : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float rotateSpeed = 360f;

    Rigidbody rb;
    float currentSpeed;
    Vector3 moveDir;
    bool hasInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // ⭐ 每帧由外部驱动
    public void SetMoveInput(Vector3 dir, float speedPercent = 1f)
    {
        dir.y = 0;

        if (dir.sqrMagnitude < 0.001f)
        {
            hasInput = false;
            return;
        }

        moveDir = dir.normalized * speedPercent;
        hasInput = true;
    }

    void FixedUpdate()
    {
        if (hasInput)
        {
            float targetSpeed = moveSpeed * moveDir.magnitude;
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.fixedDeltaTime * 10f);

            Vector3 velocity = moveDir.normalized * currentSpeed;
            rb.linearVelocity = velocity;

            Rotate(moveDir);
        }
        else
        {
            // ⭐ 强制Idle（不会残留速度）
            currentSpeed = Mathf.Lerp(currentSpeed, 0, Time.fixedDeltaTime * 12f);
            rb.linearVelocity = Vector3.zero;
        }

        // ⭐ 每帧清输入（关键！）
        hasInput = false;
    }

    void Rotate(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion rot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, rotateSpeed * Time.fixedDeltaTime);
    }

    public float GetSpeed() => currentSpeed;
}