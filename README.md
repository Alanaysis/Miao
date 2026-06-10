# Miao 游戏项目

一个使用 Godot 4.x 引擎开发的像素风刷宝射击游戏，AI 辅助开发。

## 项目特点

- **AI辅助开发**：AI负责主要代码编写，团队负责测试和审查
- **协作优先**：每个人都可以做每件事，灵活分工
- **迭代开发**：通过实验分支探索游戏方向

## 技术栈

- **引擎**：Godot 4.x
- **脚本语言**：C#（游戏逻辑）+ GDScript（引导脚本）
- **版本控制**：Git + GitHub

## 项目结构

```
Miao/
├── .github/              # GitHub模板和配置
├── docs/                 # 项目文档
│   ├── superpowers/     # 设计规格文档
│   │   └── specs/       # 各系统设计文档
│   └── game_design.md   # 游戏设计总览
├── project/              # Godot项目目录
│   ├── scenes/          # 场景文件
│   ├── scripts/         # 脚本文件（C#）
│   │   ├── player/     # 玩家相关
│   │   ├── enemy/      # 敌人相关
│   │   ├── system/     # 系统脚本
│   │   └── ui/         # UI脚本
│   ├── assets/          # 美术资源
│   │   ├── sprites/    # 精灵图
│   │   ├── tilesets/   # 瓦片集
│   │   └── audio/      # 音频
│   └── addons/          # 插件
├── tools/                # 开发工具
│   └── pixel-char-gen/ # 像素角色生成管线
├── agent.md              # AI行为规范（重要！）
└── README.md
```

## 快速开始

### 环境要求

- Godot 4.x（推荐最新稳定版）
- Git

### 克隆项目

```bash
git clone https://github.com/your-username/Miao.git
cd Miao
```

### 打开项目

1. 打开 Godot 4.x
2. 点击"导入"
3. 选择 `project/project.godot` 文件
4. 点击"导入并编辑"

## 开发流程

### 分支策略

- `main`：稳定版本，始终保持可运行
- `feature/xxx`：功能开发分支
- `bugfix/xxx`：bug修复分支
- `experiment/xxx`：实验分支（探索游戏方向）

### 协作流程

1. 从main分支创建新分支
2. 在分支上开发功能
3. 提交并推送更改
4. 创建Pull Request
5. 至少1人审查后合并

### AI协作

**重要**：所有AI助手必须先阅读 `agent.md` 文件，了解项目规范后再进行协作。

AI助手请前往 [agent.md](./agent.md) 查看完整的行为规范。

## 游戏设计

游戏类型和设计方向正在探索中。当前实验分支：

- `experiment/roguelike`：Roguelike 类型实验（完整 MVP）
- `experiment/destiny`：命运 2 风格横版刷宝射击（当前方向）
- `experiment/ARPG`：ARPG 实验

**当前方向：** 横版 2D 像素风刷宝射击游戏，灵感来自命运 2 的核心刷宝循环与职业 Build 系统。

核心设计文档：
- [MVP 设计文档](./docs/superpowers/specs/2026-06-06-destiny-looter-shooter-mvp-design.md) — 完整的武器/职业/地图/Meta 系统设计
- [实现计划](./docs/superpowers/plans/2026-06-06-destiny-looter-shooter-mvp.md) — 14 个任务的详细实现步骤
- [游戏设计总览](./docs/game_design.md) — 整体游戏设计方向

## 贡献指南

1. Fork 本仓库
2. 创建功能分支（`git checkout -b feature/xxx`）
3. 提交更改（`git commit -m 'feat: 添加xxx功能'`）
4. 推送到分支（`git push origin feature/xxx`）
5. 创建 Pull Request

## 文档

- [AI行为规范](./agent.md) - AI助手必读
- [MVP 设计文档](./docs/superpowers/specs/2026-06-06-destiny-looter-shooter-mvp-design.md) - 刷宝射击 MVP 完整设计
- [游戏设计文档](./docs/game_design.md) - 游戏设计方向
- [GitHub模板](./.github/) - Issue和PR模板

## 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](./LICENSE) 文件

## 联系方式

（待补充团队联系方式）
