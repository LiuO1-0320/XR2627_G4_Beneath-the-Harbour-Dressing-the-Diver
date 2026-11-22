# Gaze-Accessibility：头部凝视替代输入

## 当前交付范围

Session 上自动加入 GazeInput 和 AccessibilityUI。默认 Standard，保留现有手柄 Ray / Grab；Accessible 额外开启基于 Camera.main 正前方的 Head Gaze，不需要眼球追踪。Reset 回到 Standard，停留时长保留。启动前的两种模式按钮本身始终支持凝视，避免必须先拿手柄才能启用辅助模式。

默认停留 1.8 秒，可在工作人员面板、辅助菜单或 GazeInput.dwellSeconds 调整 0.5–5 秒。圆环显示进度，移开立即取消；触发后需移开至少 0.25 秒才能再次选中同一个按钮。确认窗口没有倒计时，也没有新增任务完成时限。

工作人员在待机时通过桌面工作人员面板选择「展览 / Exhibition」或「小组测试 / Team test」。每次启动默认为展览，Reset 保留本次运行的选择。展览由工作人员 Start 和 Skip；两种模式中，游客均可选择输入方式，并在体验菜单中暂停或继续。小组测试另开放头显 Start 和二次确认 Skip。

Briefing 的训练球支持直接凝视拾起，再凝视教程卡片上的「放开小球」完成练习。ACT1 已接入游客头盔的真实试戴操作。游客在头盔展台前打开「体验菜单 / Menu」，凝视「试戴头盔 / Try on helmet」即可佩戴。ACT2 展台的七个零件也支持凝视拆下、转动观察和装回。ACT3 支持凝视为潜水员戴上头盔，再跟随他前往港口。ACT4 可以直接凝视海底的垃圾，逐件拾起并放进绿色回收篮。

## 本机测试（不需要改场景）

1. 退出 Play，等待脚本导入与编译，再进入 Play。
2. 在头显/模拟器画面中找到开始前的模式选项。用头部朝向对准 Accessible，保持到圆环填满；也可以在桌面 Experience Staff session controls 勾选 Accessible Mode。
3. 展览模式由桌面工作人员点击 Start。测试头显快捷操作时，在待机的工作人员面板先选「小组测试」，再用手柄或凝视点击头显 Start。
4. 按顺序点亮介绍区教程卡片的左、右按钮。蓝球出现后，凝视它直到圆环填满。先停一会儿，确认卡片仍在第三步，再凝视「放开小球 / Let go of the ball」直到圆环填满。看到成功反馈后，单独选择「开始探索」进入 ACT1。
   另开一轮，在凝视持球中暂停或切回手柄模式，确认第三步仍待完成。用手柄抓球时，应由抓球的那只手柄松开握把才成功。
5. 打开「体验菜单 / Menu」，使用 Pause 和 Resume。标准模式使用手柄，辅助模式也可凝视。只有小组测试显示游客 Skip，点击后仍须确认；状态或会话改变会使旧确认失效。
   在 ACT1 展台外检查「试戴头盔」不可用。进入 ACT1 到达区再选择它，确认头盔跟随头部、出现原有佩戴成功提示、工作人员面板将 The Case 记为 Completed 并进入 Scale-up。暂停或用手柄抓着头盔时无法辅助佩戴。
6. 到 ACT2 展台前，打开「体验菜单 / Menu」。凝视「拆下下一件 / Detach next part」，零件会出现在你与展台之间，展台显示该件的中英文介绍。凝视「转动观察 / Turn to inspect」可以换个角度，移开后再凝视可继续旋转。看完选择「装回原位 / Refit part」，再取下一件。可以随时暂停，也可以换手柄接手。
   在最后一件装回前，工作人员面板应停在 Scale-up。装回全部七件并松手后，面板才记录 Completed 并推进到 Dress the Diver。单独阅读右侧信息按钮、走到展台或使用「散落零件归位」都不会完成这项任务。
7. 当工作人员面板显示 Dress the Diver，打开「体验菜单 / Menu」。先在展台外查看「为潜水员戴头盔 / Fit diver's helmet」是否禁用，再走到 ACT3 潜水员装备展台前。凝视这个按钮直到圆环填满，确认展台上的头盔戴到潜水员头上并随他点头。也可以用手柄抓起头盔，移到潜水员头边松手。手柄正抓着头盔时，凝视按钮应保持禁用。
   潜水员会先道谢，再邀请你同行。这时工作人员面板应仍显示 Dress the Diver。潜水员开始带路时，面板才应将 Dress the Diver 记为 Completed 并进入 Descent。跟着他走向港口。途中停下、落后一段距离时，他会停下等候，等你跟上再继续走。
   测试暂停时机时，可在潜水员致谢期间点 Pause。点 Resume 后他应接着邀请你出发，整次装配只开始一次带路。
