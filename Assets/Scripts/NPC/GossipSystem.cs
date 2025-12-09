using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Dedikodu yayılım sistemi - Zincirleme evrim
/// Her NPC önceki versiyonu duyar ve kendi yorumunu ekler
/// </summary>
public class GossipSystem : MonoBehaviour
{
    public static GossipSystem Instance { get; private set; }

    [Header("Ayarlar")]
    [SerializeField] private float spreadDelay = 0.5f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Yeni bir dedikodu oluştur ve tüm NPC'lere anında yay
    /// </summary>
    public void CreateAndSpreadGossipsImmediately()
    {
        NPCController[] allNPCs = FindObjectsByType<NPCController>(FindObjectsSortMode.None);
        if (allNPCs.Length == 0) return;

        // Rastgele bir NPC'yi dedikodu başlatıcı olarak seç
        NPCController originator = allNPCs[Random.Range(0, allNPCs.Length)];
        
        // Temel olay - çok basit, nötr
        string baseEvent = "Yabancı çocuk akşam yolda taşa tekme attı";

        Gossip mainGossip = CreateGossip(
            baseEvent,
            targetID: 0,  // Player
            originatorID: originator.npcData.npcID,
            severity: 1  // Başlangıçta düşük
        );

        // Başlatıcı NPC - ilk yorumu karakterine göre yapar
        string originatorVersion = CreateOriginatorVersion(baseEvent, originator.npcData.villageCharacterType);
        originator.npcData.HearGossipWithEvolution(mainGossip, originatorVersion, 0);

        Debug.Log($"[GossipSystem] === DEDİKODU YAYILIMI ===");
        Debug.Log($"[GossipSystem] KAYNAK: {originator.npcData.npcName}");
        Debug.Log($"[GossipSystem] Sıra 0: {originatorVersion}");

        // Diğer NPC'lere yayılma sırasına göre dağıt
        List<NPCController> informed = new List<NPCController> { originator };
        int evolutionOrder = 1;
        
        // NPC'leri karıştır - rastgele yayılma sırası
        List<NPCController> shuffledNPCs = allNPCs.Where(n => n != originator).OrderBy(x => Random.value).ToList();
        
        string previousVersion = originatorVersion;
        
        foreach (NPCController npc in shuffledNPCs)
        {
            // Önceki versiyonu al ve bu NPC'nin karakterine göre evrimleştir
            string evolvedGossip = EvolveGossip(previousVersion, npc.npcData.villageCharacterType, evolutionOrder);
            
            npc.npcData.HearGossipWithEvolution(mainGossip, evolvedGossip, evolutionOrder);
            informed.Add(npc);
            
            Debug.Log($"[GossipSystem] Sıra {evolutionOrder} ({npc.npcData.npcName}): {evolvedGossip}");
            
            previousVersion = evolvedGossip;
            evolutionOrder++;
        }

        Debug.Log($"[GossipSystem] === YAYILIM TAMAMLANDI ===");

        // GossipGameManager'a bildir
        if (GossipGameManager.Instance != null)
        {
            GossipGameManager.Instance.targetGossip = mainGossip;
            GossipGameManager.Instance.originatorNPCID = originator.npcData.npcID;
            GossipGameManager.Instance.originatorName = originator.npcData.npcName;
        }
    }

    /// <summary>
    /// Kaynak NPC'nin ilk yorumu - KISA, karaktere göre
    /// </summary>
    private string CreateOriginatorVersion(string baseEvent, VillageCharacters.CharacterType charType)
    {
        // Orijinal olay: "Taşa tekme attı"
        switch (charType)
        {
            case VillageCharacters.CharacterType.BakkalEmmi:
                return "Taşa tekme attı. Sinirli gibiydi.";
                
            case VillageCharacters.CharacterType.EmineTeyze:
                return "Taşa vurdu! Öfkeli öfkeli!";
                
            case VillageCharacters.CharacterType.CirakCeyhun:
                return "Taşa saldırdı abi! Artistlik!";
                
            case VillageCharacters.CharacterType.ImamHoca:
                return "Taşa tekme attı. Sebebi meçhul.";
                
            case VillageCharacters.CharacterType.Muhtar:
                return "Taşa vurdu. Bi şeyler var.";
                
            case VillageCharacters.CharacterType.FirinciHasan:
                return "Taşa mı vurdu? Bi şey yaptı.";
        }
        return "Taşa tekme attı.";
    }

