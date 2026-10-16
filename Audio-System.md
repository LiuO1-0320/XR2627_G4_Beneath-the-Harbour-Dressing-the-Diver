# Audio-System 环境音景与旁白系统

## 按实际位置切换背景音（当前规则）

背景音不读取 Stage 或潜水员是否带路。默认使用现有 WaterVolumeSwimming 水域包围盒在水平面的边界：距离水域 14 米之外播放博物馆音乐，5 米以内播放港口风浪声，中间平滑混合；实际头部进入水域后播放水下背景音。自由探索、返回展台、Skip 后同样按位置判断。Pause / Resume 和全局 Reset 保留原有停止控制。

Experience Audio Director 的 Location-based ambience 可调整 Harbour Full Distance（默认 5 米）、Harbour Fade Distance（默认 14 米）。Harbour Area 可选填专门的港口区域 Collider；为空时使用现有水域边界。距离按水平包围盒计算，适合当前矩形水域；自定义港口区域建议使用不旋转的 BoxCollider，设为 Trigger，避免挡路。范围边界需要在实际场景中试听调整。

头盔佩戴仅触发原有操作反馈及已配置的内部解说，不再把陆地背景音切到水下，也不改变背景声滤波。NPC 戴头盔同样不影响背景音。入水/出水效果继续由实际越过水面触发，原音频资源未更换。

验证：同一阶段在博物馆/港口往返；留在博物馆点击 Skip；自由探索返回展台；ACT1 佩戴/摘下；实际下潜/上浮；Pause/Resume/Reset。确认声音遵循实际位置，没有遗留阶段选择。

## 本次实现

- `AudioDirector` 接管原 StageCoordinator 的单独旁白 AudioSource，避免双重播放。
- `AudioMap` 为可编辑的 ScriptableObject，保存环境音、反馈音、各阶段的旁白顺序和音量参数。
- `Assets/Resources/ExperienceAudio/Experience.mixer`：Master 下的 Narration、Ambience、Feedback 三个独立组。Master 默认 -3 dB，旁白保留清晰度，头盔内低通只影响环境音。
- 本地原创合成音：32 秒港口水面/风声底噪、32 秒柔和水下底噪、轻柔交互音与成功音。不使用外部下载、联网播放或临时语音合成。这些是艺术化环境底声，不是历史现场录音；未加入突发汽笛等声音。
- 环境切换与初始淡入 2.5 秒；旁白播放时环境音降至原设定的 30%，降低用 0.25 秒、恢复用 1.2 秒。旁白有 0.18 秒起音与 0.15 秒尾部包络，反馈音本身有平滑包络。
- Pause 暂停所有本系统 AudioSource，冻结渐变与旁白队列；Resume 使用 UnPause 恢复，不重新 Play 旁白。
- Skip 清除旧旁白与短反馈；相同港口环境床可持续，环境类型变化则渐变。Reset/Completed 立即停止所有源并清空队列，优先保证没有旧会话声音残留。
- 相同阶段重复同步不会重启旁白；每个音频 Clip 在同一会话中最多入队一次。新的会话清除去重记录。
- ACT1–3 进入阶段后先等待用户到达展台。路线引导确认用户在到达范围内、且完全淡出后，才放行旁白；行走期间环境音继续。淡出时离开范围会恢复引导。到达事件校验会话与阶段，Pause 不接受新到达事件，Skip/Reset 清除等待状态；到达只放行语音，不完成互动任务。
- 到达范围优先使用 StageContent.arrivalArea；配置的 Collider 不可用时保持等待，不偷偷退回距离判定。未配置 Collider 时，ACT1–3 使用路线站立目标点附近 1.25 米（RegionGuide.exhibitArrivalRadius），其他区域保留原 arrivalRadius。可在 Inspector 调整展台站立区域。
- 实际抓起头盔、佩戴成功、拆下零件会触发低音量反馈，限制触发频率且不叠加短音效。
- 视频播放器在旁白正在播放时降低音量，视频原有会话暂停和复位逻辑保持有效。
- 本机工作人员面板显示音景/旁白状态；移动端快照新增 soundscape、audioStatus、narrationClip、narrationPositionSeconds。日志新增 AudioStage、AudioHelmetInterior、NarrationStarted、NarrationFinished。

## 阶段与声音

