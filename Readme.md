# Composite Toggle

Unity UI 多状态切换插件，支持样式复用与可中断的属性 Tween。开发工程使用 **Unity 6000.0.44f1**。

## 工程结构

```text
Assets/
  Package/                 # 可独立分发的插件本体
    Runtime/               # 运行时代码
    Editor/                # Inspector 与样式编辑器
    Tests/EditMode/         # 编辑模式回归测试
    Tests/PlayMode/         # 运行模式回归测试
    Documentation~/        # 插件文档（Unity 不导入此目录）
    package.json
    LICENSE.md
  Scenes/                  # Demo 场景及光照资源
  Scripts/                 # Demo 脚本
  Content/Demo/            # Demo 样式资源
  Editor/                  # 工程专用导出工具与 Demo 构建工具
```

插件不依赖工程层 Demo；保留原有 `Mobcast.Coffee.Toggles` 命名空间与资产 GUID。

## 运行示例

打开 `Assets/Scenes/Demo.unity` 并进入 Play Mode。Tween 示例位于滚动页顶部，原有示例保留。`Tools/Composite Toggle/Add Tween Examples to Demo` 可为已打开的 Demo 补充 Tween 示例；已有示例不会被覆盖。

## 安装与分发

- **开发本仓库**：直接用 Unity 打开仓库根目录，插件已位于 `Assets/Package`，不要再通过 Package Manager 重复安装。
- **其他工程通过本地 UPM 安装**：在 Package Manager 使用 Add package from disk，选择本仓库 `Assets/Package/package.json`。接收工程需使用兼容的 Unity 6 与 uGUI 2.0；本地安装依赖源目录持续存在。
- **仅导出插件**：菜单 `Export Package/CompositeToggle.unitypackage`，输出根目录 `CompositeToggle.unitypackage`。
- **插件与示例一起导出**：菜单 `Export Package/CompositeToggle with Demo`，输出根目录 `CompositeToggle-WithDemo.unitypackage`，包含 Demo 场景、资源、脚本及构建工具。

导出只在显式调用菜单时执行，不再在编译或 Domain Reload 时自动覆盖已有包。现有根目录 `.unitypackage` 不随源码迁移自动更新。UPM 安装不包含工程层 Demo；需要示例时打开本开发工程或使用包含 Demo 的导出包。`Documentation~` 不属于 Unity 导入资产，文档请从源码目录读取。

## 验证与文档

在 Unity Test Runner 分别运行 `CompositeToggle.Tests.EditMode` 与 `CompositeToggle.Tests.PlayMode`。在其他工程以 UPM 形式使用时，如需运行包内测试，将 `com.coffee.composite-toggle` 加入工程 manifest 的 `testables` 并安装 Unity Test Framework。

Tween 使用说明见 `Assets/Package/Documentation~/TweenSupport.md`。插件许可证位于 `Assets/Package/LICENSE.md`。
