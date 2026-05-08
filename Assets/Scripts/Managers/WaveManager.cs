using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 波次管理器 - 管理敌人波次生成
/// </summary>
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }
    
    [Header("生成设置")]
    public Transform[] SpawnPoints;           // 生成点
    public Transform TargetPoint;             // 目标点（基地）
    public GameObject[] EnemyPrefabs;         // 敌人类型预制体
    
    [Header("波次设置")]
    public float TimeBetweenWaves = 10f;      // 波次间隔
    public float SpawnInterval = 0.5f;        // 敌人生成间隔
    
    [Header("难度曲线")]
    public AnimationCurve HealthCurve;        // 血量增长曲线
    public AnimationCurve SpeedCurve;         // 速度增长曲线
    public AnimationCurve CountCurve;         // 数量增长曲线
    
    [Header("事件")]
    public UnityEngine.Events.UnityEvent OnWaveCompleted;
    
    // 运行时数据
    private bool isSpawning = false;
    private List<Enemy> activeEnemies = new List<Enemy>();
    private int enemiesToSpawn = 0;
    private int enemiesSpawned = 0;
    private int enemiesKilled = 0;
    
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
    
    private void Start()
    {
        // 订阅游戏事件
        GameManager.Instance.OnWaveStart += StartWave;
    }
    
    /// <summary>
    /// 开始波次
    /// </summary>
    private void StartWave()
    {
        if (isSpawning)
            return;
        
        int waveNumber = GameManager.Instance.CurrentWave;
        StartCoroutine(SpawnWave(waveNumber));
    }
    
    /// <summary>
    /// 生成波次协程
    /// </summary>
    private IEnumerator SpawnWave(int waveNumber)
    {
        isSpawning = true;
        
        // 计算波次参数
        float healthMultiplier = HealthCurve.Evaluate(waveNumber);
        float speedMultiplier = SpeedCurve.Evaluate(waveNumber);
        int enemyCount = Mathf.RoundToInt(CountCurve.Evaluate(waveNumber));
        
        enemiesToSpawn = enemyCount;
        enemiesSpawned = 0;
        enemiesKilled = 0;
        
        Debug.Log($"第 {waveNumber} 波: 生成 {enemyCount} 个敌人, 血量倍率: {healthMultiplier}, 速度倍率: {speedMultiplier}");
        
        // 生成敌人
        for (int i = 0; i < enemyCount; i++)
        {
            SpawnEnemy(healthMultiplier, speedMultiplier, waveNumber);
            enemiesSpawned++;
            
            yield return new WaitForSeconds(SpawnInterval);
        }
        
        isSpawning = false;
        
        // 等待所有敌人被消灭
        yield return new WaitUntil(() => activeEnemies.Count == 0);
        
        // 波次完成
        WaveCompleted();
    }
    
    /// <summary>
    /// 生成单个敌人
    /// </summary>
    private void SpawnEnemy(float healthMultiplier, float speedMultiplier, int waveNumber)
    {
        if (EnemyPrefabs.Length == 0 || SpawnPoints.Length == 0)
            return;
        
        // 选择敌人类型（根据波次解锁更高级敌人）
        int enemyIndex = GetEnemyTypeForWave(waveNumber);
        GameObject enemyPrefab = EnemyPrefabs[enemyIndex];
        
        // 选择生成点
        Transform spawnPoint = SpawnPoints[Random.Range(0, SpawnPoints.Length)];
        
        // 生成敌人
        GameObject enemyObj = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        
        if (enemy != null)
        {
            enemy.Initialize(healthMultiplier, speedMultiplier, waveNumber);
            enemy.OnDeath += OnEnemyDeath;
            enemy.OnReachEnd += OnEnemyReachEnd;
            activeEnemies.Add(enemy);
        }
    }
    
    /// <summary>
    /// 根据波次获取敌人类型
    /// </summary>
    private int GetEnemyTypeForWave(int waveNumber)
    {
        // 简单逻辑：波次越高，越可能生成高级敌人
        int maxIndex = Mathf.Min(waveNumber / 5 + 1, EnemyPrefabs.Length);
        return Random.Range(0, maxIndex);
    }
    
    /// <summary>
    /// 敌人死亡回调
    /// </summary>
    private void OnEnemyDeath(Enemy enemy)
    {
        activeEnemies.Remove(enemy);
        enemiesKilled++;
    }
    
    /// <summary>
    /// 敌人到达终点回调
    /// </summary>
    private void OnEnemyReachEnd(Enemy enemy)
    {
        activeEnemies.Remove(enemy);
    }
    
    /// <summary>
    /// 波次完成
    /// </summary>
    private void WaveCompleted()
    {
        Debug.Log($"第 {GameManager.Instance.CurrentWave} 波完成！击杀: {enemiesKilled}/{enemiesSpawned}");
        
        OnWaveCompleted?.Invoke();
        
        // 通知游戏管理器
        GameManager.Instance.EndWave();
    }
    
    /// <summary>
    /// 获取当前活跃敌人数量
    /// </summary>
    public int GetActiveEnemyCount()
    {
        return activeEnemies.Count;
    }
    
    /// <summary>
    /// 清除所有敌人
    /// </summary>
    public void ClearAllEnemies()
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }
        activeEnemies.Clear();
    }
}
