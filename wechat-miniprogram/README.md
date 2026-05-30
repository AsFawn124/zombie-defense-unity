# 🧟 僵尸防线 - 微信小程序版

基于 HTML5 Canvas 版适配的微信小程序塔防射击游戏。

## 快速开始

### 1. 准备工作
- 安装 [微信开发者工具](https://developers.weixin.qq.com/miniprogram/dev/devtools/download.html)
- 注册微信小程序 AppID（测试可用测试号）

### 2. 导入项目
1. 打开微信开发者工具
2. 选择「导入项目」
3. 目录选择 `wechat-miniprogram/`
4. 填入你的 AppID（或选择「测试号」）
5. 点击「确定」

### 3. 预览
- 点击工具栏「预览」生成二维码
- 用微信扫码即可在手机上体验

## 项目结构

```
wechat-miniprogram/
├── app.js              # 小程序入口
├── app.json            # 小程序配置
├── app.wxss            # 全局样式
├── project.config.json # 开发者工具配置
└── pages/
    └── game/
        ├── game.js     # 游戏主逻辑 (2914行)
        ├── game.wxml   # 页面模板 (Canvas)
        ├── game.wxss   # 页面样式
        └── game.json   # 页面配置
```

## 适配说明

| 浏览器 API | 微信小程序 API |
|-----------|--------------|
| `document.getElementById` | `wx.createSelectorQuery()` |
| `localStorage` | `wx.getStorageSync/setStorageSync` |
| `AudioContext` | `wx.createWebAudioContext()` |
| `window.innerWidth/Height` | `wx.getSystemInfoSync()` |
| `mousemove/click/dblclick` | `bindtouchstart/move/end` |
| `contextmenu` (右键出售) | 长按 600ms |
| `keydown` (键盘快捷键) | 已移除 (移动端无键盘) |
| `requestAnimationFrame` | `canvas.requestAnimationFrame()` |

## 功能清单

- 🗺️ 3 个关卡：城市街区 / 沙漠基地 / 地狱堡垒
- 🏗️ 5 种防御塔 + 3 级升级
- 👤 4 位英雄可选
- 📦 物品掉落 + 主动技能
- 🔥 Roguelike 技能选择系统
- ♾️ 无尽模式
- 🏆 成就系统
- 🌧️ 天气特效
- 📳 屏幕震动
- 🎵 合成音效 (Web Audio)

## 注意事项

1. **AppID**: `project.config.json` 中的 `appid` 需要替换为你的真实 AppID
2. **音效**: 需要基础库 ≥ 2.19.0 才能使用 `wx.createWebAudioContext`
3. **Canvas**: 使用 Canvas 2D API (`type="2d"`)，需要基础库 ≥ 2.9.0
4. **性能**: 建议在 iPhone 8 / 骁龙 660 及以上设备运行，复杂波次可能有帧率下降

## 部署流程

1. 在微信公众平台注册小程序
2. 填写 AppID 到 `project.config.json`
3. 在开发者工具中上传代码
4. 提交审核 → 通过后发布

---

🛠️ 从原版 HTML5 Canvas 游戏适配 | 2026-05-30
