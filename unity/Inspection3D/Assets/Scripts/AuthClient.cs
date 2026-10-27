using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// JsonUtility 通过反射填字段，编译器看不到赋值，这里关掉“字段未赋值”的提示
#pragma warning disable 0649

namespace Inspection
{
    /// <summary>
    /// 登录与 token 管理。
    /// Unity 端作为一个“巡检员客户端”启动时自动登录，后续请求都带上 Authorization。
    /// </summary>
    public static class AuthClient
    {
        public static string Token { get; private set; }
        public static string DisplayName { get; private set; }
        public static string Role { get; private set; }

        public static bool IsLoggedIn => !string.IsNullOrEmpty(Token);

        [Serializable]
        class LoginRequest
        {
            public string username;
            public string password;
        }

        [Serializable]
        class LoginUser
        {
            public string username;
            public string displayName;
            public string role;
        }

        [Serializable]
        class LoginData
        {
            public string token;
            public LoginUser user;
        }

        [Serializable]
        class LoginEnvelope
        {
            public LoginData data;
        }

        public static IEnumerator Login(string baseUrl, string username, string password,
            Action onSuccess, Action<string> onError)
        {
            var payload = new LoginRequest { username = username, password = password };
            string url = baseUrl.TrimEnd('/') + "/api/login";

            using (var req = new UnityWebRequest(url, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload)));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = Mathf.CeilToInt(IssueApiClient.TimeoutSeconds);

                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke(DescribeLoginFailure(req));
                    yield break;
                }

                LoginEnvelope envelope = null;
                try
                {
                    envelope = JsonUtility.FromJson<LoginEnvelope>(req.downloadHandler.text);
                }
                catch (Exception e)
                {
                    onError?.Invoke("登录返回解析失败：" + e.Message);
                    yield break;
                }

                if (envelope?.data == null || string.IsNullOrEmpty(envelope.data.token))
                {
                    onError?.Invoke("登录返回的数据不完整");
                    yield break;
                }

                Token = envelope.data.token;
                DisplayName = envelope.data.user != null ? envelope.data.user.displayName : username;
                Role = envelope.data.user != null ? envelope.data.user.role : string.Empty;
                onSuccess?.Invoke();
            }
        }

        static string DescribeLoginFailure(UnityWebRequest req)
        {
            string text = req.downloadHandler != null ? req.downloadHandler.text : null;
            if (!string.IsNullOrEmpty(text) && text.TrimStart().StartsWith("{"))
            {
                try
                {
                    var err = JsonUtility.FromJson<ErrorEnvelope>(text);
                    if (err?.error != null && !string.IsNullOrEmpty(err.error.message))
                    {
                        return err.error.message;
                    }
                }
                catch (Exception) { /* 走下面的兜底 */ }
            }

            if (req.result == UnityWebRequest.Result.ConnectionError)
            {
                return "无法连接后端，请确认 Go 服务已启动（" + req.url + "）";
            }
            return "登录失败：" + req.responseCode;
        }
    }
}
