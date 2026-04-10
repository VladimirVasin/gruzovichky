using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class AddShadersToGraphicsSettings
{
    [MenuItem("Tools/Fix Shader Includes")]
    public static void Fix()
    {
        string[] shaderNames =
        {
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Unlit",
            "Sprites/Default",
        };

        var gs = GraphicsSettings.GetGraphicsSettings();
        var so = new SerializedObject(gs);
        var prop = so.FindProperty("m_AlwaysIncludedShaders");

        bool changed = false;
        foreach (string name in shaderNames)
        {
            Shader sh = Shader.Find(name);
            if (sh == null) { Debug.LogWarning("Not found: " + name); continue; }

            bool found = false;
            for (int i = 0; i < prop.arraySize; i++)
                if (prop.GetArrayElementAtIndex(i).objectReferenceValue == sh) { found = true; break; }

            if (!found)
            {
                prop.arraySize++;
                prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = sh;
                changed = true;
                Debug.Log("Added: " + name);
            }
        }

        if (changed) { so.ApplyModifiedProperties(); AssetDatabase.SaveAssets(); }
        Debug.Log("Done. Shader count: " + prop.arraySize);
    }
}
