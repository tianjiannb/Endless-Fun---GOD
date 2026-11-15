# 游趣无限 · 白模画廊（Placeholder Gallery）

按 `游戏策划案_框架.md` 3.1 / 3.2 / 3.6 搭的最小可玩骨架：横版侧视画廊，A/D 走动，走进画框按 E 进入画作场景，Esc 或按 1/2 带回选择结果，走到最右侧结局墙汇总。
所有素材都是运行时/编辑器生成的纯色带描边白模 PNG，不依赖任何外部美术。

## 一、打开 Unity 前必须先修好的问题

当前 Unity 2022.3.62f3c1 安装缺了 UPM 本地服务端：

```
E:\unity_editer\2022.3.62f3c1\Editor\Data\Resources\PackageManager\Server\UnityPackageManager.exe
```

`Server` 整个目录不存在（已确认：`Diagnostics`、`Editor`、`Packages...` 都在，唯独缺 `Server`），所以编辑器启动即报 H0007 / `Could not find Unity Package Manager local server application`。**这个只能靠重装/补装编辑器修复**，任何项目侧配置都绕不过去。

修复选项（任选其一）：

1. **Unity Hub 里 Verify / 重新安装 2022.3.62f3c1**（推荐）：Hub → Installs → 该版本右侧齿轮 → Uninstall 后重装。安装时如果杀软拦截 `UnityPackageManager.exe`，重装后再检查一次该路径是否存在。
2. **用 Hub 装同代 2022.3.x 版本**（例如 2022.3.62f1），然后把项目 `ProjectSettings/ProjectVersion.txt` 改成新版本号。美术/脚本全是白模，跨小版本没有迁移成本。
3. 若一定要保留现版本：找同版本安装包离线修复（`UnitySetup64-2022.3.62f3c1.exe`）覆盖安装到同一目录。

自检命令（PowerShell）：

```powershell
Test-Path 'E:\unity_editer\2022.3.62f3c1\Editor\Data\Resources\PackageManager\Server\UnityPackageManager.exe'
# 期望输出 True
```

## 二、Unity 打开后三步生成画廊

菜单栏 `Tools / 游趣无限`：

- `0. 一键生成全部（素材 + 场景）` — 一次跑完下面两步
- `1. 重建白模素材` — 在 `Assets/Art/Placeholder/` 生成 9 张白模图（画框、玩家、地面、墙面、背景）
- `2. 重建画廊场景` — 生成 `Assets/Scenes/Gallery/Gallery.unity` 与 `Assets/Scenes/Gallery/Painting_P1.unity`，同时写好 Build Settings 与 `Assets/Data/Paintings/*.asset`

生成完成后双击 `Gallery.unity` 直接 Play。

## 三、操作

| 按键 | 行为 |
| --- | --- |
| A / D | 左右移动（钳制 x ∈ [-8.5, 8.5]） |
| E | 进入身旁画框 / 打开结局墙 |
| 1 / 2 | 在画作场景里带回 `ending_a` / `ending_b` |
| Esc | 画作场景放弃，回画廊（结果记为 `abandon`） |

## 四、场景结构（对应策划案 3.1）

```
Main Camera        Orthographic, size 5.4, pos (0, 4, -10)
=== Environment ===  Backdrop / WallBase / Floor / FloorLine_0..8
=== Frames ===       Frame_p1 (x=-6) / Frame_p2 (x=0) / Frame_ending (x=+6), 全部 y=2.2, 尺寸 2.4x1.8
=== Player ===       Player @ x=-8, 高 1.8, BoxCollider2D trigger
=== UI ===           HUD (提示 + 左下操作说明) / ScreenFader (黑场, sortingOrder 500)
```

交互半径统一 1.2（`PaintingInteract.radius` / `EndingWall.triggerRadius`），Sprite PPU 统一 100。

## 五、脚本清单

