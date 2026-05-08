using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 伤害数字显示
/// </summary>
public class DamageText : MonoBehaviour
{
    [Header("UI元素")]
    public Text DamageTextUI;
    public Outline Outline;
    
    [Header("动画")]
    public float MoveSpeed = 2f;
    public float FadeSpeed = 1f;
    public float LifeTime = 1f;
    
    [Header("颜色配置")]
    public Color NormalColor = Color.white;
    public Color CriticalColor = Color.yellow;
    public Color PoisonColor = Color.green;
    public Color HealColor = Color.cyan;
    
    private float currentLifeTime;
    private Vector3 moveDirection;
    private CanvasGroup canvasGroup;
    
    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // 随机移动方向
        moveDirection = new Vector3(
            Random.Range(-0.5f, 0.5f),
            1f,
            0
        ).normalized;
    }
    
    /// <summary>
    /// 显示伤害
    /// </summary>
    public void Show(float damage, DamageType damageType)
    {
        currentLifeTime = LifeTime;
        
        if (DamageTextUI != null)
        {
            DamageTextUI.text = Mathf.RoundToInt(damage).ToString();
            
            // 根据伤害类型设置颜色
            switch (damageType)
            {
                case DamageType.Critical:
                    DamageTextUI.color = CriticalColor;
                    DamageTextUI.fontSize = 40;
                    transform.localScale = Vector3.one * 1.5f;
                    break;
                case DamageType.Poison:
                    DamageTextUI.color = PoisonColor;
                    break;
                default:
                    DamageTextUI.color = NormalColor;
                    break;
            }
        }
        
        // 确保朝向相机
        Transform cameraTransform = Camera.main?.transform;
        if (cameraTransform != null)
        {
            transform.rotation = cameraTransform.rotation;
        }
    }
    
    /// <summary>
    /// 显示治疗
    /// </summary>
    public void ShowHeal(float amount)
    {
        currentLifeTime = LifeTime;
        
        if (DamageTextUI != null)
        {
            DamageTextUI.text = $"+{Mathf.RoundToInt(amount)}";
            DamageTextUI.color = HealColor;
        }
    }
    
    private void Update()
    {
        // 向上移动
        transform.position += moveDirection * MoveSpeed * Time.deltaTime;
        
        // 淡出
        currentLifeTime -= Time.deltaTime;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = currentLifeTime / LifeTime;
        }
        
        // 销毁
        if (currentLifeTime <= 0)
        {
            Destroy(gameObject);
        }
    }
}
