using UnityEngine;
using UnityEditor;
using UnityEngine.AI;

/// <summary>
/// Oyunu hızlı kurmak için Editor aracı
/// </summary>
public class QuickSetup : EditorWindow
{
    [MenuItem("Tools/Quick Game Setup")]
    public static void ShowWindow()
    {
        GetWindow<QuickSetup>("Quick Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Gossip Village - Quick Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("1. Create GameManager", GUILayout.Height(40)))
        {
            CreateGameManager();
        }

        if (GUILayout.Button("2. Create Player", GUILayout.Height(40)))
        {
            CreatePlayer();
        }

        if (GUILayout.Button("3. Create Ground + NavMesh", GUILayout.Height(40)))
        {
            CreateGroundAndNavMesh();
        }

        if (GUILayout.Button("4. Create Test NPCs (x3)", GUILayout.Height(40)))
        {
            CreateTestNPCs();
        }

        GUILayout.Space(20);
        GUILayout.Label("Her butona sırayla tıklayın!", EditorStyles.helpBox);
    }

    void CreateGameManager()
    {
        // GameManager varsa çık
        if (FindFirstObjectByType<GameManager>() != null)
        {
            Debug.LogWarning("GameManager zaten var!");
            return;
        }

        GameObject gm = new GameObject("GameManager");
        gm.AddComponent<GameManager>();
        gm.AddComponent<GossipSystem>();
        
        Undo.RegisterCreatedObjectUndo(gm, "Create GameManager");
        Selection.activeGameObject = gm;
        
        Debug.Log("✅ GameManager oluşturuldu!");
    }

    void CreatePlayer()
    {
        // Player varsa çık
        if (GameObject.FindGameObjectWithTag("Player") != null)
        {
            Debug.LogWarning("Player zaten var!");
            return;
        }

        // Player capsule
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.tag = "Player";
        player.transform.position = new Vector3(0, 1, 0);

        // Collider sil, CharacterController ekle
        DestroyImmediate(player.GetComponent<Collider>());
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.center = Vector3.zero;
        cc.radius = 0.5f;
        cc.height = 2f;

        // PlayerController
        player.AddComponent<PlayerController>();

        // Kamera
        GameObject cam = GameObject.FindGameObjectWithTag("MainCamera");
        if (cam != null)
        {
            cam.transform.parent = player.transform;
            cam.transform.localPosition = new Vector3(0, 1.6f, -3);
            cam.transform.localRotation = Quaternion.Euler(10, 0, 0);
        }

        Undo.RegisterCreatedObjectUndo(player, "Create Player");
        Selection.activeGameObject = player;

        Debug.Log("✅ Player oluşturuldu!");
    }

    void CreateGroundAndNavMesh()
    {
        // Zemin
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(5, 1, 5); // 50x50 birim
        
        // Static işaretle (NavMesh için)
        ground.isStatic = true;

        Undo.RegisterCreatedObjectUndo(ground, "Create Ground");

        Debug.Log("✅ Zemin oluşturuldu!");
        
        // NavMesh otomatik bake et
        try
        {
#pragma warning disable 618
            UnityEditor.AI.NavMeshBuilder.BuildNavMeshAsync();
#pragma warning restore 618
            Debug.Log("✅ NavMesh otomatik bake edildi!");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("⚠️ NavMesh otomatik bake edilemedi: " + e.Message);
            Debug.Log("Manuel bake edin: Window → AI → Navigation → Bake");
        }
    }

    void CreateTestNPCs()
    {
        // NPC layer yoksa oluştur
        CreateLayerIfNotExists("NPC");

        string[] npcNames = { "John", "Mary", "Tom" };
        NPCData.PersonalityType[] personalities = { 
            NPCData.PersonalityType.Gossiper, 
            NPCData.PersonalityType.Trustworthy, 
            NPCData.PersonalityType.Angry 
        };

        Vector3[] positions = {
            new Vector3(5, 1, 5),
            new Vector3(-5, 1, 5),
            new Vector3(0, 1, -5)
        };

        for (int i = 0; i < 3; i++)
        {
            GameObject npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npc.name = "NPC_" + npcNames[i];
            npc.transform.position = positions[i];
            npc.layer = LayerMask.NameToLayer("NPC");

            // Material (farklı renkler)
            Renderer renderer = npc.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = Random.ColorHSV();
            renderer.material = mat;

            // NavMeshAgent
            NavMeshAgent agent = npc.AddComponent<NavMeshAgent>();
            agent.radius = 0.5f;
            agent.height = 2f;
            agent.speed = 3.5f;

            // NPCController
            NPCController controller = npc.AddComponent<NPCController>();

            // NPCData
            NPCData data = new NPCData
            {
                npcName = npcNames[i],
                npcID = i + 1,
                personalityType = personalities[i],
                trustworthiness = Random.Range(30, 80),
                gossipLoving = personalities[i] == NPCData.PersonalityType.Gossiper ? 90 : Random.Range(20, 60),
                gullibility = Random.Range(40, 70),
                aggressiveness = personalities[i] == NPCData.PersonalityType.Angry ? 85 : Random.Range(20, 50),
                cowardice = Random.Range(20, 60),
                opinionScore = Random.Range(-20, 20),
                role = NPCData.NPCRole.Villager,
                socialInfluence = Random.Range(30, 70)
            };

            controller.npcData = data;
            controller.wanderRadius = 15f;

            Undo.RegisterCreatedObjectUndo(npc, "Create NPC");
        }

        Selection.activeGameObject = GameObject.Find("NPC_John");
        Debug.Log("✅ 3 Test NPC oluşturuldu!");
        Debug.Log("   John (Gossiper), Mary (Trustworthy), Tom (Angry)");
    }

    void CreateLayerIfNotExists(string layerName)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        bool layerExists = false;
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layerSP = layers.GetArrayElementAtIndex(i);
            if (layerSP.stringValue == layerName)
            {
                layerExists = true;
                break;
            }
        }

        if (!layerExists)
        {
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty layerSP = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layerSP.stringValue))
                {
                    layerSP.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    Debug.Log($"Layer '{layerName}' oluşturuldu!");
                    break;
                }
            }
        }
    }
}
