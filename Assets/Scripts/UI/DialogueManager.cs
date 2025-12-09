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
        // Referansları otomatik bul (Editor'da atanmamışsa)
        if (dialoguePanel == null)
        {
            Transform panelTransform = transform.Find("DialoguePanel");
            if (panelTransform != null)
            {
                dialoguePanel = panelTransform.gameObject;
                
                // YENİ KONUŞMA BALONU YAPISI
                Transform bubbleTransform = panelTransform.Find("SpeechBubble");
                if (bubbleTransform != null)
                {
                    // Balon içindeki elemanlar
                    Transform nameTransform = bubbleTransform.Find("Name");
                    if (nameTransform != null) npcNameText = nameTransform.GetComponent<TextMeshProUGUI>();
                    
                    Transform messageTransform = bubbleTransform.Find("Message");
                    if (messageTransform != null) dialogueText = messageTransform.GetComponent<TextMeshProUGUI>();
                    
                    // Kapat butonu balonun içinde
                    Transform closeTransform = bubbleTransform.Find("CloseButton");
                    if (closeTransform != null)
                    {
                        optionButtons = new Button[1];
                        optionTexts = new TextMeshProUGUI[1];
                        optionButtons[0] = closeTransform.GetComponent<Button>();
                        optionTexts[0] = closeTransform.GetComponentInChildren<TextMeshProUGUI>();
                    }
                }
                
                // Input bar (balonun dışında, alt tarafta)
                Transform inputBarTransform = panelTransform.Find("InputBar");
                if (inputBarTransform != null)
                {
                    Transform inputTransform = inputBarTransform.Find("InputField");
                    if (inputTransform != null) inputField = inputTransform.GetComponent<TMP_InputField>();
                    
                    Transform sendTransform = inputBarTransform.Find("SendButton");
                    if (sendTransform != null) sendButton = sendTransform.GetComponent<Button>();
                }
                
                Debug.Log("✅ DialogueManager referansları otomatik bulundu! (Konuşma Balonu Modu)");
            }
            else
            {
                Debug.LogError("❌ DialoguePanel bulunamadı! SceneOrganizer çalıştırılmalı.");
            }
        }
        
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
        // ESC ile diyaloğu kapat
        if (isDialogueActive && Input.GetKeyDown(KeyCode.Escape))
        {
            EndDialogue();
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
        dialoguePanel.SetActive(false);
        
        // Mouse imlecini kilitle (Oyun moduna dön)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (currentNPC != null)
        {
            currentNPC.EndConversation();
            currentNPC = null;
        }
    }
}
