#!/bin/bash

# 僵尸防线 - 资源下载脚本
# 使用免费开源资源

echo "=== 开始下载游戏资源 ==="

# 创建目录
mkdir -p Audio Sprites Textures Prefabs

# 音效资源 (使用免费音效库)
echo "下载音效资源..."

# BGM (使用OpenGameArt等免费资源)
cd Audio

# 主界面BGM
curl -L -o bgm_main.mp3 "https://opengameart.org/sites/default/files/8-bit%20Detective_0.mp3" || \
curl -L -o bgm_main.mp3 "https://files.freemusicarchive.org/storage-freemusicarchive-org/music/no_curator/Tours/Enthusiast/Tours_-_01_-_Enthusiast.mp3"

# 战斗BGM
curl -L -o bgm_battle.mp3 "https://opengameart.org/sites/default/files/DST-RailJet-LongSeamlessLoop.mp3" || \
curl -L -o bgm_battle.mp3 "https://files.freemusicarchive.org/storage-freemusicarchive-org/music/ccCommunity/Chad_Crouch/Arps/Chad_Crouch_-_Algorithms.mp3"

# BOSS战BGM
curl -L -o bgm_boss.mp3 "https://opengameart.org/sites/default/files/DST-TowerDefenseTheme_1.mp3" || \
curl -L -o bgm_boss.mp3 "https://files.freemusicarchive.org/storage-freemusicarchive-org/music/ccCommunity/Komiku/Captain_Glouglou/Komiku_-_04_-_Skate.mp3"

# 胜利BGM
curl -L -o bgm_victory.mp3 "https://opengameart.org/sites/default/files/Victory%20%28Victory%20Fanfare%29_0.mp3"

# 失败BGM
curl -L -o bgm_defeat.mp3 "https://opengameart.org/sites/default/files/Game%20Over%20%28Sad%20Piano%29_0.mp3"

# 音效
curl -L -o sfx_shoot.wav "https://opengameart.org/sites/default/files/laser_shoot.wav"
curl -L -o sfx_hit.wav "https://opengameart.org/sites/default/files/hit_0.wav"
curl -L -o sfx_explosion.wav "https://opengameart.org/sites/default/files/explosion_0.wav"
curl -L -o sfx_button.wav "https://opengameart.org/sites/default/files/click_0.wav"
curl -L -o sfx_upgrade.wav "https://opengameart.org/sites/default/files/upgrade_0.wav"
curl -L -o sfx_coin.wav "https://opengameart.org/sites/default/files/coin_0.wav"
curl -L -o sfx_wave_start.wav "https://opengameart.org/sites/default/files/alert.wav"

cd ..

echo "音效下载完成！"

# 图片资源 (使用占位图和免费素材)
echo "下载图片资源..."

cd Sprites

# 使用占位图片服务
curl -L -o tower_base.png "https://via.placeholder.com/128x128/3498db/ffffff?text=Tower"
curl -L -o enemy_normal.png "https://via.placeholder.com/64x64/e74c3c/ffffff?text=Zombie"
curl -L -o enemy_fast.png "https://via.placeholder.com/64x64/f39c12/ffffff?text=Fast"
curl -L -o enemy_tank.png "https://via.placeholder.com/96x96/9b59b6/ffffff?text=Tank"
curl -L -o enemy_boss.png "https://via.placeholder.com/128x128/c0392b/ffffff?text=BOSS"
curl -L -o bullet.png "https://via.placeholder.com/16x16/f1c40f/ffffff?text=B"
curl -L -o base.png "https://via.placeholder.com/128x128/2ecc71/ffffff?text=Base"

cd ..

echo "图片下载完成！"

echo "=== 资源下载完成 ==="
echo "注意：这些是占位资源，建议替换为正式美术资源"
