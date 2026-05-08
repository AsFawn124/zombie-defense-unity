#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 快速场景设置工具
/// </summary>
public class QuickSceneSetup : EditorWindow
{
    private bool createMainMenu = true;
    private bool createGameScene = true;
    private bool createPrefabs = true;
    
    [MenuItem("Tools/Quick Scene Setup")]
    public static void ShowWindow()
    {
        GetWindow<QuickSceneSetup>("Quick Setup");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("快速场景设置", EditorStyles.boldLabel);
        
        createMainMenu = EditorGUILayout.Toggle("创建主菜单场景", createMainMenu);
        createGameScene = EditorGUILayout.Toggle("创建游戏场景", createGameScene);
        createPrefabs = EditorGUILayout.Toggle("创建预制体", createPrefabs);
        
        GUILayout.Space(20);
        
        if (GUILayout.Button("开始设置", GUILayout.Height(40)))
        {
            SetupScenes();
        }
    }
    
    private void SetupScenes()
    {
        if (createMainMenu)
        {
            CreateMainMenuScene();
        }
        
        if (createGameScene)
        {
            CreateGameScene();
        }
        
        if (createPrefabs)
        {
            CreatePrefabs();
        }
        
        EditorUtility.DisplayDialog("完成", "场景设置完成！", "确定");
    }
    
    private void CreateMainMenuScene()
    {
        // 创建新场景
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        
        // 创建Canvas
        GameObject canvas = new GameObject("Canvas");
        Canvas c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // 创建EventSystem
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        
        // 创建背景
        GameObject bg = CreateUIElement("Background", canvas.transform);
        UnityEngine.UI.Image bgImage = bg.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color32(44, 62, 80, 255);
        SetRectTransform(bg.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        
        // 创建标题
        GameObject title = CreateUIElement("Title", canvas.transform);
        UnityEngine.UI.Text titleText = title.AddComponent<UnityEngine.UI.Text>();
        titleText.text = "僵尸防线";
        titleText.fontSize = 120;
        titleText.color = new Color32(231, 76, 60, 255);
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        SetRectTransform(title.GetComponent<RectTransform>(), new Vector2(0, 600), new Vector2(0, 600), new Vector2(0.5f, 0.5f), new Vector2(800, 150));
        
        // 创建按钮
        CreateButton(canvas.transform, "StartButton", "开始游戏", new Vector2(0, 200), new Vector2(400, 120));
        CreateButton(canvas.transform, "SettingsButton", "设置", new Vector2(0, 50), new Vector2(400, 120));
        CreateButton(canvas.transform, "HelpButton", "帮助", new Vector2(0, -100), new Vector2(400, 120));
        CreateButton(canvas.transform, "ExitButton", "退出", new Vector2(0, -250), new Vector2(400, 120));
        
        // 创建管理器
        GameObject managers = new GameObject("Managers");
        managers.AddComponent<MainMenuUI>();
        managers.AddComponent<AudioManager>();
        managers.AddComponent<SaveManager>();
        
        // 保存场景
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/MainMenu.unity");
    }
    
    private void CreateGameScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        
        // 设置相机
        Camera.main.orthographic = true;
        Camera.main.orthographicSize = 5;
        Camera.main.backgroundColor = new Color32(26, 26, 46, 255);
        Camera.main.transform.position = new Vector3(0, 0, -10);
        
        // 创建Canvas
        GameObject canvas = new GameObject("GameCanvas");
        Canvas c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // 创建EventSystem
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        
        // 创建背景
        GameObject bg = new GameObject("Background");
        SpriteRenderer bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.color = new Color32(22, 33, 62, 255);
        bg.transform.localScale = new Vector3(20, 20, 1);
        bgSr.sortingOrder = -10;
        
        // 创建基地
        GameObject baseObj = new GameObject("Base");
        baseObj.tag = "Base";
        baseObj.AddComponent<BaseManager>();
        baseObj.AddComponent<CircleCollider2D>().isTrigger = true;
        baseObj.AddComponent<Rigidbody2D>().isKinematic = true;
        baseObj.transform.position = new Vector3(0, -3, 0);
        
        // 创建路径
        GameObject pathPoints = new GameObject("PathPoints");
        pathPoints.AddComponent<PathManager>();
        
        Vector3[] points = new Vector3[] {
            new Vector3(-7, 5, 0),
            new Vector3(-3, 5, 0),
            new Vector3(-3, 0, 0),
            new Vector3(3, 0, 0),
            new Vector3(3, 3, 0),
            new Vector3(7, 3, 0),
            new Vector3(7, -3, 0),
            new Vector3(0, -3, 0)
        };
        
        for (int i = 0; i < points.Length; i++)
        {
            GameObject point = new GameObject($"Point_{i}");
            point.transform.SetParent(pathPoints.transform);
            point.transform.position = points[i];
        }
        
        // 创建生成点
        GameObject spawnPoints = new GameObject("SpawnPoints");
        Vector3[] spawns = new Vector3[] {
            new Vector3(-7, 5, 0),
            new Vector3(7, 5, 0),
            new Vector3(-7, -5, 0),
            new Vector3(7, -5, 0)
        };
        
        for (int i = 0; i < spawns.Length; i++)
        {
            GameObject spawn = new GameObject($"Spawn_{i}");
            spawn.transform.SetParent(spawnPoints.transform);
            spawn.transform.position = spawns[i];
        }
        
        // 创建管理器
        GameObject managers = new GameObject("GameManagers");
        managers.AddComponent<GameManager>();
        managers.AddComponent<WaveManager>();
        managers.AddComponent<SkillManager>();
        managers.AddComponent<TowerManager>();
        managers.AddComponent<EffectManager>();
        managers.AddComponent<AudioManager>();
        
        // 保存场景
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/GameScene.unity");
    }
    
