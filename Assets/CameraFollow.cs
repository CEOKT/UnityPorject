using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;        // Player
    public Vector3 offset = new Vector3(0, 5, -7);  // Kameranın arkadaki mesafesi
    public float smoothSpeed = 10f; // Kamera yumuşak hareketi

    void LateUpdate()
    {
        if (target == null) return;

        // Hedef konum
        Vector3 desiredPosition = target.position + offset;

        // Yumuşak takip
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        transform.position = smoothedPosition;

        // Player'a bak
        transform.LookAt(target);
    }
}
