using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Her NPC'nin kişilik, hafıza ve ilişki verilerini tutan sınıf
/// </summary>
[System.Serializable]
public class NPCData
{
    [Header("Basic Info")]
    public string npcName;
    public int npcID;

    [Header("Personality Traits (0-100)")]
    [Range(0, 100)] public int trustworthiness = 50;  // Güvenilirlik
    [Range(0, 100)] public int gossipLoving = 50;     // Dedikodu sevme
    [Range(0, 100)] public int cowardice = 50;        // Korkaklık
    [Range(0, 100)] public int aggressiveness = 50;   // Agresiflik
    [Range(0, 100)] public int gullibility = 50;      // Saf/inanma

    [Header("Relationship with Player")]
    [Range(-100, 100)] public int opinionScore = 0;   // Oyuncuya bakış açısı

    [Header("Memory System")]
    public List<GossipMemory> gossipMemory = new List<GossipMemory>();
    public List<InteractionMemory> interactionMemory = new List<InteractionMemory>();

    [Header("Social Status")]
    public NPCRole role;
    public int socialInfluence = 50; // Toplumsal etki gücü

    // Personality Types
    public PersonalityType personalityType;
    
    [Header("Village Character")]
    public VillageCharacters.CharacterType villageCharacterType;
    
    [Header("Gossip Evolution")]
    public string currentGossipVersion = "";  // Bu NPC'nin bildiği dedikodu versiyonu
    public int gossipEvolutionOrder = -1;     // Dedikoduyu kaçıncı sırada duydu (-1 = duymadı, 0 = kaynak)
    public bool isGossipOriginator = false;   // Dedikoduyu başlatan bu mu?

    public enum PersonalityType
    {
        Gossiper,      // Dedikoducu - her şeyi yayar, çarpıtır
        Trustworthy,   // Güvenilir - doğruyu söyler
        Liar,          // Yalancı - kasıtlı olarak yalan söyler
        Timid,         // Çekingen - bilgi vermekten çekinir
        Sycophant,     // Dalkavuk - güçlüden yana olur
        Angry          // Sinirli - agresif, herkese düşman
    }

    public enum NPCRole
    {
        Villager,      // Sıradan köylü
        Merchant,      // Tüccar
        Elder,         // Yaşlı - yüksek etki
        Guard,         // Muhafız
        Priest,        // Papaz - toplumsal lider
        Child          // Çocuk - düşük etki
    }

    /// <summary>
    /// NPC'nin oyuncuya olan bakış açısını günceller
    /// </summary>
    public void ModifyOpinion(int change, string reason)
    {
        opinionScore = Mathf.Clamp(opinionScore + change, -100, 100);
        
        // Etkileşim kaydı
        interactionMemory.Add(new InteractionMemory
        {
            timestamp = DateTime.Now,
            opinionChange = change,
            reason = reason
        });

        Debug.Log($"{npcName}: Opinion changed by {change}. New score: {opinionScore} ({reason})");
    }

    /// <summary>
    /// NPC dedikodu duyar ve hafızasına ekler
    /// </summary>
    public void HearGossip(Gossip gossip, string source)
    {
        // Kişiliğe göre dedikodu çarpıtma
        Gossip modifiedGossip = ModifyGossipBasedOnPersonality(gossip);
        float belief = CalculateBelievability(gossip, source);
        string reaction = GenerateReaction(gossip, belief);

        gossipMemory.Add(new GossipMemory
        {
            gossip = modifiedGossip,
            heardFrom = source,
            timestamp = DateTime.Now,
            believability = belief,
            reaction = reaction
        });

        Debug.Log($"{npcName} heard gossip from {source}: {modifiedGossip.content}. Belief: {belief}, Reaction: {reaction}");
    }

    /// <summary>
    /// Dedikodu evrim sistemiyle duyar - yayılma sırasına göre farklı versiyon
    /// </summary>
    public void HearGossipWithEvolution(Gossip originalGossip, string evolvedContent, int evolutionOrder)
    {
        currentGossipVersion = evolvedContent;
        gossipEvolutionOrder = evolutionOrder;
        isGossipOriginator = (evolutionOrder == 0);
        
        // Gossip objesini evrimleşmiş içerikle güncelle
        Gossip evolvedGossip = new Gossip(originalGossip);
        evolvedGossip.content = evolvedContent;
        
        string source = isGossipOriginator ? "kendi gözlerimle gördüm" : "köyden duydum";
        float belief = isGossipOriginator ? 100f : Mathf.Max(30f, 100f - (evolutionOrder * 10f));
        
        gossipMemory.Clear(); // Eski hafızayı temizle
        gossipMemory.Add(new GossipMemory
        {
            gossip = evolvedGossip,
            heardFrom = source,
            timestamp = DateTime.Now,
            believability = belief,
            reaction = isGossipOriginator ? "Ben gördüm!" : "Duyduğuma göre..."
        });

        Debug.Log($"[{npcName}] Dedikodu aldı (sıra:{evolutionOrder}): {evolvedContent}");
    }

    /// <summary>
    /// Bu NPC'nin bildiği dedikodu versiyonunu döndürür
    /// </summary>
    public string GetCurrentGossipVersion()
    {
        return currentGossipVersion;
    }