    private void CreatePrefabs()
    {
        string prefabPath = "Assets/Resources/Prefabs/";
        System.IO.Directory.CreateDirectory(prefabPath);
        
        // 创建防御塔预制体
        GameObject tower = new GameObject("Tower");
        tower.AddComponent<Tower>();
        tower.AddComponent<CircleCollider2D>().isTrigger = true;
        tower.AddComponent<AudioSource>();
        
        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(tower.transform);
        firePoint.transform.localPosition = new Vector3(0.5f, 0, 0);
        
        PrefabUtility.SaveAsPrefabAsset(tower, prefabPath + "Tower.prefab");
        DestroyImmediate(tower);
        
        // 创建敌人预制体
        GameObject enemy = new GameObject("Enemy");
        enemy.layer = LayerMask.NameToLayer("Enemy");
        enemy.AddComponent<Enemy>();
        enemy.AddComponent<CircleCollider2D>().isTrigger = true;
        enemy.AddComponent<Rigidbody2D>().isKinematic = true;
        
        PrefabUtility.SaveAsPrefabAsset(enemy, prefabPath + "Enemy.prefab");
        DestroyImmediate(enemy);
        
        // 创建子弹预制体
        GameObject bullet = new GameObject("Bullet");
        bullet.AddComponent<Bullet>();
        bullet.AddComponent<CircleCollider2D>().isTrigger = true;
        bullet.AddComponent<Rigidbody2D>().isKinematic = true;
        
        PrefabUtility.SaveAsPrefabAsset(bullet, prefabPath + "Bullet.prefab");
        DestroyImmediate(bullet);
        
        AssetDatabase.Refresh();
    }
    
    private GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.localScale = Vector3.one;
        return obj;
    }
    
    private void CreateButton(Transform parent, string name, string text, Vector2 position, Vector2 size)
    {
        GameObject button = CreateUIElement(name, parent);
        UnityEngine.UI.Button btn = button.AddComponent<UnityEngine.UI.Button>();
        UnityEngine.UI.Image img = button.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color32(52, 152, 219, 255);
        SetRectTransform(button.GetComponent<RectTransform>(), position, position, new Vector2(0.5f, 0.5f), size);
        
        GameObject textObj = CreateUIElement("Text", button.transform);
        UnityEngine.UI.Text txt = textObj.AddComponent<UnityEngine.UI.Text>();
        txt.text = text;
        txt.fontSize = 60;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        SetRectTransform(textObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }
    
    private void SetRectTransform(RectTransform rect, Vector2 anchoredMin, Vector2 anchoredMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        rect.anchorMin = anchoredMin;
        rect.anchorMax = anchoredMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;
    }
}
#endif
