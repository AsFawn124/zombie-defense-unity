using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 血条组件
/// </summary>
public class HealthBar : MonoBehaviour
{
    [Header("UI元素")]
    public Slider HealthSlider;
    public Image FillImage;
    
    [Header("颜色设置")]
    public Color FullHealthColor = Color.green;
    public Color HalfHealthColor = Color.yellow;
    public Color LowHealthColor = Color.red;
    
    [Header("显示设置")]
    public bool AlwaysVisible = false;
    public float HideDelay = 2f;
    public CanvasGroup CanvasGroup;
    
    private float lastUpdateTime;
    private Camera mainCamera;
    
    private void Start()
    {
        mainCamera = Camera.main;
        
        if (CanvasGroup == null)
        {
            CanvasGroup = GetComponent<CanvasGroup>();
        }
        
        if (!AlwaysVisible && CanvasGroup != null)
        {
            CanvasGroup.alpha = 0f;
        }
    }
    
    private void LateUpdate()
    {
        // 血条朝向相机
        if (mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }
        
        // 自动隐藏
        if (!AlwaysVisible && CanvasGroup != null)
        {
            if (Time.time - lastUpdateTime > HideDelay && CanvasGroup.alpha > 0)
            {
                CanvasGroup.alpha = Mathf.Lerp(CanvasGroup.alpha, 0f, Time.deltaTime * 5f);
            }
        }
    }
    
    /// <summary>
    /// 更新血量显示
    /// </summary>
    public void UpdateHealth(float healthPercent)
    {
        if (HealthSlider != null)
        {
            HealthSlider.value = healthPercent;
        }
        
        // 更新颜色
        if (FillImage != null)
        {
            if (healthPercent > 0.5f)
            {
                FillImage.color = Color.Lerp(HalfHealthColor, FullHealthColor, (healthPercent - 0.5f) * 2f);
            }
            else
            {
                FillImage.color = Color.Lerp(LowHealthColor, HalfHealthColor, healthPercent * 2f);
            }
        }
        
        // 显示血条
        if (CanvasGroup != null)
        {
            CanvasGroup.alpha = 1f;
        }
        
        lastUpdateTime = Time.time;
    }
}
