using StarterAssets;
using UnityEngine;

// 接口
public interface IItemEffectHandler
{
    EffectType EffectType { get; }
    void Execute(GameObject user, ItemEffect effect);
}

/* =========== 以下都是具体处理器 =========== */

// 立即回血
public class HealHandler : IItemEffectHandler
{
    public EffectType EffectType => EffectType.Heal;

    public void Execute(GameObject user, ItemEffect eff)
    {
        if (user.TryGetComponent(out PlayerController pc))
        {
            pc.Heal(eff.intParam);
        }
            
    }
}

public class maxHPUpHandler : IItemEffectHandler
{
    public EffectType EffectType => EffectType.DefendUp;

    public void Execute(GameObject user, ItemEffect eff)
    {
        if (user.TryGetComponent(out PlayerController pc))
        {
            pc.PlusHP(eff.intParam);
        }
    }
}

// 持续加速
public class SpeedUpHandler : IItemEffectHandler
{
    public EffectType EffectType => EffectType.SpeedUp;

    public void Execute(GameObject user, ItemEffect eff)
    {
        if (user.TryGetComponent(out ThirdPersonController pc))
        {
            Debug.Log($"Speed up {user.name} by {eff.intParam}% for {eff.duration} seconds.");
            pc.SpeedUp(eff.intParam, eff.duration);
        }
    }
}

public class AttackUpHandler : IItemEffectHandler
{
    public EffectType EffectType => EffectType.AttackUp;

    public void Execute(GameObject user, ItemEffect eff)
    {
        var combat = user.GetComponentInChildren<PlayerCombatController>();

        if (combat != null)
            PlayerDataManager.Instance.AddAttack(eff.intParam, combat);
    }
}

public class PurchaseHandler : IItemEffectHandler
{
    public EffectType EffectType => EffectType.Purchase;

    public void Execute(GameObject user, ItemEffect eff)
    {
        if (user.TryGetComponent(out InventoryManager im))
        {
            im.AddItem("HPPotion_lv2",eff.intParam);
        }
    }
}