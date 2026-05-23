using Fungus;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;


public class MenuUI : MonoBehaviour, IBaseUI
{
    public static MenuUI Instance { get; private set; }
    public GameObject menuPanel;
    public Button backInitSceneBtn;

     void Awake()
    {
        // 单例模式初始化
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        backInitSceneBtn.onClick.AddListener(BackLoad);
        menuPanel.SetActive(false);
    }

    public void Open()
    {
        menuPanel.SetActive(true);
        PlayerControlManager.Instance.Lock(ControlLockType.Menu);
    }

    public void Close()
    {
        menuPanel.SetActive(false);
        PlayerControlManager.Instance.Unlock(ControlLockType.Menu);
    }

    public bool IsOpen() => menuPanel.activeSelf;

    void BackLoad()
    {
        BackInitScene().Forget();
        SFXManager.Instance.StopAllSFX();
    }

    async UniTaskVoid BackInitScene()
    {
        await SceneLoadManager.Instance.LoadSceneAsync(0);
    }
}