| 阶段/实际状态 | 环境音 | 旁白 |
| --- | --- | --- |
| Idle / Completed | 静音 | 无 |
| Briefing | 港口 | 待 M2/M5 提供开场与舒适性录音 |
| The Case / ACT1 | 港口 | act1_helmetHistory.mp3 |
| 游客实际佩戴头盔 | 渐变至低通水下/头盔内部底声 | insideNarration 队列，当前待提供 |
| Scale-up / ACT2 | 港口；仍戴着头盔时保留内部听感 | act2_helmetComponents.mp3 |
| Dress the Diver / ACT3 | 港口；给 NPC 戴头盔不会改变游客内部听感 | act3_Diver.mp3 |
| Descent / ACT4 | 岸上保持港口；游客实际入水触发短音效，穿过水面时立即切换水下底声，与入水声同时播放 | 待提供下潜说明录音 |
| Evidence & Reflection | 由游客实际是否仍在水中决定 | 待提供证据与重建总结录音 |

Inside 仍属于游客实际佩戴头盔的听觉状态，不加入新的导航阶段。NPC 的头盔不影响游客音景。水中判定直接查询 WaterVolumeSwimming 的水体 Collider 与游客头部位置，即使绳索移动暂时禁用游泳组件仍有效。穿过水面时以 0.12 秒短过渡切换水下底声，入水声叠加播放，不等待音效或绳索下潜结束；上浮时在实际离开水体后恢复港口声。WaterEntrySoft.wav 是本地原创合成的 1.2 秒跳水风格入水音（水体冲击、溅水及气泡尾声），脚本 Tests/generate-experience-water-entry.py 可复现。

## ACT1–3 录音接入

已找到用户提供的本地文件：

- `Assets/Audio/Narration/act1_helmetHistory.mp3`
- `Assets/Audio/Narration/act2_helmetComponents.mp3`
- `Assets/Audio/Narration/act3_Diver.mp3`

团结编辑器可能加密 .meta 中的 GUID，因此不把该字符串直接写成 Unity YAML 引用。`AudioSetup` 会在编辑器脚本加载后的 delayCall 中，通过 AssetDatabase 找到三段录音，自动填入 DefaultAudioMap 的空白 ACT1/2/3 槽位并保存。构建前也执行一次导入及资源校验。

自动导入只接受每个 ACT 恰好一个匹配文件，且不覆盖已配置的旁白；多版本或多段录音请手工在 Inspector 排序。已有场景 StageContent.narration 的显式录音优先于默认映射。

尚未提供的其他旁白保持空白并在进入阶段时显示提示，不会播放自编的历史解说，不新增字幕。M2/M5 提供新录音后，在映射资产中选择对应阶段的 narration 数组添加即可；Briefing 可按“开场、舒适性”顺序配置多段。

## 本机检查步骤

1. 等待 Unity/团结完成导入和编译。打开 `Assets/Resources/ExperienceAudio/DefaultAudioMap.asset`，确认 TheCase、ScaleUp、DressTheDiver 各有一段录音。如果未自动填入，点击 `Tools → Experience → Audio → Import ACT1-3 narration by filename`。
2. 点击 `Tools → Experience → Audio → Validate local audio assets`。Console 应显示四条环境/反馈音资产信息、各阶段旁白数量及 `Experience AUDIO VALIDATION PASSED`。该校验验证资产引用和 Mixer 分组，不代替试听。
3. 打开体验场景并 Play，点击工作人员面板 Start。Briefing 应听到缓慢淡入的港口底声；没有开场录音是当前明确的缺项。
4. 在介绍区继续体验，The Case 阶段先显示 Waiting for arrival。站在远处不应听到 ACT1 旁白；按引导走到展台前，引导完全消失后才自动播放 ACT1 录音并降低港口环境音。观察 Hierarchy 的 Experience Session 下四个 `Experience Audio - ...` 子对象。Narration 是单一语音源；Harbour/Underwater 是同一 Ambience 总线上的两个渐变源。
5. 旁白播放中 Pause，查看 Narration AudioSource 播放位置不继续推进；Resume 应接着播放，不从头开始。多次 Pause/Resume 不应叠音。
6. ACT1 真正戴上头盔后听感变闷；阶段进入 ACT2 后仍等待走到 ACT2 展台，引导消失后才开始 ACT2 录音，ACT3 同理。未佩戴成功时不能仅因抓取就切换内部音景。
7. 在旁白中 Skip，上一段立即停止，新阶段只播放自己的录音；ACT1–3 仍须确认新目标到达。Pause 后 Skip，应在 Resume 且满足新阶段到达条件后才播放旁白。
8. ACT3 给 NPC 戴上头盔仍保留原有 NPC 带路；在岸上进入 Descent 时保持港口音景。游客头部穿过水面时播放入水音，穿过水面时立即切换水下音景，上浮离水后恢复港口声。
9. Reset 后四个源都应停止。再次 Start，应能重新听到各阶段旁白，且没有上一名游客的语音。
10. 头显实机试听时，根据实际耳机与场馆噪声调整 DefaultAudioMap 音量；数字峰值留有余量不等于已经验证耳机的实际声压或主观舒适度。

