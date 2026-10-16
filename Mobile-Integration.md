# Experience 移动端接入契约（v1）

本次实现 Unity 侧数据与命令边界，不包含网络监听器、服务器、iPad 页面或移动应用。原有本机工作人员按钮已通过同一命令分发器执行。无需更改现有场景，SessionController 会自动添加 SessionTelemetry。

## 已提供的接口

从 `StageCoordinator.Current.Controller.Telemetry` 获取实例。

| API | 用途 | 线程 |
| --- | --- | --- |
| `GetSnapshot()` / `GetSnapshotJson()` | 读取完整状态 | Unity 主线程 |
| `SnapshotJson` 事件 | 状态变化后、以及每秒发送快照，可作为心跳 | Unity 主线程 |
| `EventJson` 事件 | 按序发送单条操作日志 | Unity 主线程 |
| `EventsAfter(long sequence)` | 补取最近 512 条事件；超出缓存时 `gap=true` | Unity 主线程 |
| `SubmitJson(string json)` | 将控制命令排队，返回是否成功入队 | 可从网络线程调用 |
| `ReplyJson` 事件 | 命令实际执行或拒绝后的回执 | Unity 主线程 |
| `ExecuteLocal(string action)` | 本机工作人员命令入口 | Unity 主线程 |
| `TaskEndpoint.ReportProgress(capturedSessionId, detail)` | M3 上报语义进度，不判定任务完成 | Unity 主线程 |

事件订阅者应把输出字符串放入自己的发送队列，禁止在回调中等待网络、访问慢速服务或重新同步执行控制命令。连接关闭时取消订阅。不要从网络线程访问 Unity 对象、读取快照或直接调用 Session。

`SubmitJson=true` 仅代表入队，必须等回执才能在移动端显示成功。队列最多 64 条，单条最多 4096 字符，每帧最多执行 8 条。入队失败由网络适配器向请求方返回 Busy/InvalidPayload；语法错误进入队列后返回 InvalidCommand（无可解析 ID 时 commandId 为空）。不要暴露任意 Unity 方法调用接口。

## 快照含义

- `protocolVersion`：当前 1。
- `runtimeId`：每次 Unity 控制器创建时生成，防止应用重启后接收旧控制命令。
- `sessionId`：每名游客独立 GUID；Idle 为全零 GUID。
- `revision`：状态机版本，控制命令的并发校验依据。
- `eventSequence`：运行实例内递增的日志序号，与 revision 不同。
- `utc`、`sessionStartedUtc`：UTC ISO 8601 时间。
- `status`、`stage`、`stages[]`：会话状态、当前目标阶段及各阶段结果（Completed 与 Skipped 区分）。Inside 为 ACT1 的兼容记录，不应作为移动端独立路线节点。
- `reflection`：游客总结选项。
- `availableCommands`：当前可请求命令；最终仍以执行时校验为准。
- `ready`：控制器已初始化且没有控制执行异常，不代表所有 M3 任务已配置，也不代表游客准备完毕。新游客可开始须同时满足 Idle、ready、Start 可用，并检查 integrationStatus。
- `integrationStatus`：M3 未配置、路线缺失等接入提示。
- `fault`：控制操作异常。发生后拒绝 Start/Pause/Resume/Skip，只允许重新 Reset。Reset 成功后清除。
- `logError`：本地日志写入异常；不阻断游客体验，但应提醒工作人员。
- `lastRuntimeError`：最近的 Unity Error/Exception/Assert 摘要，最多 2048 字符，仅诊断用途；非完整 Console 日志，也不是故障已解决标记。
- `fittedHelmets`、`heldObjects`、`ropeTravelling`：当前有效对象的佩戴数量、抓取数量和绳索旅行状态。
- `travelling`、`routeDestination`：是否仍需走向路线目标、目标名称。
- `locationEvidence`：Unknown / TravellingToTarget / TargetArrivalObserved。到达证据保留至路线切换，**不是持续定位**。
- `observedRegion`：当前头显位于哪个已配置区域；没有区域检测体或在区域外为 Unknown，重叠为 OverlappingRegions。

