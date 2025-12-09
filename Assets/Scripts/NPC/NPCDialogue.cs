using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// NPC konuşma sistemi - LLM entegrasyonu
/// </summary>
public class NPCDialogue : MonoBehaviour
{
    [Header("Dialogue Settings")]
    public NPCData npcData;
    public bool isInConversation = false;
    public bool useLLM = true; // LLM kullanılsın mı?
    
    [Header("Response Types")]
    public List<DialogueResponse> possibleResponses = new List<DialogueResponse>();

    // LLM için prompt template
    private string conversationContext = "";

    private void Start()
    {
        GenerateDialogueOptions();
    }

    /// <summary>
    /// Oyuncu NPC'ye konuşmak istediğinde çağrılır
    /// </summary>
    public void StartConversation()
    {
        isInConversation = true;
        conversationContext = BuildConversationContext();
        Debug.Log($"Conversation started with {npcData.npcName}");
        Debug.Log($"Opinion: {npcData.GetOpinionStatus()} ({npcData.opinionScore})");
    }

    /// <summary>
    /// UI için selamlama mesajı döndürür
    /// </summary>
    public string GetGreeting()
    {
        return SelectResponse(ResponseType.Greeting);
    }

    /// <summary>
    /// LLM için conversation context oluşturur
    /// </summary>
    private string BuildConversationContext()
    {
        string context = $"NPC Name: {npcData.npcName}\n";
        context += $"Personality: {npcData.personalityType}\n";
        context += $"Role: {npcData.role}\n";
        context += $"Opinion of Player: {npcData.opinionScore} ({npcData.GetOpinionStatus()})\n\n";

        context += "Personality Traits:\n";
        context += $"- Trustworthiness: {npcData.trustworthiness}\n";
        context += $"- Gossip Loving: {npcData.gossipLoving}\n";
        context += $"- Gullibility: {npcData.gullibility}\n";
        context += $"- Aggressiveness: {npcData.aggressiveness}\n";


        context += "Known Gossips:\n";
        foreach (var memory in npcData.gossipMemory)
        {
            context += $"- {memory.gossip.content} (believes {memory.believability}%, heard from {memory.heardFrom})\n";
        }

        return context;
    }

    /// <summary>
    /// Dialogue seçenekleri üretir (şimdilik sabit, sonra LLM ile dinamik olacak)
    /// </summary>
    private void GenerateDialogueOptions()
    {
        possibleResponses.Clear();

        // Genel selamlaşma
        possibleResponses.Add(new DialogueResponse
        {
            responseType = ResponseType.Greeting,
            text = GetGreetingBasedOnOpinion(),
            opinionChange = 0
        });

        // Dedikodu sorma
        possibleResponses.Add(new DialogueResponse
        {
            responseType = ResponseType.AskAboutGossip,
            text = "Son zamanlarda köyde ne olup bitiyor?",
            opinionChange = -5 // Dedikodu sormak bazılarını rahatsız eder
        });

        // Kendini savunma
        possibleResponses.Add(new DialogueResponse
        {
            responseType = ResponseType.Defense,
            text = "Benim hakkımda söylenenler doğru değil!",
            opinionChange = GetDefenseOpinionChange()
        });
    }

    /// <summary>
    /// Opinion'a göre selamlaşma metni
    /// </summary>
    private string GetGreetingBasedOnOpinion()
    {
        if (npcData.opinionScore > 70)
            return $"Merhaba dostum! Nasılsın?";
        else if (npcData.opinionScore > 30)
            return $"Selam. Ne istiyorsun?";
        else if (npcData.opinionScore > -30)
            return $"...Evet?";
        else
            return $"Defol buradan!";
    }

    /// <summary>
    /// Savunma cevabının opinion değişimini hesaplar
    /// </summary>
    private int GetDefenseOpinionChange()
    {
        // Trustworthy tipler savunmayı dinler
        if (npcData.personalityType == NPCData.PersonalityType.Trustworthy)
            return 10;
        
        // Angry tipler daha da kızar
        if (npcData.personalityType == NPCData.PersonalityType.Angry)
            return -15;

        // Gossiper tipler umursamaz
        if (npcData.personalityType == NPCData.PersonalityType.Gossiper)
            return -5;

        return 0;
    }

