# Unity 塔防射击游戏 - 实现指南

## 项目完成度

### ✅ 已完成模块

| 模块 | 文件 | 说明 |
|-----|------|-----|
| 游戏管理器 | GameManager.cs | 游戏状态、分数、金币管理 |
| 防御塔 | Tower.cs | 自动攻击、技能加成、升级系统 |
| 敌人基类 | Enemy.cs | AI移动、受伤、死亡逻辑 |
| 敌人类型 | EnemyTypes.cs | 7种敌人（快速、坦克、自爆、治疗、分裂、精英、BOSS） |
| 子弹 | Bullet.cs | 飞行、穿透、溅射效果 |
| 波次管理 | WaveManager.cs | 波次生成、难度曲线 |
| 技能系统 | SkillManager.cs | 12个Roguelike技能 |
| 基地管理 | BaseManager.cs | 基地血量、游戏结束 |
| 音频管理 | AudioManager.cs | BGM、音效、音量控制 |
| 存档系统 | SaveManager.cs | 本地存档、设置保存 |
| 对象池 | ObjectPool.cs | 性能优化 |
| 主菜单UI | MainMenuUI.cs | 开始、设置、帮助界面 |
| 游戏HUD | GameHUD.cs | 波次、分数、血量显示 |
| 技能选择UI | SkillSelectionUI.cs | 三选一技能界面 |
| 游戏结束UI | GameOverUI.cs | 结算、分享、奖励 |
| 微信SDK | WeChatManager.cs | 登录、分享、广告、排行榜 |

### 📊 代码统计

```
总文件数: 20+
总代码行数: 约5000行
脚本语言: C#
引擎: Unity 2022.3 LTS
```

## 快速开始

### 1. Unity项目设置

1. 打开 Unity Hub
2. 创建新项目，选择 2D (URP) 模板
3. 项目命名为 `ZombieDefense`

### 2. 导入代码

1. 将 `Assets/Scripts` 文件夹复制到 Unity 项目中
2. 在 Unity 中等待脚本编译完成

### 3. 场景搭建

#### 创建 MainMenu 场景

```
1. File -> New Scene
2. 创建 Canvas (Screen Space - Overlay)
3. 创建以下UI元素:
   - 标题文本 "僵尸防线"
   - 开始游戏按钮
   - 设置按钮
   - 退出按钮
   - 高分显示文本
4. 创建空物体 "MainMenuManager"
   - 添加 MainMenuUI 脚本
5. 创建空物体 "AudioManager"
   - 添加 AudioManager 脚本
6. 创建空物体 "SaveManager"
   - 添加 SaveManager 脚本
7. 保存场景为 "MainMenu"
```

#### 创建 GameScene 场景

```
1. File -> New Scene
2. 设置相机:
   - Projection: Orthographic
   - Size: 5
   - Background: 深色

3. 创建游戏管理器:
   - 空物体 "GameManager"
   - 添加 GameManager, WaveManager, SkillManager

4. 创建基地:
   - 2D Sprite "Base"
   - Tag: "Base"
   - 添加 BaseManager, Collider2D
   - 添加血条UI

5. 创建防御塔:
   - 2D Sprite "Tower"
   - 添加 Tower 脚本
   - 创建子物体 "FirePoint" (发射点)
   - 添加 AudioSource

6. 创建生成点:
   - 空物体 "SpawnPoints"
   - 创建4个子物体作为生成点 (位置: 上下左右)

7. 创建Canvas:
   - 添加 GameHUD 脚本
   - 创建以下UI:
     * 波次文本
     * 分数文本
     * 金币文本
     * 基地血条
     * 敌人数量
     * 暂停按钮
     * 暂停菜单面板

8. 创建技能选择面板:
   - 面板 "SkillSelectionPanel"
   - 添加 SkillSelectionUI 脚本
   - 创建3个技能卡片预制体

9. 创建游戏结束面板:
   - 面板 "GameOverPanel"
   - 添加 GameOverUI 脚本

10. 保存场景为 "GameScene"
```

### 4. 创建预制体

#### 敌人预制体

```
1. 创建空物体 "Enemy_Normal"
2. 添加组件:
   - Sprite Renderer (僵尸图片)
   - CircleCollider2D (Is Trigger: true)
   - Rigidbody2D (Kinematic)
   - Enemy 脚本
   - HealthBar 脚本
3. 拖入 Prefabs 文件夹
4. 重复创建其他敌人类型:
   - Enemy_Fast (快速僵尸)
   - Enemy_Tank (坦克僵尸)
   - Enemy_Bomber (自爆僵尸)
   - Enemy_Healer (治疗僵尸)
   - Enemy_Split (分裂僵尸)
   - Enemy_Elite (精英僵尸)
   - Enemy_Boss (BOSS)
```