需要实际区域定位时，在 StageCoordinator 的各 StageContent.arrivalArea 配置 Collider，包围完整站立空间及头部高度。推荐独立 BoxCollider（Is Trigger），不要使用头盔模型表面作为区域。该体积同时用于路线到达判断；进入区域只影响定位/引导，绝不自动完成任务。现有场景未自动添加这些检测体。

## 控制命令与回执

示例（各 ID 和 revision 必须使用最新快照中的真实值）：

```json
{
  "protocolVersion": 1,
  "commandId": "每次新操作生成的唯一UUID",
  "runtimeId": "最新快照的runtimeId",
  "sessionId": "最新快照的sessionId",
  "expectedRevision": 12,
  "action": "Skip"
}
```

支持 Start、Pause、Resume、Skip、Reset；没有 CompleteTask 或任意跳阶段命令。真实完成仍由场景交互与 M3 的完成事件触发。Reset 同时承担结束当前体验并准备下一轮的功能。

回执包含 commandId、accepted、code、message、runtimeId、sessionId、revision。成功为 Applied；拒绝类型包括 InvalidCommand、UnsupportedVersion、WrongRuntime、StaleState、UnknownAction、InvalidState、ExecutionFailed。

最近 512 个命令 ID 缓存原回执：重发相同 ID 不再次执行；此回执描述原执行结果，不是当前快照。超过缓存的旧成功命令仍会因旧 revision/sessionId 被拒绝。命令 ID 不得复用于另一操作。两个工作人员使用同一 revision 同时操作时，最多一个改变状态，其余必须刷新状态再由工作人员决定，禁止自动把拒绝的命令换成最新 revision 重发。

## 日志与复位

日志为 `Application.persistentDataPath/SessionLogs/<runtimeId>-<segment>.jsonl`，UTF-8，每行一个 JSON。每个文件约 4 MiB 分段；历史文件目前不会自动删除，部署时应按场馆要求配置服务端归档、磁盘容量监测和保留周期。无需游客姓名等个人信息。

每条事件包含协议版本、UTC 时间、运行实例、会话、阶段、状态版本、序号、kind、subject、detail。记录会话状态及阶段结果、命令请求/回执、Reset 开始/完成、会话结束摘要、头盔抓取/释放/取消/佩戴成功/失败、零件拆装、NPC 行为变化、绳索升降/取消、路线开始/到达、垃圾收集、M3 进度/完成上报。恢复操作也可能产生拆装/取消事件，分析时应结合 CommandRequested、ResetStarted 和状态记录，而非把每条事件都视为游客主动完成。

本地文件按低频语义事件同步写入；不记录逐帧头显姿态。不适合作为高频遥测接口。网络服务应按 `(runtimeId, sequence)` 去重存储，并在认证层补充真实工作人员身份，不能相信浏览器自行填写的身份。

Reset 通过现有同步清理流程复位头盔、NPC、绳索、媒体、M3 活动及导航；现新增海底垃圾收集复位，先取消抓取再恢复物品。只有同步回调返回且没有异常才发送 Applied 与 ResetCompleted；异常保留 fault，界面不能仅凭 Idle 判定成功。尚未引入异步 Resetting 状态、跨帧完成确认或所有对象的逐项位置校验；未来异步模块必须增加完成确认后再发成功回执。M3 的 ResetActivity 目前必须同步完成，不能启动异步重置后立即返回。

ACT2 仍需 M3 提供实际完成规则并接入 TaskEndpoint；本次日志和移动控制准备不替代该逻辑。暂停覆盖范围沿用现有活动实现，不等于冻结所有 Unity 物理对象；部署前仍需实机验证各活动暂停和手持物体复位。

