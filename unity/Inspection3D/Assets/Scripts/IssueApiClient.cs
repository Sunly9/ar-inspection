using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Inspection
{
    /// <summary>
    /// 后端 HTTP 封装：只负责发请求、解析响应、把错误变成一句中文提示。
    /// 用协程 + UnityWebRequest，不依赖任何第三方 JSON 库。
    /// </summary>
    public static class IssueApiClient
    {
        public const float TimeoutSeconds = 10f;

        /// <summary>上报一条新问题。</summary>
        public static IEnumerator CreateIssue(
            string baseUrl,
            CreateIssueRequest payload,
            Action<IssueDto> onSuccess,
            Action<string> onError)
        {
            string url = baseUrl.TrimEnd('/') + "/api/issues";
            string json = JsonUtility.ToJson(payload);

            using (var req = new UnityWebRequest(url, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = Mathf.CeilToInt(TimeoutSeconds);

                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke(DescribeFailure(req));
                    yield break;
                }

                var body = ParseIssue(req.downloadHandler.text);
                if (body == null)
                {
                    onError?.Invoke("后端返回的数据无法解析");
                    yield break;
                }
                onSuccess?.Invoke(body);
            }
        }

        /// <summary>拉取问题列表（加分项：进场景时恢复标记会用到）。</summary>
        public static IEnumerator GetIssues(
            string baseUrl,
            Action<IssueDto[]> onSuccess,
            Action<string> onError)
        {
            string url = baseUrl.TrimEnd('/') + "/api/issues";

            using (var req = UnityWebRequest.Get(url))
            {
                req.timeout = Mathf.CeilToInt(TimeoutSeconds);
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke(DescribeFailure(req));
                    yield break;
                }

                IssueListEnvelope env = null;
                try
                {
                    env = JsonUtility.FromJson<IssueListEnvelope>(req.downloadHandler.text);
                }
                catch (Exception e)
                {
                    onError?.Invoke("解析列表失败：" + e.Message);
                    yield break;
                }

                if (env?.data?.items == null)
                {
                    onError?.Invoke("后端返回的数据格式不正确");
                    yield break;
                }
                onSuccess?.Invoke(env.data.items.ToArray());
            }
        }

        static IssueDto ParseIssue(string text)
        {
            try
            {
                var env = JsonUtility.FromJson<IssueEnvelope>(text);
                return env?.data;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>把网络错误翻译成用户能看懂的一句话。</summary>
        static string DescribeFailure(UnityWebRequest req)
        {
            // 后端返回了 4xx/5xx：优先用后端给的 message
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
                catch (Exception) { /* 忽略，走下面的兜底 */ }
            }

            if (req.result == UnityWebRequest.Result.ConnectionError)
            {
                return "无法连接后端，请确认 Go 服务已启动（" + req.url + "）";
            }
            return "请求失败：" + req.responseCode + " " + req.error;
        }
    }
}
