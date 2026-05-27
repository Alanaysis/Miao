# Agent 行为规范

本文档定义了AI助手在本项目中的行为规范。所有AI协作必须遵守以下规则。

## 项目概述

这是一个使用 Godot 4.x 开发的游戏项目，由3-5人小团队协作开发。团队成员都可以做每件事，AI负责主要的代码编写，人类负责测试和审查。

## 技术栈

- 引擎：Godot 4.x
- 脚本语言：GDScript
- 版本控制：Git + GitHub

## 代码规范

### 命名规范

- 类名：PascalCase（如 `PlayerController`）
- 函数/变量：snake_case（如 `move_speed`）
- 常量：UPPER_SNAKE_CASE（如 `MAX_HEALTH`）
- 信号：snake_case（如 `health_changed`）
- 节点名：PascalCase（如 `PlayerSprite`）

### 文件组织

```
project/
├── scenes/          # Godot场景文件（.tscn）
├── scripts/         # GDScript脚本文件（.gd）
│   ├── player/     # 玩家相关脚本
│   ├── enemy/      # 敌人相关脚本
│   ├── system/     # 系统脚本（管理器、工具类）
│   └── ui/         # UI相关脚本
├── assets/          # 美术资源
│   ├── sprites/    # 精灵图
│   ├── tilesets/   # 瓦片集
│   └── audio/      # 音频文件
└── addons/          # Godot插件
```

### 注释规范

- 每个函数必须有文档注释，说明功能、参数和返回值
- 复杂逻辑必须有行内注释
- 使用中文注释（团队习惯）

示例：
```gdscript
## 处理玩家移动
## @param direction: 移动方向向量
## @param delta: 帧间隔时间
func handle_movement(direction: Vector2, delta: float) -> void:
    # 计算移动速度
    var velocity = direction * move_speed
    # 应用移动
    position += velocity * delta
```

## Git 工作流

### 分支命名

- 功能：`feature/xxx`（如 `feature/player-movement`）
- 修复：`bugfix/xxx`（如 `bugfix/collision-detection`）
- 文档：`docs/xxx`（如 `docs/readme-update`）
- 实验：`experiment/xxx`（如 `experiment/roguelike`）

### Commit 规范

格式：`<type>: <description>`

类型：
- `feat`：新功能
- `fix`：修复bug
- `docs`：文档更新
- `style`：代码格式调整
- `refactor`：重构
- `test`：测试相关
- `chore`：构建/工具相关

示例：
```
feat: 添加玩家移动系统
fix: 修复碰撞检测问题
docs: 更新README文档
```

### Pull Request 规范

1. 每个功能必须通过PR合并到main分支
2. PR标题清晰描述变更内容
3. PR描述说明变更原因和实现方式
4. 至少1人审查后才能合并
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

1. 新功能必须更新README
2. 复杂系统必须有设计文档
3. 重要决策必须记录在docs目录

## 游戏设计约束

（待定 - 根据最终确定的游戏类型填写）

## 参考资源

- [Godot 官方文档](https://docs.godotengine.org)
- [GDScript 风格指南](https://docs.godotengine.org/en/latest/tutorials/scripting/gdscript/gdscript_styleguide.html)
- [Godot 最佳实践](https://docs.godotengine.org/en/latest/tutorials/best_practices/index.html)
