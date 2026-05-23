using System.Collections.Generic;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;
    
    [Header("事件音效表")]
    public List<EventSFXEntry> eventSFX = new List<EventSFXEntry>();
    private Dictionary<string, AudioClip> eventDict;

    [Header("2D音效播放器")]
    private AudioSource sfxSource;     // OneShot（叠加）
    private AudioSource mainSource;    // 可控播放（可Stop）

    [System.Serializable]
    public class EventSFXEntry
    {
        public string key;
        public AudioClip clip;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // ✅ OneShot音效（叠加）
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.spatialBlend = 0f;
            sfxSource.playOnAwake = false;

            // ✅ 主通道（可控）
            mainSource = gameObject.AddComponent<AudioSource>();
            mainSource.spatialBlend = 0f;
            mainSource.playOnAwake = false;
            mainSource.loop = false;

            InitEventDictionary();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // =========================
    // 🔊 普通叠加音效（无法单独停止）
    // =========================
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }

    // =========================
    // 🔊 可控播放（用于场景切换）
    // =========================
    public void PlayMainSFX(AudioClip clip, bool loop = false, float volume = 1f)
    {
        if (clip == null) return;

        mainSource.Stop(); // 先停旧的
        mainSource.clip = clip;
        mainSource.loop = loop;
        mainSource.volume = volume;
        mainSource.Play();
    }

    public void StopMainSFX()
    {
        mainSource.Stop();
    }

    // =========================
    // 🔊 事件音效
    // =========================
    public void PlayEventSFX(string key, float volume = 1f)
    {
        if (eventDict == null || !eventDict.ContainsKey(key)) return;

        AudioClip clip = eventDict[key];
        if (clip == null) return;

        PlaySFX(clip, volume);
    }

    // =========================
    // 🛑 停止全部音效（关键）
    // =========================
    public void StopAllSFX()
    {
        mainSource.Stop();
        sfxSource.Stop(); // 注意：会停止当前帧的OneShot（有限）
    }

    private void InitEventDictionary()
    {
        eventDict = new Dictionary<string, AudioClip>();
        foreach (var e in eventSFX)
        {
            if (!eventDict.ContainsKey(e.key) && e.clip != null)
                eventDict.Add(e.key, e.clip);
        }
    }
}