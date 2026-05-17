using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 赛博朋克主题UI效果 - TASK-026: 主界面重设计
/// 提供霓虹灯按钮效果、数据流转场动画、全息投影提示、扫描线、故障效果
/// </summary>
[RequireComponent(typeof(Canvas))]
public class CyberpunkThemeUI : MonoBehaviour
{
    [Header("主题配置")]
    public CyberpunkThemeData ThemeData;

    [Header("=== 扫描线效果 ===")]
    public RawImage ScanlineOverlay;
    public Material ScanlineMaterial;

    [Header("=== 霓虹边框 ===")]
    public Image[] NeonBorders;
    [Range(1f, 20f)]
    public float NeonBorderWidth = 2f;

    [Header("=== 背景数据流 ===")]
    public RectTransform DataStreamContainer;
    public Text DataStreamPrefab;
    [Range(5, 50)]
    public int DataStreamCount = 15;

    [Header("=== 全息投影 ===")]
    public GameObject HoloProjectionPrefab;
    public Transform HoloProjectionAnchor;

    [Header("=== 故障效果 ===")]
    public Image GlitchOverlay;
    public Material GlitchMaterial;

    // 内部状态
    private List<Text> _dataStreams = new List<Text>();
    private List<float> _streamSpeeds = new List<float>();
    private List<float> _streamPositions = new List<float>();
    private Canvas _canvas;
    private float _glitchTimer;
    private bool _isGlitching;

    private void Awake()
    {
        _canvas = GetComponent<Canvas>();
    }

    private void Start()
    {
        if (ThemeData == null)
        {
            Debug.LogWarning("[CyberpunkThemeUI] 未配置主题数据，使用默认值");
            return;
        }

        SetupScanlines();
        SetupDataStreams();
        SetupNeonBorders();
        ApplyGlobalColors();
    }

    private void Update()
    {
        if (ThemeData == null) return;

        UpdateDataStreams();
        UpdateNeonBorders();
        UpdateGlitchEffect();
    }

    #region === 扫描线效果 (TASK-026: 主界面重设计) ===

    /// <summary>
    /// 设置扫描线覆盖层
    /// </summary>
    private void SetupScanlines()
    {
        if (!ThemeData.EnableScanlines) return;

        if (ScanlineOverlay == null)
        {
            // 自动创建扫描线覆盖层
            GameObject scanlineObj = new GameObject("ScanlineOverlay");
            scanlineObj.transform.SetParent(transform, false);
            ScanlineOverlay = scanlineObj.AddComponent<RawImage>();
            ScanlineOverlay.raycastTarget = false;

            // 生成扫描线纹理
            Texture2D scanTex = GenerateScanlineTexture();
            ScanlineOverlay.texture = scanTex;

            // 全屏拉伸
            RectTransform rt = scanlineObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        Color scanColor = Color.white;
        scanColor.a = ThemeData.ScanlineOpacity;
        ScanlineOverlay.color = scanColor;
    }

    /// <summary>
    /// 生成扫描线纹理
    /// </summary>
    private Texture2D GenerateScanlineTexture()
    {
        int width = 4;
        int height = Mathf.CeilToInt(ThemeData.ScanlineSpacing * 2);
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;

        Color clear = new Color(0, 0, 0, 0);
        Color line = new Color(0, 0, 0, 1);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isScanline = y % Mathf.CeilToInt(ThemeData.ScanlineSpacing * 2) < ThemeData.ScanlineSpacing;
                tex.SetPixel(x, y, isScanline ? line : clear);
            }
        }

