using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EnemyAnimator : MonoBehaviour
{
    Animator anim;
    EnemyMotor motor;

    void Awake()
    {
        anim = GetComponent<Animator>();
        motor = GetComponent<EnemyMotor>();
    }

    public void Tick()
    {
        // ⭐ 关键：带阻尼，防止卡住
        anim.SetFloat("Speed", motor.GetSpeed(), 0.1f, Time.deltaTime);
    }

    public void PlayAttack(int combo)
    {
        anim.SetInteger("ComboIndex", combo);
        anim.SetTrigger("Attack");
    }

    public void PlayHit()
    {
        anim.SetTrigger("Hit");
    }

    public void PlayDie()
    {
        anim.SetTrigger("Die");
    }

    public void OnAttackHit()
    {
        
    }
}