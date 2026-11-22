# Experience 移动端接入契约（v1）

## 用 iPad 控制正在运行的 BasicScene

1. 在连接头显的 Windows 电脑打开 `BasicScene` 并按 Play。在 Game 视图左上角展开 **工作人员控制 / Staff**。
2. 在 **iPad 工作人员控制 / Staff web** 下点击电脑的局域网地址（如 `192.168…`），将复制的**完整控制链接**传到工作人员 iPad，在同一局域网的 Safari 中打开。本机测试可以复制 `127.0.0.1` 对应的链接。直接输入基础地址（默认端口 `8765`）可打开监看页。
3. 在网页顶部点 **Start**。当前允许使用的按钮会随 Unity 状态更新，可以点 **Pause** 暂停、**Resume** 继续。点 **Skip** 或 **Reset** 后，先核对确认区中的游客和阶段，再点 **确认跳过** 或 **确认复位**。取消时 Unity 保持原状态。
4. Reset 成功后设备回到待机。换人时，旧标签页显示新状态并锁住按钮。核对游客会话号或待机轮次，点 **启用本次控制** 后可继续操作。重新进入 Play 时须从 Unity 复制新控制链接。

完整链接含本次 Unity 运行的配对凭据，仅交给工作人员，在受信任的展览局域网内使用。浏览器取得凭据后会清除地址栏中的密钥。Unity Console 只输出监看地址。需要更换端口时，在编辑模式的 Stage Coordinator 中调整 **Staff Monitor Port**。

### 游客、进度与操作记录

- “游客会话”的八位字符与 Unity 工作人员面板 Session 的开头一致。ACT2 展示观察和装回的件数，ACT4 展示入篮数与返岸状态，跳过与实际完成分别记录。
- **最近操作**默认折叠。点击展开可查看本次游客最近最多 30 条操作及时间。Unity 持续把完整语义事件写到 `Application.persistentDataPath/SessionLogs/<runtimeId>-<segment>.jsonl`，网页关闭也会记录。工作人员请求和回执带有来源，如 `StaffWeb` 或 `StaffDesktop`，可以与游客交互事件区分。
- 浏览器每秒读取同源 `GET /api/view`。一次响应包含同一运行、同一游客的快照及对应序号内的最近操作。后台切回、失联或运行变化时，页面重新同步，再决定按钮是否可用。

### 控制接口与换人保护

`POST /api/command` 接受五种网页操作，要求同源、JSON 正文和工作人员 Bearer 凭据。正文带 `runtimeId`、`sessionId`、`expectedRevision` 及唯一 `commandId`。只有 Unity 主线程通过现有会话控制器执行并返回 `accepted=true` 后，页面才提示成功。浏览器不会自动重发超时请求。传输失败时应先核对最新状态和操作记录。

准备 Skip 或 Reset 时会记住当时的游客和状态版本。确认前发生换人、阶段更改或离开页面，原确认会取消。延迟到达的旧请求由 Unity 再次校验并拒绝。待机的会话号固定为全零，因此跨次待机还要核对版本。重复提交同一条已执行的请求不会执行第二次；换人后这条旧请求也会收到过期回执。

### 用 BasicScene 复测

| 操作 | 核对结果 |
| --- | --- |
| 用两个 iPad 浏览器标签打开同一个工作人员链接，在第一个点 Start | 两页看到相同游客。发起 Start 的标签继续控制，另一个需要主动接管。 |
| Pause、Resume，然后点 Skip、取消，再次点 Skip 并确认 | 暂停和继续反映在头显会话。取消保持阶段，确认只前进一次，并将原阶段记为 Skipped。 |
| 一个标签保留 Reset 确认，另一端 Reset 并 Start 下一位 | 原确认关闭，旧标签按钮锁住。Unity 拒绝旧游客的延迟请求，新游客的阶段和进度保持原样。 |
| 点击 Reset 并确认，再开始下一位 | 待机时最近操作清空，下一位取得独立会话号。上一位的事件仍保存在本机日志。 |

可在空闲的 **测试用 BasicScene** 运行 `python Tests/check-mobile-controls.py` 并在提示时粘贴完整控制链接。该脚本会真实启动、跳过和复位测试会话。`Tests/run-staff-monitor-http-tests.ps1` 可单独验证 HTTP 和主线程队列。

复测使用真实 BasicScene 与 Windows 展览电脑的局域网地址。iPad 触屏尺寸的浏览器通过 29 项界面检查，包括五个按钮、二次确认、双击和换人。两个工作人员同时跳过同一阶段时，仅一条请求生效。身份过期的页面保持锁定，第二位游客的阶段和操作记录均独立。实体 iPad 的 Safari 与场馆 Wi-Fi 仍需到现场联测。

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
- `cleanupCollected`、`cleanupTotal`、`cleanupCompleted` 和 `firstPlatformReturnCompleted`：海底回收进度与返岸完成状态。
- `partsObserved`、`partsRefitted` 和 `partsTotal`：当前 ACT2 零件的观察、装回和总数，在其他阶段为 0。
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

回执包含 commandId、accepted、code、message、runtimeId、sessionId、revision。成功为 Applied；拒绝类型包括 InvalidCommand、UnsupportedVersion、WrongRuntime、StaleState、UnknownAction、InvalidState、ExecutionFailed。网页端复用同一 ID 提交不同指令时会收到 CommandIdConflict。

