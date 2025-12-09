using UnityEngine;
using UnityEditor;

public class AutoPlaceEditor : EditorWindow
{
    string folderPath = "Assets/Low-Poly Medieval Market/Prefabs";
    Vector3 startPosition = Vector3.zero;
    Vector3 spacing = new Vector3(10, 0, 10);
    int itemsPerRow = 5;
    bool randomRotation = true;

    [MenuItem("Tools/Auto Place Medieval Village")]
    public static void ShowWindow()
    {
        GetWindow<AutoPlaceEditor>("Auto Placer");
    }

    void OnGUI()
    {
        GUILayout.Label("Village Auto Placement", EditorStyles.boldLabel);

        folderPath = EditorGUILayout.TextField("Prefab Folder", folderPath);
        startPosition = EditorGUILayout.Vector3Field("Start Position", startPosition);
        spacing = EditorGUILayout.Vector3Field("Spacing", spacing);
        itemsPerRow = EditorGUILayout.IntField("Items Per Row", itemsPerRow);
        randomRotation = EditorGUILayout.Toggle("Random Y Rotation", randomRotation);

        if (GUILayout.Button("Place Prefabs"))
        {
            PlacePrefabs();
        }

        if (GUILayout.Button("Clear All Placed Objects"))
        {
            ClearPlacedObjects();
        }
    }

    void PlacePrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

        if (guids.Length == 0)
        {
            Debug.LogError("No prefabs found in folder: " + folderPath);
            return;
        }

        // Parent object for organization
        GameObject villageParent = new GameObject("Medieval Village");
        Undo.RegisterCreatedObjectUndo(villageParent, "Create Village Parent");

        Vector3 pos = startPosition;
        int currentRow = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            // BUG FIX: Sadece bir kere instantiate et
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            obj.transform.position = pos;
            obj.transform.parent = villageParent.transform;

            // Random rotation
            if (randomRotation)
            {
                obj.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            }

            Undo.RegisterCreatedObjectUndo(obj, "Place Prefab");

            // Grid placement
            currentRow++;
            if (currentRow >= itemsPerRow)
            {
                pos.x = startPosition.x;
                pos.z += spacing.z;
                currentRow = 0;
            }
            else
            {
                pos.x += spacing.x;
            }
        }

        Debug.Log($"Village placed successfully! {guids.Length} objects created.");
    }

    void ClearPlacedObjects()
    {
        GameObject village = GameObject.Find("Medieval Village");
        if (village != null)
        {
            Undo.DestroyObjectImmediate(village);
            Debug.Log("Village cleared!");
        }
    }
}