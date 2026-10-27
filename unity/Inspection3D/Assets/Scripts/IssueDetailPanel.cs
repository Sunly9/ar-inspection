using UnityEngine;
using UnityEngine.UI;

namespace Inspection
{
    /// <summary>
    /// 点击历史标记后弹出的详情面板：标题、优先级、状态、坐标、上报时间、描述。
    /// 只读，不参与提交。
    /// </summary>
    public class IssueDetailPanel : MonoBehaviour
    {
        public GameObject panel;
        public Text titleText;
        public Text metaText;
        public Text positionText;
        public Text timeText;
        public Text descText;
        public Button closeButton;

        void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Hide);
            }
            if (panel != null) panel.SetActive(false);
        }

        void Update()
        {
            if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) Hide();
        }

        public void Show(IssueDto issue)
        {
            if (issue == null) return;
            if (panel != null) panel.SetActive(true);

            if (titleText != null) titleText.text = issue.title;
            if (metaText != null)
            {
                metaText.text = "优先级：" + PriorityLabel(issue.priority) + "　状态：" + StatusLabel(issue.status);
                metaText.color = IssueMarker.ColorForStatus(issue.status);
            }
            if (positionText != null && issue.position != null)
            {
                positionText.text = string.Format("位置：x={0:F2}　y={1:F2}　z={2:F2}",
                    issue.position.x, issue.position.y, issue.position.z);
            }
            if (timeText != null) timeText.text = "上报时间：" + issue.createdAt;
            if (descText != null)
            {
                descText.text = string.IsNullOrEmpty(issue.description) ? "（没有填写描述）" : issue.description;
            }
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        static string PriorityLabel(string priority)
        {
            switch (priority)
            {
                case "high": return "高";
                case "medium": return "中";
                case "low": return "低";
                default: return priority;
            }
        }

        static string StatusLabel(string status)
        {
            switch (status)
            {
                case "open": return "待处理";
                case "in_progress": return "处理中";
                case "resolved": return "已解决";
                default: return status;
            }
        }
    }
}
