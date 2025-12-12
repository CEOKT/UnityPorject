using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SceneOrganizer : EditorWindow
{
    [MenuItem("Tools/Scene Organizer")]
    public static void ShowWindow()
    {
        GetWindow<SceneOrganizer>("Scene Organizer");
    }

    void OnGUI()
    {
        GUILayout.Label("TAM DÜZELTME (UI + NPC + İSİM)", EditorStyles.boldLabel);
        
        // Test butonu ekledim, sahne kuruluyken basıp UI çalışıyor mu görebilirsin
        if (GUILayout.Button("TEST: UI Aç/Kapa", GUILayout.Height(30))) ToggleTestUI();
        
        GUILayout.Space(10);
        if (GUILayout.Button("Clear Everything", GUILayout.Height(40))) ClearEverything();
        GUILayout.Space(10);
        if (GUILayout.Button("Setup Complete Scene", GUILayout.Height(50))) SetupCompleteScene();
    }

    void ToggleTestUI()
    {
        DialogueManager dm = FindFirstObjectByType<DialogueManager>();
        if (dm != null && dm.dialoguePanel != null)
            dm.dialoguePanel.SetActive(!dm.dialoguePanel.activeSelf);
        else
            Debug.LogWarning("Önce sahneyi kurmalısınız.");
    }

    void ClearEverything()
    {
        if (!EditorUtility.DisplayDialog("Clear Scene", "Her şey silinip baştan kurulacak.", "Evet", "İptal")) return;

        GameObject mainCam = GameObject.FindGameObjectWithTag("MainCamera");
        
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        foreach (AudioListener listener in listeners)
        {
            if (mainCam == null || listener.gameObject != mainCam)
            {
                DestroyImmediate(listener);
            }
        }
        
        GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        foreach (GameObject obj in allObjects)
        {
            if (obj == null || obj == mainCam) continue;
            if (mainCam != null && obj.transform.IsChildOf(mainCam.transform)) continue;
            if (obj.transform.parent == null && obj.tag != "MainCamera") Undo.DestroyObjectImmediate(obj);
        }

        if (mainCam != null)
        {
            mainCam.transform.parent = null;
            mainCam.transform.position = new Vector3(0, 1, -10);
            mainCam.transform.rotation = Quaternion.identity;
            
            if (mainCam.GetComponent<AudioListener>() == null)
                mainCam.AddComponent<AudioListener>();
        }
        Debug.Log("✅ Sahne temizlendi.");
    }

    void SetupCompleteScene()
    {
        ClearEverything();
        CreateLayerIfNotExists("NPC");
        CreateLighting();
        CreateGround();
        CreateGameManager(); 
        CreatePlayer();
        CreateVillageCenter();
        CreateOrganizedNPCs();
        CreateDialogueUI(); 
        CreateAIClient();
        BakeNavMesh();
        
        // YENİ EKLENDİ: Player ile UI bağlantısını zorla kur
        ConnectPlayerToUI();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        Debug.Log($"✅ Sahne kurulumu tamamlandı! AudioListener sayısı: {listeners.Length}");
        
        DialogueManager dm = FindFirstObjectByType<DialogueManager>();
        if (dm != null && dm.dialoguePanel != null)
        {
            Debug.Log($"✅ DialogueManager hazır! Panel: {dm.dialoguePanel.name}");
        }
        else
        {
            Debug.LogError("❌ DialogueManager veya Panel bulunamadı!");
        }
    }

    // YENİ EKLENDİ: PlayerController'ın DialogueManager'ı bulmasını garanti eder
    void ConnectPlayerToUI()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        DialogueManager dm = FindFirstObjectByType<DialogueManager>();
        
        if (player != null && dm != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                // Reflection kullanarak private/public fark etmeksizin dialogueManager değişkenini bulup atıyoruz
                SerializedObject so = new SerializedObject(pc);
                SerializedProperty prop = so.GetIterator();
                while (prop.NextVisible(true))
                {
                    if (prop.propertyType == SerializedPropertyType.ObjectReference && prop.type.Contains("DialogueManager"))
                    {
                        prop.objectReferenceValue = dm;
                        so.ApplyModifiedProperties();
                        Debug.Log("✅ PlayerController -> DialogueManager bağlantısı yapıldı.");
                        break;
                    }
                }
            }
        }
    }

    // --- TEMEL SİSTEMLER ---

    void CreateAIClient()
    {
        if (!GameObject.Find("AIClient"))
        {
            GameObject aiObj = new GameObject("AIClient");
            AIClient client = aiObj.AddComponent<AIClient>();
            client.serverUrl = "http://127.0.0.1:1234/v1/chat/completions";
            client.modelName = "ibm/granite-3.2-8b";
            Undo.RegisterCreatedObjectUndo(aiObj, "Create AIClient");
        }
    }

    void CreateLighting()
    {
        GameObject light = new GameObject("Directional Light");
        Light l = light.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = new Color(1f, 0.95f, 0.8f);
        l.intensity = 1.2f;
        l.shadows = LightShadows.Soft;
        light.transform.rotation = Quaternion.Euler(50, -30, 0);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.7f, 0.8f, 1f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.7f, 0.8f, 0.9f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 50f;
        RenderSettings.fogEndDistance = 150f;
        Undo.RegisterCreatedObjectUndo(light, "Create Light");
    }

    void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0, -0.5f, 0);
        ground.transform.localScale = new Vector3(150, 1, 150);
        ground.isStatic = true;
        if (!ground.GetComponent<Collider>()) ground.AddComponent<BoxCollider>();

        Renderer renderer = ground.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.color = new Color(0.25f, 0.6f, 0.25f);
        renderer.material = mat;

        // Çimler
        GameObject grassParent = new GameObject("Grass");
        grassParent.transform.parent = ground.transform;
        string[] grassPaths = {
            "Assets/Low-Poly Medieval Market/Prefabs/Environment/grass_01.prefab",
            "Assets/Low-Poly Medieval Market/Prefabs/Environment/grass_02.prefab",
            "Assets/Low-Poly Medieval Market/Prefabs/Environment/grass_03.prefab"
        };

        for (int i = 0; i < 800; i++)
        {
            string path = grassPaths[Random.Range(0, grassPaths.Length)];
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab)
            {
                Vector3 pos = new Vector3(Random.Range(-50f, 50f), 0.01f, Random.Range(-50f, 50f));
                GameObject grass = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                grass.transform.position = pos;
                grass.transform.parent = grassParent.transform;
                grass.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                
                foreach(Renderer r in grass.GetComponentsInChildren<Renderer>()) {
                    if(r.sharedMaterial && r.sharedMaterial.shader.name == "Standard") {
                        Material newMat = new Material(shader);
                        newMat.color = new Color(0.3f, 0.6f, 0.2f);
                        if(r.sharedMaterial.mainTexture) newMat.mainTexture = r.sharedMaterial.mainTexture;
                        newMat.SetFloat("_Surface", 1.0f); 
                        newMat.SetFloat("_Cutoff", 0.5f);
                        r.material = newMat;
                    }
                }
            }
        }
        Undo.RegisterCreatedObjectUndo(ground, "Create Ground");
    }

    void CreateGameManager()
    {
        if (FindFirstObjectByType<GameManager>() != null) return;

        GameObject gmObj = new GameObject("GameManager");
        gmObj.AddComponent<GameManager>();
        gmObj.AddComponent<GossipSystem>();
        GossipGameManager ggm = gmObj.AddComponent<GossipGameManager>(); 

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if(canvas == null)
        {
            GameObject c = new GameObject("Canvas");
            canvas = c.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            c.AddComponent<CanvasScaler>();
            c.AddComponent<GraphicRaycaster>();
        }

        // HUD (Süre ve Skor)
        GameObject hud = new GameObject("HUD");
        hud.transform.SetParent(canvas.transform, false);
        RectTransform hudRT = hud.AddComponent<RectTransform>();
        hudRT.anchorMin = new Vector2(0, 1); hudRT.anchorMax = new Vector2(0.2f, 1);
        hudRT.pivot = new Vector2(0, 1); hudRT.anchoredPosition = new Vector2(20, -20);
        hudRT.sizeDelta = new Vector2(200, 100);

        GameObject timerObj = new GameObject("Timer");
        timerObj.transform.SetParent(hud.transform, false);
        TextMeshProUGUI timerTxt = timerObj.AddComponent<TextMeshProUGUI>();
        timerTxt.text = "60";
        timerTxt.fontSize = 40;
        timerTxt.color = Color.cyan;
        timerTxt.fontStyle = FontStyles.Bold;
        ggm.timerText = timerTxt;

        GameObject scoreObj = new GameObject("Score");
        scoreObj.transform.SetParent(hud.transform, false);
        TextMeshProUGUI scoreTxt = scoreObj.AddComponent<TextMeshProUGUI>();
        scoreTxt.text = "Skor: 0";
        scoreTxt.fontSize = 24;
        scoreTxt.color = Color.yellow;
        RectTransform scoreRect = scoreObj.GetComponent<RectTransform>();
        scoreRect.anchoredPosition = new Vector2(0, -50);
        ggm.scoreText = scoreTxt;

        GameObject roundObj = new GameObject("Round");
        roundObj.transform.SetParent(hud.transform, false);
        TextMeshProUGUI roundTxt = roundObj.AddComponent<TextMeshProUGUI>();
        roundTxt.text = "Round: 1";
        roundTxt.fontSize = 20;
        roundTxt.color = Color.white;
        RectTransform roundRect = roundObj.GetComponent<RectTransform>();
        roundRect.anchoredPosition = new Vector2(0, -80);
        ggm.roundText = roundTxt;

        // START PANELİ
        GameObject startPanel = new GameObject("StartPanel");
        startPanel.transform.SetParent(canvas.transform, false);
        Image startBg = startPanel.AddComponent<Image>();
        startBg.color = new Color(0,0,0,0.9f);
        RectTransform startRT = startPanel.GetComponent<RectTransform>();
        startRT.anchorMin = new Vector2(0.2f, 0.2f); startRT.anchorMax = new Vector2(0.8f, 0.8f);
        startRT.offsetMin = Vector2.zero; startRT.offsetMax = Vector2.zero;
        ggm.startPanel = startPanel;

        GameObject startBtn = new GameObject("StartButton");
        startBtn.transform.SetParent(startPanel.transform, false);
        Image startBtnImg = startBtn.AddComponent<Image>();
        startBtnImg.color = Color.green;
        Button btnStart = startBtn.AddComponent<Button>();
        RectTransform startBtnRT = startBtn.GetComponent<RectTransform>();
        startBtnRT.sizeDelta = new Vector2(200, 60);
        
        GameObject startBtnTxt = new GameObject("Text");
        startBtnTxt.transform.SetParent(startBtn.transform, false);
        TextMeshProUGUI btnTxt = startBtnTxt.AddComponent<TextMeshProUGUI>();
        btnTxt.text = "OYUNA BAŞLA";
        btnTxt.alignment = TextAlignmentOptions.Center;
        btnTxt.color = Color.black;
        btnTxt.fontSize = 24;
        RectTransform btnTxtRT = startBtnTxt.GetComponent<RectTransform>();
        btnTxtRT.anchorMin = Vector2.zero; btnTxtRT.anchorMax = Vector2.one;
        ggm.startButton = btnStart;

        GameObject infoTxtObj = new GameObject("InfoText");
        infoTxtObj.transform.SetParent(startPanel.transform, false);
        TextMeshProUGUI infoTxt = infoTxtObj.AddComponent<TextMeshProUGUI>();
        infoTxt.text = "Dedikoduyu kimin başlattığını bul!";
        infoTxt.alignment = TextAlignmentOptions.Center;
        infoTxt.fontSize = 30;
        infoTxt.color = Color.white;
        RectTransform infoRT = infoTxtObj.GetComponent<RectTransform>();
        infoRT.anchoredPosition = new Vector2(0, 100);
        ggm.startInfoText = infoTxt;

        // SUÇLAMA PANELİ
        GameObject accusePanel = new GameObject("AccusationPanel");
        accusePanel.transform.SetParent(canvas.transform, false);
        Image accuseBg = accusePanel.AddComponent<Image>();
        accuseBg.color = new Color(0.1f, 0.1f, 0.2f, 0.95f);
        RectTransform accuseRT = accusePanel.GetComponent<RectTransform>();
        accuseRT.anchorMin = new Vector2(0.1f, 0.1f); accuseRT.anchorMax = new Vector2(0.9f, 0.9f);
        accuseRT.offsetMin = Vector2.zero; accuseRT.offsetMax = Vector2.zero;
        ggm.accusationPanel = accusePanel;
        accusePanel.SetActive(false);

        GameObject instrObj = new GameObject("Instr");
        instrObj.transform.SetParent(accusePanel.transform, false);
        TextMeshProUGUI instr = instrObj.AddComponent<TextMeshProUGUI>();
        instr.text = "KİMİ SUÇLUYORSUN?";
        instr.alignment = TextAlignmentOptions.Center;
        instr.fontSize = 36;
        RectTransform instrRT = instrObj.GetComponent<RectTransform>();
        instrRT.anchorMin = new Vector2(0, 0.8f); instrRT.anchorMax = new Vector2(1, 1);
        ggm.instructionText = instr;

        ggm.npcButtons = new Button[6];
        ggm.npcNameTexts = new TextMeshProUGUI[6];
        
        for(int i=0; i<6; i++) {
            GameObject btn = new GameObject($"NPCBtn_{i}");
            btn.transform.SetParent(accusePanel.transform, false);
            Image bImg = btn.AddComponent<Image>();
            bImg.color = Color.gray;
            Button b = btn.AddComponent<Button>();
            
            RectTransform bRT = btn.GetComponent<RectTransform>();
            float x = (i % 3) * 300 - 300; 
            float y = (i / 3) * -150 + 50;
            bRT.anchoredPosition = new Vector2(x, y);
            bRT.sizeDelta = new Vector2(250, 100);

            GameObject tObj = new GameObject("Text");
            tObj.transform.SetParent(btn.transform, false);
            TextMeshProUGUI t = tObj.AddComponent<TextMeshProUGUI>();
            t.text = $"NPC {i}";
            t.alignment = TextAlignmentOptions.Center;
            t.fontSize = 24;
            t.color = Color.black;
            RectTransform tRT = tObj.GetComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;

            ggm.npcButtons[i] = b;
            ggm.npcNameTexts[i] = t;
        }

        // SONUÇ PANELİ
        GameObject resPanel = new GameObject("ResultPanel");
        resPanel.transform.SetParent(canvas.transform, false);
        Image resBg = resPanel.AddComponent<Image>();
        resBg.color = new Color(0,0,0,0.95f);
        RectTransform resRT = resPanel.GetComponent<RectTransform>();
        resRT.anchorMin = Vector2.zero; resRT.anchorMax = Vector2.one;
        resRT.offsetMin = Vector2.zero; resRT.offsetMax = Vector2.zero;
        ggm.resultPanel = resPanel;
        resPanel.SetActive(false);

        GameObject resTxtObj = new GameObject("ResultText");
        resTxtObj.transform.SetParent(resPanel.transform, false);
        TextMeshProUGUI resTxt = resTxtObj.AddComponent<TextMeshProUGUI>();
        resTxt.text = "SONUÇ";
        resTxt.alignment = TextAlignmentOptions.Center;
        resTxt.fontSize = 40;
        ggm.resultText = resTxt;

        GameObject nextBtn = new GameObject("NextBtn");
        nextBtn.transform.SetParent(resPanel.transform, false);
        Image nImg = nextBtn.AddComponent<Image>();
        nImg.color = Color.blue;
        Button nBtn = nextBtn.AddComponent<Button>();
        RectTransform nRT = nextBtn.GetComponent<RectTransform>();
        nRT.anchoredPosition = new Vector2(0, -100);
        nRT.sizeDelta = new Vector2(200, 60);
        
        GameObject nTxtObj = new GameObject("Text");
        nTxtObj.transform.SetParent(nextBtn.transform, false);
        TextMeshProUGUI nTxt = nTxtObj.AddComponent<TextMeshProUGUI>();
        nTxt.text = "DEVAM ET";
        nTxt.alignment = TextAlignmentOptions.Center;
        RectTransform nTxtRT = nTxtObj.GetComponent<RectTransform>();
        nTxtRT.anchorMin = Vector2.zero; nTxtRT.anchorMax = Vector2.one;
        ggm.nextRoundButton = nBtn;

        GameObject rstrtBtn = new GameObject("RestartBtn");
        rstrtBtn.transform.SetParent(resPanel.transform, false);
        Image rImg = rstrtBtn.AddComponent<Image>();
        rImg.color = Color.red;
        Button rBtn = rstrtBtn.AddComponent<Button>();
        RectTransform rRT = rstrtBtn.GetComponent<RectTransform>();
        rRT.anchoredPosition = new Vector2(0, -180);
        rRT.sizeDelta = new Vector2(200, 60);
        
        GameObject rTxtObj = new GameObject("Text");
        rTxtObj.transform.SetParent(rstrtBtn.transform, false);
        TextMeshProUGUI rTxt = rTxtObj.AddComponent<TextMeshProUGUI>();
        rTxt.text = "BAŞA DÖN";
        rTxt.alignment = TextAlignmentOptions.Center;
        RectTransform rTxtRT = rTxtObj.GetComponent<RectTransform>();
        rTxtRT.anchorMin = Vector2.zero; rTxtRT.anchorMax = Vector2.one;
        ggm.restartButton = rBtn;

        GameObject gossipInfo = new GameObject("GossipInfo");
        gossipInfo.transform.SetParent(canvas.transform, false);
        TextMeshProUGUI gInfo = gossipInfo.AddComponent<TextMeshProUGUI>();
        gInfo.text = "Dedikodu Detayı...";
        gInfo.fontSize = 20;
        gInfo.color = Color.white;
        RectTransform gRT = gossipInfo.GetComponent<RectTransform>();
        gRT.anchorMin = new Vector2(0.5f, 0.9f); gRT.anchorMax = new Vector2(0.5f, 0.9f);
        gRT.sizeDelta = new Vector2(600, 100);
        ggm.gossipText = gInfo;

        Undo.RegisterCreatedObjectUndo(gmObj, "Create Manager");
    }

    void CreatePlayer()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FREE/Pack_FREE_PartyCharacters/Prefabs/Character_040ae0.prefab");
        if (prefab)
        {
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.name = "Player";
            player.tag = "Player";
            player.transform.position = new Vector3(0, 2f, 0);
            
            ApplyCharacterAppearance(player, new Color(0.2f, 0.5f, 0.9f), -1);

            CharacterController cc = player.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 1f, 0);
            cc.radius = 0.3f;
            cc.height = 1.8f;

            PlayerController pc = player.AddComponent<PlayerController>();
            pc.npcLayer = 1 << LayerMask.NameToLayer("NPC");

            GameObject cam = GameObject.FindGameObjectWithTag("MainCamera") ?? new GameObject("Main Camera");
            cam.tag = "MainCamera";
            if(!cam.GetComponent<Camera>()) cam.AddComponent<Camera>();
            
            AudioListener[] allListeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            foreach (AudioListener listener in allListeners)
            {
                if (listener.gameObject != cam) DestroyImmediate(listener);
            }
            if(!cam.GetComponent<AudioListener>()) cam.AddComponent<AudioListener>();
            
            cam.transform.parent = null;
            ThirdPersonCamera tpCam = cam.GetComponent<ThirdPersonCamera>() ?? cam.AddComponent<ThirdPersonCamera>();
            tpCam.target = player.transform;
            tpCam.distance = 6f;
            tpCam.height = 3f;
            tpCam.enableMouseRotation = true;

            Undo.RegisterCreatedObjectUndo(player, "Create Player");
        }
    }

    void CreateVillageCenter()
    {
        GameObject villageParent = new GameObject("Medieval Village");
        string[] markets = {
            "Assets/Low-Poly Medieval Market/Prefabs/Bakery_market_with_food.prefab",
            "Assets/Low-Poly Medieval Market/Prefabs/Meat_market_with_objects.prefab",
            "Assets/Low-Poly Medieval Market/Prefabs/Vegetable_market_with_objects.prefab",
            "Assets/Low-Poly Medieval Market/Prefabs/Fish_market_with_sections.prefab",
            "Assets/Low-Poly Medieval Market/Prefabs/Weapon_Market_with_objects.prefab"
        };

        float radius = 12f;
        float angleStep = 180f / (markets.Length - 1);

        for (int i = 0; i < markets.Length; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(markets[i]);
            if (prefab)
            {
                float rad = (i * angleStep) * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(Mathf.Cos(rad) * radius, 0, Mathf.Sin(rad) * radius);
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.position = pos;
                instance.transform.LookAt(Vector3.zero);
                instance.transform.parent = villageParent.transform;
                instance.isStatic = true;
            }
        }
        
        CreateTrees(villageParent);
        AddCollidersRecursively(villageParent);
        Undo.RegisterCreatedObjectUndo(villageParent, "Create Village");
    }

    void AddCollidersRecursively(GameObject obj)
    {
        if (obj.GetComponent<MeshRenderer>() && !obj.GetComponent<Collider>())
        {
            MeshCollider mc = obj.AddComponent<MeshCollider>();
            mc.convex = true;
        }
        foreach (Transform child in obj.transform) AddCollidersRecursively(child.gameObject);
    }

    void CreateTrees(GameObject parent)
    {
        string[] trees = {
            "Assets/Low-Poly Medieval Market/Prefabs/Environment/tree_01.prefab",
            "Assets/Low-Poly Medieval Market/Prefabs/Environment/tree_02.prefab"
        };
        Vector3[] positions = { new Vector3(25, 0, 20), new Vector3(-25, 0, 20), new Vector3(0, 0, 30) };

        foreach (Vector3 pos in positions)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(trees[Random.Range(0, trees.Length)]);
            if (prefab)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.position = pos;
                instance.transform.parent = parent.transform;
            }
        }
    }

    void CreateOrganizedNPCs()
    {
        CreateLayerIfNotExists("NPC");
        GameObject npcParent = new GameObject("NPCs");

        string[] prefabs = {
            "Assets/FREE/Pack_FREE_PartyCharacters/Prefabs/Character_0413da.prefab",
            "Assets/FREE/Pack_FREE_PartyCharacters/Prefabs/Character_4c4e64.prefab",
            "Assets/FREE/Pack_FREE_PartyCharacters/Prefabs/Character_6dd688.prefab",
            "Assets/FREE/Pack_FREE_PartyCharacters/Prefabs/Character_8d9656.prefab",
            "Assets/FREE/Pack_FREE_PartyCharacters/Prefabs/Character_cbb54a.prefab",
            "Assets/FREE/Pack_FREE_PartyCharacters/Prefabs/Character_040ae0.prefab"
        };

        VillageCharacters.CharacterType[] types = {
            VillageCharacters.CharacterType.BakkalEmmi, VillageCharacters.CharacterType.EmineTeyze,
            VillageCharacters.CharacterType.CirakCeyhun, VillageCharacters.CharacterType.ImamHoca,
            VillageCharacters.CharacterType.Muhtar, VillageCharacters.CharacterType.FirinciHasan
        };

        Vector3[] positions = {
            new Vector3(8, 1, 0), new Vector3(-8, 1, 0), new Vector3(0, 1, 8),
            new Vector3(0, 1, -8), new Vector3(6, 1, 6), new Vector3(-6, 1, -6)
        };

        Color[] colors = {
            new Color(1f, 0.8f, 0.2f), new Color(0.9f, 0.4f, 0.7f), new Color(1f, 0.5f, 0f),
            new Color(0.8f, 0.2f, 0.2f), new Color(0.9f, 0.6f, 0.2f), new Color(0.1f, 0.1f, 0.1f)
        };

        for (int i = 0; i < types.Length; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabs[i]);
            if (!prefab) continue;

            GameObject npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            npc.name = "NPC_" + VillageCharacters.Characters[types[i]].name;
            npc.transform.position = positions[i];
            npc.transform.parent = npcParent.transform;
            npc.layer = LayerMask.NameToLayer("NPC");
            
            AudioListener npcListener = npc.GetComponentInChildren<AudioListener>();
            if (npcListener != null) DestroyImmediate(npcListener);

            ApplyCharacterAppearance(npc, colors[i], i);

            NavMeshAgent agent = npc.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f; agent.height = 1.8f; agent.speed = 2.5f;

            if (!npc.GetComponent<Collider>())
            {
                CapsuleCollider col = npc.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0, 0.9f, 0); col.radius = 0.4f; col.height = 1.8f;
            }

            NPCController controller = npc.AddComponent<NPCController>();
            Animator animator = npc.GetComponent<Animator>() ?? npc.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/FREE/Pack_FREE_PartyCharacters/Animations/char_AC.controller");
            controller.animator = animator;

            controller.npcData = new NPCData {
                npcName = VillageCharacters.Characters[types[i]].name,
                npcID = i + 1,
                villageCharacterType = types[i],
                personalityType = NPCData.PersonalityType.Trustworthy
            };
            controller.wanderRadius = 5f;

            CreateNPCNameLabel(npc, VillageCharacters.Characters[types[i]].name, colors[i]);
        }
        Undo.RegisterCreatedObjectUndo(npcParent, "Create NPCs");
    }

    void CreateNPCNameLabel(GameObject npc, string npcName, Color npcColor)
    {
        GameObject canvasObj = new GameObject("NameLabel");
        canvasObj.transform.SetParent(npc.transform, false);
        canvasObj.transform.localPosition = new Vector3(0, 2.6f, 0);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform rect = canvasObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(10f, 1f); 
        rect.localScale = Vector3.one * 0.005f; 

        canvasObj.AddComponent<BillboardLabel>();

        GameObject textObj = new GameObject("NameText");
        textObj.transform.SetParent(canvasObj.transform, false);
        
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = npcName;
        text.fontSize = 72;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        
        text.outlineWidth = 0.2f;
        text.outlineColor = Color.black; 

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    // --- YENİLENEN DIALOGUE UI: KONUŞMA BALONU TARZI (Raycast Blocking Fix Dahil) ---
    void CreateDialogueUI()
    {
        GameObject canvasObj = null;
        Canvas existingCanvas = FindFirstObjectByType<Canvas>();
        if (existingCanvas != null)
        {
            canvasObj = existingCanvas.gameObject;
        }
        else
        {
            canvasObj = new GameObject("Canvas");
            Canvas c = canvasObj.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler s = canvasObj.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        if (!FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>())
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        DialogueManager dm = canvasObj.GetComponent<DialogueManager>();
        if (dm == null) dm = canvasObj.AddComponent<DialogueManager>();

        Transform oldPanel = canvasObj.transform.Find("DialoguePanel");
        if (oldPanel != null) DestroyImmediate(oldPanel.gameObject);

        // 1. ANA KONTEYNER (Tüm ekranı kaplar ama görünmezdir, tıklamaları yönetir)
        GameObject mainPanel = new GameObject("DialoguePanel");
        mainPanel.transform.SetParent(canvasObj.transform, false);
        Image mainImg = mainPanel.AddComponent<Image>();
        mainImg.color = new Color(0, 0, 0, 0.0f); // TAMAMEN ŞEFFAF
        
        // 
        // KİLİT DÜZELTME: Bu panel ekranı kaplasa bile tıklamaları engellemeyecek!
        mainImg.raycastTarget = false; 

        RectTransform mainRect = mainPanel.GetComponent<RectTransform>();
        mainRect.anchorMin = Vector2.zero; mainRect.anchorMax = Vector2.one;
        mainRect.offsetMin = Vector2.zero; mainRect.offsetMax = Vector2.zero;

        // 2. KONUŞMA BALONU (NPC'nin sözleri burada görünecek)
        // Ekranın ortasının biraz üstünde yüzer.
        GameObject bubble = new GameObject("SpeechBubble");
        bubble.transform.SetParent(mainPanel.transform, false);
        Image bubbleImg = bubble.AddComponent<Image>();
        bubbleImg.color = Color.white; // BEYAZ BALON
        
        // Outline (Çerçeve) ekle
        Outline outline = bubble.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);

        RectTransform bubbleRT = bubble.GetComponent<RectTransform>();
        // BÜYÜK BALON - Ekranın üst yarısını kaplar
        bubbleRT.anchorMin = new Vector2(0.05f, 0.4f); 
        bubbleRT.anchorMax = new Vector2(0.95f, 0.95f);
        bubbleRT.offsetMin = Vector2.zero; bubbleRT.offsetMax = Vector2.zero;

        // BALON KUYRUĞU (Görsel efekt)
        GameObject tail = new GameObject("Tail");
        tail.transform.SetParent(bubble.transform, false);
        Image tailImg = tail.AddComponent<Image>();
        tailImg.color = Color.white;
        RectTransform tailRT = tail.GetComponent<RectTransform>();
        tailRT.sizeDelta = new Vector2(30, 30);
        tailRT.anchorMin = new Vector2(0.5f, 0); tailRT.anchorMax = new Vector2(0.5f, 0);
        tailRT.anchoredPosition = new Vector2(0, -15); // Balonun altından sarksın
        tailRT.localRotation = Quaternion.Euler(0, 0, 45); // Baklava dilimi şeklinde dönsün
        Outline tailOutline = tail.AddComponent<Outline>();
        tailOutline.effectColor = Color.black;
        tailOutline.effectDistance = new Vector2(1, -1);

        // NPC İSMİ (Balonun içinde, üstte) - BÜYÜK FONT
        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(bubble.transform, false);
        TextMeshProUGUI nameTxt = nameObj.AddComponent<TextMeshProUGUI>();
        nameTxt.fontSize = 48; // BÜYÜK
        nameTxt.fontStyle = FontStyles.Bold;
        nameTxt.color = new Color(0.8f, 0.4f, 0.1f); // Turuncu/Kahve tonu
        nameTxt.alignment = TextAlignmentOptions.Center;
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.02f, 0.88f);
        nameRect.anchorMax = new Vector2(0.98f, 0.98f);
        dm.npcNameText = nameTxt;

        // MESAJ METNİ (Balonun ortasında) - BÜYÜK ve OKUNABİLİR
        GameObject msgObj = new GameObject("Message");
        msgObj.transform.SetParent(bubble.transform, false);
        TextMeshProUGUI msgTxt = msgObj.AddComponent<TextMeshProUGUI>();
        msgTxt.fontSize = 36; // BÜYÜK FONT
        msgTxt.color = Color.black; // SİYAH YAZI
        msgTxt.textWrappingMode = TextWrappingModes.Normal;
        msgTxt.overflowMode = TextOverflowModes.Overflow;
        msgTxt.alignment = TextAlignmentOptions.TopLeft;
        msgTxt.lineSpacing = 10; // Satır aralığı
        RectTransform msgRect = msgObj.GetComponent<RectTransform>();
        msgRect.anchorMin = new Vector2(0.03f, 0.05f);
        msgRect.anchorMax = new Vector2(0.97f, 0.85f);
        dm.dialogueText = msgTxt;

        // 3. OYUNCU GİRİŞ ALANI (Aşağıda BÜYÜK bar)
        GameObject inputBar = new GameObject("InputBar");
        inputBar.transform.SetParent(mainPanel.transform, false);
        Image inputBarImg = inputBar.AddComponent<Image>();
        inputBarImg.color = new Color(0.15f, 0.15f, 0.2f, 0.95f); // Koyu mavi-gri bar
        RectTransform inputBarRT = inputBar.GetComponent<RectTransform>();
        inputBarRT.anchorMin = new Vector2(0.05f, 0.02f);
        inputBarRT.anchorMax = new Vector2(0.95f, 0.18f);
        
        // INPUT FIELD
        GameObject inputObj = new GameObject("InputField");
        inputObj.transform.SetParent(inputBar.transform, false);
        Image inputImg = inputObj.AddComponent<Image>();
        inputImg.color = new Color(1f, 1f, 1f, 0.1f); // Hafif şeffaf beyaz
        
        TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
        RectTransform inputRect = inputObj.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.02f, 0.1f);
        inputRect.anchorMax = new Vector2(0.85f, 0.9f);
        inputRect.offsetMin = Vector2.zero; inputRect.offsetMax = Vector2.zero;

        // TextArea
        GameObject textArea = new GameObject("TextArea");
        textArea.transform.SetParent(inputObj.transform, false);
        RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero; textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(10, 5); textAreaRect.offsetMax = new Vector2(-10, -5);

        // Placeholder - BÜYÜK FONT
        GameObject placeholder = new GameObject("Placeholder");
        placeholder.transform.SetParent(textArea.transform, false);
        TextMeshProUGUI placeholderText = placeholder.AddComponent<TextMeshProUGUI>();
        placeholderText.text = "Sorunuzu buraya yazın...";
        placeholderText.fontSize = 28; // BÜYÜK
        placeholderText.color = new Color(0.6f, 0.6f, 0.7f);
        placeholderText.fontStyle = FontStyles.Italic;
        RectTransform placeholderRect = placeholder.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero; placeholderRect.anchorMax = Vector2.one;
        
        // Input Text - BÜYÜK FONT
        GameObject text = new GameObject("Text");
        text.transform.SetParent(textArea.transform, false);
        TextMeshProUGUI inputText = text.AddComponent<TextMeshProUGUI>();
        inputText.fontSize = 28; // BÜYÜK
        inputText.color = Color.white;
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;

        inputField.textViewport = textAreaRect;
        inputField.textComponent = inputText;
        inputField.placeholder = placeholderText;
        
        dm.inputField = inputField;

        // GÖNDER BUTONU (Barın sağında)
        GameObject sendBtnObj = new GameObject("SendButton");
        sendBtnObj.transform.SetParent(inputBar.transform, false);
        Image sendImg = sendBtnObj.AddComponent<Image>();
        sendImg.color = new Color(0.2f, 0.8f, 0.3f); // Yeşil buton
        Button sendBtn = sendBtnObj.AddComponent<Button>();
        RectTransform sendRect = sendBtnObj.GetComponent<RectTransform>();
        sendRect.anchorMin = new Vector2(0.86f, 0.1f);
        sendRect.anchorMax = new Vector2(0.98f, 0.9f);
        sendRect.offsetMin = Vector2.zero; sendRect.offsetMax = Vector2.zero;

        GameObject sendTxtObj = new GameObject("Text");
        sendTxtObj.transform.SetParent(sendBtnObj.transform, false);
        TextMeshProUGUI sendTxt = sendTxtObj.AddComponent<TextMeshProUGUI>();
        sendTxt.text = "GÖNDER";
        sendTxt.fontSize = 24; // BÜYÜK
        sendTxt.fontStyle = FontStyles.Bold;
        sendTxt.color = Color.white;
        sendTxt.alignment = TextAlignmentOptions.Center;
        RectTransform sendTxtRect = sendTxtObj.GetComponent<RectTransform>();
        sendTxtRect.anchorMin = Vector2.zero; sendTxtRect.anchorMax = Vector2.one;

        dm.sendButton = sendBtn;

        // KAPAT BUTONU (Balonun sağ üst köşesinde "X")
        dm.optionButtons = new Button[1];
        dm.optionTexts = new TextMeshProUGUI[1];

        GameObject btnObj = new GameObject("CloseButton");
        btnObj.transform.SetParent(bubble.transform, false);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.8f, 0.2f, 0.2f); // Kırmızı
        Button btn = btnObj.AddComponent<Button>();
        RectTransform btnRT = btnObj.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.92f, 0.9f);
        btnRT.anchorMax = new Vector2(0.98f, 0.98f);
        
        GameObject btnTxtObj = new GameObject("Text");
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI btnTxt = btnTxtObj.AddComponent<TextMeshProUGUI>();
        btnTxt.text = "X";
        btnTxt.fontSize = 18;
        btnTxt.color = Color.white;
        btnTxt.alignment = TextAlignmentOptions.Center;
        RectTransform btnTxtRect = btnTxtObj.GetComponent<RectTransform>();
        btnTxtRect.anchorMin = Vector2.zero; btnTxtRect.anchorMax = Vector2.one;

        dm.optionButtons[0] = btn;
        dm.optionTexts[0] = btnTxt;

        dm.dialoguePanel = mainPanel;
        mainPanel.SetActive(false); // Başlangıçta gizli
        
        Debug.Log($"✅ KONUŞMA BALONU UI OLUŞTURULDU.");
    }

    void CreateGossipGameUI()
    {
    }

    void BakeNavMesh()
    {
#pragma warning disable 618
        try { UnityEditor.AI.NavMeshBuilder.BuildNavMesh(); Debug.Log("✅ NavMesh bake edildi"); }
        catch { Debug.LogWarning("NavMesh bake manuel yapılmalı."); }
#pragma warning restore 618
    }

    void CreateLayerIfNotExists(string layerName)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        bool exists = false;
        for(int i=8; i<layers.arraySize; i++) { if(layers.GetArrayElementAtIndex(i).stringValue == layerName) exists = true; }
        if(!exists) {
            for(int i=8; i<layers.arraySize; i++) {
                SerializedProperty sp = layers.GetArrayElementAtIndex(i);
                if(string.IsNullOrEmpty(sp.stringValue)) { sp.stringValue = layerName; tagManager.ApplyModifiedProperties(); break; }
            }
        }
    }

    void ApplyCharacterAppearance(GameObject character, Color bodyColor, int npcIndex)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        Color[] hatColors = {
            new Color(0.3f, 0.3f, 0.5f), new Color(0.6f, 0.4f, 0.2f), new Color(0.9f, 0.6f, 0.8f),
            new Color(0.4f, 0.5f, 0.3f), new Color(0.9f, 0.9f, 0.9f), new Color(0.5f, 0.4f, 0.3f),
            new Color(0.2f, 0.2f, 0.2f)
        };
        Color hatColor = hatColors[Mathf.Clamp(npcIndex + 1, 0, hatColors.Length - 1)];

        foreach (Renderer r in character.GetComponentsInChildren<Renderer>())
        {
            string n = r.gameObject.name.ToLower();
            bool isHat = n.Contains("hat") || n.Contains("cap") || n.Contains("helmet") || n.Contains("hair");
            
            Material mat = new Material(shader);
            mat.color = isHat ? hatColor : bodyColor;
            
            mat.SetFloat("_Surface", 0.0f); // Opaque
            mat.SetFloat("_Blend", 0.0f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            mat.SetInt("_ZWrite", 1);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = -1;
            mat.SetFloat("_Smoothness", 0.2f);
            
            r.material = mat;
        }
        Debug.Log($"✅ {character.name} materyalleri (Opaque) ayarlandı.");
    }
}