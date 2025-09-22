using UnityEngine;
using UnityEditor;

public class FBXMaterialFixer : EditorWindow
{
    [MenuItem("Tools/FBX/Material Fixer")]
    public static void ShowWindow()
    {
        GetWindow<FBXMaterialFixer>("FBX Material Fixer");
    }

    private void OnGUI()
    {
        GUILayout.Label("선택한 FBX만 머티리얼 Location 변경", EditorStyles.boldLabel);

        if (GUILayout.Button("선택한 FBX를 External로 변경"))
        {
            FixSelectedFBXMaterials();
        }
    }

    private void FixSelectedFBXMaterials()
    {
        Object[] selectedObjects = Selection.objects;
        int count = 0;

        foreach (Object obj in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;

            if (importer != null && importer.materialLocation != ModelImporterMaterialLocation.External)
            {
                importer.materialLocation = ModelImporterMaterialLocation.External;
                importer.SaveAndReimport();
                count++;
            }
        }

        Debug.Log($"✅ {count}개의 FBX 머티리얼 Location을 External로 변경 완료");
    }
}
