# CompositeToggle Tween 支持

## 配置入口

1. 在 CompositeToggle 原有属性列表中添加需要切换的属性，填写 Boolean / Index 各状态的目标值。
2. 展开 Inspector 的 **Tween 属性过渡**。
3. 在对应属性上勾选 **启用 Tween**，设置时长、延迟、缓动和时间来源。
4. 如需退出动画，在**发起隐藏操作的控制器**上勾选 **等待退出动画后隐藏**。

Tween 与延迟隐藏均默认关闭，旧资源不需要迁移。配置以每条属性绑定为单位，而不是每个状态一份。

支持公开可读写的 `float`、`Vector2`、`Vector3`、`Vector4`、`Color` 属性，例如 CanvasGroup.alpha、RectTransform.anchoredPosition / sizeDelta、Transform.localScale、Graphic.color。

普通方法、事件、bool、整数、字符串、对象引用等继续立即执行。Count / Flag 保持原来的行为：不应用属性状态，因此没有属性 Tween。旋转向量按各分量线性插值，不包含最短角路径或四元数旋转。

## Demo 场景示例

打开 `Assets/Scenes/Demo.unity` 并进入 PlayMode。滚动页顶部新增 **Composite Toggle / Tween**，原有示例保留在其下方。

| 示例 | 操作 | 展示能力 |
|---|---|---|
| 01 Position + interruption | 连续点击 A / B / C | 三状态位移；中途从当前值改向 |
| 02 Scale + rotation | SCALE + ROTATE | 同一控制器同时驱动缩放与旋转，不同时长 |
| 03 Color + opacity | BLEND COLOR + OPACITY | Image.color 与 CanvasGroup.alpha 插值 |
| 04 Delayed resize | RESIZE AFTER DELAY | 0.25 秒延迟后改变 sizeDelta |
| 05 Custom overshoot | POP / SETTLE | 自定义曲线超调后准确回到目标缩放 |
| 06 Fade out, then hide | HIDE AFTER FADE / SHOW / REOPEN | 退出完成才隐藏；中途重新打开会撤销隐藏 |

场景对象位于 `Canvas/Scroll View/Viewport/Vertical Layout/Tween Playground`。前五组的控制器在各卡片的 `Stage/Target` 上；第六组在卡片上放置显隐控制器，同步目标上的透明度 / 缩放控制器，因此隐藏后按钮仍可操作。

所有属性状态、Tween 配置与按钮事件都序列化在场景中，没有额外运行时动画脚本。可直接选中控制器，在 Inspector 调整参数。`Tools → Composite Toggle → Add Tween Examples to Demo` 提供编辑器生成入口：已有示例时仅选中，不覆盖修改；生成操作支持 Undo，需保存场景后保留。

## 运行语义

- 控制器状态、同步传播、动作和 `onValueChanged` 仍立即生效，不等待动画；本次没有增加控制器级动画完成事件。
- 起点读取组件的实际当前值，终点取自 ParameterList。中间值只写组件，不写回状态配置。
- 快速切换会取消旧动画，从当前显示值向新目标继续过渡，不会先跳到旧终点。不保证速度连续。
- 同一目标组件 / setter 只保留一个 Tween；不同控制器也按最后一次写入处理。相同 owner 重复应用相同终点不会重新计时。
- `Property.Invoke(target, index)` 与 `Style.LoadStyle()` 保持立即应用，并取消同属性的旧动画。
- 默认 QuadOut、0.3 秒、无延迟、使用未缩放时间。可选 Linear / QuadIn / QuadOut / QuadInOut / Custom 曲线。
- 自定义曲线允许超调；结束时强制落到准确终值。时长 <= 0 或非有限值时立即应用（不等待 delay）。
- 日常编辑模式不播放动画。首次初始化和重新启用时，已开启 Tween 的可动画属性直接应用当前状态终值；未启用的属性保留原有启动 / 重启行为。
- 禁用或销毁控制器会取消其动画，不触发动画完成回调。重新启用时按当前状态恢复；从完全 inactive 重新显示不是自动淡入。

## 延迟隐藏

开启后，原有激活列表、反向激活列表和扩展显隐列表发起的隐藏请求会在 LateUpdate 处理，等待目标 GameObject 及其后代的属性 Tween 结束，再执行 `SetActive(false)`。

