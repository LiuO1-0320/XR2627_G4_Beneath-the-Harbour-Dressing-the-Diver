# Beneath the Harbour · Dressing the Diver

**港湾之下 · 为潜水员穿戴装备**

在这座 PC VR 博物馆里，你可以亲手试戴一顶铜制潜水头盔，拆下七个零件观察它的结构，再给潜水员戴好头盔，跟随他走到港口。沿绳潜入水下后，把九件海底垃圾放进回收篮，再沿绳返回平台。回到岸上，你可以自由参观，也可以结束体验。

In this PC VR museum, try on a copper diving helmet and take it apart to examine its seven removable pieces. Fit the diver’s helmet, follow him to the harbour and descend the rope. Collect nine pieces of litter from the seabed, then return to the platform by rope to choose free exploration or finish your visit.

**从源码体验 / Run from source**

本导览对应 `integration` 分支。游客可使用手柄或头部凝视完成主要展项。工作人员可在同一局域网内通过浏览器查看进度、协助游客并为下一位复位。

This guide follows the `integration` branch. Visitors can use controllers or head-gaze selection for the exhibit tasks. Staff can follow progress, assist the visitor and reset the experience from a browser on the same local network.

[项目源码 / Project source](https://github.com/LiuO1-0320/XR2627_G4_Beneath-the-Harbour-Dressing-the-Diver/tree/integration) · [凝视操作 / Head-gaze controls](https://github.com/LiuO1-0320/XR2627_G4_Beneath-the-Harbour-Dressing-the-Diver/blob/integration/Gaze-Accessibility.md) · [工作人员网页 / Staff web console](https://github.com/LiuO1-0320/XR2627_G4_Beneath-the-Harbour-Dressing-the-Diver/blob/integration/Mobile-Integration.md)


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
| 查看长篇介绍 / Read longer descriptions | 拖动文字区域或滚动条。 Drag the text area or scrollbar |
| 前往下一展区 / Find the next exhibit | 离开展区后，跟随画面中的路线提示前往下一站。 Follow the on-screen directions between exhibits. |
| 重新体验展项 / Reset an activity | 松开手里的物体，再点击对应 Reset / 重置按钮 Release any held object, then select the activity’s Reset button |
| 凝视选择 / Gaze selection | 开启辅助模式后，看向按钮或可凝视的物体，停留至圆环填满即可选择。移开视线可取消。 In Accessible Mode, look at a button or supported object until the ring fills. Look away to cancel. |
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

点亮左、右按钮后，蓝色训练球才会出现。使用手柄时，用同一只手抓住小球，再松开握把。辅助模式下，凝视小球将它拿起，再凝视教程卡上的 **放开小球**。放开后教程才完成，选择 **开始探索** 即可前往第一个展台。

Light the left button and then the right to reveal the blue practice ball. With a controller, grab the ball and release it with the same hand. In Accessible Mode, look at the ball to pick it up, then dwell on **Let go of the ball**. Once you release it, choose **Start exploring** to visit the first exhibit.

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

七个零件都需要拆下、观察并主动装回。「散落零件归位」可以找回掉落的零件，找回后仍需完成这件的观察和装回。「重新开始」会复位整个展台。

Detach, inspect and refit all seven parts to finish the exhibit. If you lose a part, use the recovery button to return it to its mount, then take it off again to inspect and refit it yourself. **Restart** resets the whole exhibit.

## 第二站：潜水员工作室 / Stop 2: The Diver’s Workshop

### ACT 3 · 打招呼，帮助潜水员戴好头盔 / Greet the Diver and Fit His Helmet

离开展览区后，进入布置有木质工作台、工具抽屉、装备架、气瓶、供气设备、软管和检查图板的工作室。

Leave the gallery and enter a workshop furnished with a wooden workbench, tool drawers, equipment racks, air cylinders, air supply equipment, hoses and inspection boards.

潜水员有面部和潜水装备造型，并通过挥手、对话手势、点头和招手回应玩家。

The diver has facial features and diving equipment. He responds with a greeting wave, conversational gestures, a nod and a beckoning gesture.

1. 靠近潜水员，他会挥手打招呼；也可以点击 **开始对话**。<br>Approach the diver to receive a greeting wave, or select **Start dialogue**.
2. 阅读潜水员的提示，观察头盔视窗和连接结构。Read his instructions and inspect the helmet’s viewports and connections.
3. 按住握把抓起展台上的头盔，移到潜水员头部附近。Hold the grip button to lift the helmet from the stand and bring it close to the diver’s head.
4. 松开握把，头盔就位后，潜水员会点头感谢。<br>Release the grip button. Once the helmet is fitted, the diver nods to thank you.
5. 跟随他的招手和带路动作，前往 ACT 4。Follow his beckoning gesture as he leads you to ACT 4.

对话框出现在潜水员旁边，让发言与角色的位置对应。带路时，如果玩家落后较远，潜水员会停下来等待。

The dialogue appears beside the diver, linking the speech to the character. He pauses during the walk if you fall too far behind.

潜水员开始带路后，展台会显示跟随提示并停用「开始对话」和展台重置按钮。落后时可以按自己的节奏追上他，到了港口再操作绳索。

Once the diver sets off, the stand displays a follow prompt and disables its dialogue and reset buttons. Catch up at your own pace, then use the rope when you reach the harbour.

## 第三站：沿绳进入水下港湾 / Stop 3: Follow the Rope into the Underwater Harbour

### ACT 4 · 下潜平台与绳索 / The Platform and Descent Rope

从潜水员工作室跟随潜水员走到港口，平台旁的绳索就是下潜入口。

Follow the diver from the workshop to the harbour. The rope beside the platform is your entry point to the dive.

潜水员到达后会说明下潜方式，并先沿绳索下潜，在海底等待玩家。

On arrival, the diver explains how to descend. He follows the rope down first and waits for you on the seabed.

**下潜方式：** 靠近绳索，握住绳索手柄，或用射线点击 **下潜 / Descend**。

**To descend:** Approach the rope and grip its handle, or point your controller’s ray at **Descend** and select it.

选好下潜后，你会先对齐绳索，再平滑下降到海底。途中可以转头观察，到达后可自由移动。

After you select Descend, you line up with the rope and travel smoothly to the seabed. You can look around on the way and move freely once you arrive.

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

靠近平台围栏时，垂直移动受绳索安全区域约束。回到绳索旁选择上浮，可以安全落在平台上，并触发正式的返岸事件。

Near the platform railing, vertical travel is constrained to the rope shaft. Select **Ascend** at the rope to land on the platform and register your return.

这种上浮方式用于水中的自由探索。若要沿指定路线返回平台，可以使用绳索手柄或 **上浮 / Ascend** 按钮。

Use this swimming control during free underwater exploration. To return to the platform along the designated route, use the rope handle or the **Ascend** button.

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

提前沿绳返岸后，可以再次下潜，已入篮的数量会保留。收齐九件后，还要沿绳上浮并落到平台，才会出现完成选项。工作人员跳过清理时，系统记录为 **Skipped**，游客仍需沿绳返岸。

If you surface early, you can dive again with your collection count intact. After the ninth item is in the basket, ride the rope back up to the platform to see the ending choices. A staff skip records the cleanup as **Skipped** and still requires a return by rope.

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

ACT 1–3 各有一段本地英文旁白。首次到达对应展台的互动范围时，引导消失，旁白开始播放。自由探索时可在展台选择 **再次播放本区解说**。

Each of the first three exhibits has a local English narration track. The route prompt disappears and narration starts when you first reach the exhibit. During free exploration, select **Replay this area's narration** to hear it again.

旁白、环境音和操作反馈分开控制，旁白播放时环境音适当降低。Pause 暂停播放，Resume 从暂停处继续，Skip 清理旧阶段解说及相关反馈，Reset 清除播放进度和本轮访问记录。已接入的音频资源保存在本地，不依赖联网播放。

Narration, ambience and feedback have separate controls, with ambience lowered during narration. Pause holds playback, Resume continues from that position, Skip clears the previous stage’s narration and related feedback, and Reset clears playback progress and visit history. Integrated audio is stored locally and does not require streaming.

### 凝视辅助模式 / Accessible Head-Gaze Mode

开始前可选择常规模式或辅助模式。辅助模式使用头部朝向，用户看向按钮并停留一段可调整的时间即可选择，不要求眼球追踪硬件；同时保留手柄操作。

Choose Standard or Accessible Mode before starting. Accessible Mode uses your head direction: look at a button for an adjustable dwell time to select it. Eye-tracking hardware is not required, and controller input remains available.

凝视时圆环显示选择进度，提前移开视线会取消。除了菜单和训练球，还可以在 ACT 1 凝视试戴头盔，在 ACT 2 逐件拆下、转动观察并装回零件，在 ACT 3 给潜水员戴上头盔，在 ACT 4 逐件拾取垃圾并放入回收篮。游客可以在同一轮体验中交替使用凝视和手柄。

The dwell ring shows your selection progress. Look away before it fills to cancel. Head gaze also lets you try on the helmet in ACT 1, remove, rotate and refit individual parts in ACT 2, fit the diver’s helmet in ACT 3, and pick up litter and deposit it in the basket in ACT 4. You can switch between gaze and controllers during a visit.

### 独立展台测试 / Testing an Exhibit in Isolation

在完整的 `BasicScene` 里，游客依靠展区间的引导移动。制作人员单独测试展台、场景未运行会话控制器时，可用快捷导航前往起点或 ACT 1–4。

In the full `BasicScene`, visitors follow the directions between exhibits. When testing a stand in isolation with the session coordinator absent, developers can use quick travel to reach the start area or ACT 1–4.

### 视频展屏 / Video Displays

`vedio1`、`vedio2`、`vedio3` 是三个独立的视频展屏，保留各自的位置和大小，显示播放器样式的黑色画面、播放按钮、进度条和时间。

`vedio1`, `vedio2` and `vedio3` are three independent video displays. Each retains its own position and size and shows a player-style black screen, play button, progress bar and time display.

三个展屏已接入 `Assets/Vedio/` 中的本地影片：`vedio1` 播放 `helmet_history.mp4`，`vedio2` 播放 `helmet_component.mp4`，`vedio3` 播放 `Diver.mp4`，支持播放、暂停和进度调整。

The displays now use local films from `Assets/Vedio/`: `vedio1` plays `helmet_history.mp4`, `vedio2` plays `helmet_component.mp4`, and `vedio3` plays `Diver.mp4`. They support playback, pause and seeking.

离开视频区域一定距离后，视频自动暂停并保留当前进度；返回时可点击播放继续观看。全局 Reset 清零视频进度，下一位游客从头开始。

Moving beyond the viewing distance automatically pauses the video and preserves its position. Select Play on returning to continue. Global Reset clears video progress so the next visitor starts from the beginning.

## 工作人员操作与接待准备 / Staff Controls and Visitor Handover

默认使用 **展览模式**，由工作人员开始体验或跳过环节。游客可选择手柄或凝视，也能在体验菜单中暂停和继续。**小组测试模式**额外开放头显上的开始和二次确认跳过，方便组员独立测试。

In **Exhibition mode**, staff start the session and can skip a stage. Visitors choose their input method and can pause or resume from their menu. **Team test mode** also offers Start and a confirmed Skip in the headset for team members testing on their own.

| 操作 / Control | 系统行为 / Behaviour |
| --- | --- |
| Start | 从待机开始正式体验 Start the guided experience from Waiting |
| Pause | 暂停流程、相关活动和音频 Pause the flow, related activities and audio |
| Resume | 从暂停位置继续 Resume from the paused state |
| Skip | 跳过当前环节并记为 Skipped，不当作任务成功 Skip the current stage and record Skipped rather than successful completion |
| Reset | 停止当前动作并恢复初始会话，回到 Waiting Stop current actions, restore initial session state and return to Waiting |
| Back（仅桌面 / desktop only） | 在同一游客会话内返回上一环节，保留更早环节的完成或跳过记录。 Return to the previous stage within the same visit, preserving earlier completion and skip records. |

全局 Reset 统一恢复头盔、零件、潜水员位置和任务状态、用户初始位置与朝向、上下潜过程、垃圾对象和计数、音视频播放状态，以及界面和凝视进度。系统先终止抓取、移动或播放等活动，再恢复状态，便于准备下一位游客，而不必逐个重置展项或重新启动编辑器。

Global Reset restores helmets, parts, the diver’s position and task state, the visitor’s starting position and orientation, rope travel, litter and counts, audio/video playback, UI and gaze progress. It first stops ongoing grabs, movement or playback, then restores state for the next visitor without individually resetting exhibits or restarting the editor.

### 工作人员网页 / Staff Web Console

1. 运行体验，在电脑窗口左上角展开 **工作人员控制 / Staff**。点击 **Staff web** 下电脑的局域网地址，复制完整控制链接。<br>With the experience running, open **Staff** at the top left of the PC window. Click its LAN address under **Staff web** to copy the full control link.
2. 用同一局域网内的 iPad 或电脑浏览器打开此链接。页面显示当前游客、阶段、任务进度和最近操作。普通地址（默认端口 `8765`）提供只读监看。<br>Open the link on an iPad or computer on the same LAN. The page shows the active visit, stage, task progress and recent actions. The base address on port `8765` opens a read-only view.
3. 工作人员可使用 **Start、Pause、Resume、Skip、Reset**。跳过和复位均需二次确认。换人后，核对新会话并点击 **启用本次控制**。<br>Staff can **Start, Pause, Resume, Skip or Reset**. Skip and Reset require confirmation. After a handover, check the new visit and enable controls for that session.

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

切换到 `integration`，用团结引擎 **1.6.13（2022.3.61t14）** 打开仓库中的 `XRG4_BeneathTheHarber` 文件夹。资源导入完成后，打开 `Assets/Scenes/BasicScene.unity`。使用头显时，先确认 PC 的 OpenXR 运行时已识别设备和手柄，再进入 Play。

Switch to `integration` and open the `XRG4_BeneathTheHarber` project folder in **Tuanjie Engine 1.6.13 (2022.3.61t14)**. After import, open `Assets/Scenes/BasicScene.unity`. For a headset session, confirm that the PC’s OpenXR runtime recognises the headset and controllers before entering Play.

进入 Play 后由工作人员面板点击 **Start**，再按教程和路线体验。使用真实头显时应停用键鼠模拟器。验证时还应检查 Pause、Resume、Skip，以及抓取、移动和播放期间的 Reset，并连续运行多轮体验，确认没有继承上一轮状态。

In Play mode, select **Start** on the staff panel, then follow the tutorial and route. Disable the keyboard/mouse simulator when using a real headset. Also check Pause, Resume, Skip and Reset during grabs, movement and playback, and run multiple sessions to confirm that previous state does not carry over.

`BasicScene.unity` 是 Windows 构建中唯一启用的场景。本项目已在完整场景中用模拟输入连续走通两轮游客流程，并在 Windows 11 上启动独立的 64 位程序，验证了工作人员网页的控制和换人保护。实体 Quest 3 的追踪、佩戴舒适度及实体 iPad Safari 仍待现场验证。

`BasicScene.unity` is the only enabled build scene. Two simulated visitors completed the full journey in the editor. The standalone 64-bit player ran on Windows 11, where browser controls and visitor handover protection were tested. Physical Quest 3 tracking, headset comfort and Safari on a physical iPad still need an on-site check.

[异常输入与流程测试 / Interaction and recovery tests](https://github.com/LiuO1-0320/XR2627_G4_Beneath-the-Harbour-Dressing-the-Diver/blob/integration/Tests/Visitor-Input-Report.md)
