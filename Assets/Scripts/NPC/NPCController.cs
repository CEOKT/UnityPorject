using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NPC hareket ve davranış kontrolü
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class NPCController : MonoBehaviour
{
    [Header("NPC Components")]
    public NPCData npcData;
    public NPCDialogue dialogue;
    public Animator animator;  // Animasyon kontrolü
    
    [Header("Movement")]
    public NavMeshAgent agent;
    public float wanderRadius = 5f;       // Küçültüldü - yakın alanda kalsın
    public float minWanderWait = 3f;
    public float maxWanderWait = 8f;

    [Header("Daily Routine")]
    public Transform homePosition;
    public Transform workPosition;
    public float workStartHour = 8f;
    public float workEndHour = 18f;

    [Header("Visual")]
    public GameObject interactionIndicator; // UI ile gösterilecek

    private Vector3 currentDestination;
    private Vector3 spawnPosition;          // Başlangıç pozisyonu - bu noktadan uzaklaşamaz
    private float nextActionTime;
    private NPCState currentState = NPCState.Idle;
    private bool isWalking = false;

    private enum NPCState
    {
        Idle,
        Wandering,
        Working,
        Talking,
        Sleeping
    }

    private void Start()
    {
        InitializeNPC();
    }

    private void Update()
    {
        UpdateNPCBehavior();
        UpdateAnimations();
        CheckPlayerProximity();
    }

    /// <summary>
    /// NPC'yi başlat
    /// </summary>
    private void InitializeNPC()
    {
        // Components
        agent = GetComponent<NavMeshAgent>();
        dialogue = GetComponent<NPCDialogue>();
        animator = GetComponentInChildren<Animator>();

        if (dialogue == null)
        {
            dialogue = gameObject.AddComponent<NPCDialogue>();
        }

        dialogue.npcData = npcData;

        // Spawn pozisyonunu kaydet - bu noktadan uzaklaşamaz
        spawnPosition = transform.position;

        // İlk hedefe git
        SetRandomWanderDestination();
    }

    /// <summary>
    /// Animasyonları güncelle
    /// </summary>
    private void UpdateAnimations()
    {
        if (animator == null) return;

        // Hareket hızına göre yürüme/koşma animasyonu
        bool shouldWalk = agent.velocity.magnitude > 0.1f && currentState != NPCState.Talking;
        
        if (shouldWalk != isWalking)
        {
            isWalking = shouldWalk;
            
            // Animator parametrelerini ayarla (eğer varsa)
            // Bazı controller'lar farklı parametre isimleri kullanabilir
            if (animator.HasParameter("isWalking"))
                animator.SetBool("isWalking", isWalking);
            if (animator.HasParameter("isRunning"))
                animator.SetBool("isRunning", isWalking);
            if (animator.HasParameter("Run"))
                animator.SetBool("Run", isWalking);
            if (animator.HasParameter("Walk"))
                animator.SetBool("Walk", isWalking);
        }

        // Hız parametresi (blend tree için)
        if (animator.HasParameter("Speed"))
            animator.SetFloat("Speed", agent.velocity.magnitude);
    }

    /// <summary>
    /// Kazanma animasyonu oynat
    /// </summary>
    public void PlayWinAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("Win");
        }
    }

    /// <summary>
    /// Kaybetme animasyonu oynat
    /// </summary>
    public void PlayLoseAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("Lose");
        }
    }

    /// <summary>
    /// NPC davranış döngüsü
    /// </summary>
    private void UpdateNPCBehavior()
    {
        if (currentState == NPCState.Talking) return; // Konuşma sırasında hareket etme

        if (Time.time >= nextActionTime)
        {
            switch (currentState)
            {
                case NPCState.Idle:
                    DecideNextAction();
                    break;

                case NPCState.Wandering:
                    if (ReachedDestination())
                    {
                        currentState = NPCState.Idle;
                        nextActionTime = Time.time + Random.Range(minWanderWait, maxWanderWait);
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Sonraki aksiyonu belirle
    /// </summary>
    private void DecideNextAction()
    {
        // Günlük rutine göre karar ver
        float currentHour = GetCurrentGameHour();

        if (currentHour >= workStartHour && currentHour < workEndHour)
        {
            // İş saatleri
            if (workPosition != null)
            {
                GoToLocation(workPosition.position);
                currentState = NPCState.Working;
            }
            else
            {
                StartWandering();
            }
        }
        else
        {
            // Serbest zaman - dolaş
            StartWandering();
        }
    }

    /// <summary>
    /// Rastgele dolaşma başlat
    /// </summary>
    private void StartWandering()
    {
        SetRandomWanderDestination();
        currentState = NPCState.Wandering;
    }

    /// <summary>
    /// Rastgele hedef seç - spawn noktasından uzaklaşamaz
    /// </summary>
    private void SetRandomWanderDestination()
    {
        // Spawn pozisyonu etrafında küçük bir alanda dolaş
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += spawnPosition; // Transform yerine spawn pozisyonu kullan
        randomDirection.y = spawnPosition.y;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
        {
            currentDestination = hit.position;
            agent.SetDestination(currentDestination);
        }
    }

    /// <summary>
    /// Belirli bir lokasyona git
    /// </summary>
    private void GoToLocation(Vector3 destination)
    {
        currentDestination = destination;
        agent.SetDestination(destination);
    }

    /// <summary>
    /// Hedefe ulaştı mı?
    /// </summary>
    private bool ReachedDestination()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Oyuncu yakınlarda mı kontrol et
    /// </summary>
    private void CheckPlayerProximity()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.transform.position);
        
        // Etkileşim mesafesi
        if (distance < 3f)
        {
            if (interactionIndicator != null)
                interactionIndicator.SetActive(true);
        }
        else
        {
            if (interactionIndicator != null)
                interactionIndicator.SetActive(false);
        }
    }

    /// <summary>
    /// Oyuncu etkileşim başlattı
    /// </summary>
    public void OnPlayerInteract()
    {
        currentState = NPCState.Talking;
        agent.isStopped = true;
        dialogue.StartConversation();

        // Oyuncuya dön
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 direction = player.transform.position - transform.position;
            direction.y = 0;
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    /// <summary>
    /// Konuşma bittiğinde
    /// </summary>
    public void EndConversation()
    {
        currentState = NPCState.Idle;
        agent.isStopped = false;
        dialogue.isInConversation = false;
        nextActionTime = Time.time + 2f;
    }

    /// <summary>
    /// Oyun içi saati hesapla (test için basit versiyon)
    /// </summary>
    private float GetCurrentGameHour()
    {
        // Gerçek zaman tabanlı basit sistem (60 saniye = 1 saat)
        return (Time.time / 60f) % 24f;
    }

    /// <summary>
    /// Debug için Gizmos
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Dolaşma alanı
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);

        // Mevcut hedef
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(currentDestination, 0.5f);
    }
}

/// <summary>
/// Animator extension method - parametre var mı kontrol
/// </summary>
public static class AnimatorExtensions
{
    public static bool HasParameter(this Animator animator, string paramName)
    {
        if (animator == null) return false;
        
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }
}
