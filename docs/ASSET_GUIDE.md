# 像素素材库 — 操作手册

> 所有素材均来自 Kenney.nl（CC0 协议，免费商用无需署名）

## 素材库位置

```
project/assets/sprites/downloaded/
├── kenney_desert_shooter/   # 517 PNG — 射击游戏完整素材包
├── kenney_roguelike/        # 4 PNG  — 角色精灵表
├── kenney_dungeon/          # 136 PNG — 地牢瓷砖/装饰
├── kenney_tiny_town/        # 136 PNG — 城镇地图瓷砖
├── kenney_pico8/            # 306 PNG — 经典像素风格
├── kenney_scifi_ui/         # 742 PNG — 科幻 UI 元素
└── kenney_space/            # 561 PNG — 太空飞船/宇航员
```

## 各包内容详解

### 1. Desert Shooter Pack（推荐优先使用）

包含完整的 16×16 射击游戏素材：

```
kenney_desert_shooter/PNG/
├── Players/Tiles/     # 角色精灵（多种颜色变体）
├── Enemies/Tiles/     # 敌人精灵（多种类型）
├── Weapons/Tiles/     # 武器图标/弹药
├── Interface/Tiles/   # UI 元素（按钮、面板、图标）
└── Tiles/             # 地图瓷砖（地板、墙壁、装饰）
```

**角色示例：** `PNG/Players/Tiles/tile_0000.png` 到 `tile_0015.png`
**敌人示例：** `PNG/Enemies/Tiles/tile_0000.png` 到 `tile_0015.png`

### 2. Roguelike Characters

包含多种颜色和造型的角色精灵表：

```
kenney_roguelike/Spritesheet/
└── roguelikeChar_transparent.png  # 16×16 精灵表，含所有角色变体
```

**精灵表布局：** 每个角色占 16×16 像素，按颜色和造型排列

### 3. Tiny Dungeon

16×16 地牢风格瓷砖：

```
kenney_dungeon/Tiles/    # 地板、墙壁、门、箱子等
kenney_dungeon/Tilemap/  # 精灵表（所有瓷砖合并）
```

### 4. Tiny Town

16×16 城镇/地图素材：

```
kenney_tiny_town/Tiles/    # 城镇建筑、树木、道路
kenney_tiny_town/Tilemap/  # 精灵表
```

### 5. Pico-8 Platformer

经典 8 位风格平台跳跃素材：

```
kenney_pico8/Transparent/Tiles/  # 透明背景瓷砖
kenney_pico8/Default/Tiles/      # 默认背景瓷砖
```

### 6. Sci-Fi UI（推荐用于界面）

科幻风格 UI 元素，按颜色主题分组：

```
kenney_scifi_ui/PNG/
├── Blue/Double/     # 蓝色主题按钮、面板、进度条
├── Green/Double/    # 绿色主题
├── Yellow/Double/   # 黄色主题
├── Red/Double/      # 红色主题
├── Grey/Double/     # 灰色主题
└── Extra/           # 特殊元素
```

**常用图标：** `button_square_header_*.png`（方形按钮）、`bar_*.png`（进度条）、`crosshair_*.png`（准星）

### 7. Space Shooter Extension

太空主题素材：

```
kenney_space/PNG/Sprites/
├── Ships/       # 太空飞船（多种类型）
├── Station/     # 空间站部件
├── Astronauts/  # 宇航员角色
├── Missiles/    # 导弹
├── Effects/     # 特效
└── Meteors/     # 陨石
```

---

## 在 Godot 中导入素材

### 方法 1：自动导入

Godot 4.x 会自动导入 `project/` 目录下的 PNG 文件。只需将素材复制到 `project/assets/sprites/` 目录即可。

### 方法 2：通过编辑器导入

1. 打开 Godot 编辑器
2. 在底部 **FileSystem** 面板中浏览 `res://assets/sprites/downloaded/`
3. 找到需要的 PNG 文件
4. 双击预览，右键选择 **Copy** 或直接拖拽到场景中

### 方法 3：在代码中加载

