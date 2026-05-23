using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

#region 数据结构

[Serializable]
public class AuthRequest {
    public string action;
    public string username;
    public string password;
    public string token;
}

[Serializable]
public class AuthResponse {
    public int code;
    public string msg;
    public string token;
}

#endregion

public class CloudBaseAuthManager : MonoBehaviour {

    private const string BASE_URL = "https://spacetime-rift-8gm72itl01daf01c-1418930762.ap-shanghai.app.tcloudbase.com/CloudAuth"; // 云函数地址

    public static CloudBaseAuthManager Instance { get; private set; }

    void Awake() 
    {
        if (Instance == null)
            Instance = this;
        else 
            Destroy(gameObject);
    }

    // ==================== 注册 ====================
    public void Register(string username, string password, Action<bool, string> callback) {
        SendAuthRequest("register", username, password, callback);
    }

    // ==================== 登录 ====================
    public void Login(string username, string password, Action<bool, string> callback) {
        SendAuthRequest("login", username, password, callback);
    }

    // ==================== 自动登录 ====================
    public void CheckLogin(Action<bool> callback) {
        string savedToken = PlayerPrefs.GetString("TOKEN", "");

        if (string.IsNullOrEmpty(savedToken)) {
            callback(false);
            return;
        }

        AuthRequest request = new AuthRequest {
            action = "check",
            token = savedToken
        };

        StartCoroutine(PostRequest(request, (success, response) => {
            if (!success || response == null) {
                callback(false);
                return;
            }

            callback(response.code == 0);
        }));
    }

    // ==================== 核心请求 ====================
    private void SendAuthRequest(string action, string username, string password, Action<bool, string> callback) {

        AuthRequest request = new AuthRequest {
            action = action,
            username = username,
            password = password
        };

        StartCoroutine(PostRequest(request, (success, response) => {

            if (!success || response == null) {
                callback(false, "网络或解析错误");
                return;
            }

            if (response.code == 0) {

                // 登录成功才保存 token
                if (action == "login" && !string.IsNullOrEmpty(response.token)) {
                    PlayerPrefs.SetString("TOKEN", response.token);
                    PlayerPrefs.Save();
                }

                callback(true, response.msg);
            } else {
                callback(false, response.msg);
            }
        }));
    }

    // ==================== HTTP请求 ====================
    private System.Collections.IEnumerator PostRequest(AuthRequest data, Action<bool, AuthResponse> callback) {

        string json = JsonUtility.ToJson(data);
        Debug.Log("请求JSON: " + json);

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest www = new UnityWebRequest(BASE_URL, "POST")) {

            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError ||
                www.result == UnityWebRequest.Result.ProtocolError) {

                Debug.LogError("请求失败: " + www.error);
                Debug.LogError("返回内容: " + www.downloadHandler.text);

                callback(false, null);
            } else {

                Debug.Log("返回: " + www.downloadHandler.text);

                AuthResponse response = null;

                try {
                    response = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text);
                } catch (Exception e) {
                    Debug.LogError("JSON解析失败: " + e.Message);
                    callback(false, null);
                    yield break;
                }

                callback(true, response);
            }
        }
    }
}