# Beneath the Harbour — 场景导览与功能介绍 / Visitor Guide & Features

**港湾之下 · 为潜水员穿戴装备**

**Beneath the Harbour · Dressing the Diver**

更新日期 / Updated: 2026-10-17

欢迎来到海港潜水装备馆。这里的体验从岸上的博物馆开始：认识铜制潜水头盔，亲手观察和拆装零件，再走进潜水员的工作室，帮助他准备下潜。最后沿绳索进入水下港湾，探索海底并参与垃圾回收。

Welcome to the harbour diving equipment gallery. Begin in the museum on shore: discover the copper diving helmet, inspect and disassemble its parts, then enter the diver’s workshop to help him prepare for a dive. Finally, follow the rope into the underwater harbour, explore the seabed and collect litter.

本文介绍当前项目中的参观路线、交互方式和场景调整入口。体验场景为 `Assets/Scenes/BasicScene.unity`。

This guide covers the visitor route, interactions and scene editing options currently available in the project. The experience scene is `Assets/Scenes/BasicScene.unity`.



|  | group member | student id |
| --- | --- | --- |
| m1 | KWOK San Chin | 22009726g |
| m2 | LIN Wanting | 26051852g |
| m3 | LIU Luiyi | 26054562g |
| m4 | RAN Kehan | 26126733g |
| m5 | XIAO Wayuan | 26124677g |
| m6 | XU Siyuan | 26069603g |


## 参观路线 / Visitor Route

**工作人员启动 → 欢迎介绍与操作教程 → ACT 1 认识与试戴 → ACT 2 拆装观察 → ACT 3 潜水员工作室 → ACT 4 沿绳下潜 → 海底探索与清理 → 沿绳上浮返回平台 → 自由探索或退出**

**Staff Start → Welcome & Tutorial → ACT 1 Discover & Try On → ACT 2 Disassemble & Inspect → ACT 3 Diver’s Workshop → ACT 4 Rope Descent → Seabed Exploration & Cleanup → Return to the Platform → Free Exploration or Exit**

团队将各展项连接为统一的体验流程。系统根据真实的点击、佩戴、装配和任务完成事件判断进度，记录操作并触发后续环节；进入区域、听完旁白或使用快捷导航本身不会算作任务完成。完成展项后，用户按引导自行前往下一处，不会因为阶段结束而被直接传送。

The exhibits form one coordinated visitor journey. Progress follows actual selections, helmet fitting, assembly and task completion events, with actions recorded for review. Entering an area, finishing narration or using quick travel does not itself complete a task. After an exhibit, follow the guidance to the next area; stage completion does not automatically teleport you there.

| 区域 / Area | 主要体验 / Main Experience | 你可以做什么 / What You Can Do |
| --- | --- | --- |
| 起点 / Start | 了解主题并熟悉设备 Discover the theme and controls | 阅读引言，完成按钮选择和抓球练习 Read the introduction and practise selecting buttons, grabbing and releasing a ball |
| ACT 1 | 认识铜制潜水头盔 Discover the copper diving helmet | 抓取、观察、试戴和归还头盔 Pick up, inspect, wear and return the helmet |
| ACT 2 | 探索头盔结构 Explore the helmet’s construction | 移动整盔，拆下零件，阅读介绍并装回 Move the helmet, remove parts, read their descriptions and reattach them |
| ACT 3 | 帮助潜水员准备 Help the diver prepare | 打招呼、对话、戴头盔并跟随潜水员 Greet the diver, talk, fit his helmet and follow him |
| ACT 4 | 从岸上进入水下 Travel from shore to the seabed | 交互绳索，体验平滑的下潜过程 Use the rope for a smooth descent |
| 海底 / Seabed | 探索与保护海港 Explore and care for the harbour | 观察环境，捡垃圾并投放回收篮，沿绳返回平台 Explore, collect litter, deposit it in the basket and return via the rope |
| 返回平台 / Return to the platform | 完成主要流程 Finish the guided journey | 选择自由探索或退出 Choose free exploration or exit |