| 文件 | 职责 |
| --- | --- |
| `Assets/Scripts/Core/ChoiceResult.cs` | 选择结果结构（paintingId / choiceId / outcome） |
| `Assets/Scripts/Core/GameSession.cs` | 跨场景会话：Results、Enter、Exit |
| `Assets/Scripts/Core/PaintingData.cs` | 画作 ScriptableObject 定义 |
| `Assets/Scripts/Core/PaintingSceneRoot.cs` | 画作场景根：Esc 退出、1/2 调试结局 |
| `Assets/Scripts/Player/PlayerMove2D.cs` | 横版移动与边界钳制 |
| `Assets/Scripts/Interaction/PaintingInteract.cs` | 画框检测 + 进入 |
| `Assets/Scripts/Interaction/EndingWall.cs` | 结局墙汇总面板 |
| `Assets/Scripts/UI/HUD.cs` | 交互提示与操作说明 |
| `Assets/Scripts/UI/ScreenFader.cs` | 黑场淡入淡出 |
| `Assets/Scripts/Editor/PlaceholderArtGenerator.cs` | 白模 PNG 生成器 |
| `Assets/Scripts/Editor/GallerySceneBootstrap.cs` | 场景 / 数据 / Build Settings 生成器 |

## 六、替换成正式美术时

1. 把正式图放进 `Assets/Art/`，Importer 设为 Sprite、PPU 100、Single。
2. 把 `Assets/Data/Paintings/*.asset` 的 `thumbnail` / `placeholderSprite` 指向正式图，`sceneName` 指向真实画作场景。
3. 场景里的画框换 sprite 即可，交互逻辑（半径 1.2、E 键、Build Settings 校验）完全不用改。

白模坐标与尺寸都写死在策划案数值上，正式美术按同尺寸导入不会错位。

## 七、分层画廊美术接入（2026-11-15）

本节记录当前分层美术的场景设置；以上白模说明保留作队友接口参考。

### 素材与透明通道

三张图片从 `~/Downloads/主界面/` 原样复制到 `Assets/Art/`，均为 **1920×1080 RGBA PNG**，没有 JPG 黑底，不需要抠图、去黑或改写像素。

| 源文件 | 项目文件 | 用途 | Sorting Order |
| --- | --- | --- | --- |
| `去一，二画.png` | `Assets/Art/Gallery_BG.png` | 带两个镂空区的洞穴主背景 | 0 |
| `一画带画框.png` | `Assets/Art/Gallery_Frame1.png` | 左上莲花画及完整画框 | 1 |
| `二画带画框.png` | `Assets/Art/Gallery_Frame2.png` | 中央雪夜窗景及完整画框 | 1 |

三张均以 Sprite / Single、PPU 100、中心 Pivot、Full Rect 网格导入，最大尺寸 2048、不压缩、不生成 Mipmap、不生成物理轮廓。保留整个 16:9 画布，不裁掉透明留白，原本显示为黑色的透明区域会正常透出底层。材质使用 URP `Sprite-Unlit-Default`，保持 Alpha 混合且不依赖灯光。

### 场景与层级

实际场景路径是 `Assets/Scenes/Gallery/Gallery.unity`；项目没有顶层的 `Assets/Scenes/Gallery.unity`，本次保留现有场景路径、GUID 和 Build Settings。

- `=== Environment ===/Gallery_BG`：新增正式背景，世界 Position `(0, 0, 0)`，Scale `(1, 1, 1)`。
- 原 `Backdrop`、`WallBase`、`Floor`、`FloorLine_*`：只关闭 SpriteRenderer，保留对象。
- `=== Frames ===/Frame_p1/Canvas`：替换为莲花图，世界 Position `(0, 0, 0)`，Scale `(1, 1, 1)`。
- `=== Frames ===/Frame_p2/Canvas`：替换为雪夜窗景图，世界 Position `(0, 0, 0)`，Scale `(1, 1, 1)`。
- 两幅画的 `PaintingInteract.canvasRenderer` 仍指向原来的 Canvas 组件；同时更新 `Assets/Resources/Paintings/p1.asset`、`p2.asset` 的 `thumbnail/placeholderSprite`，防止 `Start → ApplyData()` 恢复旧占位图。
- `Main Camera`：Position `(0, 0, -10)`，Orthographic Size `5.4`，Game 选择 **16:9** 后对应世界范围 `19.2×10.8`，三张图完整重合。原 GalleryCamera2D 组件保留，Follow 原本就是关闭状态。
- 原 `Highlight` 节点与所有引用保留，只关闭白模 SpriteRenderer，防止进入范围时弹出矩形遮挡美术。

### 视觉原点与地面 Trigger 分开

图像的位置已经包含在全尺寸画布里，不能再把画框图移到莲花或雪夜图案的中心。场景中保留交互脚本在 Frame 根节点上，并将根节点放到地面；Canvas 子节点用相反的本地位移，保持图像世界坐标为原点。

