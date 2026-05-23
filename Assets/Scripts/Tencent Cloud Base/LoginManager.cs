using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using DG.Tweening;

public class LoginManager : MonoBehaviour
{
    [Header("Auto Login Switcher")]
    public bool allowAutoLogin = false;
    [Header("UI:账号密码输入框")]
    public TMP_InputField accountInput;
    public TMP_InputField passwordInput;
    [Header("UI:登录注册按钮")]
    public Button loginBtn;
    public Button registerBtn;
    [Header("登陆方式按钮和提交按钮")]
    public Button LoginByUserBtn;
    public Button LoginByMailBtn;
    public Button confirmBtn; // ⭐ 提交按钮（必须绑定）

    public TextMeshProUGUI messageText;

    [Header("UI:登录注册面板")]
    public GameObject Panel_Login;
    public GameObject Panel_Login_sucess;
    public Button newGameBtn;
    public Button continueBtn;
    public GameObject AuthWayRoot;
    public GameObject InputFieldRoot;

    private const string LOGIN_SUCCESS_KEY = "LOGIN_SUCCESS";

    // ===== 状态机 =====
    private enum AuthMode { None, Login, Register }
    private enum AuthType { None, Username, Email }

    private AuthMode currentMode = AuthMode.None;
    private AuthType currentType = AuthType.None;

    private bool isRequesting = false;

    // ===== 正则（与后端统一）=====
    private const string USERNAME_REGEX = @"^[a-zA-Z0-9_]{3,16}$";
    private const string EMAIL_REGEX = @"^[^\s@]+@[^\s@]+\.[^\s@]+$";

    void Start()
    {
        loginBtn.onClick.AddListener(OnClickLogin);
        registerBtn.onClick.AddListener(OnClickRegister);
        LoginByUserBtn.onClick.AddListener(OnLoginByUser);
        LoginByMailBtn.onClick.AddListener(OnLoginByMail);
        confirmBtn.onClick.AddListener(OnSubmit);

        newGameBtn.onClick.AddListener(LoadNewScene);
        continueBtn.onClick.AddListener(ContinueScene);


        CloseAllUI();

        if(allowAutoLogin)
        {
            CloudBaseAuthManager.Instance.CheckLogin(success => {
                if (success) 
                {
                    OnLoginSuccess();
                    Debug.Log("检测到已存在的账号token，自动登录成功");
                } 
                else 
                {
                    Debug.Log("自动登录失败");
                }
            });
        }
    }

    // ================== 第一步 ==================
    void OnClickLogin()
    {
        SFXManager.Instance.PlayEventSFX("Click");
        ClearInputFields();

        currentMode = AuthMode.Login;
        OpenAuthWayRoot();
        messageText.text = "请选择登录方式";
    }

    void OnClickRegister()
    {
        SFXManager.Instance.PlayEventSFX("Click");
        ClearInputFields();

        currentMode = AuthMode.Register;
        OpenAuthWayRoot();
        messageText.text = "请选择注册方式";
    }

    // ================== 第二步 ==================
    void OnLoginByUser()
    {
        currentType = AuthType.Username;
        ClearInputFields();
        OpenInputFieldRoot();
        messageText.text = "请输入用户名和密码";
    }

    void OnLoginByMail()
    {
        currentType = AuthType.Email;
        ClearInputFields();
        OpenInputFieldRoot();
        messageText.text = "请输入邮箱和密码";
    }

    // ================== 第三步（核心）==================
    void OnSubmit()
    {
        if (isRequesting) return;

        SFXManager.Instance.PlayEventSFX("Click");

        string account = accountInput.text.Trim();
        string password = passwordInput.text.Trim();

        // ===== 基础校验 =====
        if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(password))
        {
            messageText.text = "请输入账号和密码";
            return;
        }

        // ===== 分流校验 =====
        if (currentType == AuthType.Email && !IsEmail(account))
        {
            messageText.text = "请输入正确的邮箱格式";
            return;
        }

        if (currentType == AuthType.Username && IsEmail(account))
        {
            messageText.text = "请输入正确的用户名格式";
            return;
        }

        // ===== 注册额外校验 =====
        if (currentMode == AuthMode.Register)
        {
            if (currentType == AuthType.Username && !IsUsername(account))
            {
                messageText.text = "用户名需3-16位字母/数字/下划线";
                return;
            }

            if (password.Length < 6)
            {
                messageText.text = "密码至少6位";
                return;
            }
        }

        isRequesting = true;
        confirmBtn.interactable = false;

        // ===== 请求 =====
        if (currentMode == AuthMode.Login)
        {
            CloudBaseAuthManager.Instance.Login(account, password, OnAuthCallback);
        }
        else if (currentMode == AuthMode.Register)
        {
            CloudBaseAuthManager.Instance.Register(account, password, (success, msg) =>
            {
                isRequesting = false;
                confirmBtn.interactable = true;

                messageText.text = msg;

                if (success)
                {
                    // 注册成功 → 自动切登录模式
                    currentMode = AuthMode.Login;
                    messageText.text = "注册成功，请登录";
                }
            });
        }
    }

    // ================== 回调 ==================
    void OnAuthCallback(bool success, string msg)
    {
        isRequesting = false;
        confirmBtn.interactable = true;

        messageText.text = msg;

        if (success)
        {
            OnLoginSuccess();
        }
    }

    // ================== 成功 ==================
    void OnLoginSuccess()
    {
        Debug.Log("登录成功");
        UIAnimationManager.Instance.Play("LoginSuccess");

        Panel_Login_sucess.SetActive(true);
        Panel_Login.SetActive(false);

        CloseAllUI();

        SFXManager.Instance.PlayEventSFX(LOGIN_SUCCESS_KEY);
    }

    // ================== UI控制 ==================
    void OpenAuthWayRoot()
    {
        AuthWayRoot.SetActive(true);
        InputFieldRoot.SetActive(false);
    }

    void OpenInputFieldRoot()
    {
        AuthWayRoot.SetActive(false);
        InputFieldRoot.SetActive(true);
    }

    void CloseAllUI()
    {
        AuthWayRoot.SetActive(false);
        InputFieldRoot.SetActive(false);
    }

    // ================== 工具函数 ==================
    bool IsEmail(string input)
    {
        return Regex.IsMatch(input, EMAIL_REGEX);
    }

    bool IsUsername(string input)
    {
        return Regex.IsMatch(input, USERNAME_REGEX);
    }

    void ClearInputFields()
    {
        accountInput.text = "";
        passwordInput.text = "";
    }

    void LoadNewScene()
    {
        Load().Forget();
    }

    void ContinueScene()
    {
        Debug.Log("ContinueScene finished");
    }

    async UniTaskVoid Load()
    {
        SFXManager.Instance.StopAllSFX();
        await SceneLoadManager.Instance.LoadSceneAsync(2);
    }
}