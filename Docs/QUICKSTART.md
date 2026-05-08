# 🚀 快速开始

## 环境要求
- Unity 2022.3 LTS 或更高版本
- Visual Studio 或 VS Code
- Git

## 项目导入

### 1. 克隆项目
```bash
git clone <项目地址>
cd unity-td-shooter
```

### 2. Unity中打开
1. 打开 Unity Hub
2. 点击 "Add" 添加项目
3. 选择 `unity-td-shooter` 文件夹
4. 点击打开项目

### 3. 场景设置
1. 打开 `Assets/Scenes` 文件夹
2. 创建新场景 `MainScene`
3. 按以下步骤设置场景

## 场景搭建步骤

### 1. 创建游戏管理器
```
创建空物体 GameObject -> Create Empty
命名为 "GameManager"
添加组件: GameManager, WaveManager, SkillManager, SaveManager
```

### 2. 创建基地
```
创建2D精灵 GameObject -> 2D Object -> Sprite
命名为 "Base"
添加组件: BaseManager
设置 Tag 为 "Base"
添加血条UI
```

### 3. 创建防御塔
```
创建2D精灵
命名为 "Tower"
添加组件: Tower
设置 FirePoint（空物体作为发射点）
添加 AudioSource
```

### 4. 创建敌人生成点
```
创建空物体
命名为 "SpawnPoints"
创建3-4个子物体作为生成点
```

### 5. 设置相机
```
Main Camera
- Projection: Orthographic
- Size: 5
- Background: 深色
```

### 6. 创建预制体
```
敌人预制体:
- 2D精灵 + Enemy脚本 + Collider2D

子弹预制体:
- 2D精灵 + Bullet脚本 + Rigidbody2D + Collider2D
```

## 运行测试

1. 点击 Play 按钮
2. 测试功能:
   - 敌人是否正常生成
   - 防御塔是否自动攻击
   - 技能选择是否弹出
   - 游戏结束逻辑

## 构建发布

### 微信小游戏
1. 安装微信小游戏插件
2. File -> Build Settings
3. 选择 WeChat Mini Game
4. 点击 Build

### Android
1. File -> Build Settings
2. 切换到 Android 平台
3. 点击 Build

## 常见问题

### Q: 敌人不移动？
A: 检查 Base 是否正确设置 Tag 为 "Base"

### Q: 防御塔不攻击？
A: 检查敌人图层是否正确设置为 "Enemy"

### Q: 技能选择不弹出？
A: 检查 SkillSelectionUI 是否正确赋值

## 下一步

- 添加更多敌人类型
- 设计更多技能
- 制作UI界面
- 添加音效
- 优化性能
