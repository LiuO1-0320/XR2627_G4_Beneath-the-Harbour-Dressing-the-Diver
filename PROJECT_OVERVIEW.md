# Beneath the Harbour — Dressing the Diver 项目说明

> 整理日期：2026-10-10。本文根据本地脚本、场景序列化内容、预制体和项目配置分析；未启动编辑器、运行 VR 设备或验证打包结果。文中“已接入”表示在场景文件中能找到相应配置，不代表已通过运行测试。

## 1. 这个项目在做什么

这是一个使用团结引擎制作的 **VR 海事博物馆互动体验**，主题为 **Beneath the Harbour（港湾之下）— Dressing the Diver（为潜水员穿戴装备）**。

参观者通过阅读中英文说明、亲手抓取潜水头盔、拆下零件观察，以及帮助虚拟潜水员戴头盔，了解传统潜水装备的结构。场景引言将体验与香港维多利亚港的历史联系起来；当前代码的重点是头盔交互与展项导览，不能据此认定已经实现完整的历史叙事。

从当前实现看，项目更接近一个可交互的 VR 展览原型：以三个 ACT 展项为主线，并加入水下港湾环境供探索。

## 2. 参观体验流程

### 起点：阅读介绍

`BasicScene.unity` 中的 `Start Introduction` 展示欢迎文字，介绍海事博物馆、铜制头盔和维多利亚港背景。文字面板支持滚动，标题、正文、字号、颜色和尺寸可以在 Inspector 中修改。

### ACT 1：认识并试戴头盔

场景配置了铜制潜水头盔介绍、头盔抓取与试戴，以及操作指引。

- 按住握把抓起头盔，将它移到玩家头部附近并松手。
- 头盔中心与头部距离不超过配置的 **0.4 米**时进入佩戴状态，并跟随头部运动。
- 玩家佩戴时，外壳渲染切换为仅投射阴影，避免模型挡住视野。
- 佩戴成功后显示提示；按 B / Y 对应的手柄次要按钮可取下并返回展台。
- 松手时若靠近展台，头盔会吸附复位；在其他位置松手则启用重力，掉落过远时自动返回。

这里的试戴主要通过吸附和头部跟随表达，代码中未见真实密封、供气或潜水生理模拟。

### ACT 2：拆解和观察头盔细节

第二个头盔作为独立观察展项，支持整盔移动、旋转和指定零件拆装。

- 抓取头盔固定部分移动、旋转整盔，头盔尺寸保持固定。
- 松开整盔后，可抓取对应零件查看中英文介绍。
- 可拆零件键为 `forward`、`left`、`right`、`up`、`fixing`、`nameplate` 和 `nail1`。
- `nail2` 到 `nail12` 不在默认可拆列表中，匹配采用完整名称后缀，避免将 `nail10` 误认为 `nail1`。
- 零件放回原安装位置附近并松手，可自动装回；场景配置距离为 **0.15 米**。
- 面板显示已拆零件数量，详情区域支持滚动，Reset 恢复展项。

整盔和仍安装在头盔上的零件之间有抓取过滤，减少同时操作造成的冲突。拆下的零件不启用重力，适合悬停观察。

### ACT 3：帮助潜水员戴头盔

第三个展项通过文字对话引导参观者为潜水员戴上 `helmet3`。

1. 使用手柄射线指向“开始对话”，按扳机打开文字弹窗。
2. 阅读提示，观察头盔视窗和连接结构。
3. 按住握把抓起 `helmet3`，移到 NPC 头部附近并松手。
4. 距离符合条件时，头盔吸附到 NPC 头部，并显示完成感谢语。
5. 使用 Reset 按钮将头盔放回展台，重新体验。

NPC 由脚本使用胶囊、球体、立方体等基础几何体生成；对话是脚本内置的固定文本。这一环节当前实现的是戴头盔，未见完整潜水服穿戴系统或 AI 对话。

### 水下港湾探索与导航

