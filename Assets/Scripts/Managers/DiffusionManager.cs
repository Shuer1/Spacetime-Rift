using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.UI;

public class DiffusionManager : MonoBehaviour
{
    [System.Serializable]
    public class RendererMaterialPair
    {
        public Renderer renderer;
        public Material normalMaterial;
        public Material effectMaterial;
    }
    public List<RendererMaterialPair> targets = new List<RendererMaterialPair>();

    private static readonly int DistanceID = Shader.PropertyToID("_Distance");
    private static readonly int CenterID = Shader.PropertyToID("_Center");

    public float maxDistance = 5f;
    public float duration = 0.6f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public bool useCustomCenter = false;
    public Vector3 customCenter;

    private MaterialPropertyBlock mpb;
    private CancellationTokenSource cts;

    void Awake()
    {
        mpb = new MaterialPropertyBlock();
        SwitchToNormal();
    }

    void Start()
    {
        
    }

    #region 材质切换

    void SwitchToEffect()
    {
        foreach (var t in targets)
        {
            if (t.renderer == null || t.effectMaterial == null) continue;

            t.renderer.sharedMaterial = t.effectMaterial;
        }
    }

    void SwitchToNormal()
    {
        foreach (var t in targets)
        {
            if (t.renderer == null || t.normalMaterial == null) continue;

            t.renderer.sharedMaterial = t.normalMaterial;
            ClearMPB(t.renderer);
        }
    }

    #endregion

    Vector3 GetCenter(Renderer r)
    {
        return useCustomCenter ? customCenter : r.bounds.center;
    }

    void Apply(float value)
    {
        foreach (var t in targets)
        {
            var r = t.renderer;
            if (r == null) continue;

            r.GetPropertyBlock(mpb);

            mpb.SetFloat(DistanceID, value);
            mpb.SetVector(CenterID, GetCenter(r));

            r.SetPropertyBlock(mpb);
        }
    }

    void KillTween()
    {
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
        }

        cts = new CancellationTokenSource();
    }

    async UniTask PlayReconstructEffect()
    {
        KillTween();

        try
        {
            // 🔥 Step 0：切效果材质
            SwitchToEffect();

            // 🔥 Step 1：初始化为“完整状态”
            Apply(maxDistance);

            // 🔥 Step 2：破碎（max → 0）
            float time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / duration);
                float value = Mathf.Lerp(maxDistance, 0f, curve.Evaluate(t));

                Apply(value);
                await UniTask.Yield(cts.Token);
            }

            Apply(0f);

            // 🔥 Step 3：重构（0 → max）
            time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / duration);
                float value = Mathf.Lerp(0f, maxDistance, curve.Evaluate(t));

                Apply(value);
                await UniTask.Yield(cts.Token);
            }

            Apply(maxDistance);

            // 🔥 Step 4：切回普通材质（关键）
            SwitchToNormal();
        }
        catch (System.OperationCanceledException)
        {
        }
    }

    public void PlayTeleportEffect(Vector3 center)
    {
        useCustomCenter = true;
        customCenter = center;

        PlayReconstructEffect().Forget();
    }

    void ClearMPB(Renderer r)
    {
        r.SetPropertyBlock(null);
    }
}