## 开始前：操作速查 / Before You Begin: Controls

| 操作 / Action | 使用方式 / How to Use |
| --- | --- |
| 点击界面 / Select a button | 用左手或右手射线指向按钮，按扳机 Point either controller’s ray at a button and press the trigger |
| 抓取物体 / Grab an object | 指向或靠近可抓取物体，按住握把；松开握把放下 Point at or reach toward a grabbable object, hold the grip button, then release it to let go |
| 试戴头盔 / Wear the helmet | 把头盔移到自己头部附近，松开握把 Bring the helmet close to your head and release the grip button |
| 归还试戴头盔 / Return the worn helmet | 在 ACT 1 佩戴状态下，按 B / Y 次要按钮 While wearing the ACT 1 helmet, press the B / Y secondary button |
| 水中上浮 / Swim upward | 在水中按住手柄 A 键上浮，松开后恢复缓慢下沉<br>While underwater, hold the controller’s A button to swim upward. Release it to resume slowly sinking |
| 查看长篇介绍 / Read longer descriptions | 拖动文字区域或滚动条Drag the text area or scrollbar |
| 打开、收起导航 / Open or close navigation | 按右手 A 主按钮，或点击菜单的打开、收起按钮 Press the right controller’s A primary button, or select the menu’s open or close button |
| 重新体验展项 / Reset an activity | 松开手里的物体，再点击对应 Reset / 重置按钮 Release any held object, then select the activity’s Reset button |
| 凝视选择 / Gaze selection | 开启辅助模式后，看向按钮并停留；移开视线取消 In Accessible Mode, look at a button and dwell to select it; look away to cancel |
| 绳索上下潜 / Rope travel | 靠近绳索，在水上选择下潜，在水下选择上浮 Approach the rope; choose Descend on the surface or Ascend underwater |
| 重听旁白 / Replay narration | 自由探索时在展台选择“再次播放本区解说” During free exploration, select the exhibit’s narration replay button |
| 退出体验 / Exit experience | 在完成提示或自由探索界面选择退出，然后等待工作人员协助 Select Exit at the ending prompt or during free exploration, then wait for staff assistance |

按钮名称以常见手柄布局和场景提示为准，其他设备的按键名称可能不同。

Button names follow common controller layouts and the on-screen instructions. Names may differ on other devices.

## 第一站：博物馆探索 / Stop 1: Explore the Museum

### 起点 · 欢迎介绍与新手教程 / Start · Welcome and Tutorial

从入口的介绍板了解展览主题，再沿指引前往展项。介绍支持中英文阅读和滚动浏览。

Read the introduction board at the entrance, then follow the signs to the exhibits. The introduction supports Chinese and English text and scrolling.

工作人员点击 **Start** 前，体验保持待机。启动后，教程通过简单步骤和鼓励性提示，带你依次选择左右目标按钮，再抓住并松开练习球。各步骤需按顺序完成，没有必须在限定时间内完成的要求；完成后会收到鼓励，并被引导开始参观。

The experience waits until staff select **Start**. A short tutorial uses encouraging prompts to guide you through selecting the left and right targets, then grabbing and releasing a practice ball. Complete the steps in order, without a time limit. A success message then guides you into the exhibition.

按钮选择可使用手柄或辅助模式下的头部凝视。**抓球练习目前仍需手柄：先抓住，再由同一手柄主动松开，才算完成。** 暂停、取消抓取或只拿着球不放，都不会完成这一练习。

Select buttons with a controller or head gaze in Accessible Mode. **The ball exercise still requires a controller: grab it, then deliberately release it with the same controller to finish.** Pausing, cancelling a grab or holding the ball without releasing it does not complete the exercise.

前段空间采用展墙、黄铜说明牌、器材展柜和休息凳，营造博物馆参观氛围。展品保持开放，方便靠近观察和亲手操作。