`BasicScene.unity` 引用了 `UnderwaterHarbourTour.prefab`。该预制体包含水面、海床、下行坡道、入口甲板、观察平台、路线标记、礁石、海草、珊瑚、港湾支柱和鱼群等内容，并配置了传送相关区域。

`UnderwaterTour` 让直属子物体中名称以 `Fish_` 开头的鱼沿缓慢轨迹运动，并根据主相机是否处于设定的局部水下范围切换雾效，离开或禁用时恢复原雾效。脚本没有主动推动玩家相机。

箭头指引提示前往下一个 ACT。快捷菜单跟随玩家视角，提供起点、ACT 1、ACT 2、ACT 3 四个目的地；右手主按钮（界面标为 A）用于展开或收起。快捷跳转通过 XR `TeleportationProvider` 排队请求，抵达展台旁并朝向展项。当前菜单没有独立的水下目的地按钮。

## 3. 技术与工程结构

### 引擎与主要依赖

以下版本来自本地配置，表示本项目使用的版本：

| 项目 | 配置 |
| --- | --- |
| 团结引擎版本 | `1.6.13` |
| Editor 版本字段 | `2022.3.61t14` |
| Universal Render Pipeline | `14.1.0` |
| XR Interaction Toolkit | `3.1.2` |
| OpenXR | `1.14.2` |
| XR Management | `4.5.1` |
| Input System | `1.14.0` |
| TextMesh Pro | `3.0.9` |

主要自定义界面使用 `UnityEngine.UI` 的世界空间 Canvas、Text、Button 和 ScrollRect；需要手柄射线交互的面板使用 `TrackedDeviceGraphicRaycaster`。虽然项目安装了 TextMesh Pro，这些脚本的主要文字组件仍是传统 UI Text。

### 目录作用

| 路径 | 作用 |
| --- | --- |
| `Assets/script/` | 八个自定义脚本，负责展项、穿戴、导览和水下氛围 |
| `Assets/Scenes/BasicScene.unity` | 已接入三个 ACT、起点说明、快捷导航和水下预制体的体验场景 |
| `Assets/Scenes/SampleScene.scene` | 当前构建列表中的场景；不能直接视为上述体验场景的等价入口 |
| `Assets/Modle/divinghelmet.fbx` | 潜水头盔模型资源，目录名称在工程中确实拼作 `Modle` |
| `Assets/UnderwaterTour/` | 水下港湾环境预制体 |
| `Assets/VRTemplateAssets/` | VR 模板资源与辅助脚本 |
| `Assets/Samples/` | XR Interaction Toolkit 示例，包括 Starter Assets 和 XR Device Simulator |
| `Assets/XR/`、`Assets/XRI/` | XR 加载器、OpenXR 与交互配置 |
| `Assets/Settings/` | 渲染及项目配置资源 |
| `Packages/`、`ProjectSettings/` | 包依赖和引擎项目设置 |
| `Library/`、`Temp/`、`Logs/` | 本地缓存、临时文件和日志，分析业务逻辑时优先查看 Assets |

### 核心脚本职责

| 脚本 | 主要职责 |
| --- | --- |
| `StartIntroduction.cs` | 生成可编辑的世界空间引言面板，支持正文滚动 |
| `ActDirectionSign.cs` | 生成箭头和文字，指向下一展项 |
| `DivingHelmetWearable.cs` | 设置头盔抓取和物理组件，处理佩戴、头部跟随、落地及展台复位 |
| `HelmetInteractionGuidance.cs` | 展示 ACT 1 操作说明与短暂的成功提示 |
| `HelmetDetailInspection.cs` | 设置 ACT 2 整盔抓取、旋转、零件拆装、详情和复位 |
| `DiverNpcInteraction.cs` | 生成简易 NPC 和对话界面，运行时复用穿戴脚本将头盔戴到 NPC 头部 |
| `PlayerQuickTravel.cs` | 生成跟随视角的导航菜单，提交四个目的地的传送请求 |
| `UnderwaterTour.cs` | 驱动鱼群运动，根据玩家位置切换并恢复水下雾效 |

