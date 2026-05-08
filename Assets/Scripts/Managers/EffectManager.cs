using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 特效管理器
/// </summary>
public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }
    
    [Header("特效预制体")]
    public GameObject MuzzleFlashPrefab;
    public GameObject ExplosionPrefab;
    public GameObject HitEffectPrefab;
    public GameObject UpgradeEffectPrefab;
    public GameObject HealEffectPrefab;
    public GameObject BuffEffectPrefab;
    
    [Header("资源路径")]
    private const string EFFECT_PATH = "Effects/";
    
    private void Start()
    {
        // 从Resources加载特效预制体
        LoadEffectsFromResources();
    }
    
    /// <summary>
    /// 从Resources加载特效
    /// </summary>
    private void LoadEffectsFromResources()
    {
        if (MuzzleFlashPrefab == null)
            MuzzleFlashPrefab = Resources.Load<GameObject>(EFFECT_PATH + "MuzzleFlash");
        if (ExplosionPrefab == null)
            ExplosionPrefab = Resources.Load<GameObject>(EFFECT_PATH + "Explosion");
        if (HitEffectPrefab == null)
            HitEffectPrefab = Resources.Load<GameObject>(EFFECT_PATH + "Hit");
        if (UpgradeEffectPrefab == null)
            UpgradeEffectPrefab = Resources.Load<GameObject>(EFFECT_PATH + "Upgrade");
        if (HealEffectPrefab == null)
            HealEffectPrefab = Resources.Load<GameObject>(EFFECT_PATH + "Heal");
        if (BuffEffectPrefab == null)
            BuffEffectPrefab = Resources.Load<GameObject>(EFFECT_PATH + "Buff");
        
        Debug.Log("特效资源加载完成");
    }
    
    [Header("对象池")]
    public int PoolSize = 20;
    
    private Dictionary<string, Queue<GameObject>> effectPools = new Dictionary<string, Queue<GameObject>>();
    
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
    }
    
    /// <summary>
    /// 播放特效
    /// </summary>
    public void PlayEffect(string effectName, Vector3 position, Quaternion rotation)
    {
        GameObject effect = GetEffectFromPool(effectName);
        if (effect != null)
        {
            effect.transform.position = position;
            effect.transform.rotation = rotation;
            effect.SetActive(true);
            
            // 自动回收
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                float duration = ps.main.duration + ps.main.startLifetime.constantMax;
                StartCoroutine(ReturnEffectToPool(effectName, effect, duration));
            }
        }
    }
    
    /// <summary>
    /// 从对象池获取特效
    /// </summary>
    private GameObject GetEffectFromPool(string effectName)
    {
        if (!effectPools.ContainsKey(effectName))
        {
            effectPools[effectName] = new Queue<GameObject>();
        }
        
        if (effectPools[effectName].Count > 0)
        {
            return effectPools[effectName].Dequeue();
        }
        
        // 创建新的
        GameObject prefab = GetPrefabByName(effectName);
        if (prefab != null)
        {
            return Instantiate(prefab);
        }
        
        return null;
    }
    
    /// <summary>
    /// 归还特效到对象池
    /// </summary>
    private System.Collections.IEnumerator ReturnEffectToPool(string effectName, GameObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        effect.SetActive(false);
        
        if (!effectPools.ContainsKey(effectName))
        {
            effectPools[effectName] = new Queue<GameObject>();
        }
        
        effectPools[effectName].Enqueue(effect);
    }
    
    /// <summary>
    /// 根据名称获取预制体
    /// </summary>
    private GameObject GetPrefabByName(string name)
    {
        switch (name)
        {
            case "MuzzleFlash": return MuzzleFlashPrefab;
            case "Explosion": return ExplosionPrefab;
            case "Hit": return HitEffectPrefab;
            case "Upgrade": return UpgradeEffectPrefab;
            case "Heal": return HealEffectPrefab;
            case "Buff": return BuffEffectPrefab;
            default: return null;
        }
    }
    
    /// <summary>
    /// 屏幕震动
    /// </summary>
    public void ScreenShake(float duration, float magnitude)
    {
        CameraShake.Instance?.Shake(duration, magnitude);
    }
    
    /// <summary>
    /// 播放枪口火焰
    /// </summary>
    public void PlayMuzzleFlash(Vector3 position, Quaternion rotation)
    {
        PlayEffect("MuzzleFlash", position, rotation);
    }
    
    /// <summary>
    /// 播放爆炸
    /// </summary>
    public void PlayExplosion(Vector3 position)
    {
        PlayEffect("Explosion", position, Quaternion.identity);
        ScreenShake(0.3f, 0.3f);
    }
    
    /// <summary>
    /// 播放受击
    /// </summary>
    public void PlayHit(Vector3 position)
    {
        PlayEffect("Hit", position, Quaternion.identity);
    }
    
    /// <summary>
    /// 播放升级
    /// </summary>
    public void PlayUpgrade(Vector3 position)
    {
        PlayEffect("Upgrade", position, Quaternion.identity);
    }
}
