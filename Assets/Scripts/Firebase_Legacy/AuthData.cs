using System;

[Serializable]
public class AuthRequest_
{
    public string email;
    public string password;
    public bool returnSecureToken = true;
}

[Serializable]
public class AuthResponse_
{
    public string localId;     // 用户UID
    public string idToken;     // 登录Token
    public string email;
    public string refreshToken;
    public string expiresIn;
}

[Serializable]
public class ErrorResponse
{
    public ErrorBody error;
}

[Serializable]
public class ErrorBody
{
    public string message;
}