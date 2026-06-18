# Godot 4 游戏开发实战指南

> 面向 AI Agent 和开发者的方法论文档，覆盖资源获取、贴图美化、交互优化、性能调优。

---

## 目录

1. [资源获取](#1-资源获取)
2. [贴图与像素美术](#2-贴图与像素美术)
3. [Shader 特效](#3-shader-特效)
4. [UI 美化](#4-ui-美化)
5. [打击感与 Juice](#5-打击感与-juice)
6. [交互系统设计](#6-交互系统设计)
7. [状态机模式](#7-状态机模式)
8. [输入处理](#8-输入处理)
9. [性能优化](#9-性能优化)
10. [推荐资源](#10-推荐资源)

---

## 1. 资源获取

### 1.1 免费素材源（CC0 优先）

| 来源 | 网址 | 许可证 | 推荐度 |
|------|------|--------|--------|
| **Kenney** | https://kenney.nl/assets | CC0（公共领域） | ⭐⭐⭐⭐⭐ |
| **itch.io** | https://itch.io/game-assets/tag-pixel-art/free | 各创作者自定 | ⭐⭐⭐⭐ |
| **OpenGameArt** | https://opengameart.org | 混合（CC0/CC-BY/CC-BY-SA） | ⭐⭐⭐ |
| **CraftPix** | https://craftpix.net/freebies/ | 免费商用 | ⭐⭐⭐ |
| **GameDevMarket** | https://www.gamedevmarket.net/assets/?free=true | 各自定 | ⭐⭐⭐ |

### 1.2 音频资源

| 来源 | 网址 | 许可证 | 说明 |
|------|------|--------|------|
| **Kenney Audio** | https://kenney.nl/assets?q=audio | CC0 | 与 Kenney 视觉素材风格匹配 |
| **Freesound** | https://freesound.org | 混合 CC | 50 万+音效，需筛选 CC0 |
| **Pixabay Audio** | https://pixabay.com/sound-effects/ | 免费商用 | 质量参差不齐 |
| **Mixkit** | https://mixkit.co/free-sound-effects/ | 免费商用 | 无需注册 |

### 1.3 许可证速查

| 许可证 | 商用 | 需署名 | 衍生作品需共享 | 适用场景 |
|--------|------|--------|----------------|----------|
| CC0 | ✅ | ❌ | ❌ | 商业游戏（最安全） |
| CC-BY | ✅ | ✅ | ❌ | 需署名的商业游戏 |
| CC-BY-SA | ✅ | ✅ | ✅ | 开源游戏 |
| CC-BY-NC | ❌ | ✅ | ❌ | 仅非商业 |

### 1.4 资源评估清单

下载前检查：
1. **许可证兼容性** — 能否商用？能否修改？
2. **分辨率匹配** — 是否匹配目标像素尺寸（16x16, 32x32）？
3. **风格一致性** — 是否与现有美术风格匹配？
4. **完整性** — 包是否覆盖所需内容（角色+瓦片+UI）？
5. **格式** — PNG 透明？精灵表还是单帧？

### 1.5 目录结构建议

```
assets/
  sprites/
    downloaded/        # 第三方素材包
    game/              # 游戏专属素材
      lobby/
      player/
      ui/
      enemies/
    characters/        # 角色精灵
    generated/         # 程序生成素材
  audio/
    sfx/               # 音效
    music/             # 音乐
  tilesets/            # 瓦片集
  fonts/               # 字体
  shaders/             # Shader 文件
  themes/              # UI 主题
```

命名规范：
- `spr_` — 精灵（spr_player_idle_01.png）
- `tile_` — 瓦片（tile_floor_stone_01.png）
- `sfx_` — 音效（sfx_explosion_large.wav）
- `mus_` — 音乐（mus_lobby_theme.ogg）
- `ui_` — UI 元素（ui_button_blue.png）

---

## 2. 贴图与像素美术

### 2.1 Godot 像素美术关键设置

**项目全局设置（必须首先配置）：**

| 设置路径 | 推荐值 | 原因 |
|----------|--------|------|
| `rendering/textures/canvas_textures/default_texture_filter` | `0` (Nearest) | **最关键** — 防止所有精灵模糊 |
| `rendering/2d/snap/snap_2d_transforms_to_pixel` | 开启 | 防止像素抖动 |
| `display/window/stretch/mode` | `"viewport"` | 渲染到视口再缩放 |
| `display/window/stretch/scale_mode` | `"integer"` | 整数缩放，无子像素模糊 |

**单个纹理导入设置：**

| 设置 | 值 | 说明 |
|------|-----|------|
| Compress Mode | Lossless | 像素美术绝不用 Lossy |
| Mipmaps > Generate | Off | Mipmaps 导致模糊 |
| Fix Alpha Border | Off | 可能裁剪边缘像素 |
| Filter | Nearest | 覆盖全局默认 |

### 2.2 精灵表与动画

**AnimatedSprite2D 最佳实践：**

| 属性 | 推荐值 | 说明 |
|------|--------|------|
| Speed (FPS) | 6-12（待机/行走），12-24（攻击） | 像素动画低帧率更好看 |
| Loop | 待机/行走=开，攻击/受伤=关 | 单次动画不应循环 |
| Autoplay | 设为默认动画 | 省代码 |

**C# 动画控制：**
```csharp
// 翻转方向
if (velocity.X > 0) anim.FlipH = true;
else if (velocity.X < 0) anim.FlipH = false;

// 切换动画
if (velocity.Length() > 0) anim.Play("walk");
else anim.Play("idle");
```

**像素美术动画工具：**

| 工具 | 价格 | 说明 |
|------|------|------|
| **Aseprite** | ~$20 | 行业标准，最佳动画支持 |
| **Pixelorama** | 免费 | Godot 原生，MIT 许可 |
| **Piskel** | 免费 | 浏览器端，适合快速原型 |

### 2.3 TileMap 使用

Godot 4.3+ 使用 `TileMapLayer` 替代 `TileMap`。

**设置流程：**
1. 创建 TileSet 资源，设置瓦片尺寸（如 32x32）
2. 拖入瓦片集图片，Godot 自动切割
3. 配置物理层（碰撞）、自定义数据层

**图层建议：**

| 层 | 用途 |
|----|------|
| Layer 0 | 地面/地形 |
| Layer 1 | 装饰（树、石头） |
| Layer 2 | 前景（树冠、屋顶） |
| Physics Layer | 特定瓦片的碰撞 |

**C# 瓦片操作：**
```csharp
// 获取世界坐标处的瓦片
Vector2I tileCoords = tileMap.LocalToMap(GetGlobalMousePosition());

// 设置瓦片
tileMap.SetCell(tileCoords, sourceId: 0, atlasCoords: new Vector2I(2, 3));

// 获取自定义数据
string tileType = tileMap.GetCellTileData(tileCoords).GetCustomData("tile_type");
```

---

## 3. Shader 特效

### 3.1 受击闪光 Shader

最简单实用的起步 Shader：

```gdshader
shader_type canvas_item;

uniform vec4 flash_color : source_color = vec4(1.0, 0.0, 0.0, 1.0);
uniform float flash_intensity : hint_range(0.0, 1.0) = 0.0;

void fragment() {
    vec4 tex_color = texture(TEXTURE, UV);
    COLOR = mix(tex_color, flash_color, flash_intensity * tex_color.a);
}
```

**C# 触发：**
```csharp
public void FlashDamage() {
    var mat = Sprite.Material as ShaderMaterial;
    mat.SetShaderParameter("flash_intensity", 1.0f);

    var tween = CreateTween();
    tween.TweenProperty(mat, "shader_parameter/flash_intensity", 0.0, 0.3);
}
```

### 3.2 像素描边 Shader

```gdshader
shader_type canvas_item;

uniform vec4 outline_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform int outline_width : hint_range(1, 4) = 1;

void fragment() {
    vec4 tex_color = texture(TEXTURE, UV);
    vec2 pixel_size = 1.0 / vec2(textureSize(TEXTURE, 0));

    if (tex_color.a < 0.1) {
        for (int x = -outline_width; x <= outline_width; x++) {
            for (int y = -outline_width; y <= outline_width; y++) {
                if (x == 0 && y == 0) continue;
                vec2 offset = vec2(float(x), float(y)) * pixel_size;
                vec4 neighbor = texture(TEXTURE, UV + offset);
                if (neighbor.a > 0.1) {
                    COLOR = outline_color;
                    return;
                }
            }
        }
        discard;
    } else {
        COLOR = tex_color;
    }
}
```

### 3.3 调色板替换 Shader

用于状态效果（中毒=绿色，燃烧=红色）或玩家颜色区分：

```gdshader
shader_type canvas_item;

uniform sampler2D palette : filter_nearest, repeat_disable;
uniform float palette_index : hint_range(0.0, 1.0) = 0.0;

void fragment() {
    vec4 original = texture(TEXTURE, UV);
    float luminance = dot(original.rgb, vec3(0.299, 0.587, 0.114));
    vec4 remapped = texture(palette, vec2(luminance, palette_index));
    COLOR = vec4(remapped.rgb, original.a);
}
```

### 3.4 溶解 Shader

用于死亡动画、传送效果：

```gdshader
shader_type canvas_item;

uniform sampler2D noise_texture : filter_nearest, repeat_disable;
uniform float dissolve_amount : hint_range(0.0, 1.0) = 0.0;
uniform vec4 dissolve_color : source_color = vec4(1.0, 0.5, 0.0, 1.0);

void fragment() {
    vec4 tex_color = texture(TEXTURE, UV);
    float noise = texture(noise_texture, UV).r;

    if (noise < dissolve_amount) {
        discard;
    } else if (noise < dissolve_amount + 0.05) {
        COLOR = dissolve_color;
    } else {
        COLOR = tex_color;
    }
}
```

---

## 4. UI 美化

### 4.1 Theme 资源

1. 创建 `Theme` 资源（`pixel_theme.tres`）
2. 用 `StyleBoxTexture` 实现像素风 9-slice 边框
3. 为每种控件类型配置样式

**StyleBoxTexture 配置：**

| 控件类型 | 纹理 | 边距 (px) |
|----------|------|-----------|
| Panel | panel.png | 4, 4, 4, 4 |
| Button Normal | btn_normal.png | 4, 4, 4, 4 |
| Button Hover | btn_hover.png | 4, 4, 4, 4 |
| Button Pressed | btn_pressed.png | 4, 4, 4, 4 |
| WindowPanel | window.png | 8, 8, 8, 8 |

### 4.2 NinePatchRect

像素美术 UI 的关键：拉伸模式设为 **Tile** 而非 Scale（Scale 会模糊像素）。

### 4.3 UI 布局要点

- 所有控件使用**整数尺寸**（像素倍数）
- 设置 `CustomMinimumSize` 确保一致的点击区域
- 使用 `VBoxContainer` / `HBoxContainer` 自动布局
- `Separation` 设为像素对齐值（4, 8）

```csharp
var btn = new Button();
btn.Text = "开始游戏";
btn.CustomMinimumSize = new Vector2(64, 16);
btn.AddThemeFontSizeOverride("font_size", 8);
```

---

## 5. 打击感与 Juice

### 5.1 屏幕震动

```csharp
// CameraShake.cs — 挂在 Camera2D 上
private float _shakeIntensity = 0f;
private float _shakeDuration = 0f;
private float _shakeTimer = 0f;

public void Shake(float intensity, float duration) {
    _shakeIntensity = intensity;
    _shakeDuration = duration;
    _shakeTimer = 0f;
}

public override void _Process(double delta) {
    if (_shakeTimer < _shakeDuration) {
        _shakeTimer += (float)delta;
        float progress = _shakeTimer / _shakeDuration;
        float currentIntensity = _shakeIntensity * (1f - progress);
        float angle = GD.Randf() * Mathf.Pi * 2f;
        Offset = new Vector2(
            Mathf.Cos(angle) * currentIntensity,
            Mathf.Sin(angle) * currentIntensity
        );
    } else {
        Offset = Vector2.Zero;
    }
}
```

**震动强度参考：**

| 事件 | 强度 | 持续时间 |
|------|------|----------|
| 子弹命中 | 2-3 | 0.08-0.12s |
| 爆炸（火箭、新星） | 6-10 | 0.2-0.35s |
| Boss 重击 | 8-12 | 0.3-0.5s |
| 玩家死亡 | 10-15 | 0.5-0.8s |

### 5.2 停顿（Hitstop）

最高性价比的打击感技巧：

```csharp
public static async void HitStop(Node tree, float duration, float timeScale = 0.05f) {
    Engine.TimeScale = timeScale;
    await tree.ToSignal(tree.GetTree().CreateTimer(
        duration * timeScale, true, false, true
    ), "timeout");
    Engine.TimeScale = 1.0f;
}
```

**停顿力度参考：**
- 轻武器（手枪）：不停顿
- 中武器（步枪、技能）：30ms, 0.1x
- 重武器（霰弹、近战、超能）：50-80ms, 0.05x
- Boss 阶段切换：150-200ms, 0.1x

### 5.3 Tween 缓动选择

| 缓动类型 | 效果 | 最佳用途 |
|----------|------|----------|
| `TRANS_BACK` | 过冲回弹 | **动作游戏"弹出"效果** |
| `TRANS_BOUNCE` | 弹跳 | 物品落地、UI 弹入 |
| `TRANS_ELASTIC` | 弹性晃动 | 活泼的 UI 效果 |
| `TRANS_CIRC` | 自然减速 | 子弹粒子散射 |
| `TRANS_EXPO` | 指数加速 | Boss 冲刺蓄力 |
| `EASE_OUT` | 快开始慢结束 | **大多数动作效果的默认选择** |

### 5.4 伤害数字改进

```csharp
var tween = GetTree().CreateTween();
tween.TweenProperty(label, "position", label.Position + new Vector2(0, -50), 0.6f);
tween.SetTrans(Tween.TransitionType.Back);  // 过冲
tween.SetEase(Tween.EaseType.Out);           // 快开始慢结束
tween.Parallel().TweenProperty(label, "modulate:a", 0.0f, 0.6f);
```

### 5.5 受击反馈层级

| 反馈 | 视觉 | 音效 | 屏幕效果 |
|------|------|------|----------|
| 玩家受伤 | 精灵红色闪光 | 受击音效 | 轻微震动 |
| 击杀敌人 | 爆炸粒子 | 击杀音效 | 30ms 停顿 |
| 拾取物品 | 浮动文字+缩放 | 拾取音效 | — |
| 升级 | 全屏覆盖 | 升级音效 | 慢动作 0.2x, 0.3s |
| Boss 阶段切换 | Boss 硬直动画 | 阶段音效 | 200ms 停顿+震动 |

---

## 6. 交互系统设计

### 6.1 背包系统

**核心原则：物品数据是 Resource，不是 Node。**

```csharp
// 物品数据基类
[GlobalClass]
public partial class ItemData : Resource {
    [Export] public string Id;
    [Export] public string DisplayName;
    [Export] public string Description;
    [Export] public Texture2D Icon;
    [Export] public int MaxStackSize = 1;
}

// 背包槽位
public class InventorySlot {
    public ItemData Item;
    public int Count;
    public bool IsEmpty => Item == null || Count <= 0;
}
```

**架构要点：**
- 背包作为玩家的子节点（组件模式），非全局单例
- 多人游戏中每个玩家独立背包
- 通过信号总线解耦 UI 和数据

### 6.2 拾取系统

两种交互模式：
- **IInteractable**（大厅设施）— 玩家主动，按 E 触发
- **IPickable**（经验球、记忆水晶）— 距离触发，自动拾取

**磁吸拾取模式（ExperienceOrb）：**
1. 使用 `GetTree().GetNodesInGroup("player")` 获取玩家引用
2. 两阶段收集：进入范围后磁吸，足够近后拾取
3. 优化：用 Area2D 的 `BodyEntered` 信号替代每帧距离检测

### 6.3 对话系统

数据模型用 Resource：

```csharp
[GlobalClass]
public partial class DialogueLine : Resource {
    [Export] public string SpeakerId;
    [Export] public string Text;
    [Export] public Texture2D Portrait;
    [Export] public DialogueOption[] Options;
}
```

### 6.4 UI/UX 要点

**输入阻断：** UI 打开时阻止玩家移动/射击
```csharp
// Player._PhysicsProcess
if (GameManager.Instance.IsUIOpen) return;
```

**鼠标模式切换：**
```csharp
// 打开背包
Input.MouseMode = Input.MouseModeEnum.Visible;
GetTree().Paused = true;

// 关闭背包
Input.MouseMode = Input.MouseModeEnum.Captured;
GetTree().Paused = false;
```

---

## 7. 状态机模式

### 7.1 为什么需要状态机

当前代码用布尔值和计时器管理状态，存在：
- 状态转换分散在 `_Process` 各处
- Enter/Exit 逻辑容易遗漏
- 调试困难（"哪些布尔值的组合？"）
- 复杂度指数增长（2^n 组合）

### 7.2 通用状态机实现

```csharp
public abstract partial class State : Node {
    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update(double delta) { }
    public virtual void PhysicsUpdate(double delta) { }
    public virtual void HandleInput(InputEvent @event) { }
}

public partial class StateMachine : Node {
    private State _currentState;
    private Dictionary<string, State> _states = new();

    public void TransitionTo(string stateName) {
        _currentState?.Exit();
        _currentState = _states[stateName];
        _currentState.Enter();
    }
}
```

### 7.3 敌人 AI 状态机

```
StateMachine
  Idle       — 等待
  Chase      — 追踪玩家
  Attack     — 攻击范围内
  Stunned    — 眩晕
  Dead       — 死亡
```

### 7.4 布尔值 vs 状态机

| 方面 | 布尔值（当前） | 状态机 |
|------|----------------|--------|
| 状态转换 | 分散在 _Process | 集中在 TransitionTo |
| Enter/Exit | 手动，易遗漏 | 保证执行 |
| 调试 | "哪些布尔组合？" | "哪个状态活跃？" |
| 复杂度 | 指数增长 | 线性增长 |
| 多人同步 | 同步 N 个布尔值 | 同步一个状态名 |

---

## 8. 输入处理

### 8.1 输入处理优先级

| 方法 | 触发时机 | 最佳用途 |
|------|----------|----------|
| `_Input()` | 任何处理器之前 | 全局热键（调试、截图） |
| `_UnhandledInput()` | UI 节点之后 | 游戏状态切换（暂停、装备） |
| `_PhysicsProcess()` | 每物理帧 | 移动、射击等持续操作 |

### 8.2 输入缓冲

动作游戏必备，防止动画期间丢失输入：

```csharp
private float _inputBufferTimer;
private bool _attackBuffered;

public override void _UnhandledInput(InputEvent @event) {
    if (@event.IsActionPressed("shoot")) {
        if (CanShoot()) Shoot();
        else { _attackBuffered = true; _inputBufferTimer = 0.15f; }
    }
}
```

### 8.3 关键原则

- **永远用 Input Action**，不用 `Input.IsKeyPressed(Key.A)` — 支持重映射和手柄
- 用 `Input.GetVector()` 做移动
- 游戏输入放 `_UnhandledInput`，全局热键放 `_Input`

---

## 9. 性能优化

### 9.1 对象池

避免频繁 `QueueFree()` 和 `new`：

```csharp
public class NodePool {
    private readonly Queue<Node> _pool = new();

    public Node Get() {
        Node node = _pool.Count > 0 ? _pool.Dequeue() : _scene.Instantiate<Node>();
        node.ProcessMode = Node.ProcessModeEnum.Inherit;
        node.Visible = true;
        return node;
    }

    public void Return(Node node) {
        node.ProcessMode = Node.ProcessModeEnum.Disabled;
        node.Visible = false;
        _pool.Enqueue(node);
    }
}
```

### 9.2 GPUParticles2D 替代手动粒子

当前代码用 `ColorRect` + Tween 做粒子效果，应该用 `GPUParticles2D`：

| 属性 | 爆炸效果 | 环境粒子 |
|------|----------|----------|
| Amount | 4-8 | 3 |
| Lifetime | 0.1-0.3s | 3.0s |
| One Shot | true | false |
| Explosiveness | 1.0 | 0 |

### 9.3 渲染优化

- **批处理：** 相同材质/纹理的精灵自动合批
- **可见性裁剪：** 用 `VisibilityNotifier2D` 禁用屏幕外敌人的逻辑
- **碰撞层效率：** 只检测需要的层

```csharp
// 禁用屏幕外敌人处理
var notifier = new VisibilityNotifier2D();
notifier.ScreenEntered += () => SetProcess(true);
notifier.ScreenExited += () => SetProcess(false);
AddChild(notifier);
```

### 9.4 内存管理

- Tween 不可复用，创建前先 Kill 旧的
- `Callable.From(() => {...})` 分配闭包，热路径缓存 Callable
- 音乐用 OGG（小文件，流式），音效用 WAV（快速加载）

### 9.5 性能分析清单

| 工具 | 用途 |
|------|------|
| `--verbose` 标志 | 帧时间、节点数 |
| `Performance.get_monitor()` | FPS、渲染调用、物理对象、内存 |
| 编辑器 Profiler | 逐帧 CPU 分析 |
| Monitors 标签 | 对象数、绘制调用、显存 |

---

## 10. 推荐资源

### 学习资源

| 资源 | 类型 | 说明 |
|------|------|------|
| **GDQuest** | 网站/YouTube | 专业 Godot 架构、状态机、RPG 系统 |
| **HeartBeast** | YouTube | Godot 4 动作 RPG 教程系列 |
| **Brackeys** | YouTube | 入门级 Godot 4 教程 |
| **Godot 官方文档** | 网站 | Resource、Signal、Input 文档 |
| **r/godot** | Reddit | 社区问答和架构讨论 |
| **Godot Forum** | 论坛 | 官方社区 |

### 工具资源

| 资源 | 网址 | 说明 |
|------|------|------|
| **Lospec 调色板** | https://lospec.com/palette-list | 数百个像素美术调色板 |
| **Godot Shaders** | https://godotshaders.com | 社区 Shader 库 |
| **Kenney Asset Creator** | https://kenney.nl/assets | 一站式素材获取 |

---

## 附录：优先级实施路线图

| 优先级 | 特性 | 工作量 | 影响 |
|--------|------|--------|------|
| 1 | 战斗屏幕震动 | 低 | 极高 |
| 2 | 重击停顿效果 | 低 | 极高 |
| 3 | 精灵受击闪光 | 低 | 高 |
| 4 | Tween 缓动升级 | 低 | 高 |
| 5 | 相机平滑+拖拽 | 低 | 高 |
| 6 | 本地玩家伤害数字 | 低 | 高 |
| 7 | 场景切换淡入淡出 | 中 | 高 |
| 8 | 房间切换动画 | 中 | 中 |
| 9 | 效果对象池化 | 中 | 中（性能） |
| 10 | GPUParticles2D 替换 | 中 | 中（性能+视觉） |
| 11 | 相机前瞻 | 低 | 中 |
| 12 | 挤压拉伸效果 | 中 | 中 |
| 13 | HUD 动画打磨 | 中 | 中 |
| 14 | Boss 阶段切换特效 | 中 | 中 |
| 15 | 无障碍选项 | 中 | 低-中 |