The first area features gallery walls, brass information signs, equipment display cabinets and benches. Exhibits remain accessible for close inspection and hands-on interaction.

### ACT 1 · 认识并试戴潜水头盔 / Discover and Try On the Diving Helmet

这里展示铜制潜水头盔的外壳、视窗和连接结构。先阅读介绍，再尝试把展品从展台上拿起，转动手柄观察不同角度。

Examine the copper helmet’s shell, viewports and connections. Read the introduction, lift the helmet from its stand and rotate your controller to view it from different angles.

1. 按住握把抓起头盔。Hold the grip button to pick up the helmet.
2. 将头盔移到自己的头部附近。Bring it close to your head.
3. 松开握把，符合距离条件时会自动佩戴，并显示成功提示。Release the grip button. When close enough, the helmet attaches automatically and a confirmation appears.
4. 按 B / Y 将头盔取下并放回展台。Press B / Y to remove the helmet and return it to the stand.

佩戴时，头盔跟随头部运动，外壳不会覆盖玩家的正常视野。也可以把头盔带回展台附近松手，让它归位。

While worn, the helmet follows your head without its shell blocking your view. You can also return a held helmet by releasing it near the stand.

这一环节以实际佩戴成功为完成依据，单纯抓起头盔不会推进流程。

This stage completes when the helmet is successfully worn; picking it up alone does not advance the journey.

### ACT 2 · 拆解和观察零件 / Disassemble and Inspect the Parts

第二个展项适合仔细观察头盔结构。你可以先抓取头盔固定部分，移动或旋转整盔；松开整盔后，再抓取可拆零件。

The second exhibit lets you study the helmet’s construction. Grab a fixed part to move or rotate the whole helmet. Release the helmet before picking up a removable part.

| 可观察零件 / Inspectable Part | 模型中的名称 / Model Name |
| --- | --- |
| 前部视窗 / Front viewport | `forward` |
| 左、右侧视窗 / Left and right viewports | `left`、`right` |
| 顶部部件 / Top component | `up` |
| 固定连接件 / Fixing component | `fixing` |
| 铭牌 / Nameplate | `nameplate` |
| 开放操作的一枚紧固件 / One interactive fastener | `nail1` |

抓取零件后，详情板会显示对应的中英文介绍。你可以转动手柄观察内外两面，再把零件移到原安装位置附近并松手，让它自动装回。

Picking up a part displays its Chinese and English description. Rotate your controller to inspect both sides, then bring the part close to its original mounting position and release it to reattach it automatically.

点击 **复位 / Reset** 可以恢复整盔和零件。其余紧固件保持固定，避免重复拆装影响观察。

Select **Reset** to restore the helmet and its parts. The other fasteners remain fixed to keep the inspection focused.

## 第二站：潜水员工作室 / Stop 2: The Diver’s Workshop

### ACT 3 · 打招呼，帮助潜水员戴好头盔 / Greet the Diver and Fit His Helmet

离开展览区后，进入布置有木质工作台、工具抽屉、装备架、气瓶、供气设备、软管和检查图板的工作室。

Leave the gallery and enter a workshop furnished with a wooden workbench, tool drawers, equipment racks, air cylinders, air supply equipment, hoses and inspection boards.

潜水员有面部和潜水装备造型，并通过挥手、对话手势、点头和招手回应玩家。

The diver has facial features and diving equipment. He responds with a greeting wave, conversational gestures, a nod and a beckoning gesture.

1. 靠近潜水员，他会挥手打招呼；也可以点击 **开始对话**。<Approach the diver to receive a greeting wave, or select **Start dialogue**.
2. 阅读潜水员的提示，观察头盔视窗和连接结构。Read his instructions and inspect the helmet’s viewports and connections.
3. 按住握把抓起展台上的头盔，移到潜水员头部附近。Hold the grip button to lift the helmet from the stand and bring it close to the diver’s head.
4. 松开握把，头盔就位后，潜水员会点头感谢。<br>Release the grip button. Once the helmet is fitted, the diver nods to thank you.
5. 跟随他的招手和带路动作，前往 ACT 4。Follow his beckoning gesture as he leads you to ACT 4.

