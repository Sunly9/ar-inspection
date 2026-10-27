# AI 使用记录

> 说明：按时间顺序记录本次任务中**主动发给 AI 的问题或指令**（含追问与纠错）。
> Prompt 保留原文，结果栏标注「采用 / 修改后采用 / 未采用」，并说明如何验证。
> 未记录 IDE 的逐字自动补全。

---

## P-001

- 目标：读懂任务书，确认需要哪些软件、整体怎么设计

### Prompt 原文

> 你了解一下这个文档，告诉我这个项目我需要一些什么软件，以及怎么去设计
> （附件：Unity-AR研发实操题-候选人任务书-简略版.docx）

### 结果

- 修改后采用
- AI 读出任务书要求：Unity LTS + AR Foundation 真机放标记 → Go 保存 → React 查看/改状态；
  给出软件清单（Unity Hub / Editor + Android 模块 / Go / VS2022）与三端架构、字段契约、接口设计。
- 我根据自己机器情况（无 Android 开发环境、Go 未装）做了取舍，先只记录为方案，未立即开工。
- 验证：对照任务书第 2、3 节逐条核对，字段名与第 4 节 JSON 完全一致。

---

## P-002

- 目标：没有 AR 设备，换备选版重新规划

### Prompt 原文

> 换了一个，我手机没有AR模块，你根据这个从新告诉我
> （附件：Unity-3D巡检实操题-候选人任务书-无AR设备备选版.docx）

### 结果

- 采用
- 备选版改为普通 3D 场景 + 鼠标射线放置，不需要 Android SDK / ARCore，改打 Windows exe；
  另外多出任务书第 5 节的 3 个 AR 论述题。
- AI 指出任务书第 7 节演示视频里“在真机中移动”是从 AR 版残留的措辞，与第 1 节“不要求使用手机”矛盾，
  建议以桌面运行为主交付，并在 README 说明。
- 验证：任务书第 1 节与第 7 节原文对照确认矛盾存在。

---

## P-003

- 目标：拿到各软件的官方下载地址

### Prompt 原文

> 你给我要下载的东西的官网或链接，然后我自己来下载

### 结果

- 采用
- 给出 Unity Hub 直链、Go 1.27.3 msi、VS2022 引导器等，并说明 `unity.com/download` 在国内会跳到
  `unity.cn`（主推团结引擎）导致找不到 Unity 6，建议直接用 Hub 直链。
- 验证：逐个 HEAD/GET 请求确认 200；Unity Hub 直链实测 137MB 可下载。

---

## P-004

- 目标：确认 Go 是否必须安装

### Prompt 原文

> （截图：Go 1.27.3）
> 这个需要安装吗

### 结果

- 采用
- 确认必须装（后端就是 Go），并补充国内 `GOPROXY=https://goproxy.cn,direct` 的配置，否则拉不到 SQLite 驱动。
- 验证：安装后 `go version` 输出 go1.27.3；`go env GOPROXY` 生效。

---

## P-005

- 目标：Unity 安装时“添加模块”页该勾什么

### Prompt 原文

> （截图：Unity 2022.3.62f3c1 安装-添加模块）
> 选什么啊

### 结果

- 采用
- 结论：只保留 VS2022（本机没装 Visual Studio），平台模块全部不勾（Windows 桌面构建支持是编辑器自带的），
  以后要打 APK 再从 Hub 补 Android 模块。总占用约 8GB。
- 验证：装完能正常打 Windows exe；Unity 版本为 2022.3.62f3c1（国内版 LTS，符合“Unity LTS”要求）。

---

## P-006

- 目标：工程建好后怎么开始

### Prompt 原文

> 我打开了，现在呀干嘛

### 结果

- 修改后采用
- AI 直接生成了 8 个脚本（视角 / 射线放置 / 表单 / API / 标记 / 提示 / DTO / 场景生成器），
  并说明先验证工具链再搭场景。
- 验证：用 Roslyn 编译器对照 UnityEngine/UnityEditor 的 DLL 编译，0 error；Unity Console 无编译错误。

