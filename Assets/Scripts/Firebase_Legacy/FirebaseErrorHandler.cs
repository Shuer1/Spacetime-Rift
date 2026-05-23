public static class FirebaseErrorHandler
{
    public static string GetMessage(string code)
    {
        switch (code)
        {
            case "EMAIL_EXISTS": return "账号已存在，请勿重复注册！";
            case "OPERATION_NOT_ALLOWED": return "未开启邮箱登录";
            case "TOO_MANY_ATTEMPTS_TRY_LATER": return "请求过多，请稍后再试";

            case "EMAIL_NOT_FOUND": return "用户不存在";
            case "INVALID_PASSWORD": return "密码错误";
            case "USER_DISABLED": return "账号被禁用";

            case "INVALID_EMAIL": return "邮箱格式错误";
            case "WEAK_PASSWORD": return "密码安全性低";

            default: return "错误：" + code;
        }
    }
}