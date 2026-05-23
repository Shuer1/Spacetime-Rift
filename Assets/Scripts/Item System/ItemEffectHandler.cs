using System.Collections.Generic;
using UnityEngine;

public class ItemEffectManager : MonoBehaviour
{
    public static ItemEffectManager Instance;

    private Dictionary<EffectType, IItemEffectHandler> handlers;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Init();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Init()
    {
        handlers = new Dictionary<EffectType, IItemEffectHandler>();

        Register(new HealHandler());
        Register(new maxHPUpHandler());
        Register(new SpeedUpHandler());
        Register(new AttackUpHandler());
    }

    void Register(IItemEffectHandler handler)
    {
        handlers[handler.EffectType] = handler;
    }

    public void ExecuteItemEffects(GameObject user, List<ItemEffect> effects)
    {
        foreach (var effect in effects)
        {
            if (handlers.TryGetValue(effect.type, out var handler))
            {
                handler.Execute(user, effect);
            }
            else
            {
                Debug.LogWarning($"No handler for effect: {effect.type}");
            }
        }
    }
}