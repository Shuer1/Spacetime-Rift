using UnityEngine;

[CreateAssetMenu(menuName = "Combat/AttackConfig")]
public class AttackConfig : ScriptableObject
{
    public string animationName;
    [Header("Combat Data")]
    public int attack = 1;
    public string vFXKey;

    [Header("Timing")]
    public float duration;

    [Header("Combo")]
    public float comboWindowStart;
    public float comboWindowEnd;
    public int nextAttackIndex = -1;

    [Header("Interrupt")]
    public bool canInterrupt = true;
}