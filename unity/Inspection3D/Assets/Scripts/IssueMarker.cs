using UnityEngine;

namespace Inspection
{
    /// <summary>
    /// 场景里的问题标记：立杆 + 球头 + 悬浮标签。
    /// 两种用途共用一套外观：
    ///   1) 待提交标记（黄色、上下浮动）
    ///   2) 从后端恢复的历史标记（按状态上色、静止）
    /// 注意：标记的所有 Collider 都销毁，否则会挡住鼠标射线。
    /// </summary>
    public class IssueMarker : MonoBehaviour
    {
        public static readonly Color PendingColor = new Color(1f, 0.78f, 0.15f);
        public static readonly Color OpenColor = new Color(0.90f, 0.30f, 0.30f);
        public static readonly Color InProgressColor = new Color(0.95f, 0.65f, 0.15f);
        public static readonly Color ResolvedColor = new Color(0.32f, 0.80f, 0.45f);

        static Font cachedFont;

        Vector3 anchor;
        Material material;
        Transform labelTransform;
        bool bobbing;
        Camera cameraCache;

        /// <summary>历史标记会挂上对应的问题数据，点击时用来看详情。</summary>
        public IssueDto Issue { get; private set; }

        public void SetIssue(IssueDto issue)
        {
            Issue = issue;
        }

        public static Color ColorForStatus(string status)
        {
            switch (status)
            {
                case "open": return OpenColor;
                case "in_progress": return InProgressColor;
                case "resolved": return ResolvedColor;
                default: return PendingColor;
            }
        }

        public static IssueMarker Create(Vector3 worldPosition, Color color, string labelText, bool bobbing, bool clickable = false)
        {
            var root = new GameObject(bobbing ? "PendingMarker" : "SavedMarker");
            root.transform.position = worldPosition;

            var material = new Material(Shader.Find("Standard")) { color = color };

            // 立杆
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(root.transform, false);
            pole.transform.localScale = new Vector3(0.03f, 0.18f, 0.03f);
            pole.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            pole.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(pole.GetComponent<Collider>());

            // 球头
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Ball";
            ball.transform.SetParent(root.transform, false);
            ball.transform.localScale = Vector3.one * 0.12f;
            ball.transform.localPosition = new Vector3(0f, 0.38f, 0f);
            ball.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(ball.GetComponent<Collider>());

            var marker = root.AddComponent<IssueMarker>();
            marker.material = material;
            marker.bobbing = bobbing;
            marker.anchor = worldPosition;

            // 只有历史标记需要被点击（看详情）；待提交标记必须没有碰撞体，否则会挡住自己的射线
            if (clickable)
            {
                var collider = root.AddComponent<SphereCollider>();
                collider.center = new Vector3(0f, 0.38f, 0f);
                collider.radius = 0.30f;
            }

            if (!string.IsNullOrEmpty(labelText))
            {
                marker.labelTransform = CreateLabel(root.transform, labelText);
            }
            return marker;
        }

        static Transform CreateLabel(Transform parent, string text)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.64f, 0f);

            var mesh = go.AddComponent<TextMesh>();
            mesh.font = Font();
            mesh.fontSize = 48;
            mesh.characterSize = 0.05f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = Color.white;
            mesh.text = text;

            var renderer = go.GetComponent<MeshRenderer>();
            if (mesh.font != null)
            {
                renderer.sharedMaterial = mesh.font.material;
            }
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go.transform;
        }

        static Font Font()
        {
            if (cachedFont != null) return cachedFont;
            cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (cachedFont == null) cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return cachedFont;
        }

        public void SetColor(Color color)
        {
            if (material != null) material.color = color;
        }

        /// <summary>移动标记（同一次点击只在原地移动，不叠加新标记）。</summary>
        public void MoveTo(Vector3 worldPosition)
        {
            anchor = worldPosition;
            transform.position = worldPosition;
        }

        /// <summary>提交成功：固化在命中点、变绿、停止浮动。</summary>
        public void Commit()
        {
            bobbing = false;
            transform.position = anchor;
            SetColor(ResolvedColor);
        }

        void Update()
        {
            if (!bobbing) return;
            float bob = Mathf.Abs(Mathf.Sin(Time.time * 2.5f)) * 0.06f;
            transform.position = anchor + Vector3.up * bob;
        }

        void LateUpdate()
        {
            // 标签始终朝向相机，绕着走也能读
            if (labelTransform == null) return;
            if (cameraCache == null) cameraCache = Camera.main;
            if (cameraCache == null) return;

            labelTransform.rotation = Quaternion.LookRotation(
                labelTransform.position - cameraCache.transform.position);
        }
    }
}
