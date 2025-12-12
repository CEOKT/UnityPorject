using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Oyunun ana yönetim sistemi
/// Quest tracking, game state, global events
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public GamePhase currentPhase = GamePhase.Investigation;
    public float investigationTimeLimit = 300f; // 5 dakika
    private float investigationStartTime;

    [Header("Quest System")]
    public List<Quest> activeQuests = new List<Quest>();
    public List<Quest> completedQuests = new List<Quest>();

    [Header("NPC System")]
    public List<NPCController> allNPCs = new List<NPCController>();

    [Header("Ending Conditions")]
    public int minOpinionToSurvive = -30;
    public int minTruthScore = 70; // Gerçeği ne kadar buldun

    private int playerTruthScore = 0;

    public enum GamePhase
    {
        Investigation,  // Araştırma fazı
        Decision,       // Karar anı
        Ending          // Final
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // DİYALOG SİSTEMİNİ KONTROL ET (Otomatik Kurtarma)
            if (FindFirstObjectByType<DialogueManager>() == null)
            {
                Debug.LogWarning("⚠️ DialogueManager sahnede bulunamadı! GameManager tarafından otomatik oluşturuluyor...");
                GameObject dialogueSystem = new GameObject("DialogueSystem_AutoCreated");
                dialogueSystem.AddComponent<DialogueManager>();
                // DialogueManager'ın kendi Start'ı şimdi UI'ı oluşturacak
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeGame();
    }

    private void Update()
    {
        UpdateGamePhase();
    }

    /// <summary>
    /// Oyunu başlat
    /// </summary>
    private void InitializeGame()
    {
        investigationStartTime = Time.time;
        
        // Tüm NPC'leri bul
        allNPCs = FindObjectsByType<NPCController>(FindObjectsSortMode.None).ToList();
        
        // Başlangıç görevleri
        CreateInitialQuests();

        Debug.Log($"Game started! {allNPCs.Count} NPCs found.");
    }

    /// <summary>
    /// Başlangıç görevlerini oluştur
    /// </summary>
    private void CreateInitialQuests()
    {
        activeQuests.Add(new Quest
        {
            questID = 1,
            title = "Dedikodu Kaynağını Bul",
            description = "Kim ilk dedikoduyu başlattı?",
            questType = QuestType.FindOrigin,
            isCompleted = false
        });

        activeQuests.Add(new Quest
        {
            questID = 2,
            title = "Köylülerin Güvenini Kazan",
            description = "En az 3 köylünün desteğini al (opinion > 50)",
            questType = QuestType.GainSupport,
            targetCount = 3,
            currentCount = 0,
            isCompleted = false
        });

        activeQuests.Add(new Quest
        {
            questID = 3,
            title = "Gerçek Olayı Çöz",
            description = "Dedikodu zincirini takip ederek gerçeği bul",
            questType = QuestType.SolveMystery,
            isCompleted = false
        });
    }

    /// <summary>
    /// Oyun fazını güncelle
    /// </summary>
    private void UpdateGamePhase()
    {
        if (currentPhase == GamePhase.Investigation)
        {
            // Zaman doldu mu?
            float elapsed = Time.time - investigationStartTime;
            if (elapsed >= investigationTimeLimit)
            {
                StartDecisionPhase();
            }

            // Görevler kontrol
            UpdateQuestProgress();
        }
    }

    /// <summary>
    /// Görev ilerlemesini kontrol et
    /// </summary>
    private void UpdateQuestProgress()
    {
        foreach (Quest quest in activeQuests)
        {
            if (quest.isCompleted) continue;

            switch (quest.questType)
            {
                case QuestType.GainSupport:
                    quest.currentCount = allNPCs.Count(npc => npc.npcData.opinionScore > 50);
                    if (quest.currentCount >= quest.targetCount)
                    {
                        CompleteQuest(quest);
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Görevi tamamla
    /// </summary>
    public void CompleteQuest(Quest quest)
    {
        quest.isCompleted = true;
        playerTruthScore += quest.truthScoreReward;
        
        activeQuests.Remove(quest);
        completedQuests.Add(quest);

        Debug.Log($"Quest completed: {quest.title} (+{quest.truthScoreReward} truth score)");
    }

    /// <summary>
    /// Karar fazına geç
    /// </summary>
    private void StartDecisionPhase()
    {
        currentPhase = GamePhase.Decision;
        Debug.Log("Investigation phase ended! Decision time...");
        
        // Burada büyük konuşma sahnesi başlayacak
        TriggerFinalDecision();
    }

    /// <summary>
    /// Final kararı
    /// </summary>
    private void TriggerFinalDecision()
    {
        // Köylülerin ortalama opinion'ı
        float averageOpinion = (float)allNPCs.Average(npc => npc.npcData.opinionScore);
        
        Debug.Log($"=== FINAL DECISION ===");
        Debug.Log($"Average Opinion: {averageOpinion}");
        Debug.Log($"Truth Score: {playerTruthScore}");

        GameEnding ending = DetermineEnding(averageOpinion, playerTruthScore);
        ShowEnding(ending);
    }

    /// <summary>
    /// Sonu belirle
    /// </summary>
    private GameEnding DetermineEnding(float averageOpinion, int truthScore)
    {
        // Kahraman sonu
        if (averageOpinion > 60 && truthScore > 80)
        {
            return GameEnding.Hero;
        }
        // Temize çıkma
        else if (averageOpinion > 30 && truthScore > 50)
        {
            return GameEnding.Cleared;
        }
        // Kovulma
        else if (averageOpinion > -30)
        {
            return GameEnding.Exiled;
        }
        // Asılma
        else
        {
            return GameEnding.Executed;
        }
    }

    /// <summary>
    /// Finali göster
    /// </summary>
    private void ShowEnding(GameEnding ending)
    {
        currentPhase = GamePhase.Ending;

        switch (ending)
        {
            case GameEnding.Hero:
                Debug.Log("🎉 KAHRAMAN SONU: Gerçeği buldun ve köylülerin güvenini kazandın!");
                break;
            case GameEnding.Cleared:
                Debug.Log("✅ TEMİZE ÇIKMA: Kendini savundun ama tam olarak kanıtlayamadın.");
                break;
            case GameEnding.Exiled:
                Debug.Log("😔 KOVULMA: Köylüler sana güvenmiyor. Köyden ayrılmalısın.");
                break;
            case GameEnding.Executed:
                Debug.Log("💀 ASILMA: Köylüler senin suçlu olduğuna ikna oldu...");
                break;
        }
    }

    /// <summary>
    /// Player'ın gerçek skoru artır
    /// </summary>
    public void AddTruthScore(int amount)
    {
        playerTruthScore += amount;
        Debug.Log($"Truth score increased by {amount}. Total: {playerTruthScore}");
    }

    public enum GameEnding
    {
        Hero,      // Kahraman
        Cleared,   // Temize çıkma
        Exiled,    // Kovulma
        Executed   // Asılma
    }
}

[System.Serializable]
public class Quest
{
    public int questID;
    public string title;
    public string description;
    public QuestType questType;
    public bool isCompleted;
    public int targetCount;
    public int currentCount;
    public int truthScoreReward = 20;
}

public enum QuestType
{
    FindOrigin,    // Dedikodu kaynağını bul
    GainSupport,   // Destek kazan
    SolveMystery   // Gerçeği çöz
}