## 验证与交付边界

- 运行时与编辑器 C# 使用项目本地引用编译检查。
- `python Tests/check-experience-audio-assets.py`：检查 WAV 格式、峰值余量、DC、循环接缝、Mixer YAML/分组与本地录音存在性。
- `Tests/run-experience-state-tests.ps1`：原有会话和命令回归测试。
- 同一测试脚本另包含 24 项旁白到达门控断言，覆盖 ACT1–3 等待、错误会话/阶段、暂停、重复事件、Skip 与 Reset；它不替代场景 Collider 和实际音频播放验证。
- `Tests/generate-experience-audio.py`：环境/反馈音与初始配置的可复现生成器，已存在的资产不覆盖。依赖 NumPy，离线校验另需 PyYAML；游戏运行不依赖 Python。
- Unity 批处理验证在当前会话没有运行成功；沙箱外运行未获授权。资源的实际 Unity 导入、AudioMixer 运行、暂停播放位置及头显舒适性必须在编辑器/实机按上表确认，不能把静态检查等同于验收通过。
- 正式开场、舒适性、内部限制、下潜说明、总结录音尚缺；ACT1–3 已有文件，没有替 M2/M5 修改旁白文本。

入水声使用独立 AudioSource（仍路由至 Feedback 总线），不受抓取反馈打断。DefaultAudioMap.waterEntryVolume 默认 0.55，可单独调整。Pause 暂停入水声并保留位置，Resume 继续；Skip/Reset 清理旧声音。音效为本地原创合成，并非下载的实录跳水素材。

## 当前更新

水下移动声已按要求删除，仅保留入水、出水与环境 BGM。出水时背景在水面穿越瞬间衔接，不等待短音效播完。最新结束流程见 Experience-Ending.md。

## 区域首次旁白与自由探索重播

- 本轮会话开始后，实际进入已配置旁白的区域会自动播放一次；离开再返回不会反复自动播放。判断使用 ACT1–3 的 ArrivalArea 展台范围，与字幕物体的位置无关。
- 正式流程当前 ACT1–3 目标仍等待到达引导完全淡出；提前走进其他展台或自由探索中首次到访，也只触发旁白，不会完成任务或推进阶段。
- 首次上浮后选择“自由探索”，走回 ACT1、ACT2 或 ACT3，原有说明面板下方会出现“再次播放本区解说 / Replay this area's narration”。用原有 UI 射线指向并点击，可从头重播。按钮只在该区域内显示；不新增阶段字幕或 Next。
- 重播使用同一个旁白音源，替换当前旁白，不叠加；连点有 0.75 秒间隔限制。Pause 暂停并禁用重播，Resume 接续；Skip、退出和 Reset 清理旧旁白。Reset 后新游客的首次访问记录重新计数。
- 介绍区和下潜平台也接入原有面板；目前只有 ACT1–3 提供了实际录音，缺录音区域显示不可点击的“本区解说尚未提供”。在 DefaultAudioMap 或 StageContent.narration 配置录音后启用。Inside 保留真实游客佩戴触发，不作为独立到访区域。
- 日志新增 RegionFirstNarration、NarrationReplayRequested；实际开始、结束仍记录 NarrationStarted、NarrationFinished。区域语音与任务阶段分开记录，播放不会伪造任务完成。

验证：运行 Tests/run-experience-state-tests.ps1，18 项命令、40 项音频门控/访问历史、140 项会话断言通过；运行时 C# 编译通过。尚需 Unity Play/头显检查按钮实际布局、射线点击与录音试听。

建议手测：Start → 到 ACT1 听首次播放 → 离开返回确认不自动重播 → 完成流程并选择自由探索 → 返回 ACT1 点击重播 → 同样检查 ACT2/3 → Pause/Resume → 退出、Reset、Start 确认新一轮可再次自动播放。被 Skip 且从未实际到访的展台，在自由探索第一次到达时仍自动播放。

### 重播按钮未出现的修复

ACT1–3 的原有面板带 HideAndDontSave；FindObjectsOfType 即使传 true 也不会返回这些对象，导致重播按钮没有创建。NarrationRegionPanels 改为 Resources.FindObjectsOfTypeAll 并仅接受有效、已加载场景中的 Canvas，排除预制件和资源资产；仍每秒补查一次以适应面板重建。创建成功记录 NarrationReplayControlAttached。此修复已通过本地运行时 C# 编译，尚需 Play 验证实际显示及射线点击。

复测：退出 Play，等待编译，再次 Play；选择自由探索并走进 ACT1–3 ArrivalArea，检查原有面板下方的重播按钮。暂停时按钮禁用，回到 Running 可点击。