---

## P-007

- 目标：找不到 Play 按钮

### Prompt 原文

> 哪里有play啊

### 结果

- 采用
- 位置：编辑器最顶部工具栏正中间 ▶，快捷键 `Ctrl+P`；若布局被拖乱用 `Window → Layouts → Default` 恢复。
- 验证：能进入 Play 模式。

---

## P-008

- 目标：点「取消」没有反应

### Prompt 原文

> 现在我发现一个问题啊，我点击之后取消是没办法取消的啊

### 结果

- 修改后采用
- AI 定位到根因：`Button.onClick.AddListener()` 在编辑器里加的是**运行时监听器，不会被序列化进场景**，
  重新加载场景后点击列表为空。修改为在 `IssueFormController.Awake()` 里运行时绑定。
- 同时发现第二个隐患：按钮回调隐藏面板后，同一帧 `MarkerPlacementController.Update()` 再判断
  “是否点在 UI 上”会得到 false，于是立刻重新放置标记、把面板又打开。
  修改为**在鼠标按下那一帧就记录是否压在 UI 上**，并用 `form.IsOpen` 阻止重复放置。
- 验证：重新 Play 后点 low/high 高亮切换正常，说明绑定生效。

---

## P-009

- 目标：修改后仍然“取消无效 + 输入框打不了字”

### Prompt 原文

> 这两个没变化，以及点击标题这些没办法填写

### 结果

- 修改后采用
- 不再猜，改为读 `Unity/Editor/Editor.log` 与场景 YAML 取证：
  - 日志显示 `Cancel()` 确实被调用，面板已 `SetActive(false)`；`NullReferenceException` 计数 0；
  - 场景 YAML 里 `panel / titleInput / submitButton / placement` 的 fileID 全部非 0，不是接线问题；
  - 但日志里**没有任何键盘输入记录** → 问题在“键盘没进输入框”，不是按钮。
- 据此加了 `InputProbe` 诊断脚本（打印当前选中 UI 对象 + `Input.inputString`），
  并给输入框加了运行时 `EventTrigger.PointerClick → ActivateInputField()` 兜底聚焦。
- 验证：重新生成场景后输入框可正常输入（用户确认）。

---

## P-010

- 目标：说清“优先级高亮切换”是什么，并质疑输入框提示不消失的体验问题

### Prompt 原文

> 可以输入，就是你不是说选择Low和Hight那个会高亮切换吗，到底是啥意思啊，而且我认为你那个输入也是有bug的，点击那个输入框可以输入，但是上面的提示不应该消失吗，不然以为不能输入呢，这里是不是对用户很不友好

### 结果

- 修改后采用
- 承认两点：① 只靠颜色表示“当前选中”不够，用户不知道选了哪个；② 占位提示（Placeholder）在打字时压在
  输入内容上层，看起来像“没输入进去”。
- 修改：
  - 优先级标签改为 `优先级（当前：medium）`，随选择实时更新；
  - **删除 Placeholder**，示例文字写进上方标签（`标题（必填）　例：1 号机柜温度异常`）；
  - 面板加标题「上报问题」；取消时弹「已取消」提示；表单未关时点场景提示「请先提交或取消当前问题」；
  - 标题框内按回车可直接提交。
- 验证：点 low/high 后文字与橙色同步变化；输入时不再有提示文字叠加；取消有明确反馈。

---

## P-011

- 目标：确认交互没问题后继续推进

### Prompt 原文

> 现在没问题了

### 结果

- 采用
- 收尾：删除临时诊断脚本 `InputProbe.cs` 与调试日志，避免污染交付代码；
  然后实现 React 管理端（列表 / 筛选 / 改状态 / 错误态）与 README。
- 验证：`npm run build`（`tsc --noEmit` + `vite build`）通过；接口联调返回 200；
  停掉 Go 后页面显示错误横幅而非白屏。

---

## P-012

- 目标：实现“从后端恢复历史标记”这个加分项

### Prompt 原文

