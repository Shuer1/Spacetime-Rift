using UnityEngine;
using TMPro;

public class LoginManager_ : MonoBehaviour
{
    [Header("Reference")]
    public FirebaseAuthService authService;

    [Header("UI")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_Text logText;


    void Start()
    {
        
    }
    // 注册
    public void OnClickRegister()
    {
        StartCoroutine(authService.Register(
            emailInput.text,
            passwordInput.text,
            OnAuthSuccess,
            OnAuthFail));
    }

    // 登录
    public void OnClickLogin()
    {
        StartCoroutine(authService.Login(
            emailInput.text,
            passwordInput.text,
            OnAuthSuccess,
            OnAuthFail));
    }

    private void OnAuthSuccess(AuthResponse_ res)
    {
        logText.text = "登录成功\nUID: " + res.localId;

        // 保存Token（自动登录用）
        PlayerPrefs.SetString("token", res.idToken);
        PlayerPrefs.SetString("uid", res.localId);

        Debug.Log("Token: " + res.idToken);
    }

    private void OnAuthFail(string errorCode)
    {
        string msg = FirebaseErrorHandler.GetMessage(errorCode);
        logText.text = msg;

        Debug.LogError("登录失败: " + errorCode);
    }
}