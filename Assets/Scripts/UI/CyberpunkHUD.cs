using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 赛博朋克战斗HUD效果 - TASK-027: 战斗界面重设计
/// 赛博朋克风格HUD、像素风格伤害数字、技能特效优化、受击反馈增强
/// </summary>
public class CyberpunkHUD : MonoBehaviour
{
    [Header("主题配置")]
    public CyberpunkThemeData ThemeData;

    [Header("=== 伤害数字系统 (像素风格) ===")]
    public GameObject PixelDamagePrefab;
    public Transform DamageTextContainer;
    [Range(0.5f, 2f)]
    public float DamageTextScale = 1f;
    [Range(0.5f, 3f)]
    public float CritScaleMultiplier = 1.5f;

    [Header("=== 受击反馈 ===")]
    public Image DamageVignette;
    [Range(0f, 0.5f)]
    public float VignetteMaxAlpha = 0.25f;
    [Range(0.1f, 2f)]
    public float VignetteFadeSpeed = 1.5f;

    [Header("=== 屏幕震动 ===")]
    public Transform CameraTransform;
    [Range(0f, 1f)]
    public float ShakeIntensity = 0.5f;
    [Range(0.05f, 0.5f)]
    public float ShakeDuration = 0.15f;

    [Header("=== 霓虹HUD元素 ===")]
    public Image TopBarBorder;
    public Image BottomBarBorder;
    public Image[] HudBorders;

    [Header("=== 低血量警告 ===")]
    public Image LowHealthOverlay;
    [Range(0f, 1f)]
    public float LowHealthThreshold = 0.3f;
    [Range(0.5f, 3f)]
    public float WarningPulseSpeed = 2f;

    [Header("=== 击杀特效 ===")]
    public GameObject KillStreakEffect;
    public Text KillStreakText;
    [Range(0f, 3f)]
    public float KillStreakDisplayTime = 2f;

    // 内部状态
    private float _currentVignetteAlpha;
    private float _targetVignetteAlpha;
    private bool _isShaking;
    private int _killStreakCount;
    private float _killStreakTimer;
    private Coroutine _shakeRoutine;
    private Vector3 _cameraOriginalPos;

    private void Start()
    {
        if (ThemeData == null)
        {
            Debug.LogWarning("[CyberpunkHUD] 未配置主题数据");
            return;
        }

        if (CameraTransform == null && Camera.main != null)
        {
            CameraTransform = Camera.main.transform;
        }
        _cameraOriginalPos = CameraTransform != null ? CameraTransform.localPosition : Vector3.zero;

        SetupHudBorders();
    }

    private void Update()
    {
        UpdateVignette();
        UpdateHudBorders();
        UpdateKillStreakDisplay();
    }

    #region === 像素风伤害数字 ===

    /// <summary>
    /// 显示伤害数字
    /// </summary>
    public void ShowDamageNumber(Vector3 worldPosition, float damage, bool isCrit = false, ElementType element = ElementType.None)
    {
        if (PixelDamagePrefab == null) return;

        // 世界坐标转屏幕坐标
        Vector3 screenPos = Camera.main != null
            ? Camera.main.WorldToScreenPoint(worldPosition)
            : worldPosition;

        // 添加随机偏移
        screenPos += new Vector3(Random.Range(-30f, 30f), Random.Range(-10f, 30f), 0);

        // 创建伤害数字
        GameObject damageObj = Instantiate(PixelDamagePrefab, DamageTextContainer ?? transform);
        RectTransform rt = damageObj.GetComponent<RectTransform>();
        rt.position = screenPos;

        // 设置文字
        Text damageText = damageObj.GetComponent<Text>();
        if (damageText != null)
        {
            damageText.text = Mathf.CeilToInt(damage).ToString();

            // 像素风格字体颜色
            if (isCrit)
            {
                damageText.color = ThemeData.DamageCrit;
                damageText.fontSize = Mathf.RoundToInt(damageText.fontSize * CritScaleMultiplier);
            }
            else if (element != ElementType.None)
            {
                damageText.color = ThemeData.GetElementColor(element);
            }
            else
            {
                damageText.color = ThemeData.DamageNormal;
            }
        }

        // 启动动画
        StartCoroutine(AnimateDamageNumber(damageObj, isCrit));
    }

