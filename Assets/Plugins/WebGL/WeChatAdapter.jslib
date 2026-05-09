mergeInto(LibraryManager.library, {
    // 微信登录
    WXLogin: function() {
        if (typeof wx !== 'undefined') {
            wx.login({
                success: function(res) {
                    if (res.code) {
                        console.log('微信登录成功，code:', res.code);
                        // 发送code到游戏逻辑
                        Module.ccall('OnWXLoginSuccess', 'null', ['string'], [res.code]);
                    }
                },
                fail: function(err) {
                    console.log('微信登录失败:', err);
                    Module.ccall('OnWXLoginFailed', 'null', ['string'], [JSON.stringify(err)]);
                }
            });
        } else {
            console.log('非微信环境，跳过登录');
            Module.ccall('OnWXLoginSuccess', 'null', ['string'], ['mock_code']);
        }
    },

    // 微信分享
    WXShare: function(title, desc, imageUrl) {
        if (typeof wx !== 'undefined') {
            wx.shareAppMessage({
                title: UTF8ToString(title),
                desc: UTF8ToString(desc),
                imageUrl: UTF8ToString(imageUrl),
                success: function() {
                    console.log('分享成功');
                    Module.ccall('OnWXShareSuccess', 'null', [], []);
                },
                fail: function(err) {
                    console.log('分享失败:', err);
                    Module.ccall('OnWXShareFailed', 'null', [], []);
                }
            });
        } else {
            console.log('非微信环境，模拟分享');
            Module.ccall('OnWXShareSuccess', 'null', [], []);
        }
    },

    // 显示激励视频广告
    WXShowRewardedAd: function(adUnitId) {
        if (typeof wx !== 'undefined') {
            var rewardedVideoAd = wx.createRewardedVideoAd({
                adUnitId: UTF8ToString(adUnitId)
            });
            
            rewardedVideoAd.onLoad(function() {
                console.log('激励视频广告加载成功');
            });
            
            rewardedVideoAd.onError(function(err) {
                console.log('激励视频广告错误:', err);
                Module.ccall('OnWXAdFailed', 'null', [], []);
            });
            
            rewardedVideoAd.onClose(function(res) {
                if (res && res.isEnded) {
                    console.log('激励视频播放完成');
                    Module.ccall('OnWXAdRewarded', 'null', [], []);
                } else {
                    console.log('激励视频提前关闭');
                    Module.ccall('OnWXAdFailed', 'null', [], []);
                }
            });
            
            rewardedVideoAd.show().catch(function(err) {
                rewardedVideoAd.load().then(function() {
                    rewardedVideoAd.show();
                });
            });
        } else {
            console.log('非微信环境，模拟广告奖励');
            setTimeout(function() {
                Module.ccall('OnWXAdRewarded', 'null', [], []);
            }, 1000);
        }
    },

    // 上报分数到排行榜
    WXReportScore: function(score) {
        if (typeof wx !== 'undefined') {
            wx.setUserCloudStorage({
                KVDataList: [{
                    key: 'score',
                    value: score.toString()
                }],
                success: function() {
                    console.log('分数上报成功');
                },
                fail: function(err) {
                    console.log('分数上报失败:', err);
                }
            });
        } else {
            console.log('非微信环境，跳过分数上报');
        }
    },

    // 显示排行榜
    WXShowRanking: function() {
        if (typeof wx !== 'undefined') {
            // 通知开放数据域显示排行榜
            var openDataContext = wx.getOpenDataContext();
            openDataContext.postMessage({
                action: 'showRanking'
            });
        } else {
            console.log('非微信环境，无法显示排行榜');
        }
    },

    // 检查更新
    WXCheckForUpdate: function() {
        if (typeof wx !== 'undefined') {
            var updateManager = wx.getUpdateManager();
            
            updateManager.onCheckForUpdate(function(res) {
                console.log('检查更新结果:', res.hasUpdate);
            });
            
            updateManager.onUpdateReady(function() {
                wx.showModal({
                    title: '更新提示',
                    content: '新版本已经准备好，是否重启应用？',
                    success: function(res) {
                        if (res.confirm) {
                            updateManager.applyUpdate();
                        }
                    }
                });
            });
            
            updateManager.onUpdateFailed(function() {
                console.log('新版本下载失败');
            });
        }
    },

    // 获取系统信息
    WXGetSystemInfo: function() {
        if (typeof wx !== 'undefined') {
            var info = wx.getSystemInfoSync();
            console.log('系统信息:', info);
            return JSON.stringify(info);
        }
        return '{}';
    },

    // 振动
    WXVibrate: function(short) {
        if (typeof wx !== 'undefined') {
            if (short) {
                wx.vibrateShort();
            } else {
                wx.vibrateLong();
            }
        }
    }
});