最近 512 个命令 ID 缓存原回执：重发相同 ID 不再次执行；此回执描述原执行结果，不是当前快照。超过缓存的旧成功命令仍会因旧 revision/sessionId 被拒绝。命令 ID 不得复用于另一操作。两个工作人员使用同一 revision 同时操作时，最多一个改变状态，其余必须刷新状态再由工作人员决定，禁止自动把拒绝的命令换成最新 revision 重发。

## 日志与复位

日志为 `Application.persistentDataPath/SessionLogs/<runtimeId>-<segment>.jsonl`，UTF-8，每行一个 JSON。每个文件约 4 MiB 分段；历史文件目前不会自动删除，部署时应按场馆要求配置服务端归档、磁盘容量监测和保留周期。无需游客姓名等个人信息。

每条事件包含协议版本、UTC 时间、运行实例、会话、阶段、状态版本、序号、kind、subject、detail。记录会话状态及阶段结果、命令请求/回执、Reset 开始/完成、会话结束摘要、头盔抓取/释放/取消/佩戴成功/失败、零件拆装、NPC 行为变化、绳索升降/取消、路线开始/到达、垃圾收集、M3 进度/完成上报。恢复操作也可能产生拆装/取消事件，分析时应结合 CommandRequested、ResetStarted 和状态记录，而非把每条事件都视为游客主动完成。

ACT2 的归位事件使用同一套零件键。`detail` 标识具体零件，例如前部视窗的 `forward`。工作人员按 `kind` 区分操作来源。

| kind | 触发条件 | 计入游客主动装回次数 |
| --- | --- | --- |
| `PartRestored` | 游客用手柄在安装位置附近松手，或完成观察后用凝视选择装回 | 是 |
| `LoosePartRecovered` | 点击“散落零件归位”，实际收回一个已拆零件 | 否 |
| `PartReset` | 展台重置、切换阶段或全局 Reset 使一个已拆零件归位 | 否 |

已经在位的零件不会再次产生归位事件。统计正式 ACT2 的操作时，筛选该游客的 `sessionId`、`stage=ScaleUp` 和 `kind=PartRestored`。统计装回过多少种零件时，再按 `detail` 去重。判断阶段是否完成请读取快照中的阶段结果。修复前的历史 `PartRestored` 混有自动归位，单凭旧记录无法还原来源。

本地文件按低频语义事件同步写入；不记录逐帧头显姿态。不适合作为高频遥测接口。网络服务应按 `(runtimeId, sequence)` 去重存储，并在认证层补充真实工作人员身份，不能相信浏览器自行填写的身份。

Reset 通过现有同步清理流程复位头盔、NPC、绳索、媒体、M3 活动及导航；现新增海底垃圾收集复位，先取消抓取再恢复物品。只有同步回调返回且没有异常才发送 Applied 与 ResetCompleted；异常保留 fault，界面不能仅凭 Idle 判定成功。尚未引入异步 Resetting 状态、跨帧完成确认或所有对象的逐项位置校验；未来异步模块必须增加完成确认后再发成功回执。M3 的 ResetActivity 目前必须同步完成，不能启动异步重置后立即返回。

ACT2 由展台脚本在全部七件均已观察、装回并松手后上报完成。暂停覆盖范围沿用现有活动实现，不等于冻结所有 Unity 物理对象；部署前仍需实机验证各活动暂停和手持物体复位。

## 多设备部署

当前一台 Windows 展览电脑负责一套头显，并通过其局域网地址提供工作人员网页。多机位部署时应分别标记机位和地址。若今后通过共享网络或场馆网关集中管理多套头显，应为每台设备分配设备标识，加入 HTTPS 与工作人员账号，并按设备转发命令，沿用会话及版本校验。

## 早期命令接口的验证记录

`Tests/run-experience-state-tests.ps1`：111 项会话断言、18 项命令断言。运行时程序集使用项目本地 Unity 引用编译通过。此处记录早期控制协议测试。当前局域网工作人员控制台用法见本文开头。

2026-10-15 接口更新：实际水下任务为 SeabedCleanup（保留阶段数值 6），不再使用 EvidenceAndReflection。快照移除 reflection，改为 cleanupCollected、cleanupTotal、cleanupCompleted。完成条件与验证流程见 Experience-Ending.md 最新章节。历史日志仍保留历史字段及名称。

最新结束接口：阶段数值 7 的名称为 ExperienceComplete；快照 firstPlatformReturnCompleted 表示清理任务完成或被工作人员跳过后，实际返回水面平台。仅浮出水面不足以结束体验。

Global-Reset：Reset 命令统一进入 GlobalReset，两阶段调用 IResettable，并兼容旧 IStageActivity。快照新增 resetInProgress、lastResetSucceeded、lastResetDurationSeconds、completedResets、resetError；日志新增 GlobalResetStarted / GlobalResetCompleted / GlobalResetFailed。同步执行期间通常不会产生周期性快照，因此移动端应在提交后显示等待回执，不能依赖一定收到 resetInProgress=true。成功依据 Applied、fault 为空及 lastResetSucceeded=true；失败显示 resetError，保留 Reset 重试入口。已存在的命令去重和版本校验继续有效。具体操作与三轮实机验收见 Global-Reset.md。

Gaze-Accessibility 快照增加 accessibleMode、gazeDwellSeconds、gazeTarget、gazeProgress；凝视/原生 UI 操作共享防重复入口。辅助模式可在体验开始前由本机工作人员设置，凝视设置保留在本机面板。M3 辅助动作接口与验收缺口见 Gaze-Accessibility.md。