        tex.Apply();
        return tex;
    }

    #endregion

    #region === 数据流效果 (TASK-026: 转场动画) ===

    /// <summary>
    /// 设置背景数据流
    /// </summary>
    private void SetupDataStreams()
    {
        if (!ThemeData.EnableDataStream) return;
        if (DataStreamPrefab == null) return;

        Transform container = DataStreamContainer != null ? DataStreamContainer : transform;
        RectTransform containerRect = container as RectTransform;
        float containerWidth = containerRect != null ? containerRect.rect.width : Screen.width;
        float containerHeight = containerRect != null ? containerRect.rect.height : Screen.height;

        for (int i = 0; i < DataStreamCount; i++)
        {
            Text stream = Instantiate(DataStreamPrefab, container);
            stream.raycastTarget = false;

            // 随机位置和字符
            RectTransform rt = stream.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(
                Random.Range(0f, containerWidth),
                Random.Range(-containerHeight, containerHeight)
            );

            stream.text = GenerateRandomStream(Random.Range(5, 20));
            stream.color = new Color(
                ThemeData.NeonGreen.r,
                ThemeData.NeonGreen.g,
                ThemeData.NeonGreen.b,
                Random.Range(0.1f, 0.4f)
            );
            stream.fontSize = Random.Range(10, 16);

            _dataStreams.Add(stream);
            _streamSpeeds.Add(Random.Range(ThemeData.DataStreamSpeed * 0.5f, ThemeData.DataStreamSpeed * 1.5f));
            _streamPositions.Add(rt.anchoredPosition.y);
        }
    }

    /// <summary>
    /// 更新数据流动画
    /// </summary>
    private void UpdateDataStreams()
    {
        if (_dataStreams.Count == 0) return;

        RectTransform containerRect = DataStreamContainer != null
            ? DataStreamContainer as RectTransform
            : transform as RectTransform;

        float containerHeight = containerRect != null ? containerRect.rect.height : Screen.height;

        for (int i = 0; i < _dataStreams.Count; i++)
        {
            if (_dataStreams[i] == null) continue;

            _streamPositions[i] -= _streamSpeeds[i] * Time.deltaTime;
            RectTransform rt = _dataStreams[i].GetComponent<RectTransform>();

            // 循环滚动
            if (_streamPositions[i] < -containerHeight * 0.6f)
            {
                _streamPositions[i] = containerHeight * 0.6f;
                _dataStreams[i].text = GenerateRandomStream(Random.Range(5, 20));
            }

            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, _streamPositions[i]);
        }
    }

    /// <summary>
    /// 生成随机数据流字符串
    /// </summary>
    private string GenerateRandomStream(int length)
    {
        if (ThemeData == null) return "";
        char[] chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            chars[i] = ThemeData.DataStreamChars[Random.Range(0, ThemeData.DataStreamChars.Length)];
        }
        return new string(chars);
    }

    #endregion

    #region === 霓虹边框效果 (TASK-026: 霓虹灯按钮效果) ===

    /// <summary>
    /// 设置霓虹边框
    /// </summary>
    private void SetupNeonBorders()
    {
        if (NeonBorders == null) return;

        foreach (var border in NeonBorders)
        {
            if (border == null) continue;
            border.color = ThemeData.ElectricBlue;
        }
    }

    /// <summary>
    /// 更新霓虹边框脉冲动画
    /// </summary>
    private void UpdateNeonBorders()
    {
        if (NeonBorders == null || ThemeData == null) return;

        Color pulseColor = ThemeData.GetPulsingNeonColor(ThemeData.ElectricBlue);

        foreach (var border in NeonBorders)
        {
            if (border == null) continue;
            border.color = Color.Lerp(border.color, pulseColor, Time.deltaTime * 3f);
        }
    }

    /// <summary>
    /// 将霓虹边框效果应用到指定Image
    /// </summary>
    public void ApplyNeonGlow(Image target, Color neonColor, float intensity = 1f)
    {
        if (target == null || ThemeData == null) return;

        // 使用Outline/Shadow组件模拟发光（需要实际组件支持）
        var outline = target.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = neonColor * intensity;
            outline.effectDistance = new Vector2(ThemeData.NeonGlowRange * 0.3f, ThemeData.NeonGlowRange * 0.3f);
        }

        // 增强边缘发光
        target.color = Color.Lerp(target.color, neonColor, 0.5f);
    }

    #endregion

    #region === 故障效果 (TASK-026: 特效) ===

    /// <summary>
    /// 更新故障效果
    /// </summary>
    private void UpdateGlitchEffect()
    {
        if (ThemeData == null || !ThemeData.EnableGlitch) return;

        if (_isGlitching)
        {
            _glitchTimer -= Time.deltaTime;
            if (_glitchTimer <= 0)
            {
                EndGlitch();
            }
            return;
        }

        // 随机触发故障
        if (Random.value < ThemeData.GlitchProbability * Time.deltaTime * 60f)
        {
            TriggerGlitch();
        }
    }

    /// <summary>
    /// 触发故障效果
    /// </summary>
    public void TriggerGlitch()
    {
        if (_isGlitching) return;
        StartCoroutine(GlitchRoutine());
    }

    private IEnumerator GlitchRoutine()
    {
        _isGlitching = true;
        _glitchTimer = ThemeData.GlitchDuration;

        // 随机偏移UI元素
        Vector3 originalPos = transform.localPosition;

        // 水平偏移
        transform.localPosition = originalPos + new Vector3(Random.Range(-10f, 10f), 0, 0);
        yield return new WaitForSeconds(0.03f);

        // 颜色偏移
        if (GlitchOverlay != null)
        {
            GlitchOverlay.gameObject.SetActive(true);
            GlitchOverlay.color = new Color(1, 0, 1, Random.Range(0.1f, 0.3f));
        }

        yield return new WaitForSeconds(0.03f);

        transform.localPosition = originalPos + new Vector3(Random.Range(-5f, 5f), Random.Range(-3f, 3f), 0);
        yield return new WaitForSeconds(0.04f);

        EndGlitch();
        transform.localPosition = originalPos;
    }

    private void EndGlitch()
    {
        _isGlitching = false;
        _glitchTimer = 0;

        if (GlitchOverlay != null)
        {
            GlitchOverlay.gameObject.SetActive(false);
        }
    }

    #endregion

    #region === 转场动画 (TASK-026: 数据流转场动画) ===

    /// <summary>
    /// 执行数据流穿梭转场（进入）
    /// </summary>
    public IEnumerator TransitionIn()
    {
        if (ThemeData == null) yield break;

        float elapsed = 0;
        float duration = ThemeData.TransitionDuration;

        // 设置初始状态：全屏霓虹色
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = gameObject.AddComponent<CanvasGroup>();
        }
        cg.alpha = 0;

        // 全息投影进场
        ShowHoloProjection("INITIALIZING...");

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 使用缓动函数
            float easedT = 1f - Mathf.Pow(1f - t, 3f); // easeOutCubic
            cg.alpha = easedT;

            yield return null;
        }

        cg.alpha = 1;
        HideHoloProjection();
    }

    /// <summary>
    /// 执行数据流穿梭转场（退出）
    /// </summary>
    public IEnumerator TransitionOut()
    {
        if (ThemeData == null) yield break;

        float elapsed = 0;
        float duration = ThemeData.TransitionDuration * 0.7f;

        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();

        ShowHoloProjection("DISCONNECTING...");

        // 短暂故障效果
        TriggerGlitch();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float easedT = Mathf.Pow(t, 2f); // easeInQuad
            cg.alpha = 1f - easedT;

            yield return null;
        }

        cg.alpha = 0;
        HideHoloProjection();
    }

    /// <summary>
    /// 显示全息投影提示
    /// </summary>
    public void ShowHoloProjection(string message)
    {
        if (HoloProjectionAnchor == null || HoloProjectionPrefab == null) return;

        GameObject projection = Instantiate(HoloProjectionPrefab, HoloProjectionAnchor);
        Text projectionText = projection.GetComponentInChildren<Text>();
        if (projectionText != null)
        {
            projectionText.text = message;
            StartCoroutine(AnimateHoloText(projectionText));
        }
    }

    private IEnumerator AnimateHoloText(Text text)
    {
        float elapsed = 0;
        while (text != null && elapsed < 2f)
        {
            elapsed += Time.deltaTime;
            text.color = ThemeData.GetPulsingNeonColor(ThemeData.NeonGreen, elapsed);
            yield return null;
        }
    }

    /// <summary>
    /// 隐藏全息投影
    /// </summary>
    public void HideHoloProjection()
    {
        if (HoloProjectionAnchor == null) return;
        foreach (Transform child in HoloProjectionAnchor)
        {
            Destroy(child.gameObject);
        }
    }

    #endregion

    #region === 全局颜色应用 ===

    /// <summary>
    /// 应用全局赛博朋克配色到UI元素
    /// </summary>
    private void ApplyGlobalColors()
    {
        if (ThemeData == null) return;

        // 查找所有Text组件并应用主题文字颜色
        Text[] allTexts = GetComponentsInChildren<Text>(true);
        foreach (var text in allTexts)
        {
            // 保留已单独设置的颜色
            if (text.color == Color.white || text.color == Color.black)
            {
                text.color = ThemeData.TextPrimary;
            }
        }

        // 查找所有Image组件并应用面板背景色
        Image[] allImages = GetComponentsInChildren<Image>(true);
        foreach (var image in allImages)
        {
            if (image.sprite == null && image.color.a > 0.9f && image.color != Color.black)
            {
                // 可能是纯色面板
                image.color = ThemeData.PanelBackground;
            }
        }

        // 更新相机背景色
        Camera cam = GetComponentInParent<Camera>();
        if (cam != null && cam.clearFlags == CameraClearFlags.SolidColor)
        {
            cam.backgroundColor = ThemeData.PureBlack;
        }
    }

    #endregion

    #region === 公共API ===

    /// <summary>
    /// 创建霓虹按钮
    /// </summary>
    public void StyleButtonAsNeon(Button button)
    {
        if (button == null || ThemeData == null) return;

        ColorBlock colors = button.colors;
        colors.normalColor = ThemeData.ButtonDefault;
        colors.highlightedColor = ThemeData.ButtonHover;
        colors.pressedColor = ThemeData.ButtonPressed;
        colors.selectedColor = ThemeData.ButtonHover;
        colors.disabledColor = ThemeData.TextSecondary;
        button.colors = colors;

        // 添加霓虹边框
        var image = button.GetComponent<Image>();
        if (image != null)
        {
            var outline = button.GetComponent<Outline>();
            if (outline == null)
            {
                outline = button.gameObject.AddComponent<Outline>();
            }
            outline.effectColor = ThemeData.ElectricBlue;
            outline.effectDistance = new Vector2(1, -1);
        }
    }

    /// <summary>
    /// 创建全息风格面板
    /// </summary>
    public void StylePanelAsHolo(Image panel)
    {
        if (panel == null || ThemeData == null) return;

        panel.color = ThemeData.PanelBackground;

        var outline = panel.GetComponent<Outline>();
        if (outline == null)
        {
            outline = panel.gameObject.AddComponent<Outline>();
        }
        outline.effectColor = ThemeData.ElectricBlue;
        outline.effectDistance = new Vector2(1, -1);
    }

    #endregion
}
