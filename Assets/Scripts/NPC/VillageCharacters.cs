using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Köy karakterleri tanımlamaları ve kişilik özellikleri
/// </summary>
public static class VillageCharacters
{
    public enum CharacterType
    {
        BakkalEmmi,     // Şüpheci, kızgın
        EmineTeyze,     // Abartan, dedikodu sever
        CirakCeyhun,    // Genç, heyecanlı
        ImamHoca,       // Sakin, temkinli
        Muhtar,         // Resmi, politik
        FirinciHasan    // Yanlış anlayan, panikçi
    }

    [System.Serializable]
    public class CharacterInfo
    {
        public string name;
        public CharacterType type;
        public string personality;
        public string speechStyle;
        public int gossipDistortion;    // Dedikodu çarpıtma oranı (0-100)
        public string[] exampleLines;
    }

    /// <summary>
    /// Karakter tanımlamaları
    /// </summary>
    public static Dictionary<CharacterType, CharacterInfo> Characters = new Dictionary<CharacterType, CharacterInfo>
    {
        {
            CharacterType.BakkalEmmi, new CharacterInfo
            {
                name = "Bakkal Emmi",
                type = CharacterType.BakkalEmmi,
                personality = "Şüpheci, kızgın, kimseye güvenmeyen",
                speechStyle = "Kısa, ters cümleler",
                gossipDistortion = 30,
                exampleLines = new string[] { "Belliydi zaten.", "Güvenilmez tip." }
            }
        },
        {
            CharacterType.EmineTeyze, new CharacterInfo
            {
                name = "Emine Teyze",
                type = CharacterType.EmineTeyze,
                personality = "Abartan, dedikodu sever, her şeyi büyütür",
                speechStyle = "Ünlemli, vallahi billahi",
                gossipDistortion = 80,
                exampleLines = new string[] { "Vallahi gördüm!", "Kan gövdeyi götürdü!" }
            }
        },
        {
            CharacterType.CirakCeyhun, new CharacterInfo
            {
                name = "Çırak Ceyhun",
                type = CharacterType.CirakCeyhun,
                personality = "Genç, heyecanlı, abartıcı",
                speechStyle = "Argo, 'abi' lafı çok",
                gossipDistortion = 70,
                exampleLines = new string[] { "Abi inanılmaz!", "Fena abi fena!" }
            }
        },
        {
            CharacterType.ImamHoca, new CharacterInfo
            {
                name = "İmam Hoca",
                type = CharacterType.ImamHoca,
                personality = "Sakin, temkinli, hikmetli",
                speechStyle = "Nasihat verir, Allah der",
                gossipDistortion = 15,
                exampleLines = new string[] { "Allah korusun.", "Hayırdır inşallah." }
            }
        },
        {
            CharacterType.Muhtar, new CharacterInfo
            {
                name = "Muhtar",
                type = CharacterType.Muhtar,
                personality = "Resmi, politik, belirsiz konuşur",
                speechStyle = "Diplomatik, net konuşmaz",
                gossipDistortion = 25,
                exampleLines = new string[] { "Durumu inceliyoruz.", "Herkes sakin olsun." }
            }
        },
        {
            CharacterType.FirinciHasan, new CharacterInfo
            {
                name = "Fırıncı Hasan",
                type = CharacterType.FirinciHasan,
                personality = "Yanlış anlayan, panikçi, karıştıran",
                speechStyle = "Sorulu, şaşkın",
                gossipDistortion = 90,
                exampleLines = new string[] { "Ha ne dedin?", "Kim ne yaptı?" }
            }
        }
    };

    /// <summary>
    /// Karakter bilgisi al
    /// </summary>
    public static CharacterInfo GetCharacterInfo(CharacterType type)
    {
        if (Characters.TryGetValue(type, out CharacterInfo info))
            return info;
        return null;
    }

    /// <summary>
    /// LLM için system prompt oluştur - KISA cevap için
    /// </summary>
    public static string GetSystemPrompt(CharacterType type, string gossipContent, bool isOriginator)
    {
        if (!Characters.TryGetValue(type, out CharacterInfo info))
            return "";

        return $@"Sen {info.name}. Köylüsün.
KARAKTERİN: {info.personality}
OLAY: ""{gossipContent}""

KURALLAR:
- SADECE 1-2 KISA CÜMLE SÖYLE
- ""Gördüm"" veya ""duydum"" de
- Karakterine göre abartabilirsin
- Kaynak SÖYLEME
- ÇOK KISA TUT";
    }

    /// <summary>
    /// Karakter tipine göre fallback cevap
    /// </summary>
    public static string GetFallbackResponse(CharacterType type, string gossipContent, bool isOriginator)
    {
        if (!Characters.TryGetValue(type, out CharacterInfo info))
            return gossipContent;

        switch (type)
        {
            case CharacterType.BakkalEmmi:
                return $"Gördüm. {gossipContent} Belliydi.";
                    
            case CharacterType.EmineTeyze:
                return $"Vallahi gördüm! {gossipContent}";
                    
            case CharacterType.CirakCeyhun:
                return $"Abi gördüm! {gossipContent}";
                    
            case CharacterType.ImamHoca:
                return $"Şahit oldum. {gossipContent}";
                    
            case CharacterType.Muhtar:
                return $"Gördüm. {gossipContent}";
                    
            case CharacterType.FirinciHasan:
                return $"Ha gördüm! {gossipContent}";
        }

        return gossipContent;
    }

    /// <summary>
    /// Dedikodu çarpıtması uygular
    /// </summary>
    public static string DistortGossip(CharacterType type, string originalGossip)
    {
        if (!Characters.TryGetValue(type, out CharacterInfo info))
            return originalGossip;

        // Düşük çarpıtma - olduğu gibi döndür
        if (info.gossipDistortion < 20)
            return originalGossip;

        // Yüksek çarpıtma - karakter bazlı değiştir
        switch (type)
        {
            case CharacterType.EmineTeyze:
                return originalGossip + " Vallahi!";
                
            case CharacterType.CirakCeyhun:
                return originalGossip + " Abi!";
                
            case CharacterType.FirinciHasan:
                return originalGossip + " Ha?";
                
            case CharacterType.BakkalEmmi:
                return originalGossip + " Belliydi.";
        }

        return originalGossip;
    }
}
