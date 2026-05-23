using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
#if  UNITY_EDITOR
using UnityEditor;
#endif

public class SceneLoadManager : MonoBehaviour
{
    private static SceneLoadManager instance;
    public static SceneLoadManager Instance => instance;

    [Header("Fade UI")]
    public CanvasGroup fadeCanvas;
    public float fadeDuration = 0.5f;

    [Header("Loading UI")]
    public Slider ProgressSlider;
    public TextMeshProUGUI ProgressText;

    [Header("进度控制")]
    public float smoothSpeed = 0.8f;
    public float minLoadTime = 1.0f;

    private bool isLoading = false;

    // ================= 单例 =================
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        //DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (fadeCanvas != null)
        {
            fadeCanvas.alpha = 0;
            fadeCanvas.blocksRaycasts = false;
        }
    }

    // ================= 最终版加载 =================
    public async UniTask LoadSceneAsync(int sceneOrder)
    {
        if (isLoading) return;
        isLoading = true;

        await FadeIn();

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneOrder);
        op.allowSceneActivation = false;

        float displayProgress = 0f;
        float timer = 0f;

        while (!op.isDone)
        {
            timer += Time.deltaTime;

            // 1️⃣ 假进度映射
            float targetProgress = Mathf.Clamp01(op.progress / 0.9f);

            // 2️⃣ 防止假进度过快
            targetProgress = Mathf.Min(targetProgress, 0.95f);

            // 3️⃣ 平滑推进
            displayProgress = Mathf.MoveTowards(
                displayProgress,
                targetProgress,
                Time.deltaTime * smoothSpeed
            );

            UpdateProgressUI(displayProgress);

            // 4️⃣ 条件满足才进入下一阶段
            if (op.progress >= 0.9f && timer >= minLoadTime)
            {
                break;
            }

            await UniTask.Yield();
        }

        // 5️⃣ 平滑补满到100%
        displayProgress = await SmoothFillToOne(displayProgress);

        op.allowSceneActivation = true;

        await UniTask.WaitUntil(() => op.isDone);

        await FadeOut();

        isLoading = false;
    }

    // ================= 平滑补满 =================
    async UniTask<float> SmoothFillToOne(float value)
    {
        while (value < 1f)
        {
            value = Mathf.MoveTowards(value, 1f, Time.deltaTime);
            UpdateProgressUI(value);
            await UniTask.Yield();
        }

        UpdateProgressUI(1f);
        return 1f;
    }

    // ================= UI更新 =================
    void UpdateProgressUI(float value)
    {
        if (ProgressSlider != null)
        {
            ProgressSlider.DOValue(value, 0.2f).SetEase(Ease.OutQuad);
        }

        if (ProgressText != null)
        {
            ProgressText.text = $"{Mathf.RoundToInt(value * 100)}%";
        }
    }

    // ================= Fade =================
    async UniTask FadeIn()
    {
        if (fadeCanvas == null) return;

        fadeCanvas.blocksRaycasts = true;

        await fadeCanvas
            .DOFade(0.95f, fadeDuration)
            .SetEase(Ease.InOutQuad)
            .AsyncWaitForCompletion();
    }

    async UniTask FadeOut()
    {
        if (fadeCanvas == null) return;

        await fadeCanvas
            .DOFade(0f, fadeDuration)
            .SetEase(Ease.InOutQuad)
            .AsyncWaitForCompletion();

        fadeCanvas.blocksRaycasts = false;
    }

    public void ExitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
        #endif
    }
}