using UnityEngine;

/// <summary>
/// 3. şahıs kamera kontrolü - karakteri takip eder ve mouse ile döner
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // Player

    [Header("Camera Settings")]
    public float distance = 5f;
    public float height = 2f;
    public float rotationSpeed = 5f;
    public float smoothSpeed = 10f;

    [Header("Mouse Control")]
    public bool enableMouseRotation = true;
    public float mouseSensitivity = 100f;

    private float currentX = 0f;
    private float currentY = 15f; // Başlangıç açısı

    private void Start()
    {
        FindTarget();
    }

    private void FindTarget()
    {
        if (target != null) return;

        // 1. Tag ile ara
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        // 2. PlayerController ile ara
        if (player == null)
        {
            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) player = pc.gameObject;
        }

        // 3. İsim ile ara
        if (player == null)
        {
            player = GameObject.Find("Player");
        }

        if (player != null)
        {
            target = player.transform;
            // Başlangıç açısını ayarla
            currentX = target.eulerAngles.y;
            Debug.Log("ThirdPersonCamera: Player bulundu ve hedeflendi.");
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            // Her frame hata vermek yerine tekrar bulmayı dene
            FindTarget();
            if (target == null) return; // Hala yoksa çık
        }

        // Mouse ile kamera dönüşü
        if (enableMouseRotation)
        {
            if (Input.GetMouseButton(1)) // Sağ tık basılıyken döndür
            {
                currentX += Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
                currentY -= Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
                currentY = Mathf.Clamp(currentY, -20f, 80f); // Açı limiti
            }
        }

        // Kamera pozisyonu hesapla
        Vector3 direction = new Vector3(0, 0, -distance);
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 desiredPosition = target.position + rotation * direction + Vector3.up * height;

        // Smooth hareket
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        
        // Karaktere bak
        transform.LookAt(target.position + Vector3.up * height);
    }

    /// <summary>
    /// Kamerayı sıfırla (karakterin arkasına)
    /// </summary>
    public void ResetCamera()
    {
        if (target != null)
        {
            currentX = target.eulerAngles.y;
            currentY = 15f;
        }
    }
}
