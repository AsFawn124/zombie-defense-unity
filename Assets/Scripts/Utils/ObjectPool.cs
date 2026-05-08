using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 对象池 - 性能优化
/// </summary>
public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }
    
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
    }
    
    [Header("对象池配置")]
    public List<Pool> pools;
    
    private Dictionary<string, Queue<GameObject>> poolDictionary;
    
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
        
        InitializePools();
    }
    
    /// <summary>
    /// 初始化对象池
    /// </summary>
    private void InitializePools()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        
        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();
            
            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }
            
            poolDictionary.Add(pool.tag, objectPool);
        }
    }
    
    /// <summary>
    /// 从对象池获取对象
    /// </summary>
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"对象池中没有标签: {tag}");
            return null;
        }
        
        Queue<GameObject> pool = poolDictionary[tag];
        GameObject obj;
        
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        else
        {
            // 如果池子空了，创建新的
            Pool poolConfig = pools.Find(p => p.tag == tag);
            if (poolConfig != null)
            {
                obj = Instantiate(poolConfig.prefab);
            }
            else
            {
                return null;
            }
        }
        
        obj.SetActive(true);
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        
        // 调用对象初始化接口
        IPoolable poolable = obj.GetComponent<IPoolable>();
        poolable?.OnSpawnFromPool();
        
        return obj;
    }
    
    /// <summary>
    /// 归还对象到对象池
    /// </summary>
    public void ReturnToPool(string tag, GameObject obj)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"对象池中没有标签: {tag}");
            Destroy(obj);
            return;
        }
        
        // 调用对象清理接口
        IPoolable poolable = obj.GetComponent<IPoolable>();
        poolable?.OnReturnToPool();
        
        obj.SetActive(false);
        poolDictionary[tag].Enqueue(obj);
    }
}

/// <summary>
/// 可池化对象接口
/// </summary>
public interface IPoolable
{
    void OnSpawnFromPool();
    void OnReturnToPool();
}