| 根节点 | 地面 Position | Canvas 本地 Position | 原进关目标 |
| --- | --- | --- | --- |
| `Frame_p1` | `(-7.245, -3.3, 0)` | `(7.245, 3.3, 0)` | `Painting_P1` |
| `Frame_p2` | `(0.035, -3.3, 0)` | `(-0.035, 3.3, 0)` | `Painting_P2` |

父子 Scale 都为 1。两个原有 BoxCollider2D 仍启用且为 Trigger，本地 Size `(2.4, 1.8)` 与 Offset `(0, 0)` 保持原样，因此范围在画框水平中心下方的地面，与玩家行走高度一致。原 E 键、交互半径 1.2、数据 GUID、事件绑定与进关脚本不变。

玩家仍是原来的对象、Sprite 和碰撞体，只将行走高度调为 Y `-3.3`、X 边界调为 `[-9.1, 9.1]`，使其能在居中的新画布内到达所有入口；移动代码、速度和加减速未修改。

本次拉取还包含第三个入口 `Frame_chie` 和结局墙。它们没有删除、没有改进关绑定：`Frame_chie → Scene_Chie` 放在右侧夜色城堡下方 `(3.9, -3.3, 0)`，结局墙放在右下装饰画下方 `(6.515, -3.3, 0)`；其旧 Canvas 占位图关闭渲染，原脚本和 Collider 保留。

### 运行与检查要点

1. 打开上述 Gallery 场景，Game 视图选 **16:9** 后 Play。确认左上为莲花，中央为雪夜窗景，周围透明区域不会形成黑色矩形遮挡。
2. 用 A/D 走到左侧莲花下方，出现原有提示后按 E，目标仍为 `Painting_P1`；Esc 返回后画面应保持对齐。
3. 走到中央雪夜画框下方按 E，目标仍为 `Painting_P2`。用户已明确选择保留这个绑定，不把雪夜画改接到 Chie。
4. 右侧第三入口仍保留 `Scene_Chie` 绑定。场景里的旧占位图和 Highlight 不应重新露出。
5. Scene 视图开启 Gizmos 后选择 Frame 根节点，查看地面 BoxCollider2D。若只想微调触发区，可改 Collider Offset；不要通过自动匹配 SpriteRenderer bounds 来放大碰撞体，因为视觉 Sprite 的 bounds 是整张 16:9 画布。
6. 如移动 Frame 根节点，须反向补偿 Canvas 本地 Position，保证三张视觉层的**世界 Position 都为零**。不需要单独移动、缩放或裁切某张画。
7. 先退出 Play 再修改和保存场景。`Tools` 中重建白模/重新接入 Chie 的菜单会重新生成画廊，审核本布局时直接使用已保存的场景即可。

本次不新增或修改交互逻辑，不写镜头动画或视频脚本，不执行 Git 提交。


## 八、莲花画入画视频（2026-11-15）

本节是在分层美术基础上新增的入画视频流程。只给 `Frame_p1` 配置视频，第二幅画和 Chie 保留本次修改前的绑定与流程。

- 素材：`~/Downloads/kgjsK.mp4` 原样复制为 `Assets/Art/Gallery_LotusIntro.mp4`，约 8.04 秒、736×400、24 帧/秒，包含原视频音轨。不改写或重新编码源视频。
- 挂载：`=== Frames ===/Frame_p1` 新增 `PaintingVideoIntro`，`Clip` 引用上述视频。原 `PaintingInteract`、PaintingData、地面 Collider、画框图层和摄像机参数保留。
- 操作：鼠标左键直接点击左上莲花画框，或按原来的方式走近后按 E，都会先播放视频，播放结束才调用原 `PaintingEntry` 进入 `Painting_P1`。鼠标点击不要求角色先走到画下方。
- 点击区域：`Click Area` 是相对于整张 Sprite 画布的归一化矩形，左下角为原点；范围只覆盖左上莲花画框，透明画布上的其他位置不响应。它独立于地面交互 Collider，不会移动或放大现有 Trigger。
- 表现：先准备好视频的实际画面，再以 0.25 秒淡入全屏。保持原始宽高比例铺满屏幕，16:9 下轻微裁切左右边缘，避免拉伸及黑边。自然播放结束后淡到黑场，再进入游戏。第一版不增加跳过按钮或额外镜头动画。
- 锁定与声音：准备、播放和切换期间暂停角色水平移动及画廊画框交互，重复点击/按 E 不会重复进入。暂停画廊 AudioManager 中正在播放的声音，用独立 AudioSource 播放原视频音轨；结束、取消或失败时恢复原状态。
- 容错：视频缺失、准备超时、解码中断会显示中文提示并恢复画廊控制，可以重试。禁用组件或退出场景会释放 VideoPlayer、RenderTexture 和运行时 UI。
- 返回：继续使用原进关与返回接口，回到画廊后恢复完整画面，可以再次点击莲花重播。

