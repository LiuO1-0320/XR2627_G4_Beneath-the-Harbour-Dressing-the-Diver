# Session-Control 会话流程与接入说明

## 运行入口

打开 `XRG4_BeneathTheHarber/Assets/Scenes/BasicScene.unity` 进入 Play。`SessionBootstrap` 为包含 `PlayerQuickTravel` 的场景创建唯一会话控制对象；如果场景已有 `StageCoordinator`，使用作者配置。无需创建七个场景。现有构建设置仍指向 `SampleScene.scene`，发布前需在编辑器中确认实际构建入口。

桌面 Game 视图左上角为工作人员面板：Start、Pause、Resume、Skip、End / Reset。游客引导仅使用项目原有区域字幕；Experience 不提供 Next/Finish 或无条件推进接口；阶段切换时只显示临时路线提示，不恢复阶段教学字幕。游客通过原区域内有语义的点击与真实操作事件推进。待机和完成时等待工作人员，原 ACT 快捷菜单在托管会话内隐藏且跳转入口被拦截。

需要持久化配置时使用 **Tools → Experience → Create editable session configuration**，保存场景。在 `stages` 中配置工作人员阶段名称、旁白、阶段专属对象及活动组件。数组保留原七个序列化槽位，但 Inside 已合并入 ACT1，不再独立进入或播放其槽位内容。`objects` 只放阶段专属显示对象，勿放地面、XR Origin、工作人员面板或会话控制器。活动组件须实现 `IStageActivity`。

## 推进规则

| 阶段 | 现有区域 | 推进条件 |
| --- | --- | --- |
| Briefing | 起点介绍 | 点击原介绍板下的“认识潜水头盔” |
| The Case | 头盔展台 | 头盔在距头部不超过 wearDistance（场景为 0.4 米）处松手，真实佩戴成功（IsWorn=true） |
| Scale-up | 细节观察展项 | M3 观察任务成功事件 |
| Inside（合并） | ACT1 内部观察 | 与 ACT1 的真实佩戴结果同步，不再作为独立阶段或导航目标 |
| Dress the Diver | 潜水员展项 | 默认使用现有 NPC 戴盔成功后的 EscortStarted 事件；配置了 M3 活动时由 M3 完成事件控制 |
| Descent | 沿绳下潜 | 玩家主动启动后，真实下潜结束事件 |
| Evidence & Reflection | 起点原有区域引导 | 点击一项观察发现（视窗或连接件），记录选择并进入 Completed |

Start 从 Briefing 开始。说明阶段使用 `ReportInteraction(sessionId, stage, action)` 验证具体操作；任务阶段仍只接受 `CompleteTask`，普通点击不能完成任务，不使用触发区域、等待时长或 NPC 佩戴动作代替装配成功。Skip 可在 Running/Paused 使用，标记 Skipped；暂停时 Skip 后仍保持暂停。Reset 清空任务状态及会话 ID，再次 Start 生成新 GUID。每次转换输出会话、阶段及各阶段结果到 Unity 日志。

游客点击和头盔事件通过 `QueueInteraction` 在下一帧处理，避免在 XR 抓取回调中清理对象。事件携带会话 ID、来源阶段及操作类型；重复、错误阶段、错误操作和上一轮事件被拒绝。暂停后新产生的点击被忽略；暂停前已接收的事件保留到恢复时处理，Skip/Reset 清除它们。

原介绍板增加原地操作按钮，正文和滚动功能保留，不恢复阶段字幕面板。NPC 对话按钮改为具体提问主题，不再显示“下一句 / Next”；对话点击不代表装配成功。Reflection 保存所选观察项，可在工作人员面板查看，Reset 清空。

## 与 M3 的接口草案（待双方确认与接线）

当前仓库没有可验证的完整装配任务发布者，也没有双方确认的观察完成标准。默认 **Scale-up 会等待 M3 事件**。Dress the Diver 在没有配置 M3 活动时恢复现有戴盔体验，以真实戴盔后 NPC 开始带路的事件完成当前阶段；这是现有戴盔任务，不替代将来的完整装配标准。若 stages 中配置了 M3 活动，其完成条件优先，NPC 带路事件不会越过 M3 判定。

