using UnityEngine;

/// <summary>
/// Kamera kurulumunu kontrol eder ve debug bilgisi verir
/// </summary>
public class CameraDebugger : MonoBehaviour
{
    private void Start()
    {
        CheckCameraSetup();
    }

    private void Update()
    {
        // Space tuşu ile tekrar kontrol
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CheckCameraSetup();
        }
    }

    void CheckCameraSetup()
    {
        Debug.Log("=== KAMERA KONTROLÜ ===");

        // Main Camera var mı?
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("❌ Main Camera bulunamadı!");
            return;
        }
        Debug.Log($"✅ Main Camera bulundu: {mainCam.gameObject.name}");

        // ThirdPersonCamera scripti var mı?
        ThirdPersonCamera tpCam = mainCam.GetComponent<ThirdPersonCamera>();
        if (tpCam == null)
        {
            Debug.LogError("❌ ThirdPersonCamera scripti kamerada yok!");
            return;
        }
        Debug.Log("✅ ThirdPersonCamera scripti var");

        // Target atanmış mı?
        if (tpCam.target == null)
        {
            Debug.LogError("❌ ThirdPersonCamera target'ı null!");
            
            // Player'ı bul ve ata
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                tpCam.target = player.transform;
                Debug.Log($"✅ Target otomatik atandı: {player.name}");
            }
            else
            {
                Debug.LogError("❌ Player objesi bulunamadı! 'Player' tag'li obje var mı?");
            }
        }
        else
        {
            Debug.Log($"✅ Target atanmış: {tpCam.target.name}");
        }

        // Player var mı?
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogError("❌ 'Player' tag'li obje bulunamadı!");
        }
        else
        {
            Debug.Log($"✅ Player bulundu: {playerObj.name} @ {playerObj.transform.position}");
        }

        // Kamera pozisyonu
        Debug.Log($"📷 Kamera pozisyonu: {mainCam.transform.position}");

        Debug.Log("=== KONTROL BİTTİ ===");
        Debug.Log("Space tuşuna basarak tekrar kontrol edebilirsiniz.");
    }
}