    /// <summary>
    /// Dedikodu ZİNCİRLEME evrimleşir - önceki versiyona göre büyür
    /// Her NPC öncekinin anlattığını kendi yorumuyla değiştirir
    /// </summary>
    private string EvolveGossip(string previousVersion, VillageCharacters.CharacterType listenerType, int order)
    {
        // Zincirleme evrim - her adımda biraz daha değişir
        // Sıra 1: taşa vurdu → sinirli, tehlikeli
        // Sıra 2: sinirli → birine saldıracak
        // Sıra 3: saldıracak → saldırdı
        // Sıra 4: saldırdı → yaraladı
        // Sıra 5: yaraladı → öldürdü/soydu
        
        switch (listenerType)
        {
            case VillageCharacters.CharacterType.EmineTeyze: // Abartan
                if (order == 1) return "Taşa öyle vurdu ki! Tehlikeli biri!";
                if (order == 2) return "Birine saldıracakmış! Taşla!";
                if (order == 3) return "Birine saldırmış! Dövmüş!";
                if (order == 4) return "Adam dövmüş! Bayıltmış!";
                return "Adam öldürmüş! Katil!";
                    
            case VillageCharacters.CharacterType.CirakCeyhun: // Genç, heyecanlı
                if (order == 1) return "Taşa saldırdı abi! Deli gibi!";
                if (order == 2) return "Kavga çıkarmış abi! Taşla!";
                if (order == 3) return "Adam dövmüş abi! Fena!";
                if (order == 4) return "Birini yaralamış! Kan varmış!";
                return "Cinayet işlemiş abi! Kaçmış!";
                    
            case VillageCharacters.CharacterType.FirinciHasan: // Yanlış anlayan
                if (order == 1) return "Taş mı attı? Cam mı kırdı?";
                if (order == 2) return "Dükkân mı soydu? Ekmek mi?";
                if (order == 3) return "Birini mi dövdü? Fırıncıyı mı?";
                if (order == 4) return "Köyü mü yaktı? Ne yaptı?";
                return "Herkesi mi öldürdü? Anlamadım!";
                    
            case VillageCharacters.CharacterType.BakkalEmmi: // Şüpheci, kızgın
                if (order == 1) return "Taşa vurmuş. Şüpheli tip.";
                if (order == 2) return "Kavga arıyormuş. Belli.";
                if (order == 3) return "Birine saldırmış. Dedim ben.";
                if (order == 4) return "Hırsızlık yapmış. Belliydi.";
                return "Adam soymuş. Hep şüpheliydim.";
                    
            case VillageCharacters.CharacterType.ImamHoca: // Sakin, temkinli
                if (order == 1) return "Taşa vurmuş. Garip.";
                if (order == 2) return "Sorun çıkarmış. Dikkat.";
                if (order == 3) return "Kavga etmiş. Üzücü.";
                if (order == 4) return "Birini yaralamış. Allah korusun.";
                return "Ağır suç işlemiş. Yazık.";
                
            case VillageCharacters.CharacterType.Muhtar: // Resmi, belirsiz
                if (order == 1) return "Taşa vurmuş. İnceliyorum.";
                if (order == 2) return "Olay çıkarmış. Bakıyoruz.";
                if (order == 3) return "Saldırı var. Araştırılıyor.";
                if (order == 4) return "Ciddi suç. Takipteyiz.";
                return "Büyük olay. Herkes sakin.";
        }
        
        return previousVersion;
    }

    /// <summary>
    /// Yeni dedikodu oluştur
    /// </summary>
    private Gossip CreateGossip(string content, int targetID, int originatorID, int severity)
    {
        return new Gossip
        {
            gossipID = System.Guid.NewGuid().GetHashCode(),
            content = content,
            targetID = targetID,
            originatorID = originatorID,
            severity = severity
        };
    }
}
