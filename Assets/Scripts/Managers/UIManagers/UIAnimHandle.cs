using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;

[System.Serializable]
public class UIAnimHandle : MonoBehaviour
{
    public const string animKey = "LoginSuccess";

    public CanvasGroup canvasGroup;
    public RectTransform rect;

    [Header("参数")]
    public float fadeInTime = 0.3f;
    public float moveTime = 0.4f;
    public float stayTime = 1.2f;
    public float fadeOutTime = 0.3f;

    public float moveOffset = 30f;
    public float scaleFrom = 0.8f;

    private Vector2 originPos;
    private Sequence seq;

    void Awake()
    {
        originPos = rect.anchoredPosition;
        canvasGroup.alpha = 0;
    }

    async void Start()
    {
        await UniTask.WaitUntil(() => UIAnimationManager.Instance != null);
        UIAnimationManager.Instance.Register(animKey, this);
    }

    void OnDestroy()
    {
        if(UIAnimationManager.Instance != null)
            UIAnimationManager.Instance.Unregister(animKey);
    }

    // ===== 播放动画 =====
    public async UniTask Play()
    {
        Kill();

        gameObject.SetActive(true);

        // 初始状态
        rect.anchoredPosition = originPos + new Vector2(0, -moveOffset);
        rect.localScale = Vector3.one * scaleFrom;
        canvasGroup.alpha = 0;

        seq = DOTween.Sequence();

        // ===== 入场 =====
        seq.Append(canvasGroup.DOFade(1, fadeInTime));
        seq.Join(rect.DOAnchorPosY(originPos.y, moveTime).SetEase(Ease.OutBack, 1.7f));
        seq.Join(rect.DOScale(1f, moveTime).SetEase(Ease.OutBack));

        // ===== 停留 =====
        seq.AppendInterval(stayTime);

        // ===== 退场 =====
        seq.Append(canvasGroup.DOFade(0, fadeOutTime));
        seq.Join(rect.DOAnchorPosY(originPos.y + moveOffset, fadeOutTime));
        seq.Join(rect.DOScale(0.9f, fadeOutTime));

        await seq.AsyncWaitForCompletion();

        gameObject.SetActive(false);
    }

    // ===== 防叠加 =====
    private void Kill()
    {
        if (seq != null && seq.IsActive())
        {
            seq.Kill();
        }
    }
}