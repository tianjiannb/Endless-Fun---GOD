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
