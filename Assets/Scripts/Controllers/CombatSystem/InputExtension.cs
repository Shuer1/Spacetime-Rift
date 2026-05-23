using UnityEngine;
using StarterAssets;

[RequireComponent(typeof(PlayerCombatController))]
public class InputExtension : MonoBehaviour
{
    private StarterAssetsInputs _input;
    private PlayerCombatController _combat;
    private ThirdPersonController _player;

    private void Awake()
    {
        _input = GetComponent<StarterAssetsInputs>();
        _combat = GetComponent<PlayerCombatController>();
        _player = GetComponent<ThirdPersonController>();
    }

    private void Update()
    {
        if (_input == null) return;

        // 移动中断攻击
        if (_input.move.sqrMagnitude > 0.01f)
        {
            _combat.TryInterruptAttackByMovement(_input.move);
        }

        if (_input.attack)
        {
            _combat.OnAttackInput();
            _input.attack = false;
        }

        if (_input.dash)
        {
            _player.Dash();
            _input.dash = false;
        }
    }
}