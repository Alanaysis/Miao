# Miao 游戏项目

一个由3-5人小团队协作开发的游戏项目，使用 Godot 4.x 引擎，AI辅助开发。

## 项目特点

- **AI辅助开发**：AI负责主要代码编写，团队负责测试和审查
- **协作优先**：每个人都可以做每件事，灵活分工
- **迭代开发**：通过实验分支探索游戏方向

## 技术栈

- **引擎**：Godot 4.x
- **脚本语言**：GDScript
- **版本控制**：Git + GitHub

## 项目结构

```
Miao/
├── .github/              # GitHub模板和配置
├── docs/                 # 项目文档
├── project/              # Godot项目目录
│   ├── scenes/          # 场景文件
│   ├── scripts/         # 脚本文件
│   │   ├── player/     # 玩家相关
│   │   ├── enemy/      # 敌人相关
│   │   ├── system/     # 系统脚本
│   │   └── ui/         # UI脚本
│   ├── assets/          # 美术资源
│   │   ├── sprites/    # 精灵图
│   │   ├── tilesets/   # 瓦片集
│   │   └── audio/      # 音频
│   └── addons/          # 插件
├── tests/                # 测试文件
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
- `docs/xxx`：文档更新
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

- `experiment/roguelike`：Roguelike类型实验

详细设计文档请查看 [docs/game_design.md](./docs/game_design.md)

## 贡献指南

1. Fork 本仓库
2. 创建功能分支（`git checkout -b feature/xxx`）
3. 提交更改（`git commit -m 'feat: 添加xxx功能'`）
4. 推送到分支（`git push origin feature/xxx`）
5. 创建 Pull Request

## 文档

- [AI行为规范](./agent.md) - AI助手必读
- [游戏设计文档](./docs/game_design.md) - 游戏设计方向
- [GitHub模板](./.github/) - Issue和PR模板

## 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](./LICENSE) 文件

## 联系方式

（待补充团队联系方式）