## 后续连接移动端的实施步骤

1. 确定 PCVR 或一体机部署。建议场馆局域网服务承载设备连接、鉴权、日志存储和移动端入口；PCVR 可与 Unity 在同一台电脑运行。一体机可主动连接场馆服务，避免要求 iPad 直接访问头显监听端口。
2. 添加 Unity 网络适配器（例如 `RemoteTransport`），连接场馆服务。订阅三个 JSON 事件并异步发送；收到经过认证、已验证设备目标的控制消息后仅调用 SubmitJson。建立连接/重连后的快照读取需要排到主线程，或等待下一次 SnapshotJson。
3. 服务定义有类型的消息封装，例如 `{ "type": "snapshot|event|command|reply", "deviceId": "...", "payload": {...} }`。绑定持久化设备 ID 与当前 runtimeId；身份认证、工作人员权限、TLS、设备配对、限流均由这一层落实。当前 runtimeId 不是设备永久身份，现有协议本身也不是认证机制。
4. 先做移动端只读监控：展示状态、实际区域、操作时间线、集成提示、异常和连接新鲜度。超过约 3 个心跳周期未收到快照应标记离线/数据过期，禁用控制；Unity 断网时继续本地体验，不自动暂停或重置。
5. 再接入控制按钮：使用 availableCommands，点击时带最新 sessionId/revision，显示“等待回执”，收到 Applied 后再更新成功提示。重连先读取快照和补取事件；gap=true 时显示历史不完整并向服务查询归档。断线期间不排队自动重放 Skip/Reset。
6. 多设备时服务按设备路由，不把一个头显的控制广播给其他头显。移动端操作身份与审计由服务保存。同步控制冲突最终仍由 Unity revision 校验处理。
7. 验收：双端同时 Skip、重复消息、断线丢回执后相同 ID 重试、应用重启后旧命令、上一游客的命令延迟到达、抓物时 Reset、NPC 带路中 Reset、下潜中 Pause/Reset、连续两轮真实体验、日志磁盘写入失败。

## 本次验证边界

`Tests/run-experience-state-tests.ps1`：111 项会话断言、18 项命令断言。运行时程序集使用项目本地 Unity 引用编译通过。未完成 Unity PlayMode、头显实机、多设备网络联调或异步复位验证；网络传输和移动端尚未实现。

2026-10-15 接口更新：实际水下任务为 SeabedCleanup（保留阶段数值 6），不再使用 EvidenceAndReflection。快照移除 reflection，改为 cleanupCollected、cleanupTotal、cleanupCompleted。完成条件与验证流程见 Experience-Ending.md 最新章节。历史日志仍保留历史字段及名称。

最新结束接口：阶段数值 7 的名称为 ExperienceComplete；快照 firstPlatformReturnCompleted 表示清理任务完成或被工作人员跳过后，实际返回水面平台。仅浮出水面不足以结束体验。

Global-Reset：Reset 命令统一进入 GlobalReset，两阶段调用 IResettable，并兼容旧 IStageActivity。快照新增 resetInProgress、lastResetSucceeded、lastResetDurationSeconds、completedResets、resetError；日志新增 GlobalResetStarted / GlobalResetCompleted / GlobalResetFailed。同步执行期间通常不会产生周期性快照，因此移动端应在提交后显示等待回执，不能依赖一定收到 resetInProgress=true。成功依据 Applied、fault 为空及 lastResetSucceeded=true；失败显示 resetError，保留 Reset 重试入口。已存在的命令去重和版本校验继续有效。具体操作与三轮实机验收见 Global-Reset.md。

Gaze-Accessibility 快照增加 accessibleMode、gazeDwellSeconds、gazeTarget、gazeProgress；凝视/原生 UI 操作共享防重复入口。辅助模式可在体验开始前由本机工作人员设置，当前未新增移动端网络命令。M3 辅助动作接口与验收缺口见 Gaze-Accessibility.md。
