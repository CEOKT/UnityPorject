using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Dedikodu bulma oyun yöneticisi
/// Oyuncu dedikoduyu kimin başlattığını bulmaya çalışır
/// </summary>
public class GossipGameManager : MonoBehaviour
{
    public static GossipGameManager Instance { get; private set; }

    [Header("Game State")]
    public bool gameActive = true;
    public int currentRound = 1;
    public int maxRounds = 3;
    public int score = 0;

    [Header("Timer Settings")]
    public float baseTime = 90f;           // İlk round süresi (saniye)
    public float timeReductionPerRound = 15f; // Her round azalacak süre
    public float currentTime = 0f;
    public bool timerRunning = false;

    [Header("Current Gossip Target")]
    public Gossip targetGossip;          // Bulunması gereken dedikodu
    public int originatorNPCID;          // Dedikoduyu başlatan NPC'nin ID'si
    public string originatorName;        // Dedikoduyu başlatan NPC'nin adı

    [Header("UI References")]
    public GameObject accusationPanel;    // Suçlama paneli
    public TextMeshProUGUI gossipText;    // Dedikodu metni
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI timerText;     // Geri sayım
    public Button[] npcButtons;           // NPC seçim butonları
    public TextMeshProUGUI[] npcNameTexts;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public Button restartButton;
    public Button nextRoundButton;

    [Header("Start/Pause Panel")]
    public GameObject startPanel;         // Başlangıç paneli
    public TextMeshProUGUI startInfoText; // Bilgi metni
    public Button startButton;            // Başla butonu
    private bool isPaused = false;

    private List<NPCController> allNPCs = new List<NPCController>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        // UI'ı gizle
        if (accusationPanel != null)
            accusationPanel.SetActive(false);
        if (resultPanel != null)
            resultPanel.SetActive(false);