    /// <summary>
    /// Oyuncu bir cevap seçtiğinde
    /// </summary>
    public string SelectResponse(ResponseType type)
    {
        DialogueResponse response = possibleResponses.FirstOrDefault(r => r.responseType == type);
        if (response == null) return "...";

        // Opinion değişimi
        npcData.ModifyOpinion(response.opinionChange, $"Player said: {response.text}");

        // Fallback cevap (LLM çalışmazsa)
        return GenerateNPCReply(type);
    }

    /// <summary>
    /// LLM ile async cevap al - DialogueManager bunu çağıracak
    /// </summary>
    public void GetLLMResponse(ResponseType type, System.Action<string> callback)
    {
        Debug.Log($"[NPCDialogue] GetLLMResponse called. useLLM={useLLM}, AIClient.Instance={AIClient.Instance}");
        
        if (!useLLM)
        {
            Debug.Log("[NPCDialogue] useLLM is FALSE - using fallback");
            callback(SelectResponse(type));
            return;
        }
        
        if (AIClient.Instance == null)
        {
            Debug.LogWarning("[NPCDialogue] AIClient.Instance is NULL! Sahnede AIClient objesi var mı kontrol edin!");
            callback(SelectResponse(type));
            return;
        }

        DialogueResponse response = possibleResponses.FirstOrDefault(r => r.responseType == type);
        string playerMessage = response?.text ?? "Merhaba";

        GetLLMResponse(playerMessage, callback);
    }

    /// <summary>
    /// LLM ile async cevap al - Custom Player Input
    /// </summary>
    public void GetLLMResponse(string playerMessage, System.Action<string> callback)
    {
        if (!useLLM || AIClient.Instance == null)
        {
            callback("..."); // Fallback
            return;
        }

        // Opinion değişimi (basitçe her konuşma biraz etkiler)
        npcData.ModifyOpinion(0, $"Player said: {playerMessage}");

        string systemPrompt = BuildSystemPrompt();
        Debug.Log($"[NPCDialogue] Sending to LLM: {playerMessage}");
        
        StartCoroutine(AIClient.Instance.AskLLM(systemPrompt, playerMessage, (llmResponse) =>
        {
            if (string.IsNullOrEmpty(llmResponse) || llmResponse == "...")
            {
                callback("...");
            }
            else
            {
                callback(llmResponse);
            }
        }));
    }

    /// <summary>
    /// LLM için system prompt oluşturur - VillageCharacters sistemini kullanır
    /// </summary>
    private string BuildSystemPrompt()
    {
        string gossipContent = npcData.currentGossipVersion;
        bool isOriginator = npcData.isGossipOriginator;
        
        if (string.IsNullOrEmpty(gossipContent) && npcData.gossipMemory.Count > 0)
        {
            gossipContent = npcData.gossipMemory[0].gossip.content;
        }

        // Yeni VillageCharacters sistemini kullan
        return VillageCharacters.GetSystemPrompt(npcData.villageCharacterType, gossipContent, isOriginator);
    }

    /// <summary>
    /// Dedikodu kaynak bilgisini oluşturur
    /// </summary>
    private string GetGossipSourceInfo()
    {
        if (npcData.gossipMemory.Count == 0)
            return "Hiçbir şey duymadın.";

        var mem = npcData.gossipMemory[0];
        if (mem.heardFrom == "kendi gözlerimle gördüm")
            return "Bu olayı KENDİN GÖRDÜN! Sen orijinal kaynaksın.";
        else
            return $"Bu bilgiyi {mem.heardFrom}'dan duydun. O da başkasından duymuş olabilir.";
    }

    /// <summary>
    /// Hafıza string'i oluşturur
    /// </summary>
    private string BuildMemoryString()
    {
        if (npcData.gossipMemory.Count == 0)
        {
            return "- Yabancı hakkında bir şey duymadın";
        }

        string memory = "";
        foreach (var mem in npcData.gossipMemory)
        {
            string beliefLevel = mem.believability > 70 ? "buna kesinlikle inanıyorsun" : 
                                 mem.believability > 40 ? "buna biraz inanıyorsun" : "bundan şüphelisin";
            
            string sourceDesc = mem.heardFrom == "kendi gözlerimle gördüm" 
                ? "bunu kendi gözünle gördün" 
                : $"bunu {mem.heardFrom}'dan duydun";
                
            memory += $"- \"{mem.gossip.content}\" ({sourceDesc}, {beliefLevel})\n";
        }
        return memory;
    }

