using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Inspection
{
    /// <summary>
    /// 问题填写面板：打开、校验、提交、取消、错误提示。
    /// 提交成功后调用 MarkerPlacementController.CommitPending()，取消时 Delete 待提交标记。
    /// </summary>
    public class IssueFormController : MonoBehaviour
    {
        [Header("后端地址（真机改成电脑局域网 IP）")]
        public string baseUrl = "http://127.0.0.1:8080";

        [Header("登录账号（Unity 端以巡检员身份自动登录）")]
        public string loginUsername = "inspector";
        public string loginPassword = "inspector123";

        [Header("面板")]
        public GameObject panel;
        public InputField titleInput;
        public InputField descInput;
        public Text positionText;
        public Text hintText;
        public Text priorityLabel;
        public Button submitButton;
        public Button cancelButton;
        public Text submitLabel;

        [Header("优先级")]
        public Button lowButton;
        public Button mediumButton;
        public Button highButton;

        [Header("引用")]
        public ToastUI toast;
        public MarkerPlacementController placement;

        static readonly Color ButtonNormal = new Color(0.23f, 0.25f, 0.30f, 1f);
        static readonly Color ButtonSelected = new Color(0.95f, 0.66f, 0.16f, 1f);
        static readonly Color LabelSelected = new Color(0.12f, 0.12f, 0.12f, 1f);
        static readonly Color LabelNormal = new Color(0.90f, 0.90f, 0.90f, 1f);

        string priority = "medium";
        bool submitting;
        Vector3 pendingPosition;
        Vector3 pendingNormal = Vector3.up;

        void Awake()
        {
            if (placement == null) placement = FindObjectOfType<MarkerPlacementController>();
            if (toast == null) toast = FindObjectOfType<ToastUI>();

            // 注意：Button.onClick.AddListener 加的是“运行时监听器”，不会写进场景文件。
            // 所以事件必须在这里（运行时）绑定，而不是在编辑器里生成场景时绑定。
            Bind(submitButton, Submit);
            Bind(cancelButton, Cancel);
            Bind(lowButton, SetPriorityLow);
            Bind(mediumButton, SetPriorityMedium);
            Bind(highButton, SetPriorityHigh);

            // 兜底：即使 Selectable 自己的点击处理没生效，也强制聚焦输入框
            EnsureClickFocus(titleInput);
            EnsureClickFocus(descInput);

            if (panel != null) panel.SetActive(false);

            // Unity 端以“巡检员”身份自动登录；上报时会带上这个 token
            StartCoroutine(LoginRoutine());
        }

        System.Collections.IEnumerator LoginRoutine()
        {
            yield return AuthClient.Login(
                baseUrl,
                loginUsername,
                loginPassword,
                () => Debug.Log("[巡检] 已登录：" + AuthClient.DisplayName + "（" + AuthClient.Role + "）"),
                error =>
                {
                    Debug.LogWarning("[巡检] 登录失败：" + error);
                    if (toast != null) toast.Show("登录失败：" + error, ToastUI.Error);
                });
        }

        /// <summary>在运行时给输入框加一个“点击即聚焦”的兜底处理。</summary>
        static void EnsureClickFocus(InputField field)
        {
            if (field == null) return;

            var trigger = field.GetComponent<EventTrigger>();
            if (trigger == null) trigger = field.gameObject.AddComponent<EventTrigger>();

            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener(_ =>
            {
                if (!field.isFocused) field.ActivateInputField();
            });
            trigger.triggers.Add(entry);
        }

        static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                Debug.LogWarning("[巡检] 有按钮没接上，检查 IssueFormController 的引用");
                return;
            }
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        void Update()
        {
            if (panel == null || !panel.activeSelf) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cancel();
                return;
            }

            // 标题框里按回车直接提交，省一次鼠标
            bool enter = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
            if (enter && titleInput != null && titleInput.isFocused)
            {
                Submit();
            }
        }

        public bool IsOpen => panel != null && panel.activeSelf;

        /// <summary>射线命中新位置时调用：打开面板并显示坐标。</summary>
        public void OpenWithPosition(Vector3 worldPosition, Vector3 surfaceNormal = default)
        {
            pendingPosition = worldPosition;
            pendingNormal = surfaceNormal == default ? Vector3.up : surfaceNormal;

            if (panel != null) panel.SetActive(true);
            if (positionText != null)
            {
                positionText.text = string.Format("标记位置：x={0:F2}  y={1:F2}  z={2:F2}",
                    worldPosition.x, worldPosition.y, worldPosition.z);
            }
            if (hintText != null) hintText.text = "";
            if (titleInput != null) titleInput.text = "";
            if (descInput != null) descInput.text = "";

            SetPriority("medium");
            SetSubmitting(false);
        }

        public void SetPriority(string value)
        {
            priority = value;
            Paint(lowButton, value == "low");
            Paint(mediumButton, value == "medium");
            Paint(highButton, value == "high");

            if (priorityLabel != null)
            {
                priorityLabel.text = "优先级（当前：" + value + "）";
            }
        }

        /// <summary>表单没关就点场景时，给一句提示，而不是默默无反应。</summary>
        public void NotifyAlreadyOpen()
        {
            if (toast != null) toast.Show("请先提交或取消当前问题", ToastUI.Info);
        }

        public void SetPriorityLow() => SetPriority("low");
        public void SetPriorityMedium() => SetPriority("medium");
        public void SetPriorityHigh() => SetPriority("high");

        static void Paint(Button button, bool selected)
        {
            if (button == null) return;

            var image = button.GetComponent<Image>();
            if (image != null) image.color = selected ? ButtonSelected : ButtonNormal;

            var label = button.GetComponentInChildren<Text>();
            if (label != null) label.color = selected ? LabelSelected : LabelNormal;
        }

        /// <summary>提交按钮。</summary>
        public void Submit()
        {
            if (submitting) return;

            string title = (titleInput != null ? titleInput.text : "").Trim();
            if (string.IsNullOrEmpty(title))
            {
                if (hintText != null) hintText.text = "标题不能为空";
                if (toast != null) toast.Show("标题不能为空", ToastUI.Error);
                return;
            }

            var payload = new CreateIssueRequest
            {
                title = title,
                description = (descInput != null ? descInput.text : "").Trim(),
                priority = priority,
                position = PositionDto.From(pendingPosition),
                normal = PositionDto.From(pendingNormal)
            };

            SetSubmitting(true);
            if (hintText != null) hintText.text = "";
            StartCoroutine(Send(payload));
        }

        IEnumerator Send(CreateIssueRequest payload)
        {
            yield return IssueApiClient.CreateIssue(
                baseUrl,
                payload,
                issue =>
                {
                    SetSubmitting(false);
                    if (placement != null) placement.CommitPending();
                    if (panel != null) panel.SetActive(false);
                    if (toast != null) toast.Show("上报成功：" + issue.title, ToastUI.Success);
                },
                error =>
                {
                    // 失败时保留标记和表单内容，让用户能直接重试
                    SetSubmitting(false);
                    if (hintText != null) hintText.text = error;
                    if (toast != null) toast.Show("上报失败：" + error, ToastUI.Error);
                });
        }

        /// <summary>取消按钮：移除未保存的标记。</summary>
        public void Cancel()
        {
            if (submitting) return;

            if (placement != null) placement.CancelPending();
            if (panel != null) panel.SetActive(false);
            if (toast != null) toast.Show("已取消", ToastUI.Info);
        }

        void SetSubmitting(bool value)
        {
            submitting = value;
            if (submitButton != null) submitButton.interactable = !value;
            if (cancelButton != null) cancelButton.interactable = !value;
            if (submitLabel != null) submitLabel.text = value ? "提交中…" : "提交";
        }
    }
}
