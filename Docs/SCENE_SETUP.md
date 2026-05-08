# 🎮 场景搭建完整指南

## 概述
本指南将手把手教你如何在Unity中搭建完整的游戏场景。

---

## 第一步：创建Unity项目

### 1.1 新建项目
1. 打开 Unity Hub
2. 点击 "New Project"
3. 选择 "2D (URP)" 模板
4. 命名为 "ZombieDefense"
5. 点击 "Create Project"

### 1.2 导入脚本
1. 将 `Assets/Scripts` 文件夹复制到项目中
2. 等待Unity编译完成

---

## 第二步：创建主菜单场景

### 2.1 场景设置
1. File → New Scene
2. 保存为 `MainMenu`
3. 删除默认的 Main Camera

### 2.2 创建Canvas
```
GameObject → UI → Canvas
- Render Mode: Screen Space - Overlay
- Canvas Scaler:
  - UI Scale Mode: Scale With Screen Size
  - Reference Resolution: 1080 x 1920
  - Match: 0.5 (Width and Height)
```

### 2.3 创建背景
```
GameObject → UI → Image (命名为 Background)
- Color: #2C3E50 (深蓝灰色)
- Rect Transform: 铺满全屏
```

### 2.4 创建标题
```
GameObject → UI → Text (命名为 Title)
- Text: 僵尸防线
- Font Size: 120
- Color: #E74C3C (红色)
- Alignment: Center
- Position: (0, 600, 0)
```

### 2.5 创建按钮
```
GameObject → UI → Button (命名为 StartButton)
- 子物体Text: 开始游戏
- Font Size: 60
- Position: (0, 200, 0)
- Size: (400, 120)

复制创建其他按钮:
- SettingsButton (设置)
- HelpButton (帮助)
- ExitButton (退出)
位置分别在 (0, 50, 0), (0, -100, 0), (0, -250, 0)
```

### 2.6 创建高分显示
```
GameObject → UI → Text (命名为 HighScoreText)
- Text: 最高分: 0
- Font Size: 40
- Position: (0, -400, 0)
```

### 2.7 添加管理器
```
创建空物体 GameManager
- 添加 MainMenuUI 脚本
- 关联所有UI元素

创建空物体 AudioManager
- 添加 AudioManager 脚本

创建空物体 SaveManager
- 添加 SaveManager 脚本
```

---

## 第三步：创建游戏场景

### 3.1 场景设置
1. File → New Scene
2. 保存为 `GameScene`

### 3.2 设置相机
```
选中 Main Camera
- Projection: Orthographic
- Size: 5
- Background Color: #1A1A2E (深色)
- Position: (0, 0, -10)
```

### 3.3 创建Canvas
```
GameObject → UI → Canvas (命名为 GameCanvas)
- Render Mode: Screen Space - Overlay
- Canvas Scaler:
  - UI Scale Mode: Scale With Screen Size
  - Reference Resolution: 1080 x 1920
```

### 3.4 创建游戏世界

#### 创建背景
```
GameObject → 2D Object → Sprite (命名为 Background)
- Sprite: 使用纯色或背景图
- Color: #16213E
- Scale: (20, 20, 1)
- Order in Layer: -10
```

#### 创建基地
```
GameObject → 2D Object → Sprite (命名为 Base)
- Tag: Base
- Layer: Default
- 添加组件:
  - BaseManager
  - CircleCollider2D (Is Trigger: true)
  - Rigidbody2D (Is Kinematic: true)
- Position: (0, -3, 0)
```

#### 创建基地血条
```
作为Base的子物体:
GameObject → UI → Slider (命名为 HealthBar)
- Position: (0, 1, 0)
- Size: (2, 0.3, 1)
- 设置Slider为World Space模式
```

#### 创建路径点
```
创建空物体 PathPoints
创建子物体 (空物体):
- Point_0: (-7, 5, 0)
- Point_1: (-3, 5, 0)
- Point_2: (-3, 0, 0)
- Point_3: (3, 0, 0)
- Point_4: (3, 3, 0)
- Point_5: (7, 3, 0)
- Point_6: (7, -3, 0)
- Point_7: (0, -3, 0) [基地]

添加 PathManager 脚本到 PathPoints
- 将路径点拖到 PathPoints 数组
```

#### 创建生成点
```
创建空物体 SpawnPoints
创建子物体 (空物体):
- Spawn_0: (-7, 5, 0)
- Spawn_1: (7, 5, 0)
- Spawn_2: (-7, -5, 0)
- Spawn_3: (7, -5, 0)
```

### 3.5 创建防御塔

#### 创建防御塔预制体
```
GameObject → 2D Object → Sprite (命名为 Tower)
- 添加组件:
  - Tower 脚本
  - CircleCollider2D (Is Trigger: true)
  - AudioSource
- 创建子物体 FirePoint (空物体)
  - Position: (0.5, 0, 0)
- 创建子物体 RangeIndicator (Sprite)
  - Sprite: 圆形
  - Color: 半透明黄色
  - Scale: (10, 10, 1) [根据射程调整]
  - 默认禁用

拖入 Prefabs 文件夹创建预制体
```

### 3.6 创建敌人预制体