#### 子弹预制体

```
1. 创建空物体 "Bullet"
2. 添加组件:
   - Sprite Renderer (子弹图片)
   - CircleCollider2D (Is Trigger: true)
   - Rigidbody2D (Kinematic)
   - Bullet 脚本
3. 拖入 Prefabs 文件夹
```

### 5. 配置 WaveManager

```
1. 选中 WaveManager
2. 设置 SpawnPoints (4个生成点)
3. 设置 TargetPoint (基地)
4. 设置 EnemyPrefabs (所有敌人类型)
5. 配置难度曲线 (AnimationCurve)
```

### 6. 配置 SkillManager

```
1. 选中 SkillManager
2. 技能数据已代码初始化，无需额外配置
3. 设置 SkillSelectionUI 引用
```

### 7. 音频资源

```
将音频文件放入 Assets/Resources/Audio/:
- bgm_main.mp3 (主界面BGM)
- bgm_battle.mp3 (战斗BGM)
- bgm_boss.mp3 (BOSS战BGM)
- sfx_shoot.wav (射击)
- sfx_hit.wav (受击)
- sfx_death.wav (死亡)
- sfx_button.wav (按钮)
- sfx_wave_start.wav (波次开始)
- sfx_skill_select.wav (选择技能)
```

### 8. 构建设置

```
1. File -> Build Settings
2. 添加场景:
   - MainMenu (index 0)
   - GameScene (index 1)
3. 选择平台:
   - PC: 直接 Build
   - Android: Switch Platform -> Build
   - 微信小游戏: 安装微信小游戏插件 -> Build
```

## 微信小游戏发布

### 1. 安装微信小游戏插件

```
1. 打开 Package Manager
2. 添加 package from git URL:
   https://github.com/wechat-miniprogram/minigame-unity-webgl-transform.git
3. 等待安装完成
```

### 2. 配置微信小游戏

```
1. 打开 WeChat Settings 窗口
2. 填写 AppID
3. 配置游戏名称、描述
4. 设置屏幕方向 (Portrait)
5. 配置内存 (建议 256MB)
```

### 3. 构建微信小游戏

```
1. WeChat Mini Game -> Build
2. 选择输出目录
3. 点击 Build
4. 使用微信开发者工具打开
```

### 4. 微信开发者工具

```
1. 下载微信开发者工具
2. 导入构建的项目
3. 填写 AppID
4. 预览/上传
```

## 扩展功能

### 添加新技能

在 `SkillManager.InitializeSkills()` 中添加:

```csharp
AllSkills.Add(new SkillData
{
    SkillId = "your_skill_id",
    SkillName = "技能名称",
    Description = "技能描述",
    SkillType = SkillType.YourType,
    Value = 0.5f,
    Icon = yourIcon,
    Rarity = SkillRarity.Rare
});
```

### 添加新敌人

1. 创建新类继承 Enemy:

```csharp
public class MyNewEnemy : Enemy
{
    protected override void Start()
    {
        base.Start();
        EnemyName = "新敌人";
        MaxHealth = 100f;
        MoveSpeed = 2f;
        // ...
    }
}
```

2. 创建预制体并添加到 WaveManager

### 添加新UI

1. 在 Canvas 下创建新面板
2. 创建脚本继承 MonoBehaviour
3. 绑定按钮事件
4. 在适当时机显示/隐藏

## 调试技巧

### 常用调试命令

```csharp
// 在 Update 中显示信息
Debug.Log($"当前波次: {GameManager.Instance.CurrentWave}");

// 绘制调试线
Debug.DrawLine(start, end, Color.red);

// 在 Scene 视图中显示
void OnDrawGizmos()
{
    Gizmos.DrawWireSphere(transform.position, range);
}
```

### 性能优化

1. 使用对象池 (已集成)
2. 减少 GetComponent 调用
3. 使用 Coroutine 替代 Update
4. 合并材质和贴图
5. 启用 Sprite Atlas

## 常见问题

### Q: 敌人不移动？
A: 检查基地 Tag 是否为 "Base"

### Q: 防御塔不攻击？
A: 检查敌人 Layer 是否为 "Enemy"

### Q: 技能选择不弹出？
A: 检查 SkillSelectionUI 是否正确赋值

### Q: 音效不播放？
A: 检查 AudioClip 是否正确赋值，AudioSource 是否启用

### Q: 微信小游戏黑屏？
A: 检查内存设置，减少同时显示的敌人数量

## 参考资源

- Unity 官方文档: https://docs.unity3d.com/
- 微信小游戏文档: https://developers.weixin.qq.com/minigame/dev/guide/
- 塔防游戏设计: https://gamedevelopment.tutsplus.com/