8. 在港口绳索旁，凝视「下潜」到达海底。靠近一个瓶子、罐子或包装，在 3 米内对准这件垃圾，保持到凝视圆环填满。一次可携带一件；此时查看篮旁计数，应与拾取前相同。
9. 带着这件垃圾回到绿色回收篮旁，在 3 米内对准篮壁或篮底，保持到圆环填满。垃圾入篮后，计数应加一。换另一件重复；也可以用手柄抓取垃圾，在篮内松手。检查混合操作时，同一件垃圾应只计一次。
10. 第九件入篮后，确认篮旁显示 9/9 或全部回收的提示，工作人员面板记录清理完成。此时仍在海底，画面应只提示返回平台。走到绳索旁，凝视「上浮」；直到沿绳上升并到达陆地平台，才应出现整段体验结束的选择。另开一轮，在清理中途请工作人员 Skip，确认面板记录 Skipped，游客同样要沿绳回到平台。
11. 选择自由探索后，可以回到篮旁选择「重新收集 / Reset cleanup」，再次用凝视或手柄清理。复玩应保留自由探索状态。上下潜跟随面板、结束选项和自由探索退出在 Accessible 下保持稳定朝向，允许转头瞄准。
12. 一边凝视一边按手柄同一个按钮，每次只执行一次。暂停、Reset、窗口失焦、目标失效或移开时不得继续累积旧进度。
13. 全局 Reset 后再次启用 Accessible 并开始新一轮，确认九件垃圾均回到起点，回收计数从零开始。

注意：部分 UI 原本使用 HideAndDontSave，普通 FindObjectsOfType 会漏掉。凝视系统使用 Resources.FindObjectsOfTypeAll，并筛选已加载场景的 World Space Canvas；每秒补查新生成的面板。

## 实现与输入规则

- GazeInput：相机前向射线，检查 Graphic 矩形、遮罩/CanvasGroup 筛选、深度和物理遮挡；忽略玩家 XR Origin 自身的碰撞体。界面按钮通过 AccessibleButton 接收选择。Briefing 训练球及 ACT4 的垃圾与回收篮支持直接凝视实物；隐藏、禁用或不允许当前状态的目标不会触发。
- GazeFeedback：不接收射线的中心光标、圆环进度；本地生成纹理。
- GazeDwell：纯逻辑计时、移开重置、触发锁存及混合输入去重。帧间隔超过 0.1 秒只计 0.1 秒，避免卡顿瞬间触发。
- AccessibleButton：原生 XRI Pointer / Submit 和凝视共用 SubmitOnce。项目自建的普通按钮已改用此类；QuickTravelPressButton 保留按下触发，但走同一入口。未来新增按钮也须使用此类，不要直接 onClick.Invoke 绕过去重。
- 凝视提交位于 LateUpdate，晚于原生 UI Update；相同目标触发后保持锁存直到真正看开，另有 0.35 秒跨输入间隔。
- AccessibilityUI 的会话命令延迟到下一次 Update 执行，并校验点击时的会话与 revision；Skip 确认另捕获会话与 revision。暂停时只允许显式标记的菜单操作，普通任务输入被阻止。
- GazePanelAnchor：辅助模式下阻止跟随面板随转头逃离视线；不改变展台固定面板。辅助菜单在离开视野很远且无凝视目标时重新定位。

## 物理任务辅助接口

接口文件：Assets/script/AccessibleTaskAction.cs。

M3 的 MonoBehaviour 实现 IAccessibleActionProvider：

- CanExecuteAccessibleAction(actionId, sessionId, stage, out reason)：根据真实部件状态、当前会话及安全条件返回是否允许辅助操作。
- ExecuteAccessibleAction(actionId, capturedSessionId, stage)：执行真正等效操作，例如辅助吸附。异步回调必须保存传入的会话 ID，不能读取之后的新 ID。

在同一场景添加 AccessibleTaskAction，设置 provider、stage、稳定的 actionId 和 caption。菜单按当前阶段显示相应操作，并自动分页；无 provider 或验证失败的按钮禁用并显示原因。Experience 在调用前复核当前会话/revision/阶段/暂停状态。

Experience 不从此接口调用 CompleteTask。M3 必须在实际操作成功后继续发原有成功事件，或使用已接入的 TaskEndpoint.ReportCompleted(capturedSessionId.ToString())；动画未结束、吸附失败等不能发成功。Reset/EndStage 应由提供方取消未完成动作；旧会话的迟到事件由原有接口拒绝。

