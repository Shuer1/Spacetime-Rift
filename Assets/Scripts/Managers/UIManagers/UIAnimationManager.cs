using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class UIAnimationManager : MonoBehaviour
{
    public static UIAnimationManager Instance { get; private set; }

    private Dictionary<string, UIAnimHandle> animDict = new();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // ===== 注册动画 =====
    public void Register(string key, UIAnimHandle handle)
    {
        if (!animDict.ContainsKey(key))
        {
            animDict.Add(key, handle);
        }
        else
        {
            animDict[key] = handle;
        }
    }

    // ===== 播放（无等待）=====
    public void Play(string key)
    {
        if (animDict.TryGetValue(key, out var handle))
        {
            handle.Play().Forget();
        }
        else
        {
            Debug.LogWarning($"UIAnimation not found: {key}");
        }
    }

    // ===== 播放（可 await）=====
    public async UniTask PlayAsync(string key)
    {
        if (animDict.TryGetValue(key, out var handle))
        {
            await handle.Play();
        }
        else
        {
            Debug.LogWarning($"UIAnimation not found: {key}");
        }
    }

    public void Unregister(string key)
    {
        if (animDict.ContainsKey(key))
            animDict.Remove(key);
    }
}