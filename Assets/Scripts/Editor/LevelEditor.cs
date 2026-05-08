#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 关卡编辑器
/// </summary>
public class LevelEditor : EditorWindow
{
    private Vector2 scrollPosition;
    private GameObject pathParent;
    private bool showPathTools = true;
    private bool showWaveTools = true;
    
    [MenuItem("Tools/Level Editor")]
    public static void ShowWindow()
    {
        GetWindow<LevelEditor>("Level Editor");
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Label("关卡编辑器", EditorStyles.boldLabel);
        
        // 路径工具
        showPathTools = EditorGUILayout.Foldout(showPathTools, "路径工具");
        if (showPathTools)
        {
            EditorGUILayout.BeginVertical("box");
            
            pathParent = EditorGUILayout.ObjectField("路径父物体", pathParent, typeof(GameObject), true) as GameObject;
            
            if (GUILayout.Button("创建路径点"))
            {
                CreatePathPoint();
            }
            
            if (GUILayout.Button("清除所有路径点"))
            {
                ClearPathPoints();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        // 波次工具
        showWaveTools = EditorGUILayout.Foldout(showWaveTools, "波次工具");
        if (showWaveTools)
        {
            EditorGUILayout.BeginVertical("box");
            
            if (GUILayout.Button("创建波次配置"))
            {
                CreateWaveConfig();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void CreatePathPoint()
    {
        if (pathParent == null)
        {
            pathParent = new GameObject("PathPoints");
        }
        
        GameObject point = new GameObject($"Point_{pathParent.transform.childCount}");
        point.transform.SetParent(pathParent.transform);
        point.transform.position = SceneView.lastActiveSceneView.camera.transform.position + SceneView.lastActiveSceneView.camera.transform.forward * 10;
        
        // 添加可视化组件
        point.AddComponent<PathPointVisualizer>();
        
        Selection.activeGameObject = point;
    }
    
    private void ClearPathPoints()
    {
        if (pathParent != null)
        {
            while (pathParent.transform.childCount > 0)
            {
                DestroyImmediate(pathParent.transform.GetChild(0).gameObject);
            }
        }
    }
    
    private void CreateWaveConfig()
    {
        // 创建波次配置文件
        string path = "Assets/Resources/WaveConfig.asset";
        WaveConfig config = ScriptableObject.CreateInstance<WaveConfig>();
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();
        
        EditorUtility.DisplayDialog("成功", $"波次配置已创建: {path}", "确定");
    }
}

/// <summary>
/// 路径点可视化
/// </summary>
public class PathPointVisualizer : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 0.3f);
        
        // 绘制连线
        int index = transform.GetSiblingIndex();
        if (index > 0)
        {
            Transform prevPoint = transform.parent.GetChild(index - 1);
            if (prevPoint != null)
            {
                Gizmos.DrawLine(prevPoint.position, transform.position);
            }
        }
    }
}

/// <summary>
/// 波次配置
/// </summary>
public class WaveConfig : ScriptableObject
{
    public WaveData[] Waves;
}

[System.Serializable]
public class WaveData
{
    public int WaveNumber;
    public EnemySpawnData[] Enemies;
    public float TimeBetweenSpawns;
    public float TimeBeforeNextWave;
}

[System.Serializable]
public class EnemySpawnData
{
    public string EnemyType;
    public int Count;
    public float HealthMultiplier;
    public float SpeedMultiplier;
}
#endif
