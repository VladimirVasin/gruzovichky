using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ProceduralTreePrefabGenerator
{
    private const string RootFolder = "Assets/Resources/Nature";
    private const string TreeFolder = RootFolder + "/Trees";
    private const string TextureFolder = TreeFolder + "/Textures";
    private const string MaterialFolder = RootFolder + "/TreeMaterials";
    private const string MeshFolder = RootFolder + "/TreeMeshes";

    private static Material bark;
    private static Material darkBark;
    private static Material paleBark;
    private static Material leafA;
    private static Material leafB;
    private static Material leafDark;
    private static Material pineLeaf;
    private static bool hasQueuedGenerateIfMissing;

    static ProceduralTreePrefabGenerator()
    {
        QueueGenerateIfMissing();
    }

    private static void QueueGenerateIfMissing()
    {
        if (hasQueuedGenerateIfMissing)
        {
            return;
        }

        hasQueuedGenerateIfMissing = true;
        EditorApplication.delayCall += GenerateIfMissing;
    }

    private static void GenerateIfMissing()
    {
        hasQueuedGenerateIfMissing = false;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        EnsureFolders();
        string[] existingTreeModels = AssetDatabase.FindAssets("t:GameObject", new[] { TreeFolder });
        if (existingTreeModels.Length > 0)
        {
            return;
        }

        Generate();
    }

    [MenuItem("Tools/Gruzovichky/Generate Procedural Tree Prefabs")]
    public static void Generate()
    {
        EnsureFolders();
        CreateMaterials();

        SaveTreePrefab("tree_oak_broad", BuildOak);
        SaveTreePrefab("tree_lime_round", BuildRoundLime);
        SaveTreePrefab("tree_pine_stacked", BuildPine);
        SaveTreePrefab("tree_birch_light", BuildBirch);
        SaveTreePrefab("tree_poplar_tall", BuildPoplar);
        SaveTreePrefab("tree_maple_young", BuildMaple);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Generated procedural tree prefabs in Assets/Resources/Nature/Trees.");
    }

    public static void GenerateBatch()
    {
        Generate();
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder(RootFolder);
        EnsureFolder(TreeFolder);
        EnsureFolder(TextureFolder);
        EnsureFolder(MaterialFolder);
        EnsureFolder(MeshFolder);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent))
        {
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private static void CreateMaterials()
    {
        bark = CreateMaterial("tree_bark_warm", new Color(0.43f, 0.27f, 0.15f), 0.18f, "tree_bark_warm_lowpoly.png");
        darkBark = CreateMaterial("tree_bark_dark", new Color(0.30f, 0.19f, 0.11f), 0.16f, "tree_bark_dark_lowpoly.png");
        paleBark = CreateMaterial("tree_bark_pale", new Color(0.76f, 0.69f, 0.55f), 0.18f, "tree_bark_pale_birch_lowpoly.png");
        leafA = CreateMaterial("tree_leaf_summer", new Color(0.17f, 0.44f, 0.22f), 0.08f, "tree_leaf_summer_lowpoly.png");
        leafB = CreateMaterial("tree_leaf_fresh", new Color(0.26f, 0.54f, 0.30f), 0.08f, "tree_leaf_fresh_lowpoly.png");
        leafDark = CreateMaterial("tree_leaf_shadow", new Color(0.10f, 0.27f, 0.16f), 0.08f, "tree_leaf_shadow_lowpoly.png");
        pineLeaf = CreateMaterial("tree_leaf_pine", new Color(0.09f, 0.29f, 0.20f), 0.06f, "tree_leaf_pine_lowpoly.png");
    }

    private static Material CreateMaterial(string name, Color color, float smoothness, string textureFileName)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/{textureFileName}");
        if (texture != null)
        {
            material.mainTexture = texture;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void SaveTreePrefab(string name, System.Action<Transform> builder)
    {
        GameObject root = new(name);
        builder(root.transform);
        StripColliders(root);
        PrefabUtility.SaveAsPrefabAsset(root, $"{TreeFolder}/{name}.prefab");
        Object.DestroyImmediate(root);
    }

    private static void BuildOak(Transform root)
    {
        AddTaperedCylinder(root, "Trunk", new Vector3(0f, 0.42f, 0f), Vector3.up, 0.84f, 0.16f, 0.10f, 8, bark);
        AddTaperedCylinder(root, "Branch_L", new Vector3(-0.16f, 0.84f, 0f), new Vector3(-0.42f, 0.64f, 0.08f), 0.58f, 0.07f, 0.035f, 7, darkBark);
        AddTaperedCylinder(root, "Branch_R", new Vector3(0.16f, 0.86f, 0.02f), new Vector3(0.38f, 0.62f, -0.12f), 0.52f, 0.065f, 0.03f, 7, darkBark);
        AddBlob(root, "Crown_Main", new Vector3(0f, 1.34f, 0f), new Vector3(0.84f, 0.62f, 0.76f), 9, leafA);
        AddBlob(root, "Crown_Left", new Vector3(-0.38f, 1.18f, 0.06f), new Vector3(0.54f, 0.42f, 0.52f), 8, leafB);
        AddBlob(root, "Crown_Right", new Vector3(0.34f, 1.20f, -0.08f), new Vector3(0.58f, 0.44f, 0.50f), 8, leafDark);
        AddBlob(root, "Crown_Top", new Vector3(0.02f, 1.70f, -0.04f), new Vector3(0.46f, 0.34f, 0.44f), 8, leafB);
    }

    private static void BuildRoundLime(Transform root)
    {
        AddTaperedCylinder(root, "Trunk", new Vector3(0f, 0.38f, 0f), Vector3.up, 0.76f, 0.13f, 0.08f, 8, bark);
        AddBlob(root, "Canopy_Back", new Vector3(0.04f, 1.06f, 0.10f), new Vector3(0.76f, 0.54f, 0.74f), 10, leafDark);
        AddBlob(root, "Canopy_Main", new Vector3(0f, 1.22f, 0f), new Vector3(0.86f, 0.66f, 0.82f), 10, leafA);
        AddBlob(root, "Canopy_Front", new Vector3(-0.08f, 1.03f, -0.24f), new Vector3(0.58f, 0.38f, 0.46f), 8, leafB);
    }

    private static void BuildPine(Transform root)
    {
        AddTaperedCylinder(root, "Trunk", new Vector3(0f, 0.52f, 0f), Vector3.up, 1.04f, 0.10f, 0.055f, 8, darkBark);
        AddCone(root, "Needles_Lower", new Vector3(0f, 0.76f, 0f), 0.58f, 0.42f, 8, pineLeaf);
        AddCone(root, "Needles_Mid", new Vector3(0f, 1.10f, 0.02f), 0.45f, 0.44f, 8, leafDark);
        AddCone(root, "Needles_Top", new Vector3(0f, 1.43f, -0.01f), 0.30f, 0.40f, 8, pineLeaf);
    }

    private static void BuildBirch(Transform root)
    {
        AddTaperedCylinder(root, "Trunk", new Vector3(0f, 0.50f, 0f), new Vector3(0.08f, 1f, 0.02f), 1.0f, 0.095f, 0.06f, 8, paleBark);
        AddTaperedCylinder(root, "Branch_A", new Vector3(-0.10f, 0.84f, 0f), new Vector3(-0.34f, 0.68f, 0.12f), 0.45f, 0.04f, 0.02f, 6, paleBark);
        AddBlob(root, "Leaves_High", new Vector3(0.06f, 1.48f, 0.02f), new Vector3(0.52f, 0.42f, 0.48f), 8, leafB);
        AddBlob(root, "Leaves_Left", new Vector3(-0.32f, 1.20f, 0.10f), new Vector3(0.46f, 0.34f, 0.42f), 8, leafA);
        AddBlob(root, "Leaves_Right", new Vector3(0.34f, 1.12f, -0.04f), new Vector3(0.44f, 0.30f, 0.40f), 8, leafB);
    }

    private static void BuildPoplar(Transform root)
    {
        AddTaperedCylinder(root, "Trunk", new Vector3(0f, 0.60f, 0f), Vector3.up, 1.20f, 0.11f, 0.06f, 8, bark);
        AddBlob(root, "Column_Lower", new Vector3(0f, 0.98f, 0f), new Vector3(0.42f, 0.48f, 0.40f), 8, leafDark);
        AddBlob(root, "Column_Mid", new Vector3(0.02f, 1.34f, 0.02f), new Vector3(0.38f, 0.58f, 0.36f), 8, leafA);
        AddBlob(root, "Column_Top", new Vector3(0f, 1.78f, 0f), new Vector3(0.30f, 0.46f, 0.28f), 8, leafB);
    }

    private static void BuildMaple(Transform root)
    {
        AddTaperedCylinder(root, "Trunk", new Vector3(0f, 0.38f, 0f), Vector3.up, 0.76f, 0.12f, 0.075f, 8, bark);
        AddTaperedCylinder(root, "Fork_A", new Vector3(-0.07f, 0.74f, 0f), new Vector3(-0.22f, 0.82f, 0.06f), 0.38f, 0.055f, 0.026f, 6, bark);
        AddTaperedCylinder(root, "Fork_B", new Vector3(0.08f, 0.76f, 0f), new Vector3(0.24f, 0.80f, -0.07f), 0.36f, 0.052f, 0.024f, 6, bark);
        AddBlob(root, "Crown_Main", new Vector3(0f, 1.18f, 0f), new Vector3(0.70f, 0.52f, 0.64f), 9, leafA);
        AddBlob(root, "Crown_Top", new Vector3(0.04f, 1.50f, -0.02f), new Vector3(0.48f, 0.34f, 0.44f), 8, leafB);
        AddBlob(root, "Crown_Shade", new Vector3(-0.22f, 1.04f, 0.16f), new Vector3(0.42f, 0.30f, 0.38f), 8, leafDark);
    }

    private static void AddTaperedCylinder(Transform parent, string name, Vector3 center, Vector3 direction, float length, float bottomRadius, float topRadius, int sides, Material material)
    {
        GameObject obj = CreateMeshObject(parent, name, CreateTaperedCylinderMesh(name, length, bottomRadius, topRadius, sides), material);
        Vector3 dir = direction.sqrMagnitude <= 0.0001f ? Vector3.up : direction.normalized;
        obj.transform.localPosition = center;
        obj.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
    }

    private static void AddBlob(Transform parent, string name, Vector3 center, Vector3 scale, int sides, Material material)
    {
        GameObject obj = CreateMeshObject(parent, name, CreateBlobMesh(name, sides), material);
        obj.transform.localPosition = center;
        obj.transform.localScale = scale;
        obj.transform.localRotation = Quaternion.Euler(0f, Mathf.Abs(name.GetHashCode()) % 360, 0f);
    }

    private static void AddCone(Transform parent, string name, Vector3 baseCenter, float radius, float height, int sides, Material material)
    {
        GameObject obj = CreateMeshObject(parent, name, CreateConeMesh(name, radius, height, sides), material);
        obj.transform.localPosition = baseCenter;
    }

    private static GameObject CreateMeshObject(Transform parent, string name, Mesh mesh, Material material)
    {
        GameObject obj = new(name);
        obj.transform.SetParent(parent, false);
        MeshFilter filter = obj.AddComponent<MeshFilter>();
        MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
        filter.sharedMesh = mesh;
        renderer.sharedMaterial = material;
        return obj;
    }

    private static Mesh CreateTaperedCylinderMesh(string name, float length, float bottomRadius, float topRadius, int sides)
    {
        List<Vector3> vertices = new();
        List<int> triangles = new();
        for (int i = 0; i < sides; i++)
        {
            float a = i / (float)sides * Mathf.PI * 2f;
            vertices.Add(new Vector3(Mathf.Cos(a) * bottomRadius, -length * 0.5f, Mathf.Sin(a) * bottomRadius));
            vertices.Add(new Vector3(Mathf.Cos(a) * topRadius, length * 0.5f, Mathf.Sin(a) * topRadius));
        }

        int bottomCenter = vertices.Count;
        vertices.Add(new Vector3(0f, -length * 0.5f, 0f));
        int topCenter = vertices.Count;
        vertices.Add(new Vector3(0f, length * 0.5f, 0f));
        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;
            int b0 = i * 2;
            int t0 = b0 + 1;
            int b1 = next * 2;
            int t1 = b1 + 1;
            triangles.AddRange(new[] { b0, t0, b1, t0, t1, b1, bottomCenter, b1, b0, topCenter, t0, t1 });
        }

        return SaveMesh(name, vertices, triangles);
    }

    private static Mesh CreateConeMesh(string name, float radius, float height, int sides)
    {
        List<Vector3> vertices = new() { new Vector3(0f, height, 0f), Vector3.zero };
        List<int> triangles = new();
        for (int i = 0; i < sides; i++)
        {
            float a = i / (float)sides * Mathf.PI * 2f;
            vertices.Add(new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
        }

        for (int i = 0; i < sides; i++)
        {
            int current = 2 + i;
            int next = 2 + ((i + 1) % sides);
            triangles.AddRange(new[] { 0, current, next, 1, next, current });
        }

        return SaveMesh(name, vertices, triangles);
    }

    private static Mesh CreateBlobMesh(string name, int sides)
    {
        List<Vector3> vertices = new();
        List<int> triangles = new();
        int rings = 4;
        for (int r = 0; r <= rings; r++)
        {
            float v = r / (float)rings;
            float polar = v * Mathf.PI;
            float y = Mathf.Cos(polar) * 0.5f;
            float ringRadius = Mathf.Sin(polar) * 0.5f;
            for (int s = 0; s < sides; s++)
            {
                float a = s / (float)sides * Mathf.PI * 2f;
                float jitter = 0.92f + (((r * 17 + s * 13 + name.Length) % 7) * 0.025f);
                vertices.Add(new Vector3(Mathf.Cos(a) * ringRadius * jitter, y, Mathf.Sin(a) * ringRadius * jitter));
            }
        }

        for (int r = 0; r < rings; r++)
        {
            for (int s = 0; s < sides; s++)
            {
                int next = (s + 1) % sides;
                int a = r * sides + s;
                int b = r * sides + next;
                int c = (r + 1) * sides + s;
                int d = (r + 1) * sides + next;
                triangles.AddRange(new[] { a, c, b, b, c, d });
            }
        }

        return SaveMesh(name, vertices, triangles);
    }

    private static Mesh SaveMesh(string name, List<Vector3> vertices, List<int> triangles)
    {
        Mesh mesh = new()
        {
            name = $"proc_{name}"
        };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        string path = $"{MeshFolder}/{mesh.name}.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        EditorUtility.CopySerialized(mesh, existing);
        EditorUtility.SetDirty(existing);
        Object.DestroyImmediate(mesh);
        return existing;
    }

    private static void StripColliders(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Object.DestroyImmediate(colliders[i]);
        }
    }
}
