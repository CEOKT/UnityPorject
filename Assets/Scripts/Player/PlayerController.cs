using UnityEngine;

/// <summary>
/// Oyuncu karakter kontrolü ve NPC etkileşimi
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;

    [Header("Interaction")]
    public float interactionRange = 3f;
    public LayerMask npcLayer;
    public KeyCode interactionKey = KeyCode.E;

    [Header("Camera")]
    public Transform cameraTransform;

    private CharacterController controller;
    private Vector3 velocity;
    private NPCController currentNearbyNPC;

    private void Start()
    {
        // 1. Önce Player tag'ini garantiye al
        if (!gameObject.CompareTag("Player"))
        {
            gameObject.tag = "Player";
            Debug.Log("PlayerController: Player tag'i otomatik atandı.");
        }

        controller = GetComponent<CharacterController>();
        
        // 2. Kamera referansını bul ve onar
        if (cameraTransform == null)
        {
            // A) Camera.main kontrolü
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                cameraTransform = mainCam.transform;
            }
            else
            {
                // B) Tag ile bulmayı dene
                GameObject camObj = GameObject.FindGameObjectWithTag("MainCamera");
                if (camObj != null)
                {
                    cameraTransform = camObj.transform;
                }
                else
                {
                    // C) Herhangi bir kamera bul
                    Camera anyCam = Object.FindAnyObjectByType<Camera>();
                    if (anyCam != null)
                    {
                        cameraTransform = anyCam.transform;
                        
                        // Tag düzelt
                        if (anyCam.gameObject.tag != "MainCamera")
                        {
                            anyCam.gameObject.tag = "MainCamera";
                            Debug.Log("PlayerController: Kamera bulundu ve 'MainCamera' olarak etiketlendi.");
                        }
                    }
                }
            }

            // 3. Kamera bulunduysa ThirdPersonCamera kontrolü yap
            if (cameraTransform != null)
            {
                ThirdPersonCamera tpCam = cameraTransform.GetComponent<ThirdPersonCamera>();
                if (tpCam == null)
                {
                    tpCam = cameraTransform.gameObject.AddComponent<ThirdPersonCamera>();
                    Debug.Log("PlayerController: Kameraya ThirdPersonCamera scripti eklendi.");
                }
                
                // Target'ı zorla ata
                if (tpCam.target == null)
                {
                    tpCam.target = this.transform;
                }
            }
            else
            {
                Debug.LogWarning("PlayerController: Main Camera bulunamadı! Kamera olmadan hareket edecek.");
            }
        }

        Debug.Log($"PlayerController başlatıldı. Position: {transform.position}");
    }

    private void Update()
    {
        HandleMovement();
        HandleInteraction();
    }

    /// <summary>
    /// Karakter hareketi
    /// </summary>
    private void HandleMovement()
    {
        // Input - WASD tuşları
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.W)) vertical = 1f;
        if (Input.GetKey(KeyCode.S)) vertical = -1f;
        if (Input.GetKey(KeyCode.A)) horizontal = -1f;
        if (Input.GetKey(KeyCode.D)) horizontal = 1f;

        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        // Hareket yönü (dünya koordinatlarına göre - basit)
        Vector3 moveDirection = new Vector3(horizontal, 0, vertical).normalized;
        
        // Hız
        float currentSpeed = isRunning ? runSpeed : moveSpeed;
        Vector3 move = moveDirection * currentSpeed;

        // Hareket uygula
        if (controller != null)
        {
            controller.Move(move * Time.deltaTime);

            // Karakteri hareket yönüne döndür
            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // Yerçekimi
            if (controller.isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }
    }

    /// <summary>
    /// NPC etkileşim sistemi
    /// </summary>
    private void HandleInteraction()
    {
        // Yakındaki NPC'yi bul
        FindNearbyNPC();

        // E tuşuna basıldı ve NPC var
        if (Input.GetKeyDown(interactionKey))
        {
            Debug.Log($"[DEBUG] E pressed. currentNearbyNPC: {(currentNearbyNPC != null ? currentNearbyNPC.npcData?.npcName : "NULL")}");
            
            if (currentNearbyNPC != null)
            {
                InteractWithNPC(currentNearbyNPC);
            }
        }
    }

    /// <summary>
    /// Yakındaki NPC'yi tespit et
    /// </summary>
    private void FindNearbyNPC()
    {
        // Debug: LayerMask değerini kontrol et
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log($"[DEBUG] npcLayer mask value: {npcLayer.value}");
            Debug.Log($"[DEBUG] NPC Layer index: {LayerMask.NameToLayer("NPC")}");
            
            // Tüm NPC'leri bul ve layer'larını yazdır
            NPCController[] allNPCs = Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None);
            foreach (var npc in allNPCs)
            {
                Debug.Log($"[DEBUG] NPC: {npc.npcData?.npcName ?? npc.name}, Layer: {npc.gameObject.layer} ({LayerMask.LayerToName(npc.gameObject.layer)})");
            }
        }

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactionRange, npcLayer);
        
        if (hitColliders.Length > 0)
        {
            // En yakın NPC'yi bul
            float closestDistance = Mathf.Infinity;
            NPCController closestNPC = null;

            foreach (Collider col in hitColliders)
            {
                NPCController npc = col.GetComponent<NPCController>();
                if (npc != null)
                {
                    float distance = Vector3.Distance(transform.position, col.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestNPC = npc;
                    }
                }
            }

            currentNearbyNPC = closestNPC;
        }
        else
        {
            currentNearbyNPC = null;
        }
    }

    /// <summary>
    /// NPC ile etkileşime geç
    /// </summary>
    private void InteractWithNPC(NPCController npc)
    {
        Debug.Log($"Interacting with {npc.npcData.npcName}");
        npc.OnPlayerInteract();

        // Burada UI dialogue sistemi açılacak
        OpenDialogueUI(npc);
    }

    /// <summary>
    /// Dialogue UI'ı aç
    /// </summary>
    private void OpenDialogueUI(NPCController npc)
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(npc);
        }
        else
        {
            Debug.LogError("DialogueManager bulunamadı! Sahneyi tekrar kurun.");
        }
    }

    /// <summary>
    /// Dialogue seçimi yap (test için)
    /// </summary>
    public void SelectDialogueOption(int optionIndex)
    {
        if (currentNearbyNPC == null) return;

        ResponseType responseType = ResponseType.Greeting;
        
        switch (optionIndex)
        {
            case 1:
                responseType = ResponseType.Greeting;
                break;
            case 2:
                responseType = ResponseType.AskAboutGossip;
                break;
            case 3:
                responseType = ResponseType.Defense;
                break;
        }

        string npcReply = currentNearbyNPC.dialogue.SelectResponse(responseType);
        Debug.Log($"{currentNearbyNPC.npcData.npcName}: {npcReply}");
    }

    /// <summary>
    /// Konuşmayı bitir
    /// </summary>
    public void EndCurrentConversation()
    {
        if (currentNearbyNPC != null)
        {
            currentNearbyNPC.EndConversation();
            currentNearbyNPC = null;
        }
    }

    /// <summary>
    /// Debug için Gizmos
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Etkileşim mesafesi
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
