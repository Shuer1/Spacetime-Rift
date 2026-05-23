using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

public class DamageText : MonoBehaviour
{
    [Header("Text Settings")]
    public TMP_Text damageText;
    public float defaultFontSize = 32f;
    public float moveDistance = 10f;
    public float fadeDuration = 1f;
    public float spread = 2f;

    private Vector3 startPosition;
    private Color startColor;
    private RectTransform rectTransform;

    // 当前是否正在播放动画
    public bool isAnimating;

    // UniTask CancellationToken
    private CancellationTokenSource cts;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (damageText == null)
        {
            damageText = GetComponentInChildren<TMP_Text>();
        }
    }

    /// <summary>
    /// 初始化伤害数字并播放动画
    /// </summary>
    public void Initialize(int damage, Vector3 worldPosition)
    {
        // 取消当前动画
        cts?.Cancel();
        cts = new CancellationTokenSource();

        isAnimating = true;

        if (damageText != null)
        {
            damageText.text = damage.ToString();
            damageText.color = Color.red;
            damageText.fontSize = defaultFontSize;
        }

        // 随机偏移位置（防止重叠）
        startPosition = worldPosition + new Vector3(
            UnityEngine.Random.Range(-spread, spread),
            UnityEngine.Random.Range(-spread, spread),
            0f
        );

        if (rectTransform != null)
        {
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(startPosition);
            screenPosition.x = Mathf.Clamp(screenPosition.x, 50, Screen.width - 50);
            screenPosition.y = Mathf.Clamp(screenPosition.y, 50, Screen.height - 50);
            rectTransform.position = screenPosition;
        }

        startColor = damageText != null ? damageText.color : Color.white;

        // 启动动画
        AnimateAndRecycleAsync(cts.Token).Forget();
    }

    /// <summary>
    /// 动画播放并回收到对象池（UniTask 版）
    /// </summary>
    private async UniTaskVoid AnimateAndRecycleAsync(CancellationToken token)
    {
        float elapsedTime = 0f;
        Vector3 start = rectTransform.position;
        Vector3 target = start + Vector3.up * moveDistance;

        try
        {
            while (elapsedTime < fadeDuration)
            {
                token.ThrowIfCancellationRequested();

                float t = elapsedTime / fadeDuration;

                // 移动
                if (rectTransform != null)
                    rectTransform.position = Vector3.Lerp(start, target, t);

                // 渐隐
                if (damageText != null)
                {
                    Color c = startColor;
                    c.a = 1f - t;
                    damageText.color = c;
                }

                elapsedTime += Time.unscaledDeltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        catch (OperationCanceledException)
        {
            // 动画被取消，直接回收
        }

        Recycle();
    }

    /// <summary>
    /// 回收该对象（交给对象池）
    /// </summary>
    private void Recycle()
    {
        isAnimating = false;
        cts?.Dispose();
        cts = null;

        // 重置透明度
        if (damageText != null)
        {
            Color c = damageText.color;
            c.a = 1f;
            damageText.color = c;
        }

        // 隐藏自身（不销毁）
        gameObject.SetActive(false);

        // 归还到对象池
        DamageTextManager.Instance?.ReturnToPool(this);
    }

    private void OnDisable()
    {
        // 如果对象被隐藏，确保取消动画
        cts?.Cancel();
    }
}