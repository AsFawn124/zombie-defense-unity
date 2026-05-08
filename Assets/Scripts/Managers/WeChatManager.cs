using UnityEngine;
using System;

/// <summary>
/// 微信SDK管理器 - 微信小游戏适配
/// </summary>
public class WeChatManager : MonoBehaviour
{
    public static WeChatManager Instance { get; private set; }
    
    [Header("微信配置")]
    public string AppId = "";
    public bool IsWeChatEnvironment = false;
    
    // 事件
    public Action OnLoginSuccess;
    public Action<string> OnLoginFailed;
    public Action OnShareSuccess;
    public Action OnShareFailed;
    public Action OnAdRewarded;
    public Action OnAdFailed;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // 检测是否在微信环境
        DetectWeChatEnvironment();
        
        // 初始化微信SDK
        if (IsWeChatEnvironment)
        {
            InitializeWeChatSDK();
        }
    }
    
    #region 环境检测
    
    /// <summary>
    /// 检测是否在微信环境
    /// </summary>
    private void DetectWeChatEnvironment()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
            // 在微信小游戏环境中
            IsWeChatEnvironment = true;
        #else
            IsWeChatEnvironment = false;
        #endif
        
        Debug.Log($"微信环境检测: {IsWeChatEnvironment}");
    }
    
    /// <summary>
    /// 初始化微信SDK
    /// </summary>
    private void InitializeWeChatSDK()
    {
        // TODO: 调用微信小游戏初始化API
        // WX.InitSDK(AppId, OnSDKInitialized);
        
        Debug.Log("初始化微信SDK");
    }
    
    #endregion
    
    #region 登录
    
    /// <summary>
    /// 微信登录
    /// </summary>
    public void Login()
    {
        if (!IsWeChatEnvironment)
        {
            Debug.Log("非微信环境，跳过登录");
            OnLoginSuccess?.Invoke();
            return;
        }
        
        // TODO: 调用微信登录API
        // WX.Login(OnLoginCallback);
        
        Debug.Log("调用微信登录");
    }
    
    /// <summary>
    /// 登录回调
    /// </summary>
    private void OnLoginCallback(string code)
    {
        if (!string.IsNullOrEmpty(code))
        {
            Debug.Log($"登录成功，code: {code}");
            OnLoginSuccess?.Invoke();
        }
        else
        {
            Debug.Log("登录失败");
            OnLoginFailed?.Invoke("登录失败");
        }
    }
    
    #endregion
    
    #region 分享
    
    /// <summary>
    /// 分享游戏
    /// </summary>
    public void Share(string title, string description, string imageUrl = null)
    {
        if (!IsWeChatEnvironment)
        {
            Debug.Log($"分享（模拟）: {title}");
            OnShareSuccess?.Invoke();
            return;
        }
        
        // TODO: 调用微信分享API
        // WX.ShareAppMessage(new ShareParams {
        //     title = title,
        //     desc = description,
        //     imageUrl = imageUrl,
        //     success = OnShareSuccess,
        //     fail = OnShareFailed
        // });
        
        Debug.Log($"调用微信分享: {title}");
    }
    
    /// <summary>
    /// 分享成绩
    /// </summary>
    public void ShareScore(int wave, int kills)
    {
        string title = $"我在僵尸防线存活了{wave}波！";
        string desc = $"击杀了{kills}个僵尸，你能超过我吗？";
        Share(title, desc);
    }
    
    #endregion
    
    #region 广告
    
    /// <summary>
    /// 播放激励视频广告
    /// </summary>
    public void ShowRewardedAd(Action onRewarded = null, Action onFailed = null)
    {
        if (!IsWeChatEnvironment)
        {
            Debug.Log("播放激励视频（模拟）");
            onRewarded?.Invoke();
            OnAdRewarded?.Invoke();
            return;
        }
        
        // 保存回调
        if (onRewarded != null)
        {
            OnAdRewarded = onRewarded;
        }
        if (onFailed != null)
        {
            OnAdFailed = onFailed;
        }
        
        // TODO: 调用微信广告API
        // WX.CreateRewardedVideoAd({
        //     adUnitId = "your-ad-unit-id",
        //     success = OnRewardedAdLoaded,
        //     fail = OnAdFailed
        // });
        
        Debug.Log("调用微信激励视频广告");
    }
    
    /// <summary>
    /// 播放插屏广告
    /// </summary>
    public void ShowInterstitialAd()
    {
        if (!IsWeChatEnvironment)
        {
            Debug.Log("播放插屏广告（模拟）");
            return;
        }
        
        // TODO: 调用微信插屏广告API
        // WX.CreateInterstitialAd({ adUnitId = "your-ad-unit-id" });
        
        Debug.Log("调用微信插屏广告");
    }
    
    /// <summary>
    /// 播放Banner广告
    /// </summary>
    public void ShowBannerAd()
    {
        if (!IsWeChatEnvironment)
        {
            Debug.Log("显示Banner广告（模拟）");
            return;
        }
        
        // TODO: 调用微信Banner广告API
        // WX.CreateBannerAd({ adUnitId = "your-ad-unit-id" });
        
        Debug.Log("调用微信Banner广告");
    }
    
    /// <summary>
    /// 隐藏Banner广告
    /// </summary>
    public void HideBannerAd()
    {
        if (!IsWeChatEnvironment)
        {
            Debug.Log("隐藏Banner广告（模拟）");
            return;
        }
        
        // TODO: 调用隐藏Banner广告API
        Debug.Log("隐藏微信Banner广告");
    }
    
    #endregion
    
    #region 排行榜
    
    /// <summary>
    /// 上报分数到微信排行榜
    /// </summary>
    public void ReportScore(int score)
    {
        if (!IsWeChatEnvironment)
        {
            Debug.Log($"上报分数（模拟）: {score}");
            return;
        }
        
        // TODO: 调用微信排行榜API
        // WX.SetUserCloudStorage({
        //     KVDataList = new[] {
        //         new KVData { key = "score", value = score.ToString() }
        //     }
        // });
        
        Debug.Log($"上报分数到微信: {score}");
    }
    
    /// <summary>
    /// 显示微信排行榜
    /// </summary>
    public void ShowRanking()
    {
        if (!IsWeChatEnvironment)
        {
            Debug.Log("显示排行榜（模拟）");
            return;
        }
        
        // TODO: 调用微信开放数据域显示排行榜
        // OpenDataContext.ShowRanking();
        
        Debug.Log("显示微信排行榜");
    }
    
    #endregion
    
    #region 其他功能
    
    /// <summary>
    /// 显示分享菜单
    /// </summary>
    public void ShowShareMenu()
    {
        if (!IsWeChatEnvironment)
        {
            Debug.Log("显示分享菜单（模拟）");
            return;
        }
        
        // TODO: 调用微信显示分享菜单API
        // WX.ShowShareMenu();
        
        Debug.Log("显示微信分享菜单");
    }
    
    /// <summary>
    /// 发起挑战
    /// </summary>
    public void ChallengeFriend()
    {
        if (!IsWeChatEnvironment)
        {
            Debug.Log("发起挑战（模拟）");
            return;
        }
        
        // TODO: 调用微信挑战API
        Debug.Log("发起微信挑战");
    }
    
    /// <summary>
    /// 订阅消息
    /// </summary>
    public void SubscribeMessage()
    {
        if (!IsWeChatEnvironment)
        {
            Debug.Log("订阅消息（模拟）");
            return;
        }
        
        // TODO: 调用微信订阅消息API
        Debug.Log("调用微信订阅消息");
    }
    
    /// <summary>
    /// 获取用户信息
    /// </summary>
    public void GetUserInfo()
    {
        if (!IsWeChatEnvironment)
        {
            Debug.Log("获取用户信息（模拟）");
            return;
        }
        
        // TODO: 调用微信获取用户信息API
        // WX.GetUserInfo();
        
        Debug.Log("获取微信用户信息");
    }
    
    /// <summary>
    /// 检查更新
    /// </summary>
    public void CheckForUpdate()
    {
        if (!IsWeChatEnvironment)
        {
            Debug.Log("检查更新（模拟）");
            return;
        }
        
        // TODO: 调用微信检查更新API
        // WX.UpdateManager.CheckForUpdate();
        
        Debug.Log("检查微信小游戏更新");
    }
    
    #endregion
}