    /// <summary>
    /// 伤害数字动画
    /// </summary>
    private IEnumerator AnimateDamageNumber(GameObject damageObj, bool isCrit)
    {
        if (damageObj == null) yield break;

        RectTransform rt = damageObj.GetComponent<RectTransform>();
        Text text = damageObj.GetComponent<Text>();
        CanvasGroup cg = damageObj.GetComponent<CanvasGroup>();
        if (cg == null) cg = damageObj.AddComponent<CanvasGroup>();

        Vector3 startPos = rt.position;
        float elapsed = 0;
        float duration = isCrit ? 1.2f : 0.8f;

        while (elapsed < duration && damageObj != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 上升动画
            rt.position = startPos + Vector3.up * (t * 60f);

            // 缩放动画（暴击有二次弹跳）
            float scale;
            if (isCrit)
            {
                scale = t < 0.3f ? Mathf.Lerp(1.5f, 1.8f, t / 0.3f)
                    : Mathf.Lerp(1.8f, 0.5f, (t - 0.3f) / 0.7f);
            }
            else
            {
                scale = Mathf.Lerp(1f, 0.6f, t);
            }
            rt.localScale = Vector3.one * scale * DamageTextScale;

            // 淡出
            cg.alpha = t < 0.3f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.3f) / 0.7f);