#### 普通僵尸
```
GameObject → 2D Object → Sprite (命名为 Enemy_Normal)
- Layer: Enemy
- 添加组件:
  - Enemy 脚本 (或 EnemyTypes 中的具体类型)
  - CircleCollider2D (Is Trigger: true)
  - Rigidbody2D (Is Kinematic: true)
- 创建子物体 HealthBar (使用HealthBar预制体)

拖入 Prefabs 文件夹
```

#### 其他敌人类型
- Enemy_Fast (快速僵尸)
- Enemy_Tank (坦克僵尸)
- Enemy_Bomber (自爆僵尸)
- Enemy_Healer (治疗僵尸)
- Enemy_Split (分裂僵尸)
- Enemy_Elite (精英僵尸)
- Enemy_Boss (BOSS)

### 3.7 创建子弹预制体
```
GameObject → 2D Object → Sprite (命名为 Bullet)
- Layer: Default
- 添加组件:
  - Bullet 脚本
  - CircleCollider2D (Is Trigger: true)
  - Rigidbody2D (Is Kinematic: true)
- 添加 Trail Renderer (可选)

拖入 Prefabs 文件夹
```

### 3.8 创建游戏HUD

#### 顶部信息栏
```
在GameCanvas下创建:
Panel (命名为 TopBar)
- Height: 150
- Position: 顶部

子物体:
- WaveText: 波次: 1
- ScoreText: 分数: 0
- GoldText: 金币: 100
- EnemyCountText: 敌人: 0
```

#### 基地血量
```
Panel (命名为 BaseHealthPanel)
- Position: 左下角
- Slider: 基地血量条
```

#### 控制按钮
```
Button (命名为 PauseButton)
- Text: 暂停
- Position: 右上角

Button (命名为 SpeedButton)
- Text: 1x
- Position: 暂停按钮下方
```

#### 暂停菜单
```
Panel (命名为 PausePanel)
- 默认禁用
- 半透明黑色背景

子物体:
- ResumeButton (继续)
- RestartButton (重新开始)
- MenuButton (主菜单)
```

### 3.9 创建技能选择界面
```
Panel (命名为 SkillSelectionPanel)
- 默认禁用
- 半透明黑色背景

子物体:
- TitleText: 选择技能
- SkillContainer (Horizontal Layout Group)
  - 3个技能卡片预制体位置

技能卡片预制体:
- Panel (SkillCard)
  - Icon (Image)
  - NameText
  - DescText
  - SelectButton
```

### 3.10 创建游戏结束界面
```
Panel (命名为 GameOverPanel)
- 默认禁用

子物体:
- VictoryTitle / DefeatTitle
- FinalScoreText
- FinalWaveText
- KillCountText
- HighScoreText
- NewRecordBadge
- RestartButton
- MenuButton
- ShareButton
- DoubleRewardButton
```

### 3.11 创建塔信息面板
```
Panel (命名为 TowerInfoPanel)
- 默认禁用
- 位置: 屏幕右侧

子物体:
- TowerNameText
- TowerLevelText
- DamageText
- RangeText
- FireRateText
- UpgradeButton
- SellButton
- CloseButton
```

### 3.12 添加管理器
```
创建空物体 GameManagers
添加子管理器:
- GameManager (GameManager.cs)
- WaveManager (WaveManager.cs)
  - 设置 SpawnPoints
  - 设置 TargetPoint (Base)
  - 设置 EnemyPrefabs
- SkillManager (SkillManager.cs)
- TowerManager (TowerManager.cs)
  - 设置 TowerPrefabs
- EffectManager (EffectManager.cs)
- AudioManager (AudioManager.cs)
- SaveManager (SaveManager.cs)
```

---

## 第四步：配置资源

### 4.1 音频配置
在 AudioManager 中设置:
- MainBGM: bgm_main.mp3
- BattleBGM: bgm_battle.mp3
- BossBGM: bgm_boss.mp3
- VictoryBGM: bgm_victory.mp3
- DefeatBGM: bgm_defeat.mp3
- FireSound: sfx_shoot.wav
- 其他音效...

### 4.2 精灵配置
为所有Sprite Renderer分配图片:
- Tower: tower_base.png
- Enemy: enemy_normal.png
- Bullet: bullet.png
- Base: base.png

### 4.3 颜色配置
- 普通僵尸: 绿色 (#27AE60)
- 快速僵尸: 黄色 (#F1C40F)
- 坦克僵尸: 紫色 (#8E44AD)
- BOSS: 红色 (#C0392B)

---

## 第五步：测试运行

### 5.1 运行场景
1. 按 Play 按钮
2. 测试主菜单
3. 点击开始游戏
4. 测试游戏流程

### 5.2 常见问题

**问题1: 敌人不移动**
- 检查基地 Tag 是否为 "Base"
- 检查 PathManager 是否正确配置

**问题2: 防御塔不攻击**
- 检查敌人 Layer 是否为 "Enemy"
- 检查 Tower 的 FirePoint 是否设置

**问题3: UI不显示**
- 检查 Canvas 的 Render Mode
- 检查 UI 元素的层级

---

## 第六步：构建发布

### 6.1 PC构建
```
File → Build Settings
- 添加 MainMenu 和 GameScene
- 选择 PC, Mac & Linux Standalone
- 点击 Build
```

### 6.2 微信小游戏构建
```
1. 安装微信小游戏插件
2. WeChat Mini Game → Build
3. 使用微信开发者工具打开
4. 预览并上传
```

---

## 完成！

按照以上步骤，你将拥有一个完整的可运行游戏！
