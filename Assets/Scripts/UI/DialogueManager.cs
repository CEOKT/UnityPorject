using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro; // TextMeshPro kullanacağız

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI Components")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI npcNameText;
    public TextMeshProUGUI dialogueText;
    public Button[] optionButtons;
    public TextMeshProUGUI[] optionTexts;
    
    [Header("Input Components")]
    public TMP_InputField inputField;
    public Button sendButton;

    private NPCController currentNPC;
    private bool isDialogueActive = false;
    
    public bool IsDialogueActive => isDialogueActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Eski veya bozuk UI kalıntılarını temizle
        Transform existingPanel = transform.Find("DialoguePanel");
        if (existingPanel != null)
        {
            Debug.Log("🗑️ Eski DialoguePanel bulundu, siliniyor...");
            DestroyImmediate(existingPanel.gameObject);
        }

        // Temiz bir sayfa aç ve UI'ı kodla oluştur
        CreateDialogueUI();
        
        // Başlangıçta paneli gizle
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void StartDialogue(NPCController npc)
    {
        if (dialoguePanel == null)
        {
            Debug.LogError("[DialogueManager] dialoguePanel is NULL! SceneOrganizer düzgün çalıştı mı?");
            return;
        }

        currentNPC = npc;
        isDialogueActive = true;
        
        // UI'ı aç
        dialoguePanel.SetActive(true);
        Debug.Log("[DialogueManager] Dialogue panel açıldı!");
        
        // Canvas'ı kontrol et
        Canvas canvas = dialoguePanel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.gameObject.SetActive(true);
            Debug.Log($"[DialogueManager] Canvas bulundu: {canvas.name}");
        }
        else
        {
            Debug.LogError("[DialogueManager] Canvas bulunamadı!");
        }
        
        // Mouse imlecini aç
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // NPC ismini göster
        if (npcNameText != null)
            npcNameText.text = npc.npcData.npcName;
        else
            Debug.LogError("[DialogueManager] npcNameText is NULL!");
        
        // Başlangıç mesajı
        if (dialogueText != null)
            dialogueText.text = "Merhaba, ne sormak istersin?";
        else
            Debug.LogError("[DialogueManager] dialogueText is NULL!");
        
        // Input alanını hazırla
        if (inputField != null)
        {
            inputField.text = "";
            inputField.gameObject.SetActive(true);
            inputField.interactable = true;
            inputField.ActivateInputField();
            Debug.Log($"[DialogueManager] InputField hazır: {inputField.gameObject.name}, Active: {inputField.gameObject.activeInHierarchy}");
        }
        else
        {
            Debug.LogError("[DialogueManager] inputField is NULL!");
        }

        if (sendButton != null)
        {
            sendButton.gameObject.SetActive(true);
            sendButton.interactable = true;
            sendButton.onClick.RemoveAllListeners();
            sendButton.onClick.AddListener(OnSendButtonClicked);
            Debug.Log($"[DialogueManager] SendButton hazır: {sendButton.gameObject.name}, Active: {sendButton.gameObject.activeInHierarchy}");
        }
        else
        {
            Debug.LogError("[DialogueManager] sendButton is NULL!");
        }

        // Butonları gizle (sadece kapat butonu kalsın)
        HideAllButtons();
        ShowCloseButton();
    }

    public void OnSendButtonClicked()
    {
        if (currentNPC == null || inputField == null || string.IsNullOrWhiteSpace(inputField.text)) return;

        string question = inputField.text;
        inputField.text = ""; // Temizle
        inputField.interactable = false; // Cevap gelene kadar kilitle
        if (sendButton != null) sendButton.interactable = false;

        dialogueText.text = "Düşünüyor...";

        currentNPC.dialogue.GetLLMResponse(question, (response) =>
        {
            StopAllCoroutines();
            StartCoroutine(TypeSentence(response));
            
            // Input'u tekrar aç
            if (inputField != null) inputField.interactable = true;
            if (sendButton != null) sendButton.interactable = true;
            
            // Focus
            if (inputField != null) inputField.ActivateInputField();
        });
    }

    private void HideAllButtons()
    {
        foreach (var btn in optionButtons)
        {
            if (btn != null)
                btn.gameObject.SetActive(false);
        }
    }

    private void ShowCloseButton()
    {
        // İlk butonu "Kapat" olarak ayarla
        if (optionButtons.Length > 0 && optionButtons[0] != null)
        {
            optionButtons[0].gameObject.SetActive(true);
            optionTexts[0].text = "[ESC] Kapat";
            optionButtons[0].onClick.RemoveAllListeners();
            optionButtons[0].onClick.AddListener(EndDialogue);
        }
    }

    private void ShowOptions()
    {
        // Artık kullanılmıyor - direkt cevap veriliyor
    }

    private void SetOption(int index, string text, UnityEngine.Events.UnityAction action)
    {
        if (index < optionButtons.Length)
        {
            optionButtons[index].gameObject.SetActive(true);
            optionTexts[index].text = text;
            optionButtons[index].onClick.RemoveAllListeners();
            optionButtons[index].onClick.AddListener(action);
        }
    }

    public void SelectOption(ResponseType type)
    {
        // Artık kullanılmıyor
    }

    private void SetButtonsInteractable(bool interactable)
    {
        foreach (var btn in optionButtons)
        {
            if (btn != null)
                btn.interactable = interactable;
        }
    }

    private void Update()
    {
        if (!isDialogueActive) return;
        
        // ESC ile diyaloğu kapat
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            EndDialogue();
        }
        
        // Enter ile mesaj gönder
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (inputField != null && inputField.interactable && !string.IsNullOrWhiteSpace(inputField.text))
            {
                OnSendButtonClicked();
            }
        }
    }

    IEnumerator TypeSentence(string sentence)
    {
        dialogueText.text = "";
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(0.02f);
        }
    }

    public void EndDialogue()
    {
        isDialogueActive = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        
        // Mouse imlecini kilitle (Oyun moduna dön)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (currentNPC != null)
        {
            currentNPC.EndConversation();
            currentNPC = null;
        }
    }

    private void CreateDialogueUI()
    {
        Debug.Log("⚠️ Dialogue UI bulunamadı, otomatik oluşturuluyor... (Yeni Tasarım: Alt Bant + Sağ Üst İsim)");

        // Canvas kontrolü
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();
        }

        // 1. Dialogue Panel (Container) - EKRAN KAPLAYAN BOSLUK
        GameObject panelObj = new GameObject("DialoguePanel");
        panelObj.transform.SetParent(this.transform, false);
        Image panelImg = panelObj.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0); // Şeffaf
        panelImg.raycastTarget = false;
        
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        this.dialoguePanel = panelObj;

        // 2. ALT BANT (Dialogue Box)
        // Ekranın altını boydan boya kaplasın
        GameObject bubble = new GameObject("DialogueBox");
        bubble.transform.SetParent(panelObj.transform, false);
        Image bubbleImg = bubble.AddComponent<Image>();
        bubbleImg.color = new Color(0.05f, 0.05f, 0.05f, 0.9f); // Neredeyse siyah
        
        // Üst kenarına ince bir çizgi (Border)
        GameObject borderTop = new GameObject("BorderTop");
        borderTop.transform.SetParent(bubble.transform, false);
        Image borderImg = borderTop.AddComponent<Image>();
        borderImg.color = new Color(1f, 0.8f, 0.2f); // Altın
        RectTransform borderRT = borderTop.GetComponent<RectTransform>();
        borderRT.anchorMin = new Vector2(0, 1); borderRT.anchorMax = new Vector2(1, 1);
        borderRT.sizeDelta = new Vector2(0, 3); // 3 pixel yükseklik
        borderRT.pivot = new Vector2(0.5f, 1);
        borderRT.anchoredPosition = Vector2.zero;

        RectTransform bubbleRT = bubble.GetComponent<RectTransform>();
        // Ekranın alt %25'i
        bubbleRT.anchorMin = new Vector2(0f, 0f); 
        bubbleRT.anchorMax = new Vector2(1f, 0.25f);
        bubbleRT.offsetMin = Vector2.zero;
        bubbleRT.offsetMax = Vector2.zero;

        // İSİM PANELİ (Sağ Üst Köşe - Balonun İçinde ama Üste Yapışık)
        GameObject nameObj = new GameObject("NameBox");
        nameObj.transform.SetParent(bubble.transform, false);
        Image nameBg = nameObj.AddComponent<Image>();
        nameBg.color = new Color(1f, 0.8f, 0.2f); // Altın Sarısı Zemin
        
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        // Sağ Üst (Right Top)
        nameRect.anchorMin = new Vector2(1f, 1f); 
        nameRect.anchorMax = new Vector2(1f, 1f); 
        nameRect.pivot = new Vector2(1f, 0f); // Köşesi panele takılsın diye pivot aşağıda
        nameRect.anchoredPosition = new Vector2(-50, 0); // Biraz içeride
        nameRect.sizeDelta = new Vector2(300, 50);

        // İsim Metni
        GameObject nameTextObj = new GameObject("NameText");
        nameTextObj.transform.SetParent(nameObj.transform, false);
        TextMeshProUGUI nameTxt = nameTextObj.AddComponent<TextMeshProUGUI>();
        nameTxt.fontSize = 32;
        nameTxt.fontStyle = FontStyles.Bold;
        nameTxt.color = Color.black; // Siyah yazı (Sarı üstüne)
        nameTxt.alignment = TextAlignmentOptions.Center;
        
        RectTransform nameTextRect = nameTextObj.GetComponent<RectTransform>();
        nameTextRect.anchorMin = Vector2.zero; nameTextRect.anchorMax = Vector2.one;
        nameTextRect.offsetMin = Vector2.zero; nameTextRect.offsetMax = Vector2.zero;
        this.npcNameText = nameTxt;

        // Mesaj Metni (Ortada Geniş)
        GameObject msgObj = new GameObject("Message");
        msgObj.transform.SetParent(bubble.transform, false);
        TextMeshProUGUI msgTxt = msgObj.AddComponent<TextMeshProUGUI>();
        msgTxt.fontSize = 28;
        msgTxt.color = new Color(0.9f, 0.9f, 0.9f); // Kırık beyaz
        msgTxt.alignment = TextAlignmentOptions.TopLeft;
        msgTxt.enableWordWrapping = true;
        
        RectTransform msgRect = msgObj.GetComponent<RectTransform>();
        msgRect.anchorMin = new Vector2(0.1f, 0.1f);
        msgRect.anchorMax = new Vector2(0.9f, 0.85f); // Biraz boşluk bırak
        this.dialogueText = msgTxt;

        // Close Button (Sağ Alt Köşe - Küçük)
        GameObject closeBtnObj = new GameObject("CloseButton");
        closeBtnObj.transform.SetParent(bubble.transform, false);
        Image closeImg = closeBtnObj.AddComponent<Image>();
        closeImg.color = new Color(0.8f, 0.2f, 0.2f, 0.0f); // Görünmez alan
        Button closeBtn = closeBtnObj.AddComponent<Button>();
        
        RectTransform closeRT = closeBtnObj.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(0.95f, 0.05f);
        closeRT.anchorMax = new Vector2(0.99f, 0.25f);
        
        GameObject closeTxtObj = new GameObject("Text");
        closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
        TextMeshProUGUI closeTxt = closeTxtObj.AddComponent<TextMeshProUGUI>();
        closeTxt.text = "[ESC] Kapat";
        closeTxt.alignment = TextAlignmentOptions.Right;
        closeTxt.fontSize = 16;
        closeTxt.color = new Color(1f, 1f, 1f, 0.5f);
        
        RectTransform closeTxtRT = closeTxtObj.GetComponent<RectTransform>();
        closeTxtRT.anchorMin = Vector2.zero; closeTxtRT.anchorMax = Vector2.one;

        this.optionButtons = new Button[] { closeBtn };
        this.optionTexts = new TextMeshProUGUI[] { closeTxt };


        // 3. Input Bar (Diyalog Kutusunun ÜSTÜNE YAPIŞIK)
        GameObject inputBar = new GameObject("InputBar");
        inputBar.transform.SetParent(panelObj.transform, false);
        Image inputBarImg = inputBar.AddComponent<Image>();
        inputBarImg.color = new Color(0f, 0f, 0f, 0.5f);
        
        RectTransform inputBarRT = inputBar.GetComponent<RectTransform>();
        // Alt %25'in hemen üstü
        inputBarRT.anchorMin = new Vector2(0.2f, 0.25f); 
        inputBarRT.anchorMax = new Vector2(0.8f, 0.32f); 

        // InputField
        GameObject inputObj = new GameObject("InputField");
        inputObj.transform.SetParent(inputBar.transform, false);
        Image inputImg = inputObj.AddComponent<Image>();
        inputImg.color = new Color(1f, 1f, 1f, 0.1f);
        
        TMP_InputField inputFieldComp = inputObj.AddComponent<TMP_InputField>();
        this.inputField = inputFieldComp;
        
        RectTransform inputRect = inputObj.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.02f, 0.1f);
        inputRect.anchorMax = new Vector2(0.85f, 0.9f);
        
        // TextArea
        GameObject textArea = new GameObject("TextArea");
        textArea.transform.SetParent(inputObj.transform, false);
        RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero; textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(10, 5); textAreaRect.offsetMax = new Vector2(-10, -5);
        inputFieldComp.textViewport = textAreaRect;

        GameObject inputTextObj = new GameObject("Text");
        inputTextObj.transform.SetParent(textArea.transform, false);
        TextMeshProUGUI inputText = inputTextObj.AddComponent<TextMeshProUGUI>();
        inputText.fontSize = 20;
        inputText.color = Color.white;
        inputFieldComp.textComponent = inputText;
        
        RectTransform inputTextRT = inputTextObj.GetComponent<RectTransform>();
        inputTextRT.anchorMin = Vector2.zero; inputTextRT.anchorMax = Vector2.one;

        // Placeholder
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(textArea.transform, false);
        TextMeshProUGUI placeholderTxt = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholderTxt.text = "Bir soru sor...";
        placeholderTxt.fontSize = 20;
        placeholderTxt.color = new Color(1f,1f,1f,0.5f);
        placeholderTxt.fontStyle = FontStyles.Italic;
        inputFieldComp.placeholder = placeholderTxt;
        RectTransform phRT = placeholderObj.GetComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;

        // Send Button
        GameObject sendBtnObj = new GameObject("SendButton");
        sendBtnObj.transform.SetParent(inputBar.transform, false);
        Image sendImg = sendBtnObj.AddComponent<Image>();
        sendImg.color = new Color(0.2f, 0.8f, 0.2f);
        Button sendBtn = sendBtnObj.AddComponent<Button>();
        this.sendButton = sendBtn;
        
        RectTransform sendRT = sendBtnObj.GetComponent<RectTransform>();
        sendRT.anchorMin = new Vector2(0.86f, 0.1f); sendRT.anchorMax = new Vector2(0.98f, 0.9f);
        
        GameObject sendTxtObj = new GameObject("Text");
        sendTxtObj.transform.SetParent(sendBtnObj.transform, false);
        TextMeshProUGUI sendTxt = sendTxtObj.AddComponent<TextMeshProUGUI>();
        sendTxt.text = "SOR";
        sendTxt.fontSize = 18;
        sendTxt.alignment = TextAlignmentOptions.Center;
        sendTxt.color = Color.black; 
        RectTransform sendTxtRT = sendTxtObj.GetComponent<RectTransform>();
        sendTxtRT.anchorMin = Vector2.zero; sendTxtRT.anchorMax = Vector2.one;

        Debug.Log("✅ UI Hazır: Tam Alt Bant + Sağ Üst İsim Etiketi.");
    }
}
