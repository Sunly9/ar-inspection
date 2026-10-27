using UnityEngine;
using UnityEngine.EventSystems;

namespace Inspection
{
    /// <summary>
    /// 射线放置标记：
    /// 1) 先判断鼠标是否点在 UI 上，是就完全忽略（题目硬性要求）
    /// 2) 从相机沿鼠标位置发射射线，用 hit.point（不是物体中心）放置标记
    /// 3) 同一次点击最多产生一个待提交标记，重复点击只移动它
    /// </summary>
    public class MarkerPlacementController : MonoBehaviour
    {
        [Header("射线")]
        public Camera targetCamera;
        public float maxDistance = 40f;
        public LayerMask clickableMask = ~0;

        [Header("引用")]
        public IssueFormController form;
        public IssueDetailPanel detail;

        IssueMarker pending;

        // 在“鼠标按下”这一帧记录指针是否压在 UI 上。
        // 不能等到回调把面板关掉之后再判断，否则会把“点取消”误判成“点场景”。
        bool pointerDownOverUI;

        public bool HasPending => pending != null;

        void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (form == null) form = FindObjectOfType<IssueFormController>();
            if (detail == null) detail = FindObjectOfType<IssueDetailPanel>();
        }

        void Update()
        {
            if (targetCamera == null || form == null) return;

            if (Input.GetMouseButtonDown(0))
            {
                pointerDownOverUI = IsPointerOverUI();
            }

            // 只有左键按下才算；按住右键转视角时不放置
            if (!Input.GetMouseButtonDown(0)) return;
            if (Input.GetMouseButton(1)) return;
            if (pointerDownOverUI) return;

            // 表单还开着时不再放新标记：点「取消」后不会立刻又冒出来一个
            if (form.IsOpen)
            {
                form.NotifyAlreadyOpen();
                return;
            }

            Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, clickableMask, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            // 点到历史标记 → 看详情，不放置新标记
            var marker = hit.collider.GetComponentInParent<IssueMarker>();
            if (marker != null && marker.Issue != null)
            {
                if (detail != null) detail.Show(marker.Issue);
                return;
            }

            PlaceAt(hit.point, hit.normal);
        }

        static bool IsPointerOverUI()
        {
            var es = EventSystem.current;
            return es != null && es.IsPointerOverGameObject();
        }

        void PlaceAt(Vector3 point, Vector3 normal)
        {
            if (detail != null) detail.Hide();
            if (pending == null)
            {
                pending = CreateMarker(point, normal);
            }
            else
            {
                pending.MoveTo(point, normal);
            }
            form.OpenWithPosition(point, normal);
        }

        IssueMarker CreateMarker(Vector3 point, Vector3 normal)
        {
            return IssueMarker.Create(point, IssueMarker.PendingColor, string.Empty, true, false, normal);
        }

        /// <summary>取消填写：把未保存的标记删掉。</summary>
        public void CancelPending()
        {
            if (pending != null)
            {
                Destroy(pending.gameObject);
                pending = null;
            }
        }

        /// <summary>提交成功：标记留下并变绿。</summary>
        public void CommitPending()
        {
            if (pending != null)
            {
                pending.Commit();
                pending = null;
            }
        }
    }
}