对话框出现在潜水员旁边，让发言与角色的位置对应。带路时，如果玩家落后较远，潜水员会停下来等待。

The dialogue appears beside the diver, linking the speech to the character. He pauses during the walk if you fall too far behind.

想重新体验时，松开手里的头盔，点击 ACT 3 的 **重置 / Reset**，恢复潜水员和头盔的初始状态。

To repeat the activity, release the helmet and select ACT 3’s **Reset** button to restore the diver and helmet to their starting state.

## 第三站：沿绳进入水下港湾 / Stop 3: Follow the Rope into the Underwater Harbour

### ACT 4 · 下潜平台与绳索 / The Platform and Descent Rope

ACT 4 以平台旁的下潜绳索作为交互入口。原来的独立小方块已移除，快捷导航会把玩家送到绳索旁的平台。

ACT 4 begins at the descent rope beside the platform. The separate cube marker has been removed. Quick navigation takes you to the platform next to the rope.

潜水员到达后会说明下潜方式，并先沿绳索下潜，在海底等待玩家。

On arrival, the diver explains how to descend. He follows the rope down first and waits for you on the seabed.

**下潜方式：** 靠近绳索，握住绳索手柄，或用射线点击 **下潜 / Descend**。

**To descend:** Approach the rope and grip its handle, or point your controller’s ray at **Descend** and select it.

玩家会先靠近绳索，再平滑下降到海底。到达后恢复自由操作。过程中保留头部观察方向，可以继续看向周围；绳索移动期间，快捷跳转会暂时等待，避免打断下潜或上浮。

You move into position beside the rope, then descend smoothly to the seabed. Free control resumes on arrival. Head tracking remains active so you can look around during the journey. Quick travel is temporarily unavailable during rope movement to avoid interrupting the descent or ascent.

绳索交互需在附近的触发范围内使用，单纯走近不会自动下潜。选择上下潜后，系统先把用户定位到安全的绳索轴线，再进行垂直移动，减少与水面围栏穿模。面板平时位于绳索附近，用户靠近后才跟随；移动过程中保持在视野内，并根据水上或水下状态显示对应操作。

Rope interaction is available within its nearby activation area; approaching alone does not start a descent. After a travel selection, the system first aligns you with the safe rope axis, then moves vertically to reduce clipping through the surface railing. The panel stays near the rope until you approach, remains in view during travel, and offers the appropriate action for your surface or underwater position.

### 海底探索 · 观察港湾环境 / Seabed Exploration · Discover the Harbour

水下空间包含海床、礁石、珊瑚、海草、港湾支柱和缓慢游动的鱼群。进入水下后，蓝绿色雾效帮助表现海水氛围。

The underwater area contains a seabed, rocks, coral, aquatic plants, harbour pillars and slowly swimming fish. Blue-green fog creates the atmosphere of being underwater.

水池扩大并加入蓝色边界包边，形成连续的探索区域；平台底面也可从水下看到。

The pool has been enlarged and fitted with blue perimeter boundaries to create a continuous exploration area. The underside of the platform is also visible from below.

### 水中重力与上浮 / Underwater Gravity and Swimming Upward

水中设有独立的重力与沉降控制。进入水中后，玩家会缓慢下沉；**按住手柄 A 键可以主动上浮**，松开后逐渐恢复下沉。离开水域后恢复陆地重力。

The water uses a separate gravity and sinking system. After entering the water, you slowly sink. **Hold the controller’s A button to swim upward**; releasing it gradually resumes sinking. Normal land gravity returns when you leave the water.

