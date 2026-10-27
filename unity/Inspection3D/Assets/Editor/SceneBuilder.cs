#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Inspection.EditorTools
{
    /// <summary>
    /// 一键生成巡检场景：环境（地面/墙/机柜/管道/设备）、相机、UI 面板，并把脚本引用接好。
    /// 全部用 Unity 基础体 + 基础 UI，不用任何外部素材。
    /// 菜单：巡检 / 1. 一键生成场景
    /// </summary>
    public static class SceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Inspection.unity";

        [MenuItem("巡检/1. 一键生成场景", priority = 1)]
        public static void Build()
        {
            EnsureFolders();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            ConfigureLighting();
            BuildEnvironment();
            var camera = BuildCamera();

            var canvas = BuildCanvas();
            var toast = BuildToast(canvas);
            BuildHud(canvas);
            var form = BuildFormPanel(canvas);
            var detail = BuildDetailPanel(canvas);

            var placement = BuildSystems(camera, form, toast, detail);
            form.placement = placement;
            form.toast = toast;
            placement.form = form;

            ConfigurePlayerSettings();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[巡检] 场景已生成并保存：" + ScenePath);
            EditorUtility.DisplayDialog("生成完成",
                "场景已保存到 " + ScenePath + "\n\n直接点 Play 试一下：\nWASD 移动 · 右键拖拽转视角 · 左键点击设备", "好");
        }

        [MenuItem("巡检/2. 构建 Windows exe", priority = 2)]
        public static void BuildWindowsPlayer()
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Build/Inspection3D.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log("[巡检] 构建成功:" + report.summary.outputPath);
                EditorUtility.DisplayDialog("构建成功", "输出：Build/Inspection3D.exe", "好");
            }
            else
            {
                Debug.LogError("[巡检] 构建失败:" + report.summary.result);
                EditorUtility.DisplayDialog("构建失败", "看 Console 里的红字", "好");
            }
        }

        // ---------------- 环境 ----------------

        static void ConfigureLighting()
        {
            var go = new GameObject("Directional Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.05f;
            light.shadows = LightShadows.Soft;
            go.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.52f, 0.54f, 0.58f);
            RenderSettings.fog = false;
        }

        static void BuildEnvironment()
        {
            var root = new GameObject("Environment").transform;

            var floorMat = CreateMaterial("Floor", new Color(0.60f, 0.62f, 0.64f));
            var wallMat = CreateMaterial("Wall", new Color(0.80f, 0.80f, 0.78f));
            var cabinetMat = CreateMaterial("Cabinet", new Color(0.28f, 0.31f, 0.36f));
            var panelMat = CreateMaterial("CabinetPanel", new Color(0.10f, 0.35f, 0.55f));
            var pipeMat = CreateMaterial("Pipe", new Color(0.72f, 0.72f, 0.70f));
            var equipMat = CreateMaterial("Equipment", new Color(0.55f, 0.22f, 0.20f));

            // 地面 8m x 6m
            Cube("Floor", new Vector3(0f, -0.05f, 0f), new Vector3(8f, 0.1f, 6f), floorMat, root);

            // 四面墙
            Cube("Wall_North", new Vector3(0f, 1.5f, 3f), new Vector3(8f, 3f, 0.2f), wallMat, root);
            Cube("Wall_South", new Vector3(0f, 1.5f, -3f), new Vector3(8f, 3f, 0.2f), wallMat, root);
            Cube("Wall_East", new Vector3(4f, 1.5f, 0f), new Vector3(0.2f, 3f, 6f), wallMat, root);
            Cube("Wall_West", new Vector3(-4f, 1.5f, 0f), new Vector3(0.2f, 3f, 6f), wallMat, root);

            // 机柜 A01 / A02
            Cube("Cabinet_A01", new Vector3(2f, 0.9f, 1.2f), new Vector3(0.6f, 1.8f, 0.6f), cabinetMat, root);
            Cube("Cabinet_A01_Panel", new Vector3(2f, 1.25f, 1.51f), new Vector3(0.5f, 0.9f, 0.03f), panelMat, root);
            Cube("Cabinet_A02", new Vector3(-2f, 0.9f, 1.2f), new Vector3(0.6f, 1.8f, 0.6f), cabinetMat, root);
            Cube("Cabinet_A02_Panel", new Vector3(-2f, 1.25f, 1.51f), new Vector3(0.5f, 0.9f, 0.03f), panelMat, root);

            // 配电箱（西墙）
            Cube("Distribution_Box", new Vector3(-3.82f, 1.5f, 0.6f), new Vector3(0.14f, 0.8f, 0.6f), cabinetMat, root);

            // 管道（沿北墙横穿）
            Cylinder("Pipe_01", new Vector3(0f, 2.5f, 2.8f),
                new Vector3(0.14f, 3.5f, 0.14f), Quaternion.Euler(0f, 0f, 90f), pipeMat, root);

            // 灭火器
            Cylinder("Extinguisher", new Vector3(3.3f, 0.35f, -2.2f),
                new Vector3(0.3f, 0.35f, 0.3f), Quaternion.identity, equipMat, root);

            // 箱子/杂物，让场景有层次
            Cube("Crate_01", new Vector3(-3.2f, 0.3f, -2.2f), new Vector3(0.8f, 0.6f, 0.8f),
                CreateMaterial("Crate", new Color(0.55f, 0.45f, 0.30f)), root);
        }

        static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        static GameObject Cylinder(string name, Vector3 position, Vector3 scale, Quaternion rotation, Material material, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = rotation;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        // ---------------- 相机 ----------------

        static Camera BuildCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();

            go.transform.position = new Vector3(0f, 1.65f, -2.45f);
            go.transform.rotation = Quaternion.Euler(3f, 0f, 0f);

            cam.fieldOfView = 65f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.17f);

            go.AddComponent<CameraController>();
            return cam;
        }

        // ---------------- UI ----------------

        static GameObject BuildCanvas()
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            return go;
        }

        static void BuildHud(GameObject canvas)
        {
            var text = MakeText(canvas.transform, "HudHint",
                "WASD 移动　·　Q/E 升降　·　按住右键拖拽转视角　·　左键点击设备放置标记\n" +
                "标记颜色：红 = 待处理　橙 = 处理中　绿 = 已解决（进场景时自动恢复历史标记）",
                18, new Color(1f, 1f, 1f, 0.88f), TextAnchor.UpperLeft);

            var rt = text.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(24f, -18f);
            rt.sizeDelta = new Vector2(1100f, 60f);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
        }

        static ToastUI BuildToast(GameObject canvas)
        {
            var go = new GameObject("Toast", typeof(RectTransform), typeof(CanvasGroup), typeof(Text));
            go.transform.SetParent(canvas.transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -70f);
            rt.sizeDelta = new Vector2(1100f, 46f);

            var text = go.GetComponent<Text>();
            text.font = UIFont();
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = ToastUI.Success;
            text.text = "";
            text.supportRichText = false;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var group = go.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;

            var toast = go.AddComponent<ToastUI>();
            toast.label = text;
            toast.group = group;
            return toast;
        }

        static IssueFormController BuildFormPanel(GameObject canvas)
        {
            var panel = new GameObject("FormPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            panel.transform.SetParent(canvas.transform, false);

            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0f, 0f);
            prt.anchorMax = new Vector2(0f, 0f);
            prt.pivot = new Vector2(0f, 0f);
            prt.anchoredPosition = new Vector2(24f, 24f);
            prt.sizeDelta = new Vector2(460f, 0f);

            panel.GetComponent<Image>().color = new Color(0.07f, 0.08f, 0.10f, 0.94f);

            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 16, 16);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;

            var fitter = panel.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            MakeText(panel.transform, "Header", "上报问题", 20,
                new Color(0.96f, 0.96f, 0.96f), TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;

            MakeText(panel.transform, "TitleLabel", "标题（必填）　例：1 号机柜温度异常", 16,
                new Color(0.75f, 0.78f, 0.83f), TextAnchor.MiddleLeft);
            var titleInput = MakeInputField(panel.transform, "Input_Title", false);

            MakeText(panel.transform, "DescLabel", "描述（可选）　例：机柜右侧温度明显偏高", 16,
                new Color(0.75f, 0.78f, 0.83f), TextAnchor.MiddleLeft);
            var descInput = MakeInputField(panel.transform, "Input_Desc", true);

            var priorityLabel = MakeText(panel.transform, "PriorityLabel", "优先级（当前：medium）", 16,
                new Color(0.75f, 0.78f, 0.83f), TextAnchor.MiddleLeft);
            var row = MakeRow(panel.transform, "PriorityRow", 36f, 8f);
            var low = MakeButton(row.transform, "Btn_Low", "low", 36f, Selectable.Transition.None);
            var medium = MakeButton(row.transform, "Btn_Medium", "medium", 36f, Selectable.Transition.None);
            var high = MakeButton(row.transform, "Btn_High", "high", 36f, Selectable.Transition.None);

            var positionText = MakeText(panel.transform, "Text_Position", "标记位置：-", 17,
                new Color(0.85f, 0.88f, 0.92f), TextAnchor.MiddleLeft);
            positionText.horizontalOverflow = HorizontalWrapMode.Wrap;

            var hintText = MakeText(panel.transform, "Text_Hint", "", 16,
                new Color(1f, 0.45f, 0.45f), TextAnchor.MiddleLeft);
            hintText.horizontalOverflow = HorizontalWrapMode.Wrap;

            var buttonRow = MakeRow(panel.transform, "ButtonRow", 40f, 10f);
            var submit = MakeButton(buttonRow.transform, "Btn_Submit", "提交", 40f, Selectable.Transition.ColorTint);
            var cancel = MakeButton(buttonRow.transform, "Btn_Cancel", "取消", 40f, Selectable.Transition.ColorTint);

            var form = panel.AddComponent<IssueFormController>();
            form.panel = panel;
            form.titleInput = titleInput;
            form.descInput = descInput;
            form.positionText = positionText;
            form.hintText = hintText;
            form.priorityLabel = priorityLabel;
            form.submitButton = submit;
            form.cancelButton = cancel;
            form.submitLabel = submit.GetComponentInChildren<Text>();
            form.lowButton = low;
            form.mediumButton = medium;
            form.highButton = high;

            // 按钮事件在 IssueFormController.Awake() 里运行时绑定，
            // 因为 Button.onClick.AddListener 的监听器不会被序列化进场景。
            return form;
        }

        static IssueDetailPanel BuildDetailPanel(GameObject canvas)
        {
            var panel = new GameObject("DetailPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            panel.transform.SetParent(canvas.transform, false);

            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-24f, 24f);
            rt.sizeDelta = new Vector2(420f, 0f);

            panel.GetComponent<Image>().color = new Color(0.07f, 0.08f, 0.10f, 0.94f);

            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 16, 16);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;

            var fitter = panel.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            MakeText(panel.transform, "Header", "问题详情", 20,
                new Color(0.96f, 0.96f, 0.96f), TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;

            var title = MakeText(panel.transform, "Text_Title", "", 19,
                new Color(0.96f, 0.96f, 0.96f), TextAnchor.UpperLeft);
            title.fontStyle = FontStyle.Bold;
            Grow(title, 56f);

            var meta = MakeText(panel.transform, "Text_Meta", "", 16,
                new Color(0.90f, 0.90f, 0.90f), TextAnchor.MiddleLeft);
            var position = MakeText(panel.transform, "Text_Position", "", 16,
                new Color(0.85f, 0.88f, 0.92f), TextAnchor.MiddleLeft);
            var time = MakeText(panel.transform, "Text_Time", "", 16,
                new Color(0.72f, 0.76f, 0.82f), TextAnchor.MiddleLeft);

            var desc = MakeText(panel.transform, "Text_Desc", "", 17,
                new Color(0.90f, 0.92f, 0.95f), TextAnchor.UpperLeft);
            Grow(desc, 110f);

            var row = MakeRow(panel.transform, "Row", 40f, 10f);
            var close = MakeButton(row.transform, "Btn_Close", "关闭", 40f, Selectable.Transition.ColorTint);

            var detail = panel.AddComponent<IssueDetailPanel>();
            detail.panel = panel;
            detail.titleText = title;
            detail.metaText = meta;
            detail.positionText = position;
            detail.timeText = time;
            detail.descText = desc;
            detail.closeButton = close;
            return detail;
        }

        /// <summary>让文字块在纵向布局里占更高，允许多行换行。</summary>
        static void Grow(Text text, float height)
        {
            var le = text.GetComponent<LayoutElement>();
            if (le != null)
            {
                le.preferredHeight = height;
                le.minHeight = height;
            }
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }

        static Text MakeText(Transform parent, string name, string content, int size, Color color, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var text = go.GetComponent<Text>();
            text.font = UIFont();
            text.fontSize = size;
            text.color = color;
            text.text = content;
            text.alignment = align;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = size + 8f;
            le.minHeight = size + 4f;
            return text;
        }

        static GameObject MakeRow(Transform parent, string name, float height, float spacing)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleLeft;

            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            return go;
        }

        static InputField MakeInputField(Transform parent, string name, bool multiline)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var bg = go.GetComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.10f);

            float height = multiline ? 66f : 36f;
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;

            var field = go.GetComponent<InputField>();
            field.targetGraphic = bg;
            field.lineType = multiline ? InputField.LineType.MultiLineNewline : InputField.LineType.SingleLine;
            field.characterLimit = multiline ? 200 : 60;

            var text = MakeChildText(go.transform, "Text", new Color(0.95f, 0.95f, 0.95f),
                multiline ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft, 18);

            // 不再使用 Placeholder：占位文字打字时压在输入内容上层，容易让人以为没输入进去。
            // 示例文本改放到上方标签里。
            field.textComponent = text;
            field.text = "";

            // 深色背景上默认光标几乎看不见，显式给一个亮色
            field.customCaretColor = true;
            field.caretColor = new Color(1f, 0.76f, 0.20f, 1f);
            field.selectionColor = new Color(0.30f, 0.55f, 0.90f, 0.55f);
            field.interactable = true;
            return field;
        }

        static Text MakeChildText(Transform parent, string name, Color color, TextAnchor align, int size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(10f, 4f);
            rt.offsetMax = new Vector2(-10f, -4f);

            var text = go.GetComponent<Text>();
            text.font = UIFont();
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            // 子层文字不参与射线检测，否则点击会被 Text 吃掉，输入框拿不到焦点
            text.raycastTarget = false;
            return text;
        }

        static Button MakeButton(Transform parent, string name, string label, float height, Selectable.Transition transition)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.color = new Color(0.23f, 0.25f, 0.30f, 1f);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = transition;

            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;

            var text = MakeChildText(go.transform, "Text", new Color(0.92f, 0.92f, 0.92f), TextAnchor.MiddleCenter, 18);
            text.text = label;
            return button;
        }

        static Font UIFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null) Debug.LogWarning("[巡检] 找不到内置字体，UI 文字可能不显示");
            return font;
        }

        // ---------------- 组装 ----------------

        static MarkerPlacementController BuildSystems(Camera camera, IssueFormController form, ToastUI toast, IssueDetailPanel detail)
        {
            var go = new GameObject("GameManager");
            var placement = go.AddComponent<MarkerPlacementController>();
            placement.targetCamera = camera;
            placement.form = form;
            placement.detail = detail;

            // 加分项：进场景时从后端恢复历史标记
            var restorer = go.AddComponent<MarkerRestorer>();
            restorer.form = form;
            restorer.toast = toast;

            form.placement = placement;
            form.toast = toast;
            return placement;
        }

        static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "InspectionDemo";
            PlayerSettings.productName = "虚拟巡检问题标注";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
        }

        static void AddSceneToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == ScenePath))
            {
                scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        }

        static Material CreateMaterial(string name, Color color)
        {
            string path = "Assets/Materials/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.color = color;
                return existing;
            }

            var shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse");
            var material = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
#endif
