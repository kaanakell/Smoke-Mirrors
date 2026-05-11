using UnityEngine;

public class VisionConeAim : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag your Main Camera here")]
    [SerializeField] private Camera mainCam;

    [Header("Settings")]
    [SerializeField] private float rotationOffset = -90f;

    private void Start()
    {
        if (mainCam == null) mainCam = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCam == null) return;

        if (transform.parent != null)
        {
            float parentDirection = Mathf.Sign(transform.parent.localScale.x);
            transform.localScale = new Vector3(parentDirection, 1f, 1f);
        }

        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        Vector3 lookDir = mouseWorldPos - transform.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);
    }
}