    /// <summary>
    /// Dedikodu duyduğunda LLM ile yorum üretir
    /// </summary>
    public void InterpretGossipWithLLM(Gossip gossip, System.Action<string> callback)
    {
        if (AIClient.Instance == null)
        {
            // Fallback - LLM yoksa statik yorumlama
            callback(InterpretGossipFallback(gossip));
            return;
        }

        string personalityDesc = GetPersonalityDescription();
        
        string systemPrompt = $@"Sen {npcData.npcName} adında bir ortaçağ köylüsüsün.
Kişiliğin: {personalityDesc}

Köyde yeni bir dedikodu dolaşmaya başladı.
Dedikodu: ""{gossip.content}""

Senin görevin bu dedikoduyu karakterine göre yorumlamak:
- Kişiliğine göre abart, küçült, çarpıt veya korkup yarım anlat.
- Sadece 1-2 cümle konuş, Türkçe cevap ver.";

        StartCoroutine(AIClient.Instance.AskLLM(systemPrompt, "Bu dedikodu hakkında ne düşünüyorsun?", (response) =>
        {
            if (string.IsNullOrEmpty(response) || response == "...")
            {
                callback(InterpretGossipFallback(gossip));
            }
            else
            {
                callback(response);
            }
        }));
    }

    /// <summary>
    /// Kişilik açıklaması oluşturur
    /// </summary>
    private string GetPersonalityDescription()
    {
        string desc = "";
        
        switch (npcData.personalityType)
        {
            case NPCData.PersonalityType.Gossiper:
                desc = "Dedikoducu - her şeyi abartarak anlatırsın, detaylar eklersin";
                break;
            case NPCData.PersonalityType.Trustworthy:
                desc = "Güvenilir - olduğu gibi anlatırsın, yorum katmazsın";
                break;
            case NPCData.PersonalityType.Liar:
                desc = "Yalancı - hikayeyi tamamen değiştirirsin, kendi çıkarına göre çarpıtırsın";
                break;
            case NPCData.PersonalityType.Timid:
                desc = "Korkak - yarım yamalak anlatırsın, korkudan bazı şeyleri söyleyemezsin";
                break;
            case NPCData.PersonalityType.Sycophant:
                desc = "Dalkavuk - güçlüden yana yorumlarsın";
                break;
            case NPCData.PersonalityType.Angry:
                desc = "Sinirli - agresif yorumlarsın, suçlamalar yaparsın";
                break;
        }
        
        if (npcData.cowardice > 70) desc += ", çok korkaksın";
        if (npcData.aggressiveness > 70) desc += ", çok agresifsin";
        if (npcData.gossipLoving > 70) desc += ", dedikodu yapmayı çok seversin";
        
        return desc;
    }

    /// <summary>
    /// LLM olmadan dedikodu yorumlama (Fallback)
    /// </summary>
    private string InterpretGossipFallback(Gossip gossip)
    {
        string baseContent = gossip.content;
        
        switch (npcData.personalityType)
        {
            case NPCData.PersonalityType.Gossiper:
                return $"Duydun mu?! {baseContent}! Ve dahası var... ama söyleyemem!";
                
            case NPCData.PersonalityType.Trustworthy:
                return $"Şöyle bir şey duydum: {baseContent}. Ama doğruluğundan emin değilim.";
                
            case NPCData.PersonalityType.Liar:
                return $"Aslında olay öyle değil... {baseContent.Replace("o", "onun düşmanı")}!";
                
            case NPCData.PersonalityType.Timid:
                return $"Ş-şey... bir şeyler duydum ama... söylememeliyim... {baseContent.Substring(0, Mathf.Min(20, baseContent.Length))}...";
                
            case NPCData.PersonalityType.Angry:
                return $"Evet! {baseContent}! Ve bunun cezasını çekmeli!";
                
            case NPCData.PersonalityType.Sycophant:
                return $"Efendim öyle diyorsa doğrudur: {baseContent}.";
                
            default:
                return $"Duyduğuma göre {baseContent}.";
        }
    }