水中垂直移动还受绳索安全区域限制；在该区域外不会通过上浮、下沉穿过平台围栏。正常返回平台应使用绳索上浮交互。

Vertical swimming is also limited to the safe rope area, preventing ascent or sinking through the platform railing outside that area. Use the rope ascent interaction to return to the platform.

这种上浮方式用于水中的自由探索。若要沿指定路线返回平台，可以使用绳索手柄或 **上浮 / Ascend** 按钮。

Use this swimming control during free underwater exploration. To return to the platform along the designated route, use the rope handle or the **Ascend** button.

A 键也用于快捷导航的展开与收起。水中需要操作导航时，可用射线点击菜单上的打开、收起按钮。

The A button also toggles quick navigation. To control the menu while underwater, use your controller’s ray to select its open or close button.

### 海底清理 · 捡垃圾并投放回收篮 / Seabed Cleanup · Collect and Deposit Litter

绳索附近的海底散落着 **9 件垃圾**，包括塑料瓶、饮料罐和废弃包装。潜水员到达海底后会提示清理方法。

There are **9 pieces of litter** near the rope, including plastic bottles, drinks cans and discarded packaging. The diver explains the cleanup activity when he reaches the seabed.

1. 找到垃圾，按住握把将它捡起。<br>Find a piece of litter and hold the grip button to pick it up.
2. 带到绿色回收篮旁，将垃圾放进篮内。<br>Take it to the green collection basket and place it inside.
3. 松开握把，投放成功后回收进度增加。<br>Release the grip button. A successful deposit increases the collection count.
4. 收集全部 9 件垃圾后，说明板显示完成提示。<br>After all 9 items are collected, the instruction board displays a completion message.

**计数发生在投放回收篮后，单纯捡起不会计数。** 每件垃圾只计一次。想再次收集，可以先松开物体，再点击 **重新收集 / Reset cleanup**。

**An item counts only after it is deposited in the basket; picking it up alone does not count.** Each item counts once. To repeat the activity, release any held object and select **Reset cleanup**.

### 沿绳上浮 · 返回平台 / Rope Ascent · Return to the Platform

回到海底绳索旁，握住上浮手柄，或点击 **上浮 / Ascend**。玩家会沿绳索平滑上升，再移动到平台落脚点，随后恢复自由操作。

Return to the rope on the seabed and grip the ascent handle, or select **Ascend**. You rise smoothly along the rope and move onto the platform landing point before free control resumes.

### 完成体验 · 自由探索或退出 / Finish · Explore Freely or Exit

首次完成海底清理后，仍需上浮返回陆地平台，系统才显示整段体验完成。如果工作人员跳过清理任务，也保留返回平台的要求；跳过会单独记录，不算用户成功完成清理。

After your first completed cleanup, return to the land platform before the system presents the end of the guided experience. If staff skip cleanup, returning to the platform is still required. A skip is recorded separately from successful task completion.

完成提示提供 **自由探索 / Free exploration** 和 **退出体验 / Exit experience**。选择自由探索后，可继续参观和重听区域解说，界面保留明显的退出入口，便于随时结束。

The completion prompt offers **Free exploration** and **Exit experience**. Free exploration lets you revisit exhibits and replay narration, with a clearly visible exit control available whenever you wish to finish.

选择退出后，系统进入结束状态，显示感谢信息，并提醒你停留原地，等待工作人员协助摘下头显。工作人员可随后全局复位，准备下一轮体验。

On exit, the session ends with a thank-you message asking you to remain still and wait for staff to help remove the headset. Staff can then reset the experience for the next visitor.

## 展览辅助功能 / Additional Exhibition Features

### 按进度显示的区域引导 / Guidance Based on Progress

处于任一体验区域内时，路线引导隐藏，给用户留出观察和探索空间。离开区域、处于展区之间时，系统根据体验进度提示下一步前往哪里；到达目标展台后，引导消失。到达范围以展台及配置区域为依据，不以字幕牌的位置判断。