> 「从后端恢复标记」这个是干嘛的，你先给我解释一下
>
> 要做的，我刚想说的就是这个问题

### 结果

- 采用
- 思路：进场景时 `GET /api/issues`，按每条记录的 x/y/z 生成标记，按状态上色
  （红 = 待处理、橙 = 处理中、绿 = 已解决），标签始终朝向相机。
- 实现：
  - 新增 `IssueMarker.cs`：标记外观统一（立杆 + 球头 + TextMesh 标签 + 浮动/静止），
    **销毁所有 Collider**，避免挡住鼠标射线；
  - 新增 `MarkerRestorer.cs`：启动时拉列表 → 校验坐标（NaN/越界跳过）→ 逐个生成；
    后端未启动时只打 warning，不打断使用；
  - 删除原 `PendingMarker.cs`，待提交标记改用 `IssueMarker`（黄色、浮动），提交成功后 `Commit()` 变绿静止；
  - HUD 增加颜色图例。
- 取舍：3D 标签用 `TextMesh` 只放 `#序号 + 优先级` 这类 ASCII 文本，
  避免中文字形缺失变成方块；中文标题留给网页端和表单。
- 验证：重新生成场景后 Play，场景中出现与后端记录数量一致的标记；改状态后重新 Play，颜色跟着变。

---

## P-013

- 目标：继续做加分项（点击标记看详情 + 网页删除）

### Prompt 原文

> 做A吧
> （A = 点击标记看详情 + React 删除）

### 结果

- 采用
- 后端：新增 `DELETE /api/issues/{id}`（store.Delete + handler + 路由 + CORS 允许 DELETE），
  删除不存在的 id 返回 404。
- Unity：
  - `IssueMarker` 增加 `Issue` 数据与可选碰撞体（只有历史标记可点，待提交标记仍然无碰撞体）；
  - 新增 `IssueDetailPanel`：标题、优先级、状态、坐标、上报时间、描述，`Esc` 或「关闭」收起；
  - `MarkerPlacementController` 射线命中历史标记时弹详情、不放置新标记；点空白处自动收起详情；
  - `SceneBuilder` 生成右侧详情面板并接线。
- React：每行加「删除」按钮 + `window.confirm` 二次确认，成功后从列表移除，失败走错误横幅。
- 验证：`go vet` 通过、`go build` 通过；`npm run build`（tsc + vite）通过；Unity 脚本用 Roslyn 编译 0 error。

---

## P-014

- 目标：用 XR Simulation 演示“迁移到真实 AR”的思路（题面加分项）

### Prompt 原文

> AR Foundation 6.x + XR Simulation（Unity Package）这个是干嘛的，给我解释一下呢

### 结果

- 采用
- AI 解释：AR Foundation 是 AR 抽象层，XR Simulation 是编辑器里的模拟设备，
  同一套 `ARPlaneManager` / `ARRaycastManager` 代码可在模拟环境和真机之间切换。
- **纠正了 AI 自己之前的错误**：早先清单写的 “AR Foundation 6.x” 对应 Unity 6，
  本项目是 2022.3.62f3c1，应装 **AR Foundation 5.1.x**（实际 5.1.6）。
- 实操：Package Manager 切 `Unity Registry` 搜索安装 → Project Settings → XR Plug-in Management
  （`PC / Standalone` 页签）勾 `XR Simulation` → Window → XR → AR Foundation → XR Environment 选环境。
- 新增 `ARSimulationDemo.cs`：给检测到的平面加 MeshFilter/MeshRenderer/ARPlaneMeshVisualizer 可视化（半透明），
  点击时 `ARRaycastManager.Raycast(..., TrackableType.PlaneWithinPolygon)` 在命中点放方块。
- 验证：Play 后 Game 视图出现模拟房间，蓝色平面被检测出来，点击地面成功放置方块；移动模拟设备方块位置不变。
- 诚实说明：这是模拟环境，不是真机 AR；模拟场景中的方块未挂 `ARAnchor`，已在 README 的“未完成”里注明。



