using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections;

public class FirebaseAuthService : MonoBehaviour
{
    private const string DefaultApiKey = "AIzaSyCSWQFSHnIJ_erxSMxJw5tzETjfRtwBB-Q";
    [Header("Firebase")]
    [SerializeField] private string apiKey = "";

    private string signUpUrl;
    private string signInUrl;

    void Awake()
    {
        if (apiKey == null)
            apiKey = DefaultApiKey;

        signUpUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={apiKey}";
        signInUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}";
    }

    private IEnumerator SendRequest(string url, AuthRequest_ data,
        System.Action<AuthResponse_> onSuccess,
        System.Action<string> onFail)
    {
        string json = JsonUtility.ToJson(data);

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);

            www.uploadHandler = new UploadHandlerRaw(body);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AuthResponse_ res = JsonUtility.FromJson<AuthResponse_>(www.downloadHandler.text);
                onSuccess?.Invoke(res);
            }
            else
            {
                string errorMsg = ParseError(www.downloadHandler.text);
                onFail?.Invoke(errorMsg);
            }
        }
    }

    private string ParseError(string json)
    {
        try
        {
            ErrorResponse err = JsonUtility.FromJson<ErrorResponse>(json);
            return err.error.message;
        }
        catch
        {
            return "未知错误";
        }
    }

    public IEnumerator Register(string email, string password,
        System.Action<AuthResponse_> onSuccess,
        System.Action<string> onFail)
    {
        yield return SendRequest(signUpUrl,
            new AuthRequest_ { email = email, password = password },
            onSuccess, onFail);
    }

    public IEnumerator Login(string email, string password,
        System.Action<AuthResponse_> onSuccess,
        System.Action<string> onFail)
    {
        yield return SendRequest(signInUrl,
            new AuthRequest_ { email = email, password = password },
            onSuccess, onFail);
    }
}