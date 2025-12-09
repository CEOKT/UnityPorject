# GrafikGame - Unity Dedikodu Oyunu 

Unity tabanlı NPC konuşma sistemi ve LLM entegrasyonlu dedikodu bulma oyunu.

##  Özellikler

- **LM Studio Entegrasyonu**: Yerel LLM (ibm/granite-3.2-8b) ile doğal NPC konuşmaları
- **6 Karakterli Köy**: Bakkal Emmi, Emine Teyze, Çırak Ceyhun, İmam Hoca, Muhtar, Fırıncı Hasan
- **Dedikodu Sistemi**: NPC'ler arasında yayılan dinamik dedikodu mekanizması
- **Konuşma Balonu UI**: Klavye girişli, modern konuşma arayüzü
- **NavMesh AI**: Özgürce dolaşan NPC'ler
- **Billboard Name Tags**: Her NPC'nin başında isim etiketi
- **URP Graphics**: Universal Render Pipeline ile modern grafikler

##  Kurulum

1. Unity 2022.3+ gerekli
2. LM Studio'yu indirin ve başlatın: http://127.0.0.1:1234
3. \ibm/granite-3.2-8b\ modelini yükleyin
4. Projeyi açın
5. **Tools  Scene Organizer  Setup Complete Scene**
6. Play!

##  Nasıl Oynanır?

1. WASD ile hareket edin
2. NPC'ye yaklaşın
3. **E** tuşuna basarak konuşmaya başlayın
4. Sorunuzu yazın ve **GÖNDER** veya **Enter**
5. Dedikodu kaynağını bulun!

##  Proje Yapısı

- \Assets/Scripts/AI/\: LLM client
- \Assets/Scripts/NPC/\: NPC davranışları, konuşma, dedikodu
- \Assets/Scripts/UI/\: DialogueManager, Billboard
- \Assets/Editor/\: SceneOrganizer (otomatik sahne kurulumu)

##  Teknik Detaylar

- **Engine**: Unity 2022.3+
- **Render Pipeline**: URP
- **AI**: NavMesh
- **LLM**: LM Studio REST API
- **UI**: TextMeshPro

##  Lisans

MIT License - İstediğiniz gibi kullanın!