    /// <summary>
    /// Kişiliğe göre dedikodu çarpıtır
    /// </summary>
    private Gossip ModifyGossipBasedOnPersonality(Gossip original)
    {
        Gossip modified = new Gossip(original);
        modified.content = DistortGossip(original.content);
        modified.isDistorted = (personalityType != PersonalityType.Trustworthy);

        // Severity ayarla
        switch (personalityType)
        {
            case PersonalityType.Gossiper:
                modified.severity = Mathf.Min(modified.severity + 2, 10);
                break;
            case PersonalityType.Liar:
                modified.severity = Mathf.Min(modified.severity + 3, 10);
                break;
            case PersonalityType.Timid:
                modified.severity = Mathf.Max(modified.severity - 2, 1);
                break;
            case PersonalityType.Angry:
                modified.severity = Mathf.Min(modified.severity + 3, 10);
                break;
            case PersonalityType.Sycophant:
                modified.severity += (opinionScore > 0) ? -1 : +2;
                break;
        }

        return modified;
    }

    /// <summary>
    /// Metni kişiliğe göre çarpıtır
    /// </summary>
    public string DistortGossip(string text)
    {
        switch (personalityType)
        {
            case PersonalityType.Gossiper:
                // Abartır ve detay ekler
                return text + " (duyduğuma göre daha da kötüymüş!)";
                
            case PersonalityType.Liar:
                // Tamamen değiştirir
                string[] lies = {
                    "Aslında olan şu: " + text.Replace("biri", "o şüpheli kişi"),
                    text + " Ama asıl mesele bu değil, daha kötüsü var!",
                    "Herkes yanlış biliyor, gerçek şu ki " + text
                };
                return lies[UnityEngine.Random.Range(0, lies.Length)];
                
            case PersonalityType.Timid:
                // Korkarak, yarım yamalak
                if (text.Length > 30)
                    return text.Substring(0, 30) + "... daha fazlasını söyleyemem, tehlikeli...";
                return "Bilmiyorum ama... " + text + "... sanırım...";
                
            case PersonalityType.Angry:
                // Agresif ve suçlayıcı
                return text.Replace("biri", "o alçak").Replace("kişi", "o hain") + " Ve bunun hesabı sorulmalı!";
                
            case PersonalityType.Sycophant:
                // Güçlüden yana çarpıtır
                if (opinionScore > 0)
                    return text + " Ama eminim bir yanlış anlaşılma vardır.";
                else
                    return text + " Zaten hep böyle şeyler yapacağını biliyordum!";
                
            case PersonalityType.Trustworthy:
            default:
                // Olduğu gibi aktarır
                return text;
        }
    }

    /// <summary>
    /// Bu NPC'nin dedikoduya ne kadar inandığını hesaplar
    /// </summary>
    private float CalculateBelievability(Gossip gossip, string source)
    {
        float believability = 50f;

        // Saf ise daha çok inanır
        believability += gullibility * 0.3f;

        // Dedikodu şiddeti
        believability += gossip.severity * 5f;

        // Kaynak güvenilirliği (buraya NPC arası ilişki sistemi eklenebilir)
        believability += UnityEngine.Random.Range(-20f, 20f);

        return Mathf.Clamp(believability, 0f, 100f);
    }

    /// <summary>
    /// Dedikoduya verilecek reaksiyonu belirler
    /// </summary>
    private string GenerateReaction(Gossip gossip, float belief)
    {
        if (belief > 80)
        {
            if (aggressiveness > 70) return "Bunun hesabını soracağım!";
            if (gossipLoving > 70) return "Bunu hemen başkalarına anlatmalıyım!";
            return "İnanılmaz, şok oldum.";
        }
        else if (belief < 20)
        {
            return "Saçmalık, buna inanmıyorum.";
        }
        else
        {
            if (cowardice > 60) return "Benim başım belaya girmesin de...";
            return "Olabilir, aklımda tutacağım.";
        }
    }

    /// <summary>
    /// Bu NPC'nin oyuncuya karşı tavrını döndürür
    /// </summary>
    public string GetOpinionStatus()
    {
        if (opinionScore > 80) return "Çok olumlu";
        if (opinionScore > 50) return "Olumlu";
        if (opinionScore > 20) return "Nötr-Olumlu";
        if (opinionScore > -20) return "Nötr";
        if (opinionScore > -50) return "Olumsuz";
        if (opinionScore > -80) return "Çok olumsuz";
        return "Düşman";
    }
}

/// <summary>
/// Dedikodu verisi
/// </summary>
[System.Serializable]
public class Gossip
{
    public int gossipID;
    public string content;              // Dedikodu içeriği
    public int targetID;                // Konu edilen kişi (0 = player)
    public int originatorID;            // İlk başlatan NPC
    public int severity;                // Ciddiyet (1-10)
    public bool isDistorted;            // Çarpıtılmış mı?
    public DateTime creationTime;

    public Gossip()
    {
        creationTime = DateTime.Now;
    }

    public Gossip(Gossip original)
    {
        gossipID = original.gossipID;
        content = original.content;
        targetID = original.targetID;
        originatorID = original.originatorID;
        severity = original.severity;
        isDistorted = original.isDistorted;
        creationTime = original.creationTime;
    }
}

/// <summary>
/// NPC'nin duyduğu dedikodu hafızası
/// </summary>
[System.Serializable]
public class GossipMemory
{
    public Gossip gossip;
    public string heardFrom;        // Kimden duydu
    public DateTime timestamp;
    public float believability;     // Ne kadar inandı (0-100)
    public string reaction;         // Dedikoduya verdiği reaksiyon
}

/// <summary>
/// Oyuncu ile etkileşim hafızası
/// </summary>
[System.Serializable]
public class InteractionMemory
{
    public DateTime timestamp;
    public int opinionChange;
    public string reason;
}
