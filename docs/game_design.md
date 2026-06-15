# 游戏设计文档

本文档记录游戏的设计方向和决策。当前状态：**Destiny 风格刷宝射击**

## 项目概述

- **项目名称**：Miao
- **游戏类型**：俯视角刷宝射击（Top-down Looter Shooter）
- **目标平台**：PC（Windows, macOS, Linux）
- **引擎**：Godot 4.x + C#

## 设计方向

灵感来自 Destiny 2 的核心刷宝循环与职业 Build 系统。详细设计请参阅：
- **设计文档**：`docs/superpowers/specs/2026-06-06-destiny-looter-shooter-mvp-design.md`
- **实施计划**：`docs/superpowers/plans/2026-06-06-destiny-looter-shooter-mvp.md`

## 核心玩法

- 2D 俯视角 + 鼠标 360° 自由瞄准 + WASD 八方向移动
- 3 个职业（猎人/泰坦/术士），每个职业 3 个元素子类
- 10 种武器类型，6 档稀有度，Perk 系统
- 局内碎片升级 + 局外 Meta 解锁
- 5 个主题地图，每个地图 4 房间 + 1 Boss
- 单人 / 多人联机（ENet 主机/客户端）

## 决策记录

| 日期 | 决策 | 原因 |
|------|------|------|
| 2026-05-27 | 开始 Roguelike 类型实验 | 适合小团队+AI协作，核心玩法清晰 |
| 2026-06-06 | 转向 Destiny 风格刷宝射击 | Roguelike 实验后确定方向，深度 Build 系统更有吸引力 |
| 2026-06-15 | 核对设计文档与实现，修正不一致 | 确保文档准确反映当前代码状态 |
