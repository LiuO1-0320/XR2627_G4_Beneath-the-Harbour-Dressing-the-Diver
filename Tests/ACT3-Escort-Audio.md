# ACT3 带路期间编辑器崩溃复测

2026-11-22，在 `integration` 当前工作树的完整 `BasicScene` 上复测。场景 SHA-256 为 `d7b5ceb0050c4a3fff3ceb190744b362df4a7b7dce8c165c74dbea31410414e4`，受测播放器 SHA-256 为 `2ab5964c7dbff41bb3b7ba08ca24865a6cdc2c44590762840b847c01b98a3284`。

## 故障与修复

修复前三次正常音频测试均在 ACT3 配装后、跟随潜水员期间退出。三份转储都显示原生音频线程在 `ma_custom_read_node_process_pcm_frames` 调用的重采样路径发生访问冲突 `0xC0000005`。三轮都在此前播放过历史影片、离场、续播并再次离场。仅关闭视频音轨的既有对照跑通一次。转储能定位音频线程，未能确定引擎哪一处内部实现导致了访问冲突。[修复前的原始证据](Visitor-Input-Evidence.txt)

旧播放器在离场后继续保留已经暂停的解码器和绑定的空间音轨。修复后，屏幕准备播放时绑定影片自带的第一个音轨，离场先保存游标、停止解码，再解除音轨绑定。回到屏幕前恢复暂停位置，按播放后继续。固定展台扬声器关闭多普勒变调，保留空间定位、距离衰减和旁白播放时压低影片音量的行为。工作人员在展台前暂停会话时保留原位，全局复位则清除影片进度。[展台播放器](../XRG4_BeneathTheHarber/Assets/script/ExhibitVideoScreen.cs)

## 完整场景重复测试

使用 Windows 团结引擎 1.6.13（运行时报告 2022.3.61t14）、XRI 3.1.2、批处理 Play 模式。每轮单独启动编辑器，读取诊断记录 `mode=NORMAL engineAudio=True ambience=True narration=True video=True`。测试驱动控制 XR Origin、头部凝视和手柄事件，护送与绳索升降均为 1 倍速。流程中从影片扬声器、ACT1 旁白和博物馆背景音分别读回音频采样，三个通道都须有非零信号。

1. 由工作人员 Start，完成 Briefing，进入 ACT1。实际播放历史影片，离开、返回并续播，再次离开展台。
2. 完成试戴和七件零件拆装，为潜水员戴好头盔。游客原地停下，让潜水员在约 4.5 米处等待，再赶上并跟随他走到港口。
3. 沿绳下潜，回收 1/9 后提前上浮，确认尚未出现结束面板。再下潜，收齐 9/9。确认仅在抵达平台后出现结束面板。
4. 选择自由探索、退出，工作人员 Reset 后再 Start。验证三块视频屏幕均已停止，游标归零，新一位游客从 Briefing 第一步开始。

| 轮次 | 护送时间 | NPC 水平起终点距离 | 流程断言 | 结束状态 |
|---|---:|---:|---:|---|
| 06 | 47.571 秒 | 44.500 米 | 190 | 返岸、退出、复位后新 Start，退出码 0 |
| 07 | 47.573 秒 | 44.500 米 | 190 | 返岸、退出、复位后新 Start，退出码 0 |
| 08 | 47.576 秒 | 44.500 米 | 190 | 返岸、退出、复位后新 Start，退出码 0 |

三轮均到达码头，主流程日志未记录运行期 Error、Exception 或 Assert。第 08 轮还在码头及海底各采样 1.1 秒，港口 `HarbourSoftLoop` 和水下 `UnderwaterSoftLoop` 均在播放。以下为该轮来自不同音源的实测记录。