            yield return null;
        }

        if (damageObj != null)
        {
            Destroy(damageObj);
        }
    }

    #endregion

    #region === 受击反馈 ===

    /// <summary>
    /// 触发受击反馈（基地受伤时调用）
    /// </summary>
    public void TriggerHitFeedback(float damagePercent)
    {
        // 暗角效果
        _targetVignetteAlpha = Mathf.Min(VignetteMaxAlpha, damagePercent * 2f);

        // 屏幕震动
        TriggerShake(ShakeIntensity * Mathf.Clamp01(damagePercent * 2f));

        // 大伤害额外效果
        if (damagePercent > 0.15f)
        {
            // 短暂故障效果
            CyberpunkThemeUI themeUI = FindObjectOfType<CyberpunkThemeUI>();
            if (themeUI != null)
            {
                themeUI.TriggerGlitch();
            }
        }
    }

    /// <summary>
    /// 更新暗角效果
    /// </summary>
    private void UpdateVignette()
    {
        if (DamageVignette == null) return;

        _currentVignetteAlpha = Mathf.Lerp(_currentVignetteAlpha, _targetVignetteAlpha,
            Time.deltaTime * VignetteFadeSpeed);
        _targetVignetteAlpha = Mathf.Lerp(_targetVignetteAlpha, 0,
            Time.deltaTime * VignetteFadeSpeed * 0.5f);

        Color vignetteColor = DamageVignette.color;
        vignetteColor.a = _currentVignetteAlpha;
        DamageVignette.color = vignetteColor;
    }

    #endregion

    #region === 屏幕震动 ===

    /// <summary>
    /// 触发屏幕震动
    /// </summary>
    public void TriggerShake(float intensity = -1f)
    {
        if (intensity < 0) intensity = ShakeIntensity;
        if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
        _shakeRoutine = StartCoroutine(ShakeRoutine(intensity));
    }

    private IEnumerator ShakeRoutine(float intensity)
    {
        if (CameraTransform == null) yield break;

        _isShaking = true;
        float elapsed = 0;
        float duration = ShakeDuration;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float decay = 1f - (elapsed / duration);

            float x = Random.Range(-1f, 1f) * intensity * decay * 5f;
            float y = Random.Range(-1f, 1f) * intensity * decay * 5f;

            CameraTransform.localPosition = _cameraOriginalPos + new Vector3(x, y, 0);

            yield return null;
        }

        CameraTransform.localPosition = _cameraOriginalPos;
        _isShaking = false;
        _shakeRoutine = null;
    }

    #endregion

    #region === 霓虹HUD边框 ===

    /// <summary>
    /// 设置HUD边框
    /// </summary>
    private void SetupHudBorders()
    {
        if (ThemeData == null) return;

        // 顶部栏边框
        if (TopBarBorder != null)
        {
            TopBarBorder.color = ThemeData.ElectricBlue;
        }

        // 底部栏边框
        if (BottomBarBorder != null)
        {
            BottomBarBorder.color = ThemeData.ElectricBlue;
        }

        // 通用边框
        if (HudBorders != null)
        {
            foreach (var border in HudBorders)
            {
                if (border != null) border.color = ThemeData.ElectricBlue;
            }
        }
    }

    /// <summary>
    /// 更新HUD边框脉冲
    /// </summary>
    private void UpdateHudBorders()
    {
        if (ThemeData == null) return;

        Color pulseColor = ThemeData.GetPulsingNeonColor(ThemeData.ElectricBlue);

        UpdateBorderPulse(TopBarBorder, pulseColor);
        UpdateBorderPulse(BottomBarBorder, pulseColor);

        if (HudBorders != null)
        {
            foreach (var border in HudBorders)
            {
                UpdateBorderPulse(border, pulseColor);
            }
        }
    }

    private void UpdateBorderPulse(Image border, Color pulseColor)
    {
        if (border == null) return;
        border.color = Color.Lerp(border.color, pulseColor, Time.deltaTime * 2f);
    }

    #endregion

    #region === 低血量警告 ===

    /// <summary>
    /// 更新低血量警告（在GameHUD中每帧调用）
    /// </summary>
    public void UpdateLowHealthWarning(float healthPercent)
    {
        if (LowHealthOverlay == null || ThemeData == null) return;

        if (healthPercent <= LowHealthThreshold)
        {
            LowHealthOverlay.gameObject.SetActive(true);

            float pulse = Mathf.Abs(Mathf.Sin(Time.time * WarningPulseSpeed));
            float alpha = (1f - healthPercent / LowHealthThreshold) * 0.4f * pulse;

            Color overlayColor = LowHealthOverlay.color;
            overlayColor.a = alpha;
            LowHealthOverlay.color = overlayColor;
        }
        else
        {
            LowHealthOverlay.gameObject.SetActive(false);
        }
    }

    #endregion

    #region === 击杀连杀特效 ===

    /// <summary>
    /// 记录击杀（由外部调用）
    /// </summary>
    public void RegisterKill()
    {
        _killStreakCount++;
        _killStreakTimer = KillStreakDisplayTime;

        // 显示连杀提示
        if (_killStreakCount >= 3 && KillStreakEffect != null)
        {
            ShowKillStreakEffect();
        }

        if (KillStreakText != null)
        {
            KillStreakText.text = $"击杀 x{_killStreakCount}";
            KillStreakText.gameObject.SetActive(true);
            StartCoroutine(AnimateKillStreakText());
        }
    }

    /// <summary>
    /// 显示连杀特效
    /// </summary>
    private void ShowKillStreakEffect()
    {
        if (KillStreakEffect == null) return;

        GameObject effect = Instantiate(KillStreakEffect, transform);
        Destroy(effect, 2f);

        // 根据连杀数增强特效
        string streakMessage = _killStreakCount switch
        {
            >= 20 => "GODLIKE!",
            >= 15 => "UNSTOPPABLE!",
            >= 10 => "RAMPAGE!",
            >= 5 => "MEGA KILL!",
            >= 3 => "KILLING SPREE!",
            _ => ""
        };

        Text effectText = effect.GetComponentInChildren<Text>();
        if (effectText != null)
        {
            effectText.text = streakMessage;
        }
    }

    private IEnumerator AnimateKillStreakText()
    {
        if (KillStreakText == null) yield break;

        KillStreakText.transform.localScale = Vector3.one * 1.5f;
        float elapsed = 0;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            KillStreakText.transform.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.one, elapsed / duration);
            yield return null;
        }
    }

    /// <summary>
    /// 更新连杀显示计时器
    /// </summary>
    private void UpdateKillStreakDisplay()
    {
        if (_killStreakCount == 0) return;

        _killStreakTimer -= Time.deltaTime;
        if (_killStreakTimer <= 0)
        {
            _killStreakCount = 0;
            if (KillStreakText != null)
            {
                KillStreakText.gameObject.SetActive(false);
            }
        }
    }

    #endregion

    #region === 技能特效优化 ===

    /// <summary>
    /// 为技能创建赛博朋克风格特效
    /// </summary>
    public void PlaySkillEffect(Vector3 position, ElementType element, float radius = 1f)
    {
        StartCoroutine(SkillEffectRoutine(position, element, radius));
    }

    private IEnumerator SkillEffectRoutine(Vector3 position, ElementType element, float radius)
    {
        // 创建技能特效指示器
        GameObject indicator = new GameObject("SkillIndicator");
        indicator.transform.position = position;

        // 添加圆环渲染（简化版，实际需要粒子系统和材质）
        for (int i = 0; i < 6; i++)
        {
            GameObject line = new GameObject("EffectLine");
            line.transform.SetParent(indicator.transform);
            line.transform.localPosition = Vector3.zero;
            line.transform.localRotation = Quaternion.Euler(0, 0, i * 60f);

            // 实际项目中应使用LineRenderer或粒子系统
            Destroy(line, 0.3f);
        }

        yield return new WaitForSeconds(0.3f);
        Destroy(indicator);
    }

    #endregion

    #region === 公共API ===

    /// <summary>
    /// 为Battle UI应用主题样式
    /// </summary>
    public void ApplyThemeToBattleUI()
    {
        if (ThemeData == null) return;

        // 查找并样式化所有按钮
        Button[] allButtons = GetComponentsInChildren<Button>(true);
        foreach (var btn in allButtons)
        {
            var themeUI = FindObjectOfType<CyberpunkThemeUI>();
            if (themeUI != null)
            {
                themeUI.StyleButtonAsNeon(btn);
            }
        }

        // 样式化滑动条
        Slider[] allSliders = GetComponentsInChildren<Slider>(true);
        foreach (var slider in allSliders)
        {
            var fillImage = slider.fillRect?.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = ThemeData.ElectricBlue;
            }
        }
    }

    #endregion
}
