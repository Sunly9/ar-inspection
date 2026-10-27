using System.Collections;
using UnityEngine;

namespace Inspection
{
    /// <summary>
    /// 进场景时从后端拉问题列表，按记录的 x/y/z 把历史标记恢复出来。
    /// 这是本题的加分项：证明“固定场景的世界坐标可以精确还原”（AR 做不到，见 README 第 8 节 Q2）。
    ///
    /// 规则：
    ///   · 红色 = 待处理，橙色 = 处理中，绿色 = 已解决
    ///   · 坐标非法（NaN / 越界）的记录直接跳过，不让脏数据炸场景
    ///   · 后端没起来就静默跳过，不弹一堆错误
    /// </summary>
    public class MarkerRestorer : MonoBehaviour
    {
        [Header("引用")]
        public IssueFormController form;

        [Header("选项")]
        [Tooltip("是否连已解决的标记一起恢复")]
        public bool includeResolved = true;

        [Tooltip("恢复失败时是否在屏幕上提示")]
        public bool showError = true;

        public ToastUI toast;

        void Start()
        {
            if (form == null) form = FindObjectOfType<IssueFormController>();
            if (toast == null) toast = FindObjectOfType<ToastUI>();
            StartCoroutine(RestoreRoutine());
        }

        IEnumerator RestoreRoutine()
        {
            // 列表接口需要登录：等 IssueFormController 的自动登录完成，最多等 5 秒
            float waited = 0f;
            while (!AuthClient.IsLoggedIn && waited < 5f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!AuthClient.IsLoggedIn)
            {
                Debug.LogWarning("[巡检] 未登录，跳过历史标记恢复");
                yield break;
            }

            string baseUrl = form != null ? form.baseUrl : "http://127.0.0.1:8080";

            IssueDto[] issues = null;
            string error = null;

            yield return IssueApiClient.GetIssues(
                baseUrl,
                list => issues = list,
                err => error = err);

            if (error != null)
            {
                Debug.LogWarning("[巡检] 恢复历史标记失败：" + error);
                if (showError && toast != null) toast.Show("历史标记加载失败：" + error, ToastUI.Error);
                yield break;
            }

            if (issues == null) yield break;

            int restored = 0;
            foreach (var issue in issues)
            {
                if (issue == null || issue.position == null) continue;
                if (!includeResolved && issue.status == "resolved") continue;

                Vector3 position = new Vector3(issue.position.x, issue.position.y, issue.position.z);
                if (!IsUsable(position)) continue;

                // 用保存时记录的法线还原朝向；老数据没有 normal 就按“朝上”处理
                Vector3 normal = Vector3.up;
                if (issue.normal != null)
                {
                    var n = new Vector3(issue.normal.x, issue.normal.y, issue.normal.z);
                    if (n.sqrMagnitude > 0.0001f) normal = n.normalized;
                }

                restored++;
                var marker = IssueMarker.Create(
                    position,
                    IssueMarker.ColorForStatus(issue.status),
                    "#" + restored + " " + issue.priority,
                    false,
                    true,
                    normal);
                marker.name = "SavedMarker_" + restored;
                marker.SetIssue(issue);
            }

            Debug.Log("[巡检] 已恢复 " + restored + " 个历史标记");
            if (restored > 0 && toast != null)
            {
                toast.Show("已恢复 " + restored + " 个历史标记", ToastUI.Info);
            }
        }

        /// <summary>坐标在合理范围内才使用，避免脏数据把标记放到看不到的地方。</summary>
        static bool IsUsable(Vector3 p)
        {
            if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z)) return false;
            if (float.IsInfinity(p.x) || float.IsInfinity(p.y) || float.IsInfinity(p.z)) return false;
            return Mathf.Abs(p.x) < 100f && Mathf.Abs(p.y) < 100f && Mathf.Abs(p.z) < 100f;
        }
    }
}