```text
AUDIO_FIX_MIX voice=act1_helmetHistory voicePeak=0.3675194 bed=MuseumGentleLoop bedPeak=0.003721619
AUDIO_FIX_VIDEO clip=helmet_history enabled=True volume=0.1625 doppler=0 embeddedHz=48000 outputHz=48000 pcmPeak=0.01102193 time=0.533196785186085
AUDIO_FIX_BED stage=Descent field=harbour active=True clip=HarbourSoftLoop gain=0.25 pcmPeak=0.03104927 scale=1
AUDIO_FIX_BED stage=SeabedCleanup field=underwater active=True clip=UnderwaterSoftLoop gain=0.25 pcmPeak=0.03264737 scale=1
```

## 三个视频展台

另起编辑器，分别实际播放 `Diver.mp4`、`helmet_component.mp4` 和 `helmet_history.mp4`。三条嵌入音轨均为 48 kHz 双声道，均从各自展台的空间音源读到非零 PCM。此轮 93 条断言通过，进程退出码为 0。

| 影片 | 实测 PCM 峰值 | 拖动后立即离场时保存的位置 | 两次进出都没按播放时的显示 |
|---|---:|---:|---|
| Diver | 0.03410039 | 94.680 秒 | 01:34 / 02:37，进度条 60% |
| helmet_component | 0.0005369628 | 156.200 秒 | 02:36 / 04:20，进度条 60% |
| helmet_history | 0.009964055 | 73.681 秒 | 01:13 / 02:02，进度条 60% |

测量取播放期间一小段样本，峰值用于确认各影片独立输出声音。补测还覆盖准备尚未完成就离场、工作人员 Pause / Resume、回到展台按播放后从长时间书签继续，以及 Reset 后下一位游客从片头开始。引擎重新准备并暂停时会暂时报告 `frame=-1`、`time=0`。此时界面和保存逻辑使用已有书签，直到引擎呈现有效帧。这一处理与 Unity 对 `time` 的定义一致，它表示当前已呈现帧的时间。[Unity VideoPlayer.time 说明](https://docs.unity3d.com/cn/current/ScriptReference/Video.VideoPlayer-time.html)

有一次补测启动在加载 `BasicScene` 纹理时退出，发生在正常音频初始化与任何游客动作之前，调用栈为 `DynamicHeapAllocator::CreateTLSFBlock → SharedTextureData → LoadSceneOperation`。这一轮没有进入护送测试。三片补测在同一份项目上将批处理编辑器的后台工作线程限制为 2 后通过。现场已单独保存该转储。

## 其他回归及测试边界

会话、复位、Back、凝视、ACT2 零件、工作人员命令、音频入区和教程的既有测试均通过。此前异常输入报告中的两项结果在本轮仍可复现。展台自动归位与游客主动装回共用同一日志事件（A24），潜水员带路期间原展台的“开始对话”按钮仍接受点击且不反馈（ACT3-03）。两项都未中断护送或后续流程。[异常输入报告](Visitor-Input-Report.md)

Windows 编辑器内验证了空间音源的实际 PCM 输出与流程状态。XR Origin 与输入由测试驱动控制，未在 Quest 3 真机或头显扬声器上实听。

## 原始记录

本机完整日志、测试驱动和各轮进程退出记录保存在 `%LOCALAPPDATA%\Temp\FullHarbourHarness\audio-fix-293054924`。下列校验值对应交付版的三轮旅程和三片补测。

| 日志文件 | SHA-256 |
|---|---|
| `normal-06.log` | `6acfd1508a38068be0ab3441145f855e11f920cfcd4281e6bdcbff51ace0d2f4` |
| `normal-07.log` | `7e15c9063aa8562900674161c81a7a6d7f206a7b1d7b1d30ea805f1731e506e7` |
| `normal-08.log` | `613ebe96be3c97e2c408ff9631625b2429330cd18aa75d879d4d68e35497697e` |
| `three-films-04.log` | `2d0f61458763bde12f9abad86cfd19e8a65cd6449a47b0dc0c0a59ebf07deb90` |