ACT2 的 HelmetDetailInspection 从展台配置读取零件，在运行时注册三个辅助操作。每个零件由 HelmetPartVisit 记录拆下、观察和归位的状态。展台沿用原有的 Scale-up 完成事件。ACT3 的辅助操作 fit-diver-helmet 复用手柄的头盔佩戴逻辑。潜水员戴好头盔并开始带路时，原有的 EscortStarted 事件通知会话控制器完成 Dress the Diver。ACT4 的 SeabedCleanup 对手柄与凝视投放共用入篮计数，SessionState.ReportCleanupProgress 记录已回收件数。沿绳上浮到平台后，DiveRopeStation 发出的 PlatformReturnCompleted 事件才会触发整段体验结束。

## 日志与后续移动端

快照增加 accessibleMode、gazeDwellSeconds、gazeTarget、gazeProgress；事件包括 AccessibilityModeChanged、GazeActivated、UIActivated、InformationViewed、AccessibleTaskRequested。不记录连续头部姿态。现有本地日志与快照出口继续使用，未新增网络服务。

## 验证状态

运行 Tests/run-experience-state-tests.ps1 可复查凝视计时、中断、长注视、混合输入与会话状态。ACT2 的 64 项零件断言覆盖逐件拆装、最后一件未归位、重复观察、手柄接手、回收、自由探索与新会话。

在隔离的团结引擎 Play Mode 中，ACT2 使用原头盔模型和 XRI 3.1.2 通过 92 项检查，包括七个真实零件的拆卸、旋转、装回、完成时机和复位。另用模拟相机逐个瞄准体验菜单与三个动作按钮，验证前部视窗完成一次凝视拆装、长时间注视只提交一次，共通过 14 项检查。隔离测试使用模拟区域与场景管理。完整 BasicScene 的视线遮挡、菜单可读性与停留时长仍需在 Quest 3 上体验确认。

ACT3 的隔离 Play Mode 测试使用项目头盔模型、真实的会话控制器，以及简化的展台与港口路线。模拟相机凝视菜单完成佩戴后，潜水员依次致谢、邀请、发出一次 EscortStarted，并戴着头盔走到终点。测试也覆盖暂停、继续、Skip、全局 Reset 和第二轮体验。完整展厅里的路线和视线遮挡仍待 Quest 3 实测。

ACT4 的隔离 Play Mode 测试使用真实凝视输入、XRI 抓取、九件垃圾、回收篮与绳索。已验证拾起和携带不计数、篮内投放每件只记一次、清理完成后仍停留在海底、沿绳上浮到平台才进入结束阶段。工作人员跳过、暂停恢复、全局复位和自由探索重新收集也已验证。完整展厅的物件可见性和头戴舒适度仍待 Quest 3 实测。

## 完整 BasicScene 凝视回归

在团结引擎 1.6.13 的 Play Mode 中，以同一位游客从待机走到结束面板。使用 integration 工作树的完整场景、展台、物理遮挡和任务组件。工作人员从原有的 Start 命令入口启动，游客通过头部凝视选择。测试驱动模拟头部姿态与游客位置，将该轮停留时间设为 0.65 秒，护送和绳索行程用三倍游戏时间运行。Quest 3 真机佩戴舒适度、画面清晰度和移动输入仍需现场验证。

| 环节 | 实测结果 |
| --- | --- |
| Briefing | 依次点亮左、右按钮后训练球出现。凝视拿起、放开后才通过练习。持续看着放开按钮不会跳过出发确认。 |
| ACT1 | 到达试戴展台后，通过体验菜单为自己佩戴头盔，原佩戴事件推进阶段。 |
| ACT2 | 凝视前部视窗可打开介绍。整盔与七个可拆件的碰撞体各自归属正确，七件逐一拆下、观察、装回，最后一件归位后才进入 ACT3。 |
| ACT3 | 在先前被展台遮挡的站位面朝同一方向，经体验菜单为潜水员戴上头盔。潜水员发出一次带路事件，自行走到码头。 |
| ACT4 | 凝视绳旁面板下潜。每次凝视拾起垃圾时计数保持原值，再凝视回收篮才增加一件。第九件入篮后，水下和沿绳上浮途中都没有结束提示。抵达平台后才显示可选择的「自由探索」与「退出体验」。 |

末轮会话 `ecb93f30-b9ac-4740-bfca-410dbaecb6c1` 通过 160 项检查，旅程期间未捕获应用错误、异常或断言失败。会话日志文件为 `9727172e-df52-45b2-8dd9-b25ef3ea2ee7-0.jsonl`。事件 188 记录第九件入篮，189 仍是海底清理，192 开始上浮，194 头部出水，195 绳索上浮完成，196 才进入 `ExperienceComplete`。
