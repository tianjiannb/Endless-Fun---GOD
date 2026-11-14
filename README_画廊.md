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
