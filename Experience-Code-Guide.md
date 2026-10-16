# 体验系统代码索引

运行脚本位于 `XRG4_BeneathTheHarber/Assets/script`，编辑器工具位于 `Assets/Editor`。文件名直接描述功能，不再使用小组任务编号作为模块前缀。

| 功能 | 首先查看的文件 | 配套文件 |
| --- | --- | --- |
| 会话开始、暂停、跳过、阶段完成 | `SessionController.cs`、`SessionState.cs` | `SessionBootstrap.cs`、`StageCoordinator.cs` |
| 工作人员操作面板 | `StaffSessionUI.cs` | `SessionSetup.cs`（Editor） |
| 阶段事件与任务接入 | `TaskEndpoint.cs` | `ExperienceStage`、`VisitorAction`（定义于 `SessionState.cs`） |
| 区域范围、到达判断和引导 | `StageCoordinator.cs`、`RegionGuide.cs` | `Region-Guidance.md` |
| 初次操作教程 | `RegionalActions.cs`、`BriefingPractice.cs` | `TutorialButton.cs`、`Briefing-Tutorial.md` |
| 环境音、旁白、入水出水音 | `AudioDirector.cs`、`AudioMap.cs` | `AudioPolicy.cs`、`AudioSetup.cs`（Editor） |
| 展台旁白重播面板 | `NarrationRegionPanels.cs` | `AudioDirector.cs` |
| 凝视输入及进度反馈 | `GazeInput.cs`、`GazeDwell.cs` | `GazeFeedback.cs`、`GazePanelAnchor.cs` |
| 可访问性菜单及辅助任务接口 | `AccessibilityUI.cs` | `AccessibleButton.cs`、`AccessibleTaskAction.cs` |
| 全局复位 | `GlobalReset.cs`、`ResetProtocol.cs` | `IResettable`、`ResetSequence`（定义于 `ResetProtocol.cs`） |
| 首次完成、自由探索、退出 | `ExperienceEndingUI.cs` | `SessionState.cs`、`Experience-Ending.md` |
| 工作人员移动端接口、事件日志 | `SessionTelemetry.cs`、`ControlProtocol.cs` | `Mobile-Integration.md` |
| 绳索上下潜及活动范围 | `DiveRopeStation.cs`、`DiveShaftGeometry.cs` | `UnderwaterTour.cs`、`WaterVolumeSwimming.cs` |
| ACT1 佩戴头盔 | `DivingHelmetWearable.cs` | `HelmetInteractionGuidance.cs` |
| ACT2 模型观察与拆装 | `HelmetDetailInspection.cs` | `StageCoordinator.cs` |
| ACT3 为潜水员装配 | `DiverNpcInteraction.cs` | `DivingHelmetWearable.cs` |
| 海底清理 | `SeabedCleanup.cs` | `StageCoordinator.cs` |
| 视频播放、离开暂停及进度复位 | `ExhibitVideoScreen.cs` | `ExhibitVideoSetup.cs`（Editor） |

## Unity 中的入口

- Hierarchy 中的会话管理对象：`Experience Session`。
- 工作人员面板标题：`Experience - Staff session controls`。
- 创建可编辑配置：`Tools > Experience > Create editable session configuration`。
- 音频校验：`Tools > Experience > Audio > Validate local audio assets`。
- 音频映射：`Assets/Resources/ExperienceAudio/DefaultAudioMap.asset`。
- 混音器：`Assets/Resources/ExperienceAudio/Experience.mixer`。
- 本地合成环境音和音乐：`Assets/Audio/Ambience`；已有旁白仍位于 `Assets/Audio/Narration`。

## 重命名兼容性

脚本、混音器和资源目录移动时保留原始 `.meta` 文件，场景及预制体仍通过原有 GUID 引用资源。已保存的配置字段名、阶段枚举数值、移动端 JSON 字段及命令名称未改变。

Unity 类型的 `MovedFrom` 声明保留旧类名，这是加载旧资源的迁移信息，不是仍在使用的模块名。不要删除这些声明，也不要重新生成现有资源的 `.meta`。

外部尚未合并的代码如果调用旧类名，需要改为对应的新名字。大多数类名仅去掉旧编号前缀；特别注意阶段枚举为 `ExperienceStage`，工作人员面板为 `StaffSessionUI`，接口为 `IResettable`、`IStageActivity`、`IAccessibleActionProvider`。

新日志写入 `Application.persistentDataPath/SessionLogs`。以前的日志目录不删除、不搬迁；需要分析旧测试记录时仍可读取原目录。移动端传输的 JSON 结构不因类名调整而改变。

## 验证

- `Tests/run-experience-state-tests.ps1`：会话、命令、复位、凝视、音频到达及教程逻辑回归。
- `python Tests/check-experience-audio-assets.py`：本地音频与混音器资源检查。
- `python Tests/check-functional-naming.py`：功能命名、迁移声明、脚本配套 meta 与 Resources 路径检查。

重新打开 Unity 等待导入与编译后，应检查 BasicScene 没有 Missing Script，并通过真实设备复测 Start → 教程 → ACT1–3 → 下潜清理 → 上浮 → 自由探索/退出 → Reset。命令行编译与逻辑测试不能代替头显体验验收。
