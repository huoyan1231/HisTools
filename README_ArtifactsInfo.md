# ArtifactsInfo - 钢筋回收距离显示功能

本功能为 `HisTools` 插件新增的子功能，用于在场景中显示正在回收（Returning）状态的钢筋（`Projectile_ReturnRebar`）与玩家之间的实时距离。

## 功能说明

1. **自动挂载**：当功能开启时，系统会自动检测场景中的钢筋实例并挂载 `RebarDistanceDisplay` 脚本。
2. **实时距离**：仅当钢筋处于 `returning == true` 状态时，才会显示 UI 标签。
3. **性能模式**：提供两种扫描方式，可在功能设置中切换：
   - **事件驱动（推荐）**：通过 Harmony Patch 监听钢筋的生成与销毁，几乎零额外开销。
   - **Update 轮询**：每帧扫描场景中的所有钢筋实例，适用于特殊情况下的兼容。

## 配置与路径

- **扫描方式切换**：在 `ArtifactsInfo` 的设置面板中切换 `Event-driven scanning`。
- **UI 预设路径**：预设应位于 `histools/RebarDistanceUI`。该预设需包含：
  - `Canvas` (World Space)
  - `TextMeshProUGUI` (用于显示文字)
  - `UT_LookatPlayer` (用于使 UI 始终朝向玩家)
- **排序层**：UI 默认在世界空间渲染，建议在预设中设置较高的 `Sorting Order` 以确保不被遮挡。

## 性能建议

- 推荐始终开启 **事件驱动** 模式。
- 在包含大量钢筋的复杂场景中，本功能通过 `Dictionary` 缓存和按需激活 UI 的方式最大限度降低了 `GC.Alloc` 和 `DrawCall`。

## 开发者说明

- 核心逻辑位于 `HisTools.Features.ArtifactsInfo`。
- 补丁代码位于 `HisTools.Patches.Projectile_ReturnRebarPatch`。
- UI 显示逻辑位于 `HisTools.Features.RebarDistanceDisplay`。