推荐 M3 实现 `IStageActivity`，或为每个任务添加 `TaskEndpoint`：

1. 将 endpoint 的 `controller` 指向场景的 `SessionController`；设置对应 `stage`；将 endpoint 放入该阶段 `activities`。
2. `onBegin(string sessionId)` 传入本次会话 ID。M3 必须将其作为该次活动的上下文保存，供异步操作回调使用，不能在回调时读取最新会话 ID。
3. M3 自行验证任务成功后调用 `endpoint.ReportCompleted(capturedSessionId)`。该入口不是游客按钮，不连接 TriggerEnter 或固定延时。
4. `onPause/onResume` 暂停、继续 M3 自己的动画、声音、交互及任务时钟。重复暂停不会重复通知。暂停期间已在途的成功通知由 endpoint 暂存，恢复后消费一次。
5. `onEnd` 必须取消异步操作、释放抓取、清理阶段物体；`onReset` 还要恢复全部任务初始状态。跳过与成功都调用 End，M3 不应在 End 中自行认定成功。

直接使用状态机 `CompleteTask(Guid, ExperienceStage)` 时，暂停期间返回 false；调用方负责保存、在恢复后重试。endpoint 已处理这个队列。错误会话、错误阶段、重复完成事件不会推进；转换回调中的重入被拒绝。

## 生命周期与媒体

内置适配覆盖头盔抓取/拆件、试戴、NPC 活动、沿绳下潜、导航和展览视频。切换前取消抓取，再恢复零件和头盔、NPC、绳索状态。中断绳索移动先返回安全平台、恢复其挂起的运动组件，再显示下一阶段的路线指引。正常阶段完成不会改变玩家位置；下潜完成保留海底位置，游客可使用绳索主动上浮后返回回顾区。暂停禁用软件移动及相关交互，但不停止头显追踪，不修改全局 `Time.timeScale`。

旁白使用阶段专属 AudioSource；Pause/Resume 使用 Pause/UnPause，切换时 Stop。Experience 阶段字幕字段和时间轴已移除，文字引导统一采用项目原有区域内容。当前未提供录音素材，需要内容负责人补充旁白。新增 M3、下潜演示等模块须通过活动接口实现自己的暂停和清理，不能只隐藏对象。

## 验证

运行 `Tests/run-experience-state-tests.ps1`（Windows、.NET SDK）：111 条断言覆盖启动、说明推进、任务限制、错误/重复/旧会话事件、暂停恢复、跳过结果、总结完成、重入保护及连续两轮会话。

已使用项目现有 Unity/XRI 引用完成运行时 C# 编译检查。仍需编辑器 Play Mode / 真机联调：

- 待机时不能抓取展项；Start 后工作人员界面的阶段为 Briefing，游客只看到原有区域引导与“认识潜水头盔”区域按钮。
- M3 接线后，观察和装配仅由其真实 成功事件推进；进入区域、普通点击、戴 NPC 头盔均不替代完整装配成功。
- 手持整盔/脱离父级的零件时 Pause/Resume、Skip、Reset，确认无遗留零件、速度异常或重复监听。
- 下潜途中暂停：位置和播放停住；恢复从同一位置继续；Skip 中断移动时先回到安全平台，再显示指引；Reset 恢复起点。
- 播放旁白/视频时暂停恢复，不重播；阶段退出后无旧声音；游客视野中无 Experience 新增字幕。
- 选择观察发现后等待工作人员 End / Reset；第二轮零件、NPC、播放和任务全部恢复初始状态。

状态机测试及编译检查不等同于完整 VR 验收。M3 真实任务接入、录音配置与上述设备测试是尚待完成的联调项。

## 自主移动与临时引导

完成阶段或工作人员 Skip 后，控制器不再调用自动传送。`RegionGuide` 在约 1.5 秒内淡入目标区域名称和前方/左侧/右侧/后方提示；游客使用原有移动方式自行前往。进入目标区域后约 0.4 秒淡出，离开后不再次弹出；下一次阶段切换建立新的引导。提示没有按钮，不拦截射线。

