using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 基地管理器 - 玩家基地/终点
/// </summary>
public class BaseManager : MonoBehaviour
{
    public static BaseManager Instance { get; private set; }
    
    [Header("基地属性")]
    public int MaxHealth = 10;
    public int CurrentHealth { get; private set; }
    
    [Header("UI引用")]
    public Slider HealthSlider;
    public Text HealthText;
    
    [Header("特效")]
    public ParticleSystem DamageEffect;
    public AudioClip DamageSound;
    
    private AudioSource audioSource;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    private void Start()
    {
        CurrentHealth = MaxHealth;
        UpdateHealthUI();
    }
    
    /// <summary>
    /// 受到伤害
    /// </summary>
    public void TakeDamage(int damage)
    {
        CurrentHealth -= damage;
        
        // 播放特效
        if (DamageEffect != null)
        {
            DamageEffect.Play();
        }
        
        // 播放音效
        if (DamageSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(DamageSound);
        }
        
        // 屏幕震动效果
        CameraShake.Instance?.Shake(0.2f, 0.3f);
        
        UpdateHealthUI();
        
        Debug.Log($"基地受到伤害! 剩余生命值: {CurrentHealth}");
        
        // 检查游戏结束
        if (CurrentHealth <= 0)
        {
            GameOver();
        }
    }
    
    /// <summary>
    /// 恢复生命
    /// </summary>
    public void Heal(int amount)
    {
        CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
        UpdateHealthUI();
    }
    
    /// <summary>
    /// 更新血条UI
    /// </summary>
    private void UpdateHealthUI()
    {
        if (HealthSlider != null)
        {
            HealthSlider.value = (float)CurrentHealth / MaxHealth;
        }
        
        if (HealthText != null)
        {
            HealthText.text = $"{CurrentHealth}/{MaxHealth}";
        }
    }
    
    /// <summary>
    /// 游戏结束
    /// </summary>
    private void GameOver()
    {
        Debug.Log("基地被摧毁! 游戏结束!");
        GameManager.Instance.GameOver();
    }
    
    /// <summary>
    /// 重置基地
    /// </summary>
    public void ResetBase()
    {
        CurrentHealth = MaxHealth;
        UpdateHealthUI();
    }
}

/// <summary>
/// 相机震动效果
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }
    
    private Vector3 originalPosition;
    private bool isShaking = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
    
    /// <summary>
    /// 震动相机
    /// </summary>
    public void Shake(float duration, float magnitude)
    {
        if (!isShaking)
        {
            originalPosition = transform.localPosition;
            StartCoroutine(DoShake(duration, magnitude));
        }
    }
    
    private System.Collections.IEnumerator DoShake(float duration, float magnitude)
    {
        isShaking = true;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            
            transform.localPosition = originalPosition + new Vector3(x, y, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localPosition = originalPosition;
        isShaking = false;
    }
}