审核时：打开 `Assets/Scenes/Gallery/Gallery.unity`，Game 选择 16:9 后 Play；点击莲花画，确认全屏动画与声音、约 8 秒后才进入第一关、播放中 A/D 与重复点击无效；从游戏返回后重复测试，再验证走近按 E 也播放动画。第二幅画与 Chie 不应新增这段视频。可在 `Frame_p1 / PaintingVideoIntro` 调整视频引用、淡入淡出时长、准备超时与鼠标点击区域。

暂不执行 Git 提交，等待审核与试玩。


## 九、雪夜画通关 MV（2026-11-15）

当前中央 `Frame_p2` 已由队友接到 `chie.asset → Scene_Chie`，本次沿用实际绑定，不改回旧的 `Painting_P2` 占位场景。莲花入画视频保持原样。

- 源文件：`~/Downloads/11月14日(3).mp4`，生成游戏兼容副本 `Assets/Art/Gallery_SnowEnding.mp4`；约 6.55 秒，原片为 3844×2160、30 帧/秒、H.265，带双声道音轨。
- 导入：Unity 内置转码对这份 H.265 原片丢失约 0.67 秒，因此改用 FFmpeg 生成 H.264 / 1922×1080 兼容副本，保留全部 196 帧与原 AAC 音轨；Unity 关闭二次转码。下载目录原片不变，另在 `Logs/GallerySnowEndingReview/source/` 留存原片备份，不加入版本管理。
- 挂载：`Assets/Scenes/Gallery/Scene_Chie.unity` 的 `ChieBootstrap` 对象新增 `ChieEndingMovie`，Clip 绑定上述文件。没有改动原 ChieBootstrap 组件、相机、配置资产或画廊入口。
- 时机：原结局选择 → 回应与终幕台词 → 原程序化抽离演出及文字淡出 → 通关 MV → 原退出黑场 → 原 `GalleryBridge.Exit` 记录结局并返回画廊。进雪夜游戏时不播放这段 MV。
- 分支：修满六处或提前放下笔，均在各自结局正常结束后播放同一支 MV；原 choiceId 与 `restoredN` 结果不变，不新增修复数量门槛。
- 播放：先等待实际视频画面可用，再淡入全屏；保持原始宽高比铺满屏幕，播放原视频音轨，期间暂停场景内正在播放的其他音源。MV 结束后淡到黑场，并保持黑色底板直到切回画廊，避免闪回旧游戏画面。
- 返回目标：修正 `ChieGalleryLink.Enter` 的重复登记；画廊已经记录相同 paintingId 时，不在雪夜场景再次调用 BeginEnter，避免将 ReturnScene 覆盖成 Scene_Chie 自己。
- 输入：原流程在 Exit 阶段保持 Busy，修画已停止，运行时视频层阻挡 UI 点击，不增加跳过按钮；重复调用不会同时播放两支 MV。
- 容错：缺少视频时继续原返回流程；准备超时、解码错误或播放停滞会显示中文提示，再继续返回与结果记录。禁用视频组件或退出场景时释放播放器、纹理、运行时 UI，并恢复之前暂停的声音。

试玩步骤：从 Gallery 走到中央雪夜画下方按 E，正常完成对话与修画，选择结局并推进终幕台词，等待原结局演出结束；确认随后全屏播放约 6.5 秒 MV，播完自动回画廊。也可用「放下笔」走较短结局检查播放。返回后原结局记录应保留，莲花点击入画动画仍可正常播放。若要调整视频或淡入淡出，选择 `Scene_Chie / ChieBootstrap / ChieEndingMovie` 修改 Clip、Fade Duration 与 Prepare Timeout。

本次不执行 Git 提交，等待审核与试玩。
