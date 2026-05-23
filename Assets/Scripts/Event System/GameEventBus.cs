using System;
using System.Collections.Generic;

public static class GameEventBus
{
    private static readonly Dictionary<Type, Delegate> listeners = new();

    public static void Subscribe<T>(Action<T> callback) where T : GameEvent
    {
        if (listeners.TryGetValue(typeof(T), out var del))
            listeners[typeof(T)] = Delegate.Combine(del, callback);
        else
            listeners[typeof(T)] = callback;
    }

    public static void Unsubscribe<T>(Action<T> callback) where T : GameEvent
    {
        if (listeners.TryGetValue(typeof(T), out var del))
            listeners[typeof(T)] = Delegate.Remove(del, callback);
    }

    public static void Publish<T>(T e) where T : GameEvent
    {
        if (listeners.TryGetValue(typeof(T), out var del))
            (del as Action<T>)?.Invoke(e);
    }
}