    /// <summary>
    /// NPC'nin oyuncuya verdiği cevap (Fallback - LLM çalışmazsa)
    /// </summary>
    private string GenerateNPCReply(ResponseType playerQuestion)
    {
        switch (playerQuestion)
        {
            case ResponseType.Greeting:
                return GetGreetingBasedOnOpinion();

            case ResponseType.AskAboutGossip:
                return ShareGossipWithPlayer();

            case ResponseType.Defense:
                return RespondToDefense();

            default:
                return "...";
        }
    }

    /// <summary>
    /// Oyuncuya dedikodu anlatır
    /// </summary>
    private string ShareGossipWithPlayer()
    {
        if (npcData.gossipMemory.Count == 0)
            return "Pek bir şey duymadım.";

        // En yüksek believability'e sahip dedikodu
        var mostBelieved = npcData.gossipMemory.OrderByDescending(m => m.believability).First();

        // Gossipy tipiyse abartarak anlatır
        if (npcData.gossipLoving > 70)
        {
            return $"Duydum ki, {mostBelieved.gossip.content}! Ve bu kesinlikle doğru!";
        }
        else if (npcData.trustworthiness > 70)
        {
            return $"Şunu duydum: {mostBelieved.gossip.content}. Ama emin değilim.";
        }
        else
        {
            return $"{mostBelieved.gossip.content}... sanırım.";
        }
    }

    /// <summary>
    /// Oyuncunun sorusuna cevap - "Benim hakkımda ne duydun?"
    /// VillageCharacters sistemini kullanır, kaynak söylemez
    /// </summary>
    private string RespondToDefense()
    {
        // Evrimleşmiş dedikoduyu kullan
        string gossipContent = npcData.currentGossipVersion;
        
        // Fallback - eski sistemden al
        if (string.IsNullOrEmpty(gossipContent) && npcData.gossipMemory.Count > 0)
        {
            gossipContent = npcData.gossipMemory[0].gossip.content;
        }
        
        if (string.IsNullOrEmpty(gossipContent))
            return "Senin hakkında bir şey duymadım.";

        // VillageCharacters fallback sistemini kullan
        return VillageCharacters.GetFallbackResponse(
            npcData.villageCharacterType, 
            gossipContent, 
            npcData.isGossipOriginator
        );
    }

    /// <summary>
    /// Debug için NPC'nin zihinsel durumunu döndürür
    /// </summary>
    public string GetContextString()
    {
        string context = "";
        context += $"Name: {npcData.npcName}\n";
        context += $"Personality: {npcData.personalityType}\n";
        context += $"Role: {npcData.role}\n";
        context += $"Opinion: {npcData.opinionScore} ({npcData.GetOpinionStatus()})\n";
        context += $"Traits: Trust={npcData.trustworthiness}, Gossip={npcData.gossipLoving}, Aggro={npcData.aggressiveness}, Coward={npcData.cowardice}\n";
        context += "Memory:\n";
        foreach (var mem in npcData.gossipMemory)
        {
            context += $"- Heard: {mem.gossip.content} (Belief: {mem.believability}%, Reaction: {mem.reaction})\n";
        }
        return context;
    }

    /// <summary>
    /// LLM için prompt oluşturur (gelecekteki entegrasyon için)
    /// </summary>
    public string GenerateLLMPrompt(string playerInput)
    {
        string prompt = $@"
You are {npcData.npcName}, a {npcData.role} in a medieval village.

Your personality type is: {npcData.personalityType}
Your traits:
- Trustworthiness: {npcData.trustworthiness}/100
- Gossip Loving: {npcData.gossipLoving}/100
- Aggressiveness: {npcData.aggressiveness}/100

Your opinion of the player: {npcData.opinionScore}/100 ({npcData.GetOpinionStatus()})

Gossips you know:
{string.Join("\n", npcData.gossipMemory.Select(m => $"- {m.gossip.content} (believability: {m.believability}%)"))}

The player says: ""{playerInput}""

Respond in character. Your response should reflect your personality and opinion of the player.
";
        return prompt;
    }
}

[System.Serializable]
public class DialogueResponse
{
    public ResponseType responseType;
    public string text;
    public int opinionChange;
}

public enum ResponseType
{
    Greeting,
    AskAboutGossip,
    Defense,
    Accusation,
    Bribe,
    Threaten,
    Befriend
}
