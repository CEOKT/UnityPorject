using UnityEngine;

/// <summary>
/// Objenin her zaman kameraya bakmasını sağlar (Billboard efekti)
/// NPC isim etiketleri için kullanılır
/// </summary>
public class BillboardLabel : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        // Kameraya bak
        transform.LookAt(transform.position + mainCamera.transform.forward);
    }
}