Route guidance hides while you are inside an experience area, leaving room to observe and explore. Between areas, it shows where to go next based on your progress, then disappears when you reach the target exhibit. Arrival uses the exhibit’s configured area rather than the position of its text panel.

### 环境音与自动旁白 / Ambience and Automatic Narration

博物馆内播放柔和背景音乐；靠近港口时逐渐过渡为风声和海浪声；实际进入水下后切换为水下氛围。背景音由用户位置决定，佩戴头盔等行为音效由真实操作事件触发。给潜水员戴上头盔不会把游客当成佩戴者并切换水下背景音。入水、出水时分别播放相应音效。

Soft music accompanies the museum visit, gradually blending into wind and waves near the harbour and underwater ambience when you enter the water. Background sound follows your location; actions such as fitting a helmet trigger their own feedback. Fitting the diver’s helmet does not treat you as the wearer or switch your background to underwater sound. Water entry and exit have separate sound effects.

ACT 1–3 已接入本地英文旁白。首次进入展台的指定范围时自动播放；正式流程中的目标展台还会等待到达引导淡出后开始。自由探索时，可在展台选择 **再次播放本区解说 / Replay this area's narration**，重复播放不会叠加多条解说。

ACT 1–3 include local English narration that plays automatically on your first arrival within the exhibit area. For the current guided destination, playback also waits for the arrival guidance to fade out. During free exploration, select **Replay this area's narration** at the exhibit to listen again without overlapping narration tracks.

旁白、环境音和操作反馈分开控制，旁白播放时环境音适当降低。Pause 暂停播放，Resume 从暂停处继续，Skip 清理旧阶段解说及相关反馈，Reset 清除播放进度和本轮访问记录。已接入的音频资源保存在本地，不依赖联网播放。

Narration, ambience and feedback have separate controls, with ambience lowered during narration. Pause holds playback, Resume continues from that position, Skip clears the previous stage’s narration and related feedback, and Reset clears playback progress and visit history. Integrated audio is stored locally and does not require streaming.

### 凝视辅助模式 / Accessible Head-Gaze Mode

开始前可选择常规模式或辅助模式。辅助模式使用头部朝向，用户看向按钮并停留一段可调整的时间即可选择，不要求眼球追踪硬件；同时保留手柄操作。

Choose Standard or Accessible Mode before starting. Accessible Mode uses your head direction: look at a button for an adjustable dwell time to select it. Eye-tracking hardware is not required, and controller input remains available.

凝视时显示选择进度，移开视线会取消；长时间注视同一目标不会连续重复激活，手柄和凝视也不会重复提交同一次操作。当前主要支持按钮和菜单，抓取、佩戴、装配等物理任务的完整凝视替代仍需后续辅助操作接口，不能视为完全免手柄体验。

A progress indicator shows the dwell selection; looking away cancels it. Continued gaze does not repeatedly activate the same target, and controller and gaze input do not duplicate the same submission. Current support focuses on buttons and menus. Full alternatives for grabbing, wearing and assembly still require assisted-action integration, so the experience is not yet entirely controller-free.

### 快捷导航 / Quick Navigation

导航菜单提供 **起点、ACT 1、ACT 2、ACT 3、ACT 4** 五个目的地。左右手都可用射线和扳机点击；菜单放在视野侧边，并使用半透明背景。

The navigation menu offers five destinations: **Start, ACT 1, ACT 2, ACT 3 and ACT 4**. Use either controller’s ray and trigger to select a destination. The menu sits to the side of your view and has a translucent background.

按右手 A 主按钮或点击菜单按钮可以收起、展开导航。海底往返使用绳索交互，菜单中没有独立的海底目的地。

Press the right controller’s A primary button or select the menu button to open or close navigation. Travel to and from the seabed uses the rope; the menu has no separate seabed destination.

### 视频展屏 / Video Displays

