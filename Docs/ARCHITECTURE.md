# 🏗️ 僵尸防线 — 架构设计文档

---

## 游戏状态机流程

```
                    ┌──────────┐
                    │  APP启动  │
                    └────┬─────┘
                         ▼
                  ┌────────────┐
                  │  Splash    │  加载画面(2秒)
                  └─────┬──────┘
                        ▼
                  ┌────────────┐
                  │  登录/初始化 │
                  └─────┬──────┘
                        ▼
              ┌─────────────────┐
              │     MainMenu     │◄──────────────────┐
              └───┬───┬───┬─────┘                    │
                  │   │   │                          │
    ┌─────────────┘   │   └─────────────┐            │
    ▼                 ▼                  ▼           │
┌────────┐    ┌────────────┐    ┌────────────┐       │
│故事模式│    │ Roguelike  │    │  无尽模式   │       │
└───┬────┘    └─────┬──────┘    └─────┬──────┘       │
    │               │                │               │
    ▼               ▼                ▼               │
┌────────────────────────────────────────────┐       │
│              Battle (战斗场景)               │       │
│  TowerMgr / EnemyMgr / WaveMgr / SkillMgr  │       │
└────────────────────┬───────────────────────┘       │
                     ▼                                │
              ┌────────────┐                         │
              │   结算      │                         │
              └──┬──────┬──┘                         │
                 │      │                            │
           胜利  │      │  失败                      │
                 ▼      ▼                            │
         ┌─────────┐ ┌─────────┐                    │
         │奖励发放  │ │失败处理  │                    │
         └────┬────┘ └────┬────┘                    │
              │           │                          │
              └─────┬─────┘                          │
                    └────────────────────────────────┘
```

## 数据流向

```
                      ┌──────────────┐
                      │   DataHub    │  ← 统一数据访问层
                      └──────┬───────┘
                             │
           ┌─────────────────┼─────────────────┐
           ▼                 ▼                  ▼
    ┌─────────────┐  ┌─────────────┐   ┌─────────────┐
    │  SaveManager │  │ CloudManager │   │  Cache/Memory│
    │  本地JSON    │  │  服务端API   │   │  运行时数据  │
    └─────────────┘  └─────────────┘   └─────────────┘
```

## 模块依赖关系

```
GameBootstrap (启动器)
├── ResourceManager (资源加载)
├── ConfigManager (配置读取)
├── SaveManager (存档读写)
├── GameStateMachine (状态管理)
│   ├── SplashState
│   ├── LoginState
│   ├── MainMenuState
│   ├── BattleState
│   │   ├── TowerManager
│   │   ├── EnemyManager
│   │   ├── WaveManager
│   │   ├── SkillManager
│   │   ├── EffectManager
│   │   └── UIManager
│   └── ResultState
├── MonetizationManager (变现)
├── SocialManager (社交)
├── RoguelikeModeManager (肉鸽)
├── TowerSkinSystem (皮肤)
├── BattleReplayManager (回放)
├── AchievementSystem (成就)
├── DailyMissionManager (日常)
├── EventManager (活动)
├── SeasonManager (赛季)
└── NetworkManager (网络)
```

## 通信模式

```
管理器之间通过事件总线通信：

高频事件（每帧）→ 直接引用调用
中频事件（每波/每阶段）→ 事件总线
低频事件（结算/购买）→ 事件总线 + 持久化

击杀敌人事件链示例：
1. Enemy.OnDeath()
2. WaveManager 检查波次清空
3. TowerManager 更新击杀计数/进化进度
4. GameManager 更新金币
5. AchievementSystem 检查成就触发
6. DailyMissionManager 更新任务进度
7. SeasonManager 更新通行证经验
8. UIManager 刷新界面
9. SaveManager 存阶段数据
```

## 文件清单

```
Assets/Scripts/
├── Core/ 启动入口/状态机/事件总线
├── Entities/ 实体类 (Bullet/Enemy/Tower/HealthBar)
├── Managers/ 管理器 (17个系统管理器)
├── Systems/ 子系统
│   ├── Artifact/ 神器系统
│   ├── Boss/ Boss机制
│   ├── Chip/ 芯片系统
│   ├── Director/ 动态难度
│   ├── Elemental/ 元素系统
│   ├── Equipment/ 装备系统 (5个文件)
│   ├── Hero/ 英雄系统 (2个文件)
│   ├── Inventory/ 背包系统
│   ├── LiveOps/ 运营分析
│   ├── Replay/ 战斗回放
│   ├── Retention/ 留存 (成就/日常/活动)
│   ├── Roguelike/ 肉鸽模式
│   ├── Skin/ 皮肤系统
│   ├── Talent/ 天赋系统 (2个文件)
│   └── Terrain/ 地形系统
├── Data/ 数据定义 (12个文件)
├── Editor/ 编辑器工具
├── Graphics/ 渲染相关
├── Tests/ 测试 (4个文件)
├── UI/ 界面 (9个文件)
└── Utils/ 工具类 (对象池/存档)
```

---

## 完整游戏循环

```
启动 → Splash → 登录 → 主菜单
                         │
          ┌──────────────┼──────────────┐
          ▼              ▼              ▼
      故事模式       Roguelike       无尽模式
          │              │              │
          ▼              ▼              ▼
      ┌───────────────────────────────────┐
      │         战斗场景                   │
      │  • 放置塔防    • 升级/卖塔        │
      │  • 释放技能    • 使用道具         │
      │  • 暂停/加速   • 战术支援         │
      └───────────────┬───────────────────┘
                      ▼
              ┌──────────────┐
              │  结算/奖励     │  → 金币/EXP/掉落/成就
              └──────┬───────┘
                     ▼
              ┌──────────────┐
              │  返回主菜单    │  → 商店购买/塔升级/皮肤更换
              └──────────────┘
```

## 外部循环

```
每日循环:  登录奖励 → 每日任务 → 每日挑战 → 通行证进度
每周循环:  周常任务 → 社区挑战 → 公会贡献 → 周排行榜
赛季循环:  赛季通行证 → 赛季排名 → 赛季奖励 → 段位重置
```

---

*文档版本: v1.0 | 2026-05-29*
