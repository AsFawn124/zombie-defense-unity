#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 创建占位资源工具
/// </summary>
public class CreatePlaceholderAssets : EditorWindow
{
    private int textureSize = 128;
    private Color towerColor = Color.blue;
    private Color enemyColor = Color.red;
    private Color baseColor = Color.green;
    private Color bulletColor = Color.yellow;
    
    [MenuItem("Tools/Create Placeholder Assets")]
    public static void ShowWindow()
    {
        GetWindow<CreatePlaceholderAssets>("Create Assets");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("创建占位资源", EditorStyles.boldLabel);
        
        textureSize = EditorGUILayout.IntField("Texture Size", textureSize);
        
        GUILayout.Label("颜色设置:", EditorStyles.boldLabel);
        towerColor = EditorGUILayout.ColorField("Tower Color", towerColor);
        enemyColor = EditorGUILayout.ColorField("Enemy Color", enemyColor);
        baseColor = EditorGUILayout.ColorField("Base Color", baseColor);
        bulletColor = EditorGUILayout.ColorField("Bullet Color", bulletColor);
        
        GUILayout.Space(20);
        
        if (GUILayout.Button("创建所有占位资源", GUILayout.Height(40)))
        {
            CreateAllAssets();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("仅创建精灵"))
        {
            CreateSprites();
        }
        
        if (GUILayout.Button("仅创建材质"))
        {
            CreateMaterials();
        }
    }
    
    private void CreateAllAssets()
    {
        CreateSprites();
        CreateMaterials();
        
        EditorUtility.DisplayDialog("完成", "所有占位资源已创建！", "确定");
    }
    
    private void CreateSprites()
    {
        string path = "Assets/Resources/Sprites/";
        System.IO.Directory.CreateDirectory(path);
        
        // 创建防御塔精灵
        CreateCircleSprite(path + "Tower_Base.png", textureSize, towerColor, "Tower");
        
        // 创建敌人精灵
        CreateCircleSprite(path + "Enemy_Normal.png", textureSize, enemyColor, "Z");
        CreateCircleSprite(path + "Enemy_Fast.png", (int)(textureSize * 0.8f), Color.yellow, "F");
        CreateCircleSprite(path + "Enemy_Tank.png", (int)(textureSize * 1.2f), Color.magenta, "T");
        CreateCircleSprite(path + "Enemy_Boss.png", textureSize * 2, Color.red, "BOSS");
        
        // 创建基地精灵
        CreateSquareSprite(path + "Base.png", textureSize, baseColor, "Base");
        
        // 创建子弹精灵
        CreateCircleSprite(path + "Bullet.png", 32, bulletColor, "");
        
        // 创建范围指示器
        CreateCircleOutlineSprite(path + "Range_Indicator.png", textureSize, Color.yellow);
        
        AssetDatabase.Refresh();
        Debug.Log("Sprites created at: " + path);
    }
    
    private void CreateMaterials()
    {
        string path = "Assets/Resources/Materials/";
        System.IO.Directory.CreateDirectory(path);
        
        // 创建发光材质
        CreateGlowMaterial(path + "Tower_Glow.mat", towerColor);
        CreateGlowMaterial(path + "Enemy_Glow.mat", enemyColor);
        CreateGlowMaterial(path + "Bullet_Glow.mat", bulletColor);
        
        AssetDatabase.Refresh();
        Debug.Log("Materials created at: " + path);
    }
    
    private void CreateCircleSprite(string path, int size, Color color, string text)
    {
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 2;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= radius)
                {
                    pixels[y * size + x] = color;
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        byte[] bytes = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, bytes);
        
        AssetDatabase.ImportAsset(path);
        
        // 配置导入设置
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }
        
        DestroyImmediate(texture);
    }
    
    private void CreateSquareSprite(string path, int size, Color color, string text)
    {
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        byte[] bytes = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, bytes);
        
        AssetDatabase.ImportAsset(path);
        
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }
        
        DestroyImmediate(texture);
    }
    
    private void CreateCircleOutlineSprite(string path, int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float outerRadius = size / 2f - 2;
        float innerRadius = outerRadius - 3;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= outerRadius && dist >= innerRadius)
                {
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, 0.3f);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        byte[] bytes = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, bytes);
        
        AssetDatabase.ImportAsset(path);
        
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }
        
        DestroyImmediate(texture);
    }
    
    private void CreateGlowMaterial(string path, Color color)
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.SetColor("_Color", color);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * 0.5f);
        
        AssetDatabase.CreateAsset(mat, path);
    }
}
#endif
