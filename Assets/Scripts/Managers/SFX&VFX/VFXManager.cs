using System.Collections.Generic;
using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance;

    [Header("事件特效表")]
    public List<EventVFXEntry> eventVFX = new List<EventVFXEntry>();
    private Dictionary<string, GameObject> eventDict;

    [Header("对象池设置")]
    public int poolSize = 2;  // 每个特效的默认池大小
    private Dictionary<string, Queue<GameObject>> poolDict = new Dictionary<string, Queue<GameObject>>();

    [System.Serializable]
    public class EventVFXEntry
    {
        public string key;          // 特效标识
        public GameObject prefab;   // 特效预制体
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitEventDictionary();
            InitPools();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitEventDictionary()
    {
        eventDict = new Dictionary<string, GameObject>();
        foreach (var e in eventVFX)
        {
            if (!eventDict.ContainsKey(e.key) && e.prefab != null)
                eventDict.Add(e.key, e.prefab);
        }
    }

    private void InitPools()
    {
        foreach (var e in eventVFX)
        {
            if (!poolDict.ContainsKey(e.key) && e.prefab != null)
            {
                Queue<GameObject> q = new Queue<GameObject>();
                for (int i = 0; i < poolSize; i++)
                {
                    GameObject obj = Instantiate(e.prefab);
                    obj.SetActive(false);
                    obj.transform.SetParent(transform);
                    q.Enqueue(obj);
                }
                poolDict.Add(e.key, q);
            }
        }
    }

    private GameObject GetFromPool(string key)
    {
        if (!poolDict.ContainsKey(key)) return null;

        Queue<GameObject> q = poolDict[key];
        GameObject obj;
        if (q.Count > 0)
        {
            obj = q.Dequeue();
        }
        else
        {
            // 池中没有了，临时生成一个
            obj = Instantiate(eventDict[key]);
        }
        obj.SetActive(true);
        return obj;
    }

    private void ReturnToPool(string key, GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(transform);
        if (!poolDict.ContainsKey(key))
        {
            poolDict.Add(key, new Queue<GameObject>());
        }
        poolDict[key].Enqueue(obj);
    }

    /// <summary>
    /// 播放特效（带对象池）
    /// </summary>
    public void PlayEventVFX(string key, Vector3 position, Quaternion rotation = default)
    {
        if (eventDict == null || !eventDict.ContainsKey(key)) return;

        GameObject obj = GetFromPool(key);
        obj.transform.position = position;
        obj.transform.rotation = rotation == default ? Quaternion.identity : rotation;

        // 自动回收（根据 ParticleSystem 长度）
        ParticleSystem ps = obj.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            float duration = ps.main.duration + ps.main.startLifetime.constantMax;
            StartCoroutine(AutoReturn(key, obj, duration));
        }
        else
        {
            // 没有粒子系统，延迟 2 秒回收
            StartCoroutine(AutoReturn(key, obj, 2f));
        }
    }

    public void PlayEventVFX(string key, Transform target)
    {
        PlayEventVFX(key, target.position, target.rotation);
    }

    private System.Collections.IEnumerator AutoReturn(string key, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool(key, obj);
    }
}