- 同帧稍后由子控制器 / 同步控制器创建的 Tween 也能参与等待。
- 延迟与暂停中的 Tween 也持有显示锁。使用 scaled time 且 timeScale 为 0 时，将继续等待。
- 新的控制器显示请求会撤销旧隐藏请求，避免退出中重新打开后被过期请求关闭。
- 没有相关 Tween 时在当前帧 LateUpdate 隐藏，不会永久保留。
- 这不是对全项目 `GameObject.SetActive` 的拦截；外部直接关闭对象、父物体关闭、其它控制器的立即隐藏、Animator / Layout 等外部写入不受此机制协调。
- 延迟隐藏只延迟激活状态，不自动关闭交互。退出时如需禁止点击，可给 `CanvasGroup.interactable` / `blocksRaycasts` 配置离散状态。
- 如果希望反复淡入淡出而不触发重新启用时的终值恢复，可保持对象 active，仅切换 Alpha 和交互状态。

## 代码配置

```csharp
// 使用已经配置好状态终值的属性条目。
Property property = toggle.toggleProperties[propertyIndex];
if (property.supportsTween)
{
    property.tween.enabled = true;
    property.tween.duration = 0.25f;
    property.tween.delay = 0;
    property.tween.ease = ToggleTweenEase.QuadOut;
    property.tween.useUnscaledTime = true;
}
toggle.deferDeactivation = true; // 可选：由这个控制器发起的隐藏等待退出动画。
toggle.indexValue = nextState;
```

## 验证

项目已安装 Unity Test Framework `1.5.1`（由当前 Unity 6000.0.44f1 的 Package Manager 解析），并接入标准 NUnit / UnityTest 测试程序集。测试依赖与正式运行时代码隔离。

打开 **Window → General → Test Runner**：

- **EditMode → CompositeToggle.Tests.EditMode**：36 项独立 NUnit 测试。使用独立 Preview Scene 和确定性时间步进，覆盖插值、中断、去重、时钟、显示锁、继承属性、序列化、Style、重入及生命周期。每项用例独立 Setup / TearDown，断言失败不会跳过清理。
- **PlayMode → CompositeToggle.Tests.PlayMode**：7 项 UnityTest 协程测试，覆盖真实 Update / LateUpdate、初始化、事件时机、scaled / unscaled 时间、快速改向、退出隐藏、重新打开以及禁用 / 启用。UnityTearDown 恢复 timeScale / runInBackground 并销毁临时对象。

旧的手动测试菜单与异步冒烟入口已经由以上标准用例替代，不再作为第二套测试实现维护。

使用当前项目的 CLI：

```powershell
npx --yes uloop-cli@2.2.0 run-tests --test-mode EditMode --filter-type assembly --filter-value CompositeToggle.Tests.EditMode
npx --yes uloop-cli@2.2.0 run-tests --test-mode PlayMode --filter-type assembly --filter-value CompositeToggle.Tests.PlayMode
```

编辑器若有未保存的 Scene / Prefab 改动，CLI 会安全拒绝执行，不会自动保存或丢弃。可在处理好改动后运行，或在隔离的临时 Unity 项目中使用同一份源码做批处理验证。

标准 Unity 命令行同样支持执行（将路径替换为实际路径）：

```powershell
Unity.exe -batchmode -nographics -projectPath "<project>" -runTests -testPlatform EditMode -assemblyNames CompositeToggle.Tests.EditMode -testResults "<output>/EditMode.xml" -logFile "<output>/EditMode.log"
Unity.exe -batchmode -nographics -projectPath "<project>" -runTests -testPlatform PlayMode -assemblyNames CompositeToggle.Tests.PlayMode -testResults "<output>/PlayMode.xml" -logFile "<output>/PlayMode.log"
```

不要同时对同一个项目路径启动两个 Unity Editor 进程，也不要为测试添加会导致提前退出的 `-quit`。

## 范围与限制

未引入第三方 Tween 库。运行时缓存反射访问器，但每帧仍存在值装箱和反射调用，未宣称零 GC，也未做大批量 UI 性能基准。没有序列动画、循环、路径或编辑模式动画预览。

本次验证范围为 Unity Editor。IL2CPP / 高裁剪级别 Player 构建尚未验证；反射 getter / setter 的保留策略需要结合项目已有的属性烘焙与链接配置验证，不能仅凭 Editor 测试推断 Player 兼容性。