```csharp
// 加载单张图片
var texture = GD.Load<Texture2D>("res://assets/sprites/downloaded/kenney_desert_shooter/PNG/Players/Tiles/tile_0000.png");

// 设置到 Sprite2D
sprite.Texture = texture;
```

---

## 推荐素材对应关系

### 大厅设施
| 设施 | 推荐素材 | 路径 |
|------|----------|------|
| 装备配置机器 | 科幻 UI 蓝色按钮 | `kenney_scifi_ui/PNG/Blue/Double/button_square_header_large_rectangle.png` |
| 地图沙盘 | 科幻 UI 绿色按钮 | `kenney_scifi_ui/PNG/Green/Double/...` |
| Meta 商店 | 科幻 UI 黄色按钮 | `kenney_scifi_ui/PNG/Yellow/Double/...` |
| 保险库 | 地牢宝箱 | `kenney_dungeon/Tiles/tile_0068.png`（约） |
| 多人联机 | 科幻 UI 红色按钮 | `kenney_scifi_ui/PNG/Red/Double/...` |

### 玩家角色
| 职业 | 推荐素材 | 路径 |
|------|----------|------|
| 猎人 | Roguelike 角色精灵 | `kenney_roguelike/Spritesheet/roguelikeChar_transparent.png` |
| 泰坦 | Desert Shooter 角色 | `kenney_desert_shooter/PNG/Players/Tiles/tile_0000.png` |
| 术士 | Dungeon 角色 | `kenney_dungeon/Tiles/tile_0032.png`（约） |

### 敌人
| 类型 | 推荐素材 | 路径 |
|------|----------|------|
| 小虫 | Desert Shooter 敌人 | `kenney_desert_shooter/PNG/Enemies/Tiles/tile_0000.png` |
| 快速虫 | Desert Shooter 敌人 | `kenney_desert_shooter/PNG/Enemies/Tiles/tile_0004.png` |
| 坦克虫 | Dungeon 怪物 | `kenney_dungeon/Tiles/tile_0048.png`（约） |
| Boss | Dungeon 大怪物 | `kenney_dungeon/Tiles/tile_0100.png`（约） |

### 地图/环境
| 用途 | 推荐素材 | 路径 |
|------|----------|------|
| 游戏地图地板 | Desert Shooter 地砖 | `kenney_desert_shooter/PNG/Tiles/` |
| 大厅背景 | Tiny Town 地砖 | `kenney_tiny_town/Tiles/` |
| 墙壁/障碍物 | Dungeon 墙壁 | `kenney_dungeon/Tiles/tile_0016.png`（约） |

### UI 元素
| 用途 | 推荐素材 | 路径 |
|------|----------|------|
| 血条背景 | Sci-Fi UI 灰色条 | `kenney_scifi_ui/PNG/Grey/Double/bar_*.png` |
| 按钮 | Sci-Fi UI 彩色按钮 | `kenney_scifi_ui/PNG/{Color}/Double/button_*.png` |
| 准心 | Sci-Fi UI 十字准星 | `kenney_scifi_ui/PNG/Yellow/Double/crosshair_*.png` |
| 图标 | Desert Shooter 图标 | `kenney_desert_shooter/PNG/Interface/Tiles/` |

---

## 注意事项

1. **所有素材都是 16×16 像素** — 如果需要放大显示，使用 `Image.Resampling.NEAREST` 保持像素锐利
2. **精灵表需要裁剪** — 部分包使用 tilemap 格式，需要用 Python/PIL 裁剪单个 tile
3. **背景处理** — 大部分精灵有透明背景（RGBA），可以直接叠加使用
4. **颜色变体** — 同一类型有多种颜色，选择最适合游戏风格的颜色方案

## 快速预览素材

```bash
# 预览 desert shooter 的角色
cd project/assets/sprites/downloaded
python3 -c "
from PIL import Image
img = Image.open('kenney_desert_shooter/PNG/Players/Tilemap/tilemap.png')
img.save('preview_players.png')
print('已保存: preview_players.png')
"
```
