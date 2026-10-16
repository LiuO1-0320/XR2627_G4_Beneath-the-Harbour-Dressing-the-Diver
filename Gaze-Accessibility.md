# Gaze-Accessibility：头部凝视替代输入

## 当前交付范围

Session 上自动加入 GazeInput 和 AccessibilityUI。默认 Standard，保留现有手柄 Ray / Grab；Accessible 额外开启基于 Camera.main 正前方的 Head Gaze，不需要眼球追踪。Reset 回到 Standard，停留时长保留。启动前的两种模式按钮本身始终支持凝视，避免必须先拿手柄才能启用辅助模式。

默认停留 1.8 秒，可在工作人员面板、辅助菜单或 GazeInput.dwellSeconds 调整 0.5–5 秒。圆环显示进度，移开立即取消；触发后需移开至少 0.25 秒才能再次选中同一个按钮。确认窗口没有倒计时，也没有新增任务完成时限。

已接入：Start、介绍区原有“认识头盔”、Pause/Resume、二次确认 Skip、ACT2 信息选择与文字翻页、区域旁白重播、上下潜、完成后的自由探索及退出。未新增 Next，阶段仍由现有真实互动事件推进。工作人员 IMGUI 面板不参与世界空间射线，游客使用新增的辅助菜单执行对应操作。

M3 明确尚未提供辅助接口。因此佩戴、装配、物理部件操作和捡垃圾不由凝视直接完成，完整无手柄旅程尚未验收。头部凝视 UI 不等于已经提供了凝视步行/传送；展台间移动仍使用现有移动方式或工作人员辅助。当前不得用 Skip 的恢复路径作为物理辅助任务通过的证据。

## 本机测试（不需要改场景）

1. 退出 Play，等待脚本导入与编译，再进入 Play。
2. 在头显/模拟器画面中找到开始前的模式选项。用头部朝向对准 Accessible，保持到圆环填满；也可以在桌面 Experience Staff session controls 勾选 Accessible Mode。
3. 对准 Start，持续凝视开始会话。手柄仍可正常点击、抓取。
4. 看向原介绍区的“认识潜水头盔”按钮，观察圆环。填满前移开，不应触发；一直注视不应反复执行。
5. Accessible 模式中看向“辅助菜单 / Accessibility”展开控制。Pause 后场景活动暂停；先移开目光，再看向同一位置的 Resume 继续。Skip 先进入确认，再看下方独立 Confirm；Cancel 取消。状态或会话改变会使旧确认失效。
6. ACT2 原说明面板右侧有各部件的信息按钮，选中后显示原有中英文说明；Scroll up/down 可阅读剩余内容。这些操作只显示信息，不伪造 ACT2 任务完成。
7. 在上下潜面板测试凝视按钮。Accessible 下跟随面板保留稳定朝向、随用户平移，允许用户转头瞄准；Standard 保留原跟随行为。结束选项和自由探索退出也使用此规则。
8. 一边凝视一边按手柄同一个按钮，每次只执行一次。暂停、Reset、窗口失焦、目标失效或移开时不得继续累积旧进度。
9. Reset 后再次启用 Accessible 并重复上述流程，确认新会话独立。

注意：部分 UI 原本使用 HideAndDontSave，普通 FindObjectsOfType 会漏掉。凝视系统使用 Resources.FindObjectsOfTypeAll，并筛选已加载场景的 World Space Canvas；每秒补查新生成的面板。

## 实现与输入规则

- GazeInput：相机前向射线，检查 Graphic 矩形、遮罩/CanvasGroup 筛选、深度和物理遮挡；忽略玩家 XR Origin 自身的碰撞体。仅提交给 AccessibleButton，隐藏/禁用/不允许当前状态的目标不会触发。
- GazeFeedback：不接收射线的中心光标、圆环进度；本地生成纹理。
- GazeDwell：纯逻辑计时、移开重置、触发锁存及混合输入去重。帧间隔超过 0.1 秒只计 0.1 秒，避免卡顿瞬间触发。
- AccessibleButton：原生 XRI Pointer / Submit 和凝视共用 SubmitOnce。项目自建的普通按钮已改用此类；QuickTravelPressButton 保留按下触发，但走同一入口。未来新增按钮也须使用此类，不要直接 onClick.Invoke 绕过去重。
- 凝视提交位于 LateUpdate，晚于原生 UI Update；相同目标触发后保持锁存直到真正看开，另有 0.35 秒跨输入间隔。
- AccessibilityUI 的会话命令延迟到下一次 Update 执行，并校验点击时的会话与 revision；Skip 确认另捕获会话与 revision。暂停时只允许显式标记的菜单操作，普通任务输入被阻止。
- GazePanelAnchor：辅助模式下阻止跟随面板随转头逃离视线；不改变展台固定面板。辅助菜单在离开视野很远且无凝视目标时重新定位。

## M3 接入（未提供，待协作）

接口文件：Assets/script/AccessibleTaskAction.cs。

M3 的 MonoBehaviour 实现 IAccessibleActionProvider：

- CanExecuteAccessibleAction(actionId, sessionId, stage, out reason)：根据真实部件状态、当前会话及安全条件返回是否允许辅助操作。
- ExecuteAccessibleAction(actionId, capturedSessionId, stage)：执行真正等效操作，例如辅助吸附。异步回调必须保存传入的会话 ID，不能读取之后的新 ID。

在同一场景添加 AccessibleTaskAction，设置 provider、stage、稳定的 actionId 和 caption。菜单按当前阶段显示相应操作，并自动分页；无 provider 或验证失败的按钮禁用并显示原因。Experience 在调用前复核当前会话/revision/阶段/暂停状态。

Experience 不从此接口调用 CompleteTask。M3 必须在实际操作成功后继续发原有成功事件，或使用已接入的 TaskEndpoint.ReportCompleted(capturedSessionId.ToString())；动画未结束、吸附失败等不能发成功。Reset/EndStage 应由提供方取消未完成动作；旧会话的迟到事件由原有接口拒绝。

建议 M3 分别提供真实佩戴、装配连接、部件观察及回收操作。再共同验证一次完整单手／辅助旅程，包括失败恢复、暂停中动作、连续两轮会话。不要把手动勾选成功或工作人员 Skip 作为等效操作。

## 日志与后续移动端

快照增加 accessibleMode、gazeDwellSeconds、gazeTarget、gazeProgress；事件包括 AccessibilityModeChanged、GazeActivated、UIActivated、InformationViewed、AccessibleTaskRequested。不记录连续头部姿态。现有本地日志与快照出口继续使用，未新增网络服务。

## 验证状态

本地运行时 C# 编译通过。Tests/run-experience-state-tests.ps1 共 265 项断言通过，其中凝视新增 20 项，覆盖计时中断、换目标、长注视、抖动、手柄/凝视竞争提交、重置及卡顿。

尚未执行 Unity Play 或头显实测，不能把编译和纯逻辑测试视为射线遮挡、可读性、菜单位置、双输入与所有生成面板交互的验收。1.8 秒只是初始值，须通过目标访客的可用性测试调整。M3 未接入前，完整凝视辅助物理任务旅程明确未完成。
