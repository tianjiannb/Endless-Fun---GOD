# 玩法一：滑行与分裂

根据 `/Users/bookid/Downloads/玩法一.md` 实现的 Unity 2D 解谜原型。
角色运动使用 `RectTransform.anchoredPosition` 位移插值，规则使用整数格坐标判断；没有 Rigidbody、Collider、碰撞回调或力学模拟。

## 运行和审核

1. 使用项目当前的 Unity **2022.3.62f3** 打开仓库，等待脚本导入。
2. 打开 `Assets/Scenes/SampleScene.unity`，点击 Play。若当前打开的是 Untitled 场景，需要先打开此文件。
3. 在 Game 窗口内点击一次，让键盘输入获得焦点。
4. 使用 WASD / 方向键选方向；每次按下执行一整次滑行。也可以点击界面方向按钮。
5. `Z` / Backspace 撤销一整次操作，`R` 重置本关。过关后点击 Next level 或按 `N`。
6. 侧栏数字按钮可以直接切换五个示例关卡，便于审核不同机制。

界面采用英文和字母标记，使用 Unity 内置字体，不依赖本机中文字体、外部图片、TMP 字体资源或联网下载。

| 标记 | 含义 |
| --- | --- |
| A / B | 两个身体；浅绿和浅紫 |
| 深色实心格 | 墙 |
| S | 分裂格 |
| T | 翻转开关 |
| D / - | 关闭 / 打开的门 |
| G | 目标格 |

## 已实现规则

- 每次操作只能沿一个方向滑到底，撞墙、边界、关闭的门或另一个身体才停。
- **停在**分裂格上才分裂，滑过不触发；本关只分裂一次。
- 原身体留在分裂格，新身体生成在本次移动方向的后方一格，下次操作开始参与移动。
- 分裂后两个身体同时接收同一方向，分别滑到停下，不能重叠或穿越。
- 停下的身体在本次操作剩余时间里充当墙；下次操作重新参与移动。
- 每个身体每进入一次开关就翻转一次门状态；同一个开关被两个身体先后经过，会翻转两次。
- 所有关卡指定的目标格被身体占住即过关。允许有额外身体不在目标上，门状态不参与胜利条件。
- 撤销恢复身体位置、身体数量、分裂标记、开关/门状态、步数和胜利状态；可连续撤销。
- 无法移动的输入不计步、不增加撤销记录。重置清空当前关卡的撤销历史。
- 位移动画期间不接受新的方向；撤销、重置和切换关卡可立即中断动画，并同步画面。

## 文档未明确部分的结算约定

为了让双身体和开关结果稳定，采用以下明确规则；这些是实现约定，可以按审核意见调整。

1. **同步步进**：每次输入拆为若干个“一格”的同步时刻。所有身体先用该时刻开始时的门状态判断能否进入下一格，再同时移动，最后结算进入的开关。两个身体同一时刻各踩一次开关，合计翻转两次，门状态不变。
2. **连续跟随**：同一直线前后相邻的身体可以一起前进，后面的身体可以进入前面身体在这个时刻刚离开的格子。先分析前方身体，是为了计算能否腾出格子；所有位置仍在同一个时刻更新。
3. **停下后不重启**：身体因关闭的门停下后，即使另一个身体在随后打开了门，它也要等下一次输入才移动。
4. **关闭门上的身体**：关闭的门阻止进入；如果关门时已有身体在门格上，不挤走、不销毁该身体，允许其继续离开。
5. **胜利结算**：本次滑行和分裂全部结束后检查目标。仅仅滑过目标不算占住。如果配置两个目标，需要两个目标都被占住。
6. **共享门状态**：当前玩法使用一个开关通道；任意 T 都翻转全部 D。关卡可以配置门初始是否打开。
7. **非法分裂空间**：后方若为墙、边界、关闭的门或其他身体，取消这次分裂，不消耗本关分裂机会。不生成重叠身体。示例关卡不会依赖这种布局。

## 修改或增加关卡

在 Project 窗口选择 `Assets/SplitSlide/Resources/PuzzleLevels.asset`，在 Inspector 中修改 `Levels`：

- `Title` / `Hint`：关卡名称与提示。
- `Doors Initially Open`：门初始是否打开。
- `Rows`：从上往下的 ASCII 地图，每行等宽。

```text
#########
#.....G.#
#.......#
#P.....S#
#########
```

地图字符：`#` 墙、`.` 地板、`P` 唯一起点、`S` 分裂格、`T` 开关、`D` 门、`G` 目标。
必须有一个 P 和一至两个 G。地图外自动视为墙，可以不画外圈墙。
脚本会在编辑关卡资源时报告地图格式问题。新增独立关卡库可使用 `Create > Split Slide > Level Library`，再赋给场景中 `Split Slide Game` 的 `Level Library`。

## 代码入口

```text
Assets/SplitSlide/
  Core/                       纯 C# 规则，无 Unity 引用
    Cell.cs                   格坐标、方向、格子类型
    LevelDefinition.cs        地图解析与合法性检查
    PuzzleSession.cs          同步滑行、分裂、门、胜利、撤销
  Runtime/
    PuzzleLevelLibrary.cs     可编辑关卡资源
    PuzzleGameController.cs   键盘/按钮输入、动画调度、关卡切换
    PuzzleView.cs             棋盘、身体与侧栏界面，位移插值
  Resources/PuzzleLevels.asset 五个示例关卡
  Tests/
    EditMode/                 规则边界与示例解法测试
    PlayMode/                 场景加载、动画中断、五关实际运行测试
```

`PuzzleSession.Slide()` 一次计算出整次操作及其逐格快照。`PuzzleGameController` 按顺序播放快照，`PuzzleView.Interpolate()` 直接修改身体位置。逻辑不依赖帧率，因此不会因为帧率变化漏踩开关、穿过身体或多分裂。

## 验证

Unity 菜单 `Window > General > Test Runner`，分别运行 EditMode 和 PlayMode。
规则测试覆盖滑过/停留分裂格、双身体跟随与阻挡、同开关重复经过、同一时刻双开关、门在滑行中开关、目标经过与占住、撤销和重置。PlayMode 测试通过实际场景与控制器走完五关。

本次验证使用临时工程副本执行，保留原编辑器当前打开的场景；41 项 EditMode 测试和 4 项 PlayMode 场景测试通过，并另外进行了实际界面的离屏渲染检查。连续 2,000 次随机输入的测试会逐帧检查身体不越界、不进墙、不重叠。

命令行也可以运行，例如（运行前确保同一个项目未被另一个 Unity 实例占用）：

```sh
"/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity" \
  -batchmode -projectPath "$PWD" -runTests -testPlatform EditMode \
  -testResults /tmp/split-slide-editmode.xml -logFile /tmp/split-slide-editmode.log
```

把 `EditMode` 改成 `PlayMode` 可运行场景测试。

<details>
<summary>审核用解法（希望自己试玩时可先不看）</summary>

| 关卡 | 一条可行解；U/D/L/R 表示上/下/左/右 |
| --- | --- |
| 1 First glide | U R |
| 2 Better together | R U |
| 3 Change the route | R |
| 4 A useful obstacle | D L U L D |
| 5 Crossed signals | D U D L D R |

第二、四、五关需要分裂；第四、五关还需要开关门。这些解法同时保存在自动化测试里。

</details>

所有改动保留在本地工作区，供审核后再决定提交。