`vedio1`、`vedio2`、`vedio3` 是三个独立的视频展屏，保留各自的位置和大小，显示播放器样式的黑色画面、播放按钮、进度条和时间。

`vedio1`, `vedio2` and `vedio3` are three independent video displays. Each retains its own position and size and shows a player-style black screen, play button, progress bar and time display.

三个展屏已接入 `Assets/Vedio/` 中的本地影片：`vedio1` 播放 `helmet_history.mp4`，`vedio2` 播放 `helmet_component.mp4`，`vedio3` 播放 `Diver.mp4`，支持播放、暂停和进度调整。

The displays now use local films from `Assets/Vedio/`: `vedio1` plays `helmet_history.mp4`, `vedio2` plays `helmet_component.mp4`, and `vedio3` plays `Diver.mp4`. They support playback, pause and seeking.

离开视频区域一定距离后，视频自动暂停并保留当前进度；返回时可点击播放继续观看。全局 Reset 清零视频进度，下一位游客从头开始。

Moving beyond the viewing distance automatically pauses the video and preserves its position. Select Play on returning to continue. Global Reset clears video progress so the next visitor starts from the beginning.

## 工作人员操作与接待准备 / Staff Controls and Visitor Handover

统一工作人员面板显示会话状态和当前阶段，方便团队测试和现场协助。

The staff panel shows session status and the current stage to support team testing and visitor assistance.

| 操作 / Control | 系统行为 / Behaviour |
| --- | --- |
| Start | 从待机开始正式体验 Start the guided experience from Waiting |
| Pause | 暂停流程、相关活动和音频 Pause the flow, related activities and audio |
| Resume | 从暂停位置继续 Resume from the paused state |
| Skip | 跳过当前环节并记为 Skipped，不当作任务成功 Skip the current stage and record Skipped rather than successful completion |
| Reset | 停止当前动作并恢复初始会话，回到 Waiting Stop current actions, restore initial session state and return to Waiting |

全局 Reset 统一恢复头盔、零件、潜水员位置和任务状态、用户初始位置与朝向、上下潜过程、垃圾对象和计数、音视频播放状态，以及界面和凝视进度。系统先终止抓取、移动或播放等活动，再恢复状态，便于准备下一位游客，而不必逐个重置展项或重新启动编辑器。

Global Reset restores helmets, parts, the diver’s position and task state, the visitor’s starting position and orientation, rope travel, litter and counts, audio/video playback, UI and gaze progress. It first stops ongoing grabs, movement or playback, then restores state for the next visitor without individually resetting exhibits or restarting the editor.

### 日志与移动端接入准备 / Logs and Mobile Integration Preparation

系统提供会话状态、阶段进度、完成或跳过结果、用户操作记录，以及状态查询和控制命令接口，为后续 iPad 或其他移动端工作人员页面提供基础。日志可帮助团队分析体验进度和问题发生的位置。

The system provides session status, stage progress, completion or skip results, user action records, state queries and control command interfaces. These support future staff interfaces on iPad or other mobile devices and help the team review progress and locate issues.

**移动端页面、设备间实际通信和连接管理仍需后续开发。** 当前完成的是项目内的状态、日志和控制接口，并不代表已经能够通过 iPad 远程控制体验。

**The mobile interface, actual communication between devices and connection management still require development.** The current implementation provides the in-project state, logging and control interfaces; it does not yet provide working iPad remote control.

## 场景调整与内容替换 / Scene Editing & Content Replacement

这一部分供布展或后续制作时使用。

This section is for exhibition setup and further production.