多个展示脚本带有 `[ExecuteAlways]`，因此编辑模式中也可以生成展示内容。部分 UI、材质、几何体和交互组件由脚本动态创建；了解功能时需要同时查看场景引用和脚本，不能仅凭 Hierarchy 中的静态物体判断。

## 4. 如何打开与检查

1. 优先使用项目记录的团结引擎版本 `1.6.13` 打开工程，等待资源导入和包解析。
2. 打开 `Assets/Scenes/BasicScene.unity`，检查起点说明、ACT 1–3、XR 玩家与 EventSystem 的配置。
3. 在有兼容 XR 运行环境和手柄的情况下进入 Play，依次验证试戴、拆装、NPC 穿戴、快捷传送和水下环境。
4. 若使用键鼠模拟，需先检查并配置 XR Device Simulator。工程已导入示例，但 `XRDeviceSimulatorSettings.asset` 中自动实例化开关为关闭，且模拟器预制体引用为空。
5. 打包前确认构建场景列表。当前 `EditorBuildSettings.asset` 只启用了 `Assets/Scenes/SampleScene.scene`，没有列入 `BasicScene.unity`。如果目标是交付本文描述的体验，应在编辑器中将实际体验场景加入并设置合适的启动顺序。

### 主要操作速查

| 操作 | 用途 |
| --- | --- |
| 手柄射线 + 扳机 | 点击对话、导航、Reset 等按钮 |
| 按住握把 | 抓取头盔或零件 |
| 头盔靠近头部后松手 | 玩家试戴或为 NPC 戴头盔 |
| B / Y 对应次要按钮 | ACT 1 佩戴状态下将头盔返回展台 |
| 右手主按钮，界面标为 A | 展开或收起快捷导航 |
| 零件靠近原位置后松手 | 将零件装回 |
| 展项 Reset | 恢复对应展项；持有物体时需先松手 |

具体按键名称和交互响应仍取决于实际设备、输入绑定和运行配置。

## 5. 当前实现边界与后续关注点

- **启动场景需要核对。** 三个 ACT 的自定义脚本引用在 `BasicScene.unity` 中能确认，而构建入口是 `SampleScene.scene`，两者目前不同。
- **中文字体需要准备。** 检查到的展项字体引用为空，脚本尝试使用系统字体。正式部署时应指定包含中英文字符的随包字体，并验证目标设备上的显示。
- **模型命名影响拆装。** ACT 2 通过物体名称最后一个 `.` 后的后缀匹配零件，同时要求该物体带 Renderer；改动模型层级或命名后需核对可拆零件。
- **历史信息仍有待补充。** 铭牌介绍明确说明制造信息尚未确认，固定件的准确功能也需结合实物型号确认。当前展项说明应视为项目内容，不能替代经过来源核验的藏品说明。
- **未见完整任务管理与持久化。** 八个自定义脚本中没有统一流程状态机、存档、账号、网络服务或评分系统。ACT 3 的完成反馈由头盔佩戴状态触发，快捷导航允许直接跳转展项。
- **设备与运行效果尚未验证。** 本次没有进行编译、Play Mode、真机或构建测试，因此尚不能确认目标头显兼容性、帧率、UI 可读性及所有交互是否正常。

## 6. 分析依据

主要依据为 `Assets/script/` 下的八个 C# 文件、`Assets/Scenes/BasicScene.unity`、`Assets/Scenes/SampleScene.scene`、`Assets/UnderwaterTour/UnderwaterHarbourTour.prefab`、`Packages/manifest.json`、`ProjectSettings/ProjectVersion.txt`、`ProjectSettings/EditorBuildSettings.asset` 和 XR 配置资源。

本文仅新增项目说明文件，没有修改脚本、场景、模型或构建配置。
