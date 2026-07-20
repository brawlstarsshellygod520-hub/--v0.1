using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class V01ProjectSetup
{
    const string ScenePath = "Assets/_Project/Scenes/Main.unity";

    [MenuItem("畢專/建立 v0.1 場景")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateLight();
        CreatePlatform();
        GameObject player = CreatePlayer();
        CreateCamera(player.transform);
        CreateItems();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AddGamepadAxes();
        AssetDatabase.SaveAssets();
        Debug.Log("V0.1_SETUP_PASS: Main scene, player, input, inventory and platform created.");
    }

    static void CreateLight()
    {
        GameObject go = new("Directional Light");
        Light light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.25f;
        go.transform.rotation = Quaternion.Euler(48f, -30f, 0f);
        RenderSettings.ambientLight = new Color(.46f, .5f, .58f);
    }

    static void CreatePlatform()
    {
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = "Platform";
        platform.transform.localScale = new Vector3(18f, .5f, 18f);
        platform.transform.position = new Vector3(0f, -.25f, 0f);
        platform.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Platform_Mat", new Color(.18f, .24f, .29f));
    }

    static GameObject CreatePlayer()
    {
        GameObject root = new("Player");
        root.transform.position = new Vector3(0f, .02f, 0f);
        CharacterController cc = root.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = .34f; cc.center = Vector3.up * .9f;
        root.AddComponent<PlayerController>();
        root.AddComponent<InventoryController>();

        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/HumanBody/source/body.fbx");
        if (modelPrefab)
        {
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, root.transform);
            model.name = "HumanBody_Model";
            Bounds bounds = CalculateBounds(model);
            float scale = bounds.size.y > .001f ? 1.75f / bounds.size.y : 1f;
            model.transform.localScale = Vector3.one * scale;
            bounds = CalculateBounds(model);
            model.transform.position += Vector3.up * -bounds.min.y;
        }
        return root;
    }

    static Bounds CalculateBounds(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds(go.transform.position, Vector3.one);
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    static void CreateCamera(Transform target)
    {
        GameObject go = new("Main Camera");
        go.tag = "MainCamera";
        Camera cam = go.AddComponent<Camera>();
        cam.nearClipPlane = .08f;
        go.AddComponent<AudioListener>();
        ThirdPersonCamera follow = go.AddComponent<ThirdPersonCamera>();
        follow.SetTarget(target);
    }

    static void CreateItems()
    {
        Color[] colors = { new(.9f,.3f,.2f), new(.2f,.65f,.95f), new(.95f,.75f,.15f) };
        string[] names = { "紅色方塊", "藍色方塊", "黃色方塊" };
        for (int i = 0; i < 3; i++)
        {
            GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = names[i];
            item.transform.position = new Vector3(-2f + i * 2f, .35f, 2.2f);
            item.transform.localScale = Vector3.one * .65f;
            item.GetComponent<Renderer>().sharedMaterial = MakeMaterial($"Item_{i}_Mat", colors[i]);
            WorldItem world = item.AddComponent<WorldItem>();
            world.displayName = names[i];
            world.description = "v0.1 測試物品：靠近後按 F 或手把 A 撿起／放下。";
        }
    }

    static Material MakeMaterial(string name, Color color)
    {
        string path = $"Assets/_Project/Materials/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    static void AddGamepadAxes()
    {
        SerializedObject input = new(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]);
        AddAxis(input, "Gamepad Look X", 4);
        AddAxis(input, "Gamepad Look Y", 5);
        input.ApplyModifiedProperties();
    }

    static void AddAxis(SerializedObject input, string name, int axis)
    {
        SerializedProperty axes = input.FindProperty("m_Axes");
        for (int i = 0; i < axes.arraySize; i++)
            if (axes.GetArrayElementAtIndex(i).FindPropertyRelative("m_Name").stringValue == name) return;
        axes.InsertArrayElementAtIndex(axes.arraySize);
        SerializedProperty a = axes.GetArrayElementAtIndex(axes.arraySize - 1);
        a.FindPropertyRelative("m_Name").stringValue = name;
        a.FindPropertyRelative("negativeButton").stringValue = "";
        a.FindPropertyRelative("positiveButton").stringValue = "";
        a.FindPropertyRelative("altNegativeButton").stringValue = "";
        a.FindPropertyRelative("altPositiveButton").stringValue = "";
        a.FindPropertyRelative("gravity").floatValue = 0f;
        a.FindPropertyRelative("dead").floatValue = .2f;
        a.FindPropertyRelative("sensitivity").floatValue = 1f;
        a.FindPropertyRelative("snap").boolValue = false;
        a.FindPropertyRelative("invert").boolValue = false;
        a.FindPropertyRelative("type").intValue = 2;
        a.FindPropertyRelative("axis").intValue = axis - 1;
        a.FindPropertyRelative("joyNum").intValue = 0;
    }
}