        // Başlangıç panelini göster
        ShowStartPanel();
    }

    /// <summary>
    /// Başlangıç panelini göster
    /// </summary>
    private void ShowStartPanel()
    {
        isPaused = true;
        Time.timeScale = 0f;
        
        if (startPanel != null)
        {
            startPanel.SetActive(true);
            
            float roundTime = baseTime - ((currentRound - 1) * timeReductionPerRound);
            roundTime = Mathf.Max(roundTime, 30f);
            
            if (startInfoText != null)
            {
                startInfoText.text = $"<size=36><b>DEDİKODU BULMA OYUNU</b></size>\n\n" +
                    $"<size=24>Round {currentRound}/{maxRounds}</size>\n\n" +
                    $"<color=yellow>{roundTime:F0} saniye</color> içinde\n" +
                    $"dedikoduyu kimin başlattığını bul!\n\n" +
                    $"<size=18>E - NPC ile konuş\n" +
                    $"TAB - Suçlama paneli\n" +
                    $"Q - Kopya gör\n" +
                    $"ESC - Durdur</size>";
            }
            
            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(StartGame);
            }
        }
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Oyunu başlat
    /// </summary>
    public void StartGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        
        if (startPanel != null)
            startPanel.SetActive(false);
            
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        StartNewRound();
    }

    [Header("Cheat/Kopya")]
    public GameObject cheatPanel;         // Kopya paneli
    public TextMeshProUGUI cheatText;     // Kopya metni
    private bool cheatVisible = false;

    private void Update()
    {
        // Oyun duraklatmışsa sadece ESC çalışsın
        if (isPaused)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ResumeGame();
            }
            return;
        }

        // Tab tuşu ile suçlama panelini aç/kapat (oyunu durdurur)
        if (Input.GetKeyDown(KeyCode.Tab) && gameActive)
        {
            ToggleAccusationPanel();
        }

        // Escape ile duraklat
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PauseGame();
        }

        // Q tuşu ile kopya göster/gizle
        if (Input.GetKeyDown(KeyCode.Q) && gameActive)
        {
            ToggleCheat();
        }

        // Geri sayım
        UpdateTimer();
    }

    /// <summary>
    /// Oyunu duraklat
    /// </summary>
    private void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        
        if (startPanel != null)
        {
            startPanel.SetActive(true);
            
            if (startInfoText != null)
            {
                startInfoText.text = $"<size=36><b>OYUN DURAKLATILDI</b></size>\n\n" +
                    $"<size=24>Round {currentRound}/{maxRounds}</size>\n" +
                    $"Kalan süre: <color=yellow>{currentTime:F0} saniye</color>\n\n" +
                    $"<size=18>Devam etmek için ESC veya BAŞLA</size>";
            }
        }
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Oyuna devam et
    /// </summary>
    private void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        
        if (startPanel != null)
            startPanel.SetActive(false);
            
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Kopya panelini aç/kapat
    /// </summary>
    private void ToggleCheat()
    {
        cheatVisible = !cheatVisible;
        
        if (cheatPanel != null)
        {
            cheatPanel.SetActive(cheatVisible);
            
            if (cheatVisible && cheatText != null)
            {
                cheatText.text = $"🔍 KOPYA\n\nDedikoduyu Başlatan:\n<color=yellow>{originatorName}</color>";
            }
        }
        else
        {
            // Panel yoksa debug log ile göster
            if (cheatVisible)
            {
                Debug.Log($"========== KOPYA ==========\nDedikoduyu Başlatan: {originatorName}\n============================");
            }
        }
    }

    /// <summary>
    /// Geri sayımı güncelle
    /// </summary>
    private void UpdateTimer()
    {
        if (!timerRunning || !gameActive) return;

        currentTime -= Time.deltaTime;

        // UI güncelle
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60f);
            int seconds = Mathf.FloorToInt(currentTime % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";

            // Son 10 saniye kırmızı yanıp sönsün
            if (currentTime <= 10f)
            {
                timerText.color = (Mathf.FloorToInt(currentTime * 2) % 2 == 0) ? Color.red : Color.yellow;
            }
            else if (currentTime <= 30f)
            {
                timerText.color = Color.yellow;
            }
            else
            {
                timerText.color = Color.white;
            }
        }

        // Süre bitti!
        if (currentTime <= 0)
        {
            currentTime = 0;
            timerRunning = false;
            TimeUp();
        }
    }

    /// <summary>
    /// Süre doldu - seçim ekranını aç
    /// </summary>
    private void TimeUp()
    {
        Debug.Log("[GossipGame] SÜRE DOLDU!");
        
        // Suçlama panelini zorla aç
        if (accusationPanel != null)
        {
            accusationPanel.SetActive(true);
            PopulateNPCButtons();
            
            // Mouse'u serbest bırak
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Talimat güncelle
            if (instructionText != null)
            {
                instructionText.text = "⏰ SÜRE DOLDU!\nHemen bir NPC seç!";
                instructionText.color = Color.red;
            }
        }
    }

    /// <summary>
    /// Yeni round başlat
    /// </summary>
    public void StartNewRound()
    {
        gameActive = true;
        
        // Tüm NPC'leri bul
        allNPCs = FindObjectsByType<NPCController>(FindObjectsSortMode.None).ToList();

        if (allNPCs.Count < 2)
        {
            Debug.LogError("[GossipGame] Yeterli NPC yok!");
            return;
        }

        // Round'a göre süreyi ayarla (her round azalır)
        currentTime = baseTime - ((currentRound - 1) * timeReductionPerRound);
        currentTime = Mathf.Max(currentTime, 30f); // Minimum 30 saniye
        timerRunning = true;

        // Rastgele bir NPC'yi dedikodu başlatıcı olarak seç
        NPCController originator = allNPCs[Random.Range(0, allNPCs.Count)];
        originatorNPCID = originator.npcData.npcID;
        originatorName = originator.npcData.npcName;

        // Yeni dedikodu oluştur - GossipSystem otomatik yapar
        // GossipSystem.Start() çağrılmışsa zaten dedikodu yayılmıştır

        // UI güncelle
        UpdateUI();

        Debug.Log($"[GossipGame] Round {currentRound} başladı! Süre: {currentTime}sn - Başlatan: {originatorName}");
    }

    /// <summary>
    /// Suçlama panelini aç/kapat (oyunu durdurur)
    /// </summary>
    public void ToggleAccusationPanel()
    {
        if (accusationPanel == null) return;

        bool isOpen = accusationPanel.activeSelf;
        accusationPanel.SetActive(!isOpen);

        if (!isOpen)
        {
            // Panel açıldığında oyunu durdur
            Time.timeScale = 0f;
            
            // Panel açıldığında NPC'leri listele
            PopulateNPCButtons();
            
            // Mouse'u serbest bırak
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Dedikodu metnini göster
            if (gossipText != null && targetGossip != null)
            {
                gossipText.text = $"Dedikodu: \"{targetGossip.content}\"";
            }
            if (instructionText != null)
            {
                instructionText.text = "Bu dedikoduyu KİM başlattı?\nNPC'lerle konuşarak ipucu topla, sonra suçla!";
            }
        }
        else
        {
            // Panel kapandığında oyuna devam
            Time.timeScale = 1f;
            
            // Panel kapandığında mouse'u kilitle
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// NPC butonlarını doldur
    /// </summary>
    private void PopulateNPCButtons()
    {
        for (int i = 0; i < npcButtons.Length; i++)
        {
            if (i < allNPCs.Count)
            {
                int index = i; // Closure için
                NPCController npc = allNPCs[i];

                npcButtons[i].gameObject.SetActive(true);
                npcNameTexts[i].text = $"{npc.npcData.npcName}\n({npc.npcData.personalityType})";
                
                npcButtons[i].onClick.RemoveAllListeners();
                npcButtons[i].onClick.AddListener(() => AccuseNPC(npc));
            }
            else
            {
                npcButtons[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Bir NPC'yi suçla
    /// </summary>
    public void AccuseNPC(NPCController accusedNPC)
    {
        gameActive = false;
        accusationPanel.SetActive(false);

        bool isCorrect = (accusedNPC.npcData.npcID == originatorNPCID);

        if (isCorrect)
        {
            // DOĞRU TAHMİN
            score += 100 * currentRound;
            ShowResult(true, $"DOĞRU! {originatorName} dedikoduyu başlatmıştı!\n\n+{100 * currentRound} puan!");
            
            Debug.Log($"[GossipGame] KAZANDI! Doğru tahmin: {accusedNPC.npcData.npcName}");
        }
        else
        {
            // YANLIŞ TAHMİN
            ShowResult(false, $"YANLIŞ!\n\nDedikoduyu {originatorName} başlatmıştı, {accusedNPC.npcData.npcName} değil!\n\nKöy baştan başlıyor...");
            
            Debug.Log($"[GossipGame] KAYBETTİ! Yanlış: {accusedNPC.npcData.npcName}, Doğru: {originatorName}");
        }
    }

    /// <summary>
    /// Sonuç panelini göster
    /// </summary>
    private void ShowResult(bool won, string message)
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            resultText.text = message;

            // Butonları ayarla
            if (won)
            {
                if (currentRound < maxRounds)
                {
                    nextRoundButton.gameObject.SetActive(true);
                    restartButton.gameObject.SetActive(false);
                    nextRoundButton.onClick.RemoveAllListeners();
                    nextRoundButton.onClick.AddListener(NextRound);
                }
                else
                {
                    // Oyun bitti - kazandı
                    resultText.text = $"TEBRİKLER!\n\nTüm roundları tamamladın!\n\nToplam Skor: {score}";
                    nextRoundButton.gameObject.SetActive(false);
                    restartButton.gameObject.SetActive(true);
                }
            }
            else
            {
                nextRoundButton.gameObject.SetActive(false);
                restartButton.gameObject.SetActive(true);
            }

            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
        }

        // Mouse serbest
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Sonraki round
    /// </summary>
    public void NextRound()
    {
        currentRound++;
        resultPanel.SetActive(false);
        
        // Cursor'ı kilitle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Eski dedikoduları temizle
        ClearAllGossipMemories();

        // Yeni round
        StartNewRound();
    }

    /// <summary>
    /// Oyunu baştan başlat
    /// </summary>
    public void RestartGame()
    {
        currentRound = 1;
        score = 0;
        resultPanel.SetActive(false);
        
        // Cursor'ı kilitle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Tüm NPC hafızalarını temizle
        ClearAllGossipMemories();

        // Yeni oyun
        StartNewRound();
    }

    /// <summary>
    /// Tüm NPC'lerin dedikodu hafızasını temizle
    /// </summary>
    private void ClearAllGossipMemories()
    {
        foreach (var npc in allNPCs)
        {
            npc.npcData.gossipMemory.Clear();
            npc.npcData.opinionScore = Random.Range(-10, 10); // Opinion'ı sıfırla
        }

        // GossipSystem'deki aktif dedikoduları temizle
        // (GossipSystem'e ClearGossips metodu eklenebilir)
    }

    /// <summary>
    /// UI güncelle
    /// </summary>
    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Skor: {score}";
        if (roundText != null)
            roundText.text = $"Round: {currentRound}/{maxRounds}";
    }

    /// <summary>
    /// Tüm panelleri kapat
    /// </summary>
    private void CloseAllPanels()
    {
        if (accusationPanel != null)
            accusationPanel.SetActive(false);
        if (resultPanel != null && gameActive)
            resultPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
