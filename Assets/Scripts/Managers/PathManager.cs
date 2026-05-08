using UnityEngine;

/// <summary>
/// 路径管理器 - 管理敌人移动路径
/// </summary>
public class PathManager : MonoBehaviour
{
    public static PathManager Instance { get; private set; }
    
    [Header("路径点")]
    public Transform[] PathPoints;
    
    [Header("可视化")]
    public bool ShowGizmos = true;
    public Color PathColor = Color.red;
    public float PointRadius = 0.2f;
    
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
    /// 获取路径点数组
    /// </summary>
    public Vector3[] GetPath()
    {
        if (PathPoints == null || PathPoints.Length == 0)
            return null;
        
        Vector3[] path = new Vector3[PathPoints.Length];
        for (int i = 0; i < PathPoints.Length; i++)
        {
            path[i] = PathPoints[i].position;
        }
        return path;
    }
    
    /// <summary>
    /// 获取路径长度
    /// </summary>
    public float GetPathLength()
    {
        if (PathPoints == null || PathPoints.Length < 2)
            return 0f;
        
        float length = 0f;
        for (int i = 0; i < PathPoints.Length - 1; i++)
        {
            length += Vector3.Distance(PathPoints[i].position, PathPoints[i + 1].position);
        }
        return length;
    }
    
    /// <summary>
    /// 在路径上获取最近点
    /// </summary>
    public Vector3 GetClosestPointOnPath(Vector3 position)
    {
        if (PathPoints == null || PathPoints.Length == 0)
            return position;
        
        Vector3 closestPoint = PathPoints[0].position;
        float closestDistance = float.MaxValue;
        
        for (int i = 0; i < PathPoints.Length - 1; i++)
        {
            Vector3 point = GetClosestPointOnLineSegment(
                PathPoints[i].position,
                PathPoints[i + 1].position,
                position
            );
            
            float distance = Vector3.Distance(point, position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPoint = point;
            }
        }
        
        return closestPoint;
    }
    
    /// <summary>
    /// 获取线段上最近点
    /// </summary>
    private Vector3 GetClosestPointOnLineSegment(Vector3 a, Vector3 b, Vector3 p)
    {
        Vector3 ab = b - a;
        Vector3 ap = p - a;
        
        float t = Vector3.Dot(ap, ab) / Vector3.Dot(ab, ab);
        t = Mathf.Clamp01(t);
        
        return Vector3.Lerp(a, b, t);
    }
    
    private void OnDrawGizmos()
    {
        if (!ShowGizmos || PathPoints == null || PathPoints.Length < 2)
            return;
        
        Gizmos.color = PathColor;
        
        // 绘制路径线
        for (int i = 0; i < PathPoints.Length - 1; i++)
        {
            if (PathPoints[i] != null && PathPoints[i + 1] != null)
            {
                Gizmos.DrawLine(PathPoints[i].position, PathPoints[i + 1].position);
            }
        }
        
        // 绘制路径点
        for (int i = 0; i < PathPoints.Length; i++)
        {
            if (PathPoints[i] != null)
            {
                Gizmos.DrawSphere(PathPoints[i].position, PointRadius);
            }
        }
    }
}
