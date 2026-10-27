using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Inspection
{
    /// <summary>
    /// XR Simulation 演示用：把检测到的平面画出来，点击平面就在命中点放一个方块。
    /// 这不是提交到后端的业务标记，只用来演示“真实 AR 的平面检测 + 射线放置”流程。
    ///
    /// 场景里需要：AR Session + XR Origin（带 ARPlaneManager / ARRaycastManager）+ 本脚本。
    /// </summary>
    public class ARSimulationDemo : MonoBehaviour
    {
        [Header("引用（留空会自动查找）")]
        public ARPlaneManager planeManager;
        public ARRaycastManager raycastManager;
        public Camera arCamera;

        [Header("限制")]
        public int maxMarkers = 12;

        static readonly List<ARRaycastHit> Hits = new List<ARRaycastHit>();

        Material planeMaterial;
        Material markerMaterial;
        int placed;

        void Awake()
        {
            if (arCamera == null) arCamera = Camera.main;
            if (planeManager == null) planeManager = FindObjectOfType<ARPlaneManager>();
            if (raycastManager == null) raycastManager = FindObjectOfType<ARRaycastManager>();

            planeMaterial = CreateTransparentMaterial(new Color(0.30f, 0.75f, 1f, 0.14f));
            markerMaterial = CreateSolidMaterial(new Color(1f, 0.78f, 0.15f));
        }

        void OnEnable()
        {
            if (planeManager != null) planeManager.planesChanged += OnPlanesChanged;
        }

        void OnDisable()
        {
            if (planeManager != null) planeManager.planesChanged -= OnPlanesChanged;
        }

        void Update()
        {
            if (raycastManager == null || placed >= maxMarkers) return;

            Vector2 screenPosition;
            bool pressed;

#if UNITY_EDITOR
            // 编辑器（XR Simulation）：用鼠标点击模拟手指
            pressed = Input.GetMouseButtonDown(0);
            screenPosition = Input.mousePosition;
#else
            pressed = Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
            screenPosition = pressed ? Input.GetTouch(0).position : Vector2.zero;
#endif

            if (!pressed) return;

            if (!raycastManager.Raycast(screenPosition, Hits, TrackableType.PlaneWithinPolygon)) return;

            PlaceMarker(Hits[0].pose);
        }

        void PlaceMarker(Pose pose)
        {
            placed++;

            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "ARMarker_" + placed;
            marker.transform.position = pose.position + pose.up * 0.06f;
            marker.transform.rotation = pose.rotation;
            marker.transform.localScale = Vector3.one * 0.12f;
            marker.GetComponent<Renderer>().sharedMaterial = markerMaterial;

            // 不挡住后续射线
            Destroy(marker.GetComponent<Collider>());

            // 竖直的杆，方便看出标记垂直于平面
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(marker.transform, false);
            pole.transform.localScale = new Vector3(0.12f, 1.2f, 0.12f);
            pole.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            pole.GetComponent<Renderer>().sharedMaterial = markerMaterial;
            Destroy(pole.GetComponent<Collider>());

            // 真实 AR 里这里还应该挂 ARAnchor，减少设备移动时的漂移；
            // 模拟环境里平面是静止的，挂不挂效果一样。
        }

        void OnPlanesChanged(ARPlanesChangedEventArgs args)
        {
            foreach (var plane in args.added) Visualize(plane);
            foreach (var plane in args.updated) Visualize(plane);
        }

        /// <summary>给 ARPlane 加网格可视化（淡蓝色半透明面），不依赖任何预制体。</summary>
        void Visualize(ARPlane plane)
        {
            if (plane == null) return;

            if (plane.GetComponent<MeshFilter>() == null) plane.gameObject.AddComponent<MeshFilter>();

            var renderer = plane.GetComponent<MeshRenderer>();
            if (renderer == null) renderer = plane.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = planeMaterial;

            if (plane.GetComponent<ARPlaneMeshVisualizer>() == null)
            {
                plane.gameObject.AddComponent<ARPlaneMeshVisualizer>();
            }
        }

        static Material CreateSolidMaterial(Color color)
        {
            var shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse");
            return new Material(shader) { color = color };
        }

        static Material CreateTransparentMaterial(Color color)
        {
            var material = CreateSolidMaterial(color);

            // Standard 着色器切到 Transparent 模式，才能看到半透明的平面
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            return material;
        }
    }
}
