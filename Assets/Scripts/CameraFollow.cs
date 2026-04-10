using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Smoothing")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector2 offset = Vector2.zero;

    [Header("Pixel Perfect")]
    [SerializeField] private bool pixelPerfect = true;
    [SerializeField] private float pixelsPerUnit = 16f;

    [Header("Bounds (optional — clamp camera to room)")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private float minX = -10f, maxX = 10f;
    [SerializeField] private float minY = -10f, maxY = 10f;

    private float _zDepth;

    private void Awake()
    {
        _zDepth = transform.position.z;

        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            _zDepth
        );

        Vector3 smoothed = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);

        if (useBounds)
        {
            smoothed.x = Mathf.Clamp(smoothed.x, minX, maxX);
            smoothed.y = Mathf.Clamp(smoothed.y, minY, maxY);
        }

        if (pixelPerfect)
            smoothed = SnapToPixel(smoothed);

        transform.position = smoothed;
    }

    private Vector3 SnapToPixel(Vector3 pos)
    {
        float ppu = pixelsPerUnit;
        return new Vector3(
            Mathf.Round(pos.x * ppu) / ppu,
            Mathf.Round(pos.y * ppu) / ppu,
            pos.z
        );
    }

    public void SnapToTarget()
    {
        if (target == null) return;
        transform.position = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            _zDepth
        );
    }

    private void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        Gizmos.color = Color.magenta;
        Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0f);
        Vector3 size = new Vector3(maxX - minX, maxY - minY, 0f);
        Gizmos.DrawWireCube(center, size);
    }
}
