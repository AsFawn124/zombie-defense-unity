# 📦 游戏资源清单

## 音效资源 (Audio/)

### BGM
| 文件名 | 用途 | 格式 | 建议时长 |
|--------|------|------|---------|
| bgm_main.mp3 | 主界面BGM | MP3 | 60-120s |
| bgm_battle.mp3 | 战斗BGM | MP3 | 60-120s |
| bgm_boss.mp3 | BOSS战BGM | MP3 | 60-120s |
| bgm_victory.mp3 | 胜利BGM | MP3 | 5-10s |
| bgm_defeat.mp3 | 失败BGM | MP3 | 5-10s |

### 音效
| 文件名 | 用途 | 格式 |
|--------|------|------|
| sfx_shoot.wav | 射击音效 | WAV |
| sfx_hit.wav | 受击音效 | WAV |
| sfx_explosion.wav | 爆炸音效 | WAV |
| sfx_button.wav | 按钮点击 | WAV |
| sfx_upgrade.wav | 升级音效 | WAV |
| sfx_coin.wav | 金币音效 | WAV |
| sfx_wave_start.wav | 波次开始 | WAV |
| sfx_skill_select.wav | 选择技能 | WAV |

## 图片资源 (Sprites/)

### 防御塔
| 文件名 | 尺寸 | 说明 |
|--------|------|------|
| Tower_Base.png | 128x128 | 基础炮台 |
| Tower_Sniper.png | 128x128 | 狙击塔 |
| Tower_Cannon.png | 128x128 | 加农炮 |
| Tower_Laser.png | 128x128 | 激光塔 |

### 敌人
| 文件名 | 尺寸 | 说明 |
|--------|------|------|
| Enemy_Normal.png | 64x64 | 普通僵尸 |
| Enemy_Fast.png | 64x64 | 快速僵尸 |
| Enemy_Tank.png | 96x96 | 坦克僵尸 |
| Enemy_Bomber.png | 64x64 | 自爆僵尸 |
| Enemy_Healer.png | 64x64 | 治疗僵尸 |
| Enemy_Split.png | 64x64 | 分裂僵尸 |
| Enemy_Elite.png | 80x80 | 精英僵尸 |
| Enemy_Boss.png | 128x128 | BOSS |

### 其他
| 文件名 | 尺寸 | 说明 |
|--------|------|------|
| Base.png | 128x128 | 基地 |
| Bullet.png | 16x16 | 子弹 |
| Range_Indicator.png | 128x128 | 范围指示器 |
| Path_Point.png | 32x32 | 路径点 |

### UI
| 文件名 | 尺寸 | 说明 |
|--------|------|------|
| UI_Panel.png | 可变 | 面板背景 |
| UI_Button.png | 可变 | 按钮背景 |
| UI_Slider_BG.png | 可变 | 滑块背景 |
| UI_Slider_Fill.png | 可变 | 滑块填充 |
| Icon_Gold.png | 64x64 | 金币图标 |
| Icon_Score.png | 64x64 | 分数图标 |
| Icon_Wave.png | 64x64 | 波次图标 |
| Icon_Health.png | 64x64 | 生命图标 |

### 技能图标
| 文件名 | 尺寸 | 说明 |
|--------|------|------|
| Skill_Damage.png | 128x128 | 火力强化 |
| Skill_Range.png | 128x128 | 射程提升 |
| Skill_FireRate.png | 128x128 | 攻速提升 |
| Skill_Pierce.png | 128x128 | 穿透 |
| Skill_Splash.png | 128x128 | 溅射 |
| Skill_Crit.png | 128x128 | 暴击 |
| Skill_MultiShot.png | 128x128 | 多重射击 |
| Skill_Slow.png | 128x128 | 减速 |

## 特效资源 (Effects/)

### 粒子特效
| 文件名 | 说明 |
|--------|------|
| MuzzleFlash.prefab | 枪口火焰 |
| Explosion.prefab | 爆炸效果 |
| HitEffect.prefab | 受击效果 |
| UpgradeEffect.prefab | 升级效果 |
| HealEffect.prefab | 治疗效果 |
| BuffEffect.prefab | 增益效果 |

## 材质资源 (Materials/)

| 文件名 | 说明 |
|--------|------|
| Tower_Glow.mat | 塔发光材质 |
| Enemy_Glow.mat | 敌人发光材质 |
| Bullet_Trail.mat | 子弹轨迹材质 |

## 免费资源网站推荐

### 音效
- https://opengameart.org/ (免费游戏资源)
- https://freesound.org/ (免费音效)
- https://www.zapsplat.com/ (免费音效)
- https://mixkit.co/free-sound-effects/ (免费音效)

### 图片/精灵
- https://opengameart.org/ (免费游戏美术)
- https://craftpix.net/ (免费游戏素材)
- https://itch.io/game-assets/free (免费游戏资源)

### 字体
- https://fonts.google.com/ (免费字体)
- https://www.dafont.com/ (免费字体)

## 占位资源生成

在Unity中打开菜单: `Tools → Create Placeholder Assets`
可以自动生成占位用的精灵和材质。

## 资源替换流程

1. 下载正式美术资源
2. 放入对应文件夹
3. 保持文件名一致
4. 在Unity中刷新
5. 调整Sprite Import设置
6. 测试运行