默认按展项前的站立点检测到达：水平半径 3 米、脚部高度差不超过 2 米，避免海底被误判为已到上层回顾区。可在每阶段 `arrivalArea` 指定覆盖站立空间的 BoxCollider（建议设为 Trigger），精确限定区域。可以在 Experience Session 对象添加 `RegionGuide` 并调整淡入淡出时长、默认到达半径及字体。

到达检测只隐藏指引，不调用任何任务成功接口。Pause 保留当前指引状态并停止淡入淡出，Resume 继续；Reset/Completed 清理指引。Reset 仍将游客恢复起点，正在绳索移动中执行 Skip 仍保留安全回平台行为。

Play Mode 联调：完成 The Case 后确认 XR Origin 位置不跳变；步行接近观察区，提示应消失且 Scale-up 任务仍等待真实完成事件；下潜完成后确认仍在海底，可按原“上浮”按钮返回，再自行走到回顾区。

## ACT1 佩戴完成判定修正

ACT1/The Case 的完成事件现为 `HelmetEquipped`。普通抓取、远处松手、取消抓取及旧 `HelmetExamined` 事件不会推进；松手后仍等待 XR 恢复刚体状态，再判断佩戴距离。未戴上时，展台附近松手吸附复位，其他位置恢复重力。

会话完成事件与区域 successMessage 均来自真实 `IsWorn=true`。成功进入 Scale-up 时保留刚戴上的游客头盔，不立即强制摘下；它继续跟随头部，可按 B/Y 主动取下。其他阶段切换、Skip、Reset 仍执行原清理；暂停期间只保留头部跟随，不处理操作输入。

回归测试步骤：在 The Case 中远离头部松手，确认未完成且没有下一站指引；重新抓取，移动到头部 0.4 米内松手，确认佩戴成功提示、The Case Completed、下一站指引一致；重复松手、暂停恢复和 Reset 后第二轮不能产生旧完成事件。键鼠模拟器仍用 G 抓取，不能用点击扳机代替握把。

## 单向区域顺序与 ACT3 气泡修复

运行顺序固定为 Briefing → The Case（ACT1，含 Inside 佩戴体验）→ Scale-up（ACT2）→ Dress the Diver（ACT3）→ Descent（ACT4）→ Evidence & Reflection。正常完成与工作人员 Skip 使用同一路径，ACT2 后不再返回 ACT1。Inside 枚举数值和数组槽位保留以兼容现有场景引用；其结果同步 ACT1 的 Completed/Skipped，不独立显示在工作人员阶段列表，也不再接受 InsideExplored 推进事件。ACT1 成功后主动取下头盔仅复位头盔，不推进阶段。

NPC 气泡尾部 `DiverSpeechBubbleTail` 继承的 MaskableGraphic 不会自动创建 CanvasRenderer。现已为自定义尾部声明 RequireComponent，在通用 UI 元素工厂中先创建 CanvasRenderer，并在尾部 OnEnable 中补齐旧生成对象的缺失组件。尾部保持非交互对象。重新进入 Play 后，验证进入 ACT3、打开/关闭对话、移动手柄射线和 Reset 第二轮时 Console 不再出现该 MissingComponentException。

## 恢复 NPC 戴盔后的带路

已移除 BeginEscort 中“存在 Experience 控制器便返回 Idle”的阻断。戴好 NPC 头盔后，原感谢动作约 3.5 秒、邀请动作约 2.5 秒，随后自动开始步行；游客自己跟随，NPC 会在游客落后时等待。到平台后继续原绳索介绍和 NPC 下潜动画，玩家仍主动使用下潜按钮/绳索。

NPC 发布带会话 ID 的 EscortStarted，默认现有戴盔任务由协调器在 Update 中消费一次并进入 ACT4。正常 ACT3→ACT4、ACT4→回顾保留已佩戴的 NPC、头盔和带路状态，不再被统一清理复位；Pause 停止活动计时和移动，Resume 继续，Skip/Reset/最终结束仍清理。佩戴反馈、带路和 NPC 下潜不会替代玩家自己的下潜完成事件。

Play Mode 验证：给 NPC 戴盔后看见感谢，约六秒后开始带路，工作人员面板进入 Descent；跟随到平台并下潜。带路中 Pause/Resume 不重播感谢或重新起步；Skip/Reset 后 NPC 和头盔回到初始位置；第二轮可重复完成。
