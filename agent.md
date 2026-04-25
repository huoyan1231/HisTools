# HisTools — Agent 项目指南

> **White Knuckle** (Gale 平台) BepInEx 模组 — 面向速跑玩家/新手的各种工具集

| 属性 | 值 |
|---|---|
| **名称** | HisTools |
| **版本** | 0.3.0 |
| **Plugin GUID** | `com.cyfral.HisTools` |
| **作者/GitHub** | cyfral123 / [github.com/cyfral123/HisTools](https://github.com/cyfral123/HisTools) |
| **目标框架** | .NET Standard 2.1 / C# / Unity (BepInEx) |
| **依赖** | BepInEx-BepInExPack-5.4.2100, DOTween, TextMeshPro, Newtonsoft.Json |
| **IDE** | Visual Studio 2022 v17.5 |

---

## 一、项目概述

HisTools 是一个 **BepInEx 模组插件**，运行在 Unity 游戏 **White Knuckle**（通过 Gale 模组管理器分发）中。它提供了一套面向速跑玩家和新手的辅助工具，包括路线录制/回放、速跑统计、Buff 显示、物品生成概率显示、调试 HUD 等功能。

### 核心设计理念
- **功能模块化**：每个功能继承自 `FeatureBase`，通过工厂模式注册和创建
- **事件驱动架构**：使用发布-订阅事件总线 (`EventBus`) 解耦功能间通信
- **Harmony 补丁**：通过 Harmony Patch 拦截游戏原生方法注入自定义行为
- **纯代码 UI**：所有 UI 通过代码动态构建（Unity uGUI + DOTween 动画），不依赖场景预制体（除 AssetBundle 资源）
- **设置持久化**：所有功能开关和设置项自动保存为 JSON，重启后恢复状态

---

## 二、目录结构与职责

```
D:/git/HisTools/
├── Plugin.cs                        # ★ 插件入口点 [BepInPlugin]
├── Constants.cs                      # 常量定义 (GUID/版本/路径/UI常量)
│
├── manifest.json                     # Thunderstore 发布清单 (v0.2.0)
├── README.md                         # 使用说明 (英文)
├── CHANGELOG.md                      # 版本更新日志
├── README_ArtifactsInfo.md           # Artifacts 功能中文说明文档
│
├── Assets/                           # AssetBundle 资源包 (~558KB)
│   └── histools                      # UI预制体、标记器、标签等
│
├── Libs/                             # 第三方 DLL 引用 (从游戏提取, gitignore)
│   ├── Bepinex/                      # BepInEx 核心库
│   └── *.dll                         # 游戏程序集 (Assembly-CSharp 等 100+ DLLs)
│
├── Features/                         # ★ 核心功能模块
│   ├── Controllers/                  # 功能系统基础设施
│   │   ├── FeatureBase.cs            # 功能抽象基类 (状态管理/设置系统)
│   │   ├── FeatureFactory.cs         # 工厂模式 - 注册和创建功能实例
│   │   ├── Manager.cs                # 全局功能注册表单例 (FeatureRegistry)
│   │   ├── IFeature.cs               # 功能接口定义
│   │   ├── IFeatureFactory.cs         # 工厂接口
│   │   ├── ICategory.cs              # 分类接口
│   │   ├── SettingBase.cs            # 设置基类
│   │   └── Settings.cs               # 具体设置实现 (BoolSetting / FloatSliderSetting / ColorSetting)
│   │
│   ├── Artifacts/                    # Artifacts 子功能 (继承自 ArtifactsInfo)
│   │   ├── GloveEnergyDisplay.cs     # 手套能量显示
│   │   └── RebarDistanceDisplay.cs    # 钢筋距离显示
│   │
│   ├── RoutePlayer.cs                # ★ 路线播放器 (~646行, 核心大模块)
│   ├── RouteRecorder.cs              # ★ 路线录制器 (~430行)
│   ├── ArtifactsInfo.cs              # Artifacts 信息统一入口 (管理子模块)
│   ├── BuffsDisplay.cs               # Buff 效果显示器 (堆叠/图标/剩余时间)
│   ├── TimedPerkDisplay.cs           # 限时技能倒计时显示 (~490行, 含反射)
│   ├── SpeedrunStats.cs              # 速跑统计信息 (当前/上一关卡时间/最佳/平均)
│   ├── DebugInfo.cs                  # 调试信息 HUD (坐标/速度/关卡名等)
│   ├── ShowItemInfo.cs               # 物品生成概率显示
│   ├── CustomFog.cs                  # 自定义雾效果 (通过 Shader 全局变量)
│   ├── CustomHandhold.cs             # 自定义扶手颜色
│   └── FreeBuying.cs                 # 免费购买 (技能/物品/刷新)
│
├── Patches/                          # ★ Harmony 补丁
│   ├── InputManagerPatch.cs          # 输入管理器补丁 (光标可见性: 控制台/菜单/弹窗)
│   ├── PlayerPatch.cs                # 玩家补丁 (LateUpdate → 相机锁定 + 速度事件发布)
│   ├── FXManagerPatch.cs             # FX管理器补丁 (雾效果 + 扶手材质颜色替换)
│   ├── App_PerkPagePatch.cs          # 技能页面补丁 (免费购买技能/刷新)
│   ├── CL_AssetManagerPatch.cs       # 资源管理器补丁 (加载 AssetBundle 预制体)
│   ├── CL_GameManagerPatch.cs        # 游戏管理器补丁 (GameStart 事件)
│   ├── WorldPatch.cs                 # 世界补丁 (WorldUpdate + EnterLevel 事件桥接)
│   ├── ENV_VendingMachinePatch.cs    # 自动售货机补丁 (免费购买物品)
│   └── Projectile_ReturnRebarPatch.cs # 钢筋投射物补丁 (生成事件)
│
├── UI/                               # ★ 用户界面系统
│   ├── FeaturesMenu.cs               # ★ 主菜单管理器 (Canvas 构建/显示隐藏/分类)
│   ├── SettingsUI.cs                 # 设置面板 UI 绘制 (开关/滑块/颜色选择器)
│   ├── Category.cs                   # MyCategory - 分类面板实现 (垂直布局)
│   ├── FeatureButton.cs              # 功能开关按钮组件 (Toggle + 动画)
│   ├── SettingButton.cs              # 设置按钮 (扳手图标, 从AssetBundle加载)
│   ├── MenuAnimator.cs              # 菜单动画控制器 (DOTween 淡入淡出/缩放)
│   ├── UIExtensions.cs               # Transform 扩展方法 (AddMyText)
│   │
│   └── Controllers/                  # UI 控制器
│       ├── SettingsPanelController.cs # 设置面板控制器 (单例, 底部弹出面板)
│       ├── PopupController.cs         # 弹窗控制器 (输入/保存/确认弹窗)
│       ├── UIButtonFactory.cs         # 按钮工厂 (创建功能按钮+恢复保存的状态)
│       ├── CategoriesAnimator.cs      # 分类展开动画 (级联延迟淡入)
│       ├── CategoryController.cs      # 分类头部控制器 (拖拽/折叠/右键)
│       ├── CategoryFactory.cs         # 分类工厂接口
│       └── ICategoryFactory.cs        # 分类工厂接口定义
│
├── Events/                           # ★ 事件总线系统
│   ├── EventBus.cs                   # 发布-订阅事件总线 (泛型实现, Dictionary<Type, List<Delegate>>)
│   └── Events.cs                     # 所有事件类型定义 (12种事件):
│                                       # WorldUpdateEvent, EnterLevelEvent,
│                                       # PlayerLateUpdateEvent, GameStartEvent,
│                                       # ToggleRouteEvent, FeatureToggleEvent,
│                                       # FeatureSettingsMenuToggleEvent,
│                                       # FeatureSettingChangedEvent,
│                                       # LevelChangedEvent, SettingsPanelShouldRefreshEvent
│
├── Utils/                            # 工具类库
│   ├── Files.cs                      # 文件操作 (JSON读写/配置持久化/路由文件/内置路由解压)
│   ├── Logger.cs                     # 日志封装 (BepInEx ManualLogSource)
│   ├── Palette.cs                    # 颜色工具 (HTML↔Color转换/透明度处理)
│   ├── Vectors.cs                    # 坐标转换 (世界坐标 ↔ 关卡本地坐标)
│   ├── Raycast.cs                    # 射线检测 (获取凝视目标位置)
│   ├── Anchor.cs                     # UI 锚点定位 (6个屏幕位置预设)
│   ├── CoroutineRunner.cs            # 协程运行器 (全局 DontDestroyOnLoad 单例)
│   ├── Debounce.cs                   # 防抖工具 (设置变更延迟保存)
│   ├── EscCloseCanvasGroup.cs        # ESC 键关闭 CanvasGroup 组件
│   ├── Cheat.cs                      # 作弊检测封装
│   ├── Text.cs                       # 文本工具 (关卡名紧凑显示)
│   │
│   ├── RouteFeature/                 # 路线系统专用工具
│   │   ├── RouteModel.cs             # 数据模型 (RouteData/RouteInfo/RouteInstance/Note/Vec3Dto)
│   │   ├── RouteLoader.cs            # 路线 JSON 加载 (含旧版本格式兼容)
│   │   ├── RouteMapper.cs            # 实体→DTO 映射
│   │   ├── Smooth.cs                 # 路径平滑算法
│   │   ├── RouteStateHandler.cs      # 路线状态处理器 (鼠标切换启用/禁用)
│   │   ├── LookAtPlayer.cs           # 3D文本朝向玩家组件
│   │   ├── MarkerActivator.cs        # 跳跃标记激活动画
│   │   ├── PointAppear.cs            # 历史点出现动画
│   │   ├── PointDisappear.cs         # 历史点消失动画
│   │   ├── AutoCenterHorizontalScroll.cs # 水平滚动自动居中
│   │   └── BackwardCompatibility/   # 旧版路线格式兼容
│   │       ├── RouteOldModel.cs     # V1 格式数据模型
│   │       └── RouteOldSupport.cs    # V1→V2 格式转换
│   │
│   └── SpeedrunFeature/              # 速跑统计专用工具
│       └── RunsHistory.cs            # 运行历史加载与统计计算 (最佳/中位数时间)
│
├── Prefabs/
│   └── PrefabDatabase.cs             # AssetBundle 加载与缓存管理 (单例)
│
├── BuiltinRoutes/                    # 内置路线 JSON 文件 (嵌入资源, 首次运行时解压)
│   ├── air/                         # air 区域路线 (11个)
│   ├── broken/                      # broken 区域路线 (11个)
│   └── storage/                     # storage 区域路线 (17个)
```

---

## 三、架构详解

### 3.1 插件启动流程 (`Plugin.Awake()`)

```
Awake()
  ├─ InitializeConfiguration()     // BepInEx ConfigBinding (调色板/按键绑定)
  ├─ InitializeHarmony()           // new Harmony(GUID).PatchAll()
  ├─ CreateRequiredDirectories()    // 创建配置/路线/设置/统计 目录
  ├─ InitializeUI()                // 创建 FeaturesMenu GameObject + Components
  ├─ InitializeFeatures()           // 注册并创建所有功能实例到分类:
  │   Visual → DebugInfo, CustomFog, CustomHandhold, ShowItemInfo,
  │            BuffsDisplay, TimedPerkDisplay, ArtifactsInfo
  │   Path   → RoutePlayer, RouteRecorder
  │   Misc   → FreeBuying, SpeedrunStats
  ├─ SubscribeToEvents()            // 订阅设置变更/游戏开始/设置面板切换事件
  └─ Files.EnsureBuiltinRoutes()   // 首次运行解压内置路线资源
```

### 3.2 功能系统 (`Features/Controllers/`)

每个功能是一个继承 `FeatureBase` 的类:

```csharp
public class MyFeature : FeatureBase
{
    public MyFeature() : base("MyFeature", "Description")
    {
        // 在构造函数中添加设置项
        AddSetting(new BoolSetting(this, "Option Name", "description", defaultValue));
        AddSetting(new FloatSliderSetting(this, "Slider", "desc", min, max, step, decimals));
        AddSetting(new ColorSetting(this, "Color", "desc", Color.white));
    }

    public override void OnEnable()  { /* 订阅事件 */ }
    public override void OnDisable() { /* 取消订阅 */ }
}
```

**关键机制**:
- `FeatureToggleEvent` 自动触发 `OnEnable()`/`OnDisable()` 并保存状态
- `FeatureSettingChangedEvent` 触发 `OnSettingChanged()` + 防抖 2.5s 后写入 JSON
- 所有设置值从 JSON 配置文件自动恢复

### 3.3 事件总线 (`Events/EventBus.cs`)

轻量级发布-订阅系统:
- `EventBus.Subscribe<T>(Action<T> callback)` — 订阅
- `EventBus.Unsubscribe<T>(Action<T> callback)` — 取消订阅
- `EventBus.Publish(T eventData)` — 发布

**核心事件**:
| 事件 | 触发位置 | 用途 |
|---|---|---|
| `WorldUpdateEvent` | WorldLoader.Update (每帧) | 大多数功能的更新循环 |
| `EnterLevelEvent` | CL_EventManager.EnterLevel | 进入新关卡时重新初始化 |
| `PlayerLateUpdateEvent` | ENT_Player.LateUpdate Postfix | 获取玩家速度 |
| `GameStartEvent` | CL_GameManager.Start Postfix | 新游戏开始 |
| `FeatureToggleEvent` | FeatureButton.UpdateState | 功能开关切换 |
| `ToggleRouteEvent` | RouteStateHandler | 路线启用/禁用 |

### 3.4 Harmony 补丁策略 (`Patches/`)

所有补丁在 `Plugin.Awake()` 中通过 `_harmony.PatchAll()` 自动应用。主要用途:

1. **桥接游戏事件** → 发布 EventBus 事件 (WorldPatch, PlayerPatch, CL_GameManagerPatch)
2. **修改游戏逻辑** → 免费购买 (App_PerkPagePatch, ENV_VendingMachinePatch)
3. **注入渲染逻辑** → 自定义雾/扶手颜色 (FXManagerPatch)
4. **扩展输入检测** → 光标可见性判断包含自定义 UI (InputManagerPatch)
5. **加载资源** → AssetBundle 预制体预加载 (CL_AssetManagerPatch)

### 3.5 UI 系统 (`UI/`)

**完全动态创建** — 不依赖 Unity 场景中的 UI 对象:

- **FeaturesMenu**: ScreenSpaceOverlay Canvas (SortOrder=250), 包含分类容器和设置面板
- **MyCategory**: 可拖拽、可折叠的分类面板 (DOTween 动画)
- **FeatureButton**: Toggle 按钮 + SettingsButton (可选)
- **SettingsUI**: 底部弹出面板, 支持三种设置控件:
  - `BoolSwitch` → Toggle + DOTween 动画旋钮
  - `FloatSlider` → Slider + 实时数值显示
  - `ColorPicker` → TMP_InputField (#RRGGBBAA) + 实时颜色预览
- **PopupController**: 模态弹窗 (用于录制器的添加笔记/保存操作)

### 3.6 资源管理 (`Prefabs/PrefabDatabase.cs`)

- AssetBundle 文件位于 `Assets/histools`, 构建时复制到输出目录
- 单例模式, 延迟加载 + 缓存
- 使用 `"bundleName/assetName"` 格式的 key 查找资源
- 在 `CL_AssetManager.InitializeAssetManager` Postfix 中预加载所有已知资源

---

## 四、功能模块详细说明

### 4.1 路线系统 (RoutePlayer + RouteRecorder)

**最核心的功能模块**, 占据大量代码量。

**RoutePlayer (路线播放器)**:
- 根据当前关卡名从 `Routes/` 目录查找匹配的 JSON 路线文件
- 将本地坐标转换为世界绝对坐标 → LineRenderer 绘制路径
- 支持 Catmull-Rom 样条平滑 (`SmoothUtil.Path`)
- 实时进度追踪: 根据 player 位置找到最近路径点 → Gradient 颜色渐变 (已完成=背景色, 未完成=强调色)
- 跳跃标记: 球形标记物, 靠近时触发激活动画
- 信息标签: 路线名称/作者/描述 (TextMeshPro + LookAtPlayer)
- 笔记系统: 3D 文字标注
- 路线状态持久化: 每条路线的启用/禁用状态独立保存

**RouteRecorder (路线录制器)**:
- 录制玩家移动轨迹 (本地坐标系)
- 跳跃检测: 监听 Jump Button Down → 记录跳跃索引
- 实时预览: LineRenderer 显示已录制的路径 (绿→红渐变)
- 笔记功能: 鼠标中键打开弹窗 → 射线检测位置添加文字标注
- 播放/暂停: P 键切换; H 键撤销 (含传送); K 键停止并保存
- 自动停止: 靠近关卡出口时自动结束录制
- 历史记录可视化: 横向滚动的分段历史点
- 保存为 JSON: 包含路线信息 (uid/name/author/description/targetLevel)、点位、跳跃索引、笔记

**路线数据格式 (V2)**:
```json
[{
  "info": { "uid": "...", "name": "...", "author": "...", "targetLevel": "..." },
  "points": [{ "x": 0, "y": 0, "z": 0 }],
  "jumpIndices": [10, 25, 50],
  "notes": [{ "position": { "x": 0, "y": 0, "z": 0 }, "text": "..." }]
}]
```

### 4.2 BuffsDisplay (Buff 效果显示)

- 从 `player.curBuffs.currentBuffs` 读取当前激活的 buff
- 按 buff ID 分类: Grub (gooped), Injector (roided, 无 pilled), Pills (pilled, 无 roided), FoodBar (roided + pilled)
- 显示: 图标数量 (Icons 数组) + 堆叠数量 (xN) + 最短剩余时间 (mm:ss 或 ∞)
- 时间计算考虑 `buffTimeMult` buff 的加成效果

### 4.3 TimedPerkDisplay (限时技能倒计时)

- 扫描 `player.perks` 列表, 通过反射读取技能属性
- 双重检测策略:
  1. **优先**: 反射获取 `PerkModule_RemovalTimer` 模块的时间字段
  2. **回退**: 通过 buff 系统 (`buff.loseRate` / `buffTime`) 计算
- 显示: 技能名称 + 剩余时间 (mm:ss), 按剩余时间着色 (<10s 红, <30s 橙, 其他金黄)
- 最多 8 个槽位, 按剩余时间升序排列

### 4.4 SpeedrunStats (速跑统计)

- 显示当前关卡和上一关卡的: 已用时间 / 平均时间 / 最佳时间
- **预测功能**: 基于当前高度/速度预测总耗时 (高度比例外推)
- 颜色编码: 作弊=白, 在平均20%内=强调色, 其他=灰
- 关卡切换时记录 segment 到 JSON 历史文件 (保留最近 500 个)
- 异步计算最佳和中位数时间

### 4.5 ArtifactsInfo (Artifacts 信息统一入口)

- **模块化子类系统**: 主入口类通过反射扫描程序集发现所有 `ArtifactsInfo` 子类
- 每个子类有独立的 BoolSetting 开关控制是否启用
- 共享 UI 框架: 水平布局栏, 8 个最大槽位
- 当前子模块:
  - **GloveEnergyDisplay**: 手套充能值 + 充电状态 (反射读取私有字段)
  - **RebarDistanceDisplay**: 回收中/卡住钢筋的实时距离 (监听 Projectile_ReturnRebarPatch.OnSpawned 事件)

### 4.6 DebugInfo (调试信息 HUD)

可配置的信息项:
- 关卡名称 (`levelName`)
- 关卡镜像 (`levelFlipped`, scale.x < 0)
- 关卡内相对坐标 (`levelPos`)
- 世界绝对坐标 (`absolutePos`)
- 水平速度 (`speed`, 从 PlayerLateUpdateEvent 获取)
- 右键点击复制准星世界坐标 (JSON 格式) 到剪贴板

### 4.7 ShowItemInfo (物品生成概率)

- 进入关卡时遍历所有 `GameEntity` 组件
- 查找 `UT_SpawnChance` 组件显示生成概率百分比
- 文本镜像 (scaleX=-1) 以便从背后观看正确
- 使用 `UT_LookatPlayer` (游戏内置组件) 使文字始终朝向玩家

### 4.8 CustomFog & CustomHandhold

- **CustomFog**: 通过 `Shader.SetGlobalVector("_FOG", ...)` 在 FXRender Postfix 中每帧设置雾参数
- **CustomHandhold**: 通过 Transpiler 替换 `Color.Lerp` 为自定义方法, 在插值时使用自定义闪烁颜色

### 4.9 FreeBuying (免费购买)

三个独立的 bool 开关:
- **Items**: 自动售货机购买时 `free=true` (Prefix patch)
- **Refresh perks**: 技能刷新免费 (跳过消耗, Prefix return false)
- **Paid perks**: 购买技能卡片免费 (Prefix return false + DOTween 动画反馈)

---

## 五、配置与数据存储

### 5.1 BepInEx 配置 (`Config/HisTools.cfg`)
由 BepInEx 自动管理:
- `[Palette]` 组: Background/Accent/Enabled HTML 颜色, 路线标签颜色及透明度
- `[General]` 组: 功能菜单快捷键 (默认 Right Shift)

### 5.2 自定义 JSON 配置 (位于 `{BepInExRoot}/HisTools/`)

| 文件 | 内容 |
|---|---|
| `features_state.json` | 各功能的启用/禁用状态 |
| `routes_state.json` | 各路线的显示/隐藏状态 |
| `Settings/{FeatureName}.json` | 每个功能的设置项值 (key-value) |
| `Routes/*.json` | 路线数据文件 (用户录制或内置) |
| `SpeedrunStats/*.json` | 速跑运行历史记录 |
| `Builtin_histools_routes/` | 内置路线 (首次运行时从嵌入资源解压) |

### 5.3 JSON 容错机制 (`Utils/Files.cs`)

- `LoadOrRepairJson()`: 文件不存在返回空对象; JSON 损坏尝试修复 (补全括号); 失败则备份并重建
- `BackupFile()`: 带时间戳的 `.backup` 文件

---

## 六、开发约定与注意事项

### 6.1 代码风格
- **命名空间**: `HisTools` (根) / `HisTools.Features` / `HisTools.UI` / `HisTools.Patches` / `HisTools.Utils`
- **语言版本**: C# latest (.NET Standard 2.1), 广泛使用顶层语句、文件作用域类型、模式匹配
- **日志**: 统一使用 `Utils.Logger.Info/Debug/Warn/Error`
- **协程**: 通过 `CoroutineRunner.Instance.StartCoroutine()` 启动
- **动画**: 统一使用 DOTween (DG.Tweening)

### 6.2 添加新功能的标准流程

1. 在 `Features/` 下创建新类, 继承 `FeatureBase`
2. 在构造函数中调用 `base(name, description)` 并添加设置项
3. 重写 `OnEnable()` / `OnDisable()` 处理订阅/清理
4. 在 `Plugin.InitializeFeatures()` 中调用 `RegisterFeature(categoryPos, "CategoryName", new MyFeature())`
5. 如需 Harmony 补丁, 在 `Patches/` 下创建, 打上 `[HarmonyPatch]` (会自动被 PatchAll 应用)
6. 如需新的游戏事件, 在 `Events/Events.cs` 定义事件 struct, 在对应 Patch 中 Publish

### 6.3 反射使用
- `TimedPerkDisplay`: 读取 perk 模块的私有时间字段/属性
- `GloveEnergyDisplay`: 读取手套的充电模块私有字段
- `RebarDistanceDisplay`: 读取钢筋投射物的 `hasHitSurface` 字段
- `PlayerPatch`: 读取 `ENT_Player` 的 `camSpeed` 和 `vel` 私有字段

### 6.4 性能注意事项
- `RoutePlayer.OnWorldUpdate`: 使用滑动窗口 (±60 点) 查找最近路径点, 避免全量遍历
- `RouteRecorder`: 使用最小点距离阈值控制采样密度
- `BuffsDisplay` / `TimedPerkDisplay`: 提供 `ShowOnlyInPause` 选项减少非暂停时的开销
- `Files.GetRouteFilesByTargetLevel`: 协程逐文件异步读取, 每帧 yield 避免卡顿
- 所有 UI 更新仅在菜单可见或有订阅时执行

### 6.5 版本兼容性
- `ShowItemInfo`: `LookAtPlayer` 已重命名为 `UT_LookatPlayer` (游戏更新)
- `Projectile_ReturnRebarPatch`: `OnDestroy` 不再存在 (游戏更新移除), 改用 null 检测
- `RouteLoader`: 支持 V1→V2 旧格式自动转换, 转换前备份原文件
- `manifest.json` 当前为 0.2.0 (可能需同步更新至 0.3.0)

---

## 七、构建与发布

### 7.1 构建要求
- Visual Studio 2022 v17.5+
- .NET SDK (targeting netstandard2.1)
- `Libs/` 目录下的 DLL 需要从 White Knuckle 游戏安装目录提取 (gitignore 忽略)

### 7.2 构建输出
- Debug/Release → `bin/{Configuration}/netstandard2.1/`
- Post-build: `Assets/` 目录 (AssetBundle) xcopy 到输出目录
- 最终产物: `HisTools.dll` + `Assets/histools` 文件夹

### 7.3 Thunderstore 发布
- `manifest.json` 定义包元数据和依赖
- 打包结构应符合 Thunderstore mod 格式
- 当前依赖: `BepInEx-BepInExPack-5.4.2100`

---

## 八、快捷键一览

| 按键 | 上下文 | 功能 |
|---|---|---|
| `Right Shift` (可配置) | 全局 | 打开/关闭功能菜单 |
| `ESC` | 菜单可见时 | 关闭菜单 |
| `Middle Mouse` | 路线标签 | 切换路线启用/禁用 |
| `Middle Mouse` | 录制中 | 添加笔记 (射线指向位置) |
| `P` | 录制中 | 播放/暂停录制 |
| `H` | 录制中 | 撤销最后一段 (含传送) |
| `K` | 录制中 | 停止录制并保存 |
| `Middle Mouse` | DebugInfo 开启时 | 复制准星坐标到剪贴板 |

---

*本文档由 agent 自动生成, 基于 HisTools 项目 v0.3.0 代码分析*