| 想修改什么 / What to Change | 在哪里调整 / Where to Edit |
| --- | --- |
| 静态布景的位置、旋转、大小<br>Scenery position, rotation and scale | 展开 `Harbour Museum and Diver Workshop` → `Static Scenery (editable)`，选择对应提示板、展墙、展柜或工作台，调整 Transform 后保存场景<br>Expand `Harbour Museum and Diver Workshop` → `Static Scenery (editable)`. Select a sign, gallery wall, cabinet or workbench, adjust its Transform and save the scene |
| 静态提示板文字<br>Static sign text | 展开提示板，找到 `Caption` 或文字对象，修改 Text 内容<br>Expand the sign, find `Caption` or its text object and edit the Text content |
| 操作说明板的位置<br>Instruction board position | 选择对应说明板的父对象，调整 Transform 后保存<br>Select the board’s parent object, adjust its Transform and save |
| 重新按头盔正面对齐提示板<br>Realign signs with the helmet front | 在 `HarbourSceneDressing` 组件面板点击“按头盔前部视窗重新对齐提示板”<br>In the `HarbourSceneDressing` Inspector, select “按头盔前部视窗重新对齐提示板” (Realign signs with the helmet’s front viewport) |
| 视频内容<br>Video content | 选中 `vedio1`、`vedio2` 或 `vedio3`，在 `ExhibitVideoScreen` 的 `Video Clip` 中指定影片，或填写 `Video Url`；影片文件优先于链接<br>Select `vedio1`, `vedio2` or `vedio3`. Assign a `Video Clip` or enter a `Video Url` in `ExhibitVideoScreen`. An assigned clip takes priority over the URL |
| 视频标题、循环与音量<br>Video title, looping and volume | 修改对应视频展屏组件的 `Screen Title`、`Loop` 和 `Volume`<br>Edit the display component’s `Screen Title`, `Loop` and `Volume` fields |
| 起点和头盔介绍文字<br>Welcome and helmet introduction text | 修改对应 `StartIntroduction` 组件中的标题、正文、字号和面板尺寸<br>Edit the title, body text, font sizes and panel size in the relevant `StartIntroduction` component |

展品提示板以头盔模型的 `forward` 前部视窗方向作为正面，布置在前方侧边。独立布景对象可以逐个调整，保存后保留布局；点击重新对齐会重新放置相关提示板。

Exhibit signs use the helmet model’s `forward` front viewport to define the front direction and sit to one side of it. Scenery objects can be adjusted individually, and their layout is retained when the scene is saved. Selecting realignment repositions the relevant signs.

## 打开体验场景 / Open the Experience Scene

使用项目记录的团结引擎 **1.6.13（2022.3.61t14）** 打开工程，等待资源和脚本加载，再打开 `Assets/Scenes/BasicScene.unity`，使用已配置的 XR 运行环境和手柄进入 Play。

Open the project with its recorded Tuanjie Engine version, **1.6.13 (2022.3.61t14)**. Wait for assets and scripts to load, open `Assets/Scenes/BasicScene.unity`, then enter Play using the configured XR runtime and controllers.

进入 Play 后由工作人员面板点击 **Start**，再按教程和路线体验。使用真实头显时应停用键鼠模拟器。验证时还应检查 Pause、Resume、Skip，以及抓取、移动和播放期间的 Reset，并连续运行多轮体验，确认没有继承上一轮状态。

In Play mode, select **Start** on the staff panel, then follow the tutorial and route. Disable the keyboard/mouse simulator when using a real headset. Also check Pause, Resume, Skip and Reset during grabs, movement and playback, and run multiple sessions to confirm that previous state does not carry over.

打包前需把 `BasicScene.unity` 加入构建场景并确认启动顺序；当前构建列表启用的是 `SampleScene.scene`。

Before building, add `BasicScene.unity` to the build scene list and confirm the startup order. The current build list enables `SampleScene.scene`.

近期新增功能已进行代码编译检查；完整参观流程、视频播放和头显上的视觉与交互效果仍需运行验证。本导览描述当前功能，不作为真机验收记录。

Recent additions have passed code compilation checks. The full visitor journey, video playback and headset visuals and interactions still require runtime testing. This guide describes the current features; it is not a record of headset acceptance testing.
