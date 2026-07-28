using UnityEngine;

/// <summary>Подписи станций всегда смотрят на камеру и (опционально) следуют за целью.</summary>
public class BillboardLabel : MonoBehaviour
{
    public Transform Follow;
    public float HeightOffset = 1.2f;

    void LateUpdate()
    {
        if (Follow != null)
            transform.position = Follow.position + Vector3.up * HeightOffset;

        var cam = Camera.main;
        if (cam == null) return;
        transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }
}
