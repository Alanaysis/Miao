# Agent 行为规范

本文档定义了AI助手在本项目中的行为规范。所有AI协作必须遵守以下规则。

## 项目概述

这是一个使用 Godot 4.x + C# 开发的 Destiny 风格俯视角刷宝射击游戏，由3-5人小团队协作开发。团队成员都可以做每件事，AI负责主要的代码编写，人类负责测试和审查。

## 技术栈

- 引擎：Godot 4.x
- 脚本语言：C#（.NET）
- 版本控制：Git + GitHub

## 代码规范

### 命名规范

- 类名/公共属性/公共方法：PascalCase（如 `PlayerController`、`MoveSpeed`、`GetClassName()`）
- 私有字段：camelCase 或 _camelCase（如 `cooldownTimer`、`_currentHealth`）
- 局部变量/参数：camelCase（如 `moveSpeed`、`delta`）
- 常量：UPPER_SNAKE_CASE（如 `MAX_HEALTH`）
- 信号（Signal）：PascalCase（如 `HealthChanged`）
- 节点名：PascalCase（如 `PlayerSprite`）

### 文件组织

```
project/
├── scenes/          # Godot 场景文件（.tscn）
├── scripts/         # C# 脚本文件（.cs）
│   ├── player/     # 玩家相关脚本
│   ├── enemy/      # 敌人相关脚本
│   ├── system/     # 系统脚本（管理器、工具类）
│   ├── ui/         # UI 相关脚本
│   ├── network/    # 多人联机相关脚本
│   ├── lobby/      # 大厅相关脚本
│   └── armor/      # 护甲/Mod 相关脚本
├── data/            # JSON 配置文件
├── assets/          # 美术资源
│   ├── sprites/    # 精灵图
│   ├── tilesets/   # 瓦片集
│   └── audio/      # 音频文件
└── addons/          # Godot 插件
```

### 注释规范

- 公共类和公共方法使用 XML 文档注释
- 复杂逻辑必须有行内注释
- 使用中文注释（团队习惯）

示例：
```csharp
/// <summary>
/// 处理玩家移动
/// </summary>
/// <param name="direction">移动方向向量</param>
/// <param name="delta">帧间隔时间</param>
public void HandleMovement(Vector2 direction, float delta)
{
    // 计算移动速度
    var velocity = direction * MoveSpeed;
    // 应用移动
    Position += velocity * delta;
}
```

## Git 工作流

### 分支命名

- 功能：`feature/xxx`（如 `feature/player-movement`）
- 修复：`bugfix/xxx`（如 `bugfix/collision-detection`）
- 文档：`docs/xxx`（如 `docs/readme-update`）
- 实验：`experiment/xxx`（如 `experiment/destiny`）

### Commit 规范

格式：`<type>(<scope>): <description>`

类型：
- `feat`：新功能
- `fix`：修复 bug
- `docs`：文档更新
- `style`：代码格式调整
- `refactor`：重构
- `test`：测试相关
- `chore`：构建/工具相关

示例：
```
feat(weapons): 添加融合步枪蓄力机制
fix(network): 修复 MultiplayerPeer 空引用
docs(multiplayer): 具体规范多人联机实现方式
```

### Pull Request 规范

1. 每个功能必须通过 PR 合并到 main 分支
2. PR 标题清晰描述变更内容
3. PR 描述说明变更原因和实现方式
4. 至少 1 人审查后才能合并
5. 必须通过测试

## AI 协作规范

### 代码生成

1. 必须遵循上述代码规范
2. 生成的代码必须可运行
3. 包含必要的错误处理
4. 考虑边界情况

### 测试要求

1. 核心功能必须有单元测试
2. 生成代码后必须验证可运行
3. 测试文件放在 `tests/` 目录

### 文档要求

1. 新功能必须更新 README
2. 复杂系统必须有设计文档
3. 重要决策必须记录在 docs 目录

## 游戏设计约束

- 视角：2D 俯视角（类 Helldivers 1）+ 鼠标 360° 自由瞄准
- 美术：像素风（32×32 角色）
- 脚本语言：C#（非 GDScript）
- 数据驱动：游戏数据使用 JSON 配置文件管理
- 多人联机：ENet 主机/客户端架构

## 参考资源

- [Godot 官方文档](https://docs.godotengine.org)
- [Godot C# 文档](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/index.html)
- [Godot 最佳实践](https://docs.godotengine.org/en/latest/tutorials/best_practices/index.html)
