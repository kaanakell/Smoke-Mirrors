using UnityEngine;

public class VisionConeAim : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float rotationOffset = -90f;

    [Tooltip("If true, FOV follows movement keys. If false, it follows the mouse.")]
    [SerializeField] private bool useMovementKeys = true;
    public static Transform OverrideTarget;

    private Vector2 _lastDirection = Vector2.down;
    private Camera _mainCam;

    private void Start()
    {
        _mainCam = Camera.main;
    }

    private void LateUpdate()
    {
        if (OverrideTarget != null)
        {
            Vector3 lookDir = OverrideTarget.position - transform.position;
            float targetAngle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, targetAngle + rotationOffset);
            return; // Stops here!
        }

        if (useMovementKeys)
        {
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");
            Vector2 moveInput = new Vector2(moveX, moveY).normalized;

            if (moveInput.sqrMagnitude > 0.01f)
            {
                _lastDirection = moveInput;
            }

            float angle = Mathf.Atan2(_lastDirection.y, _lastDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);
        }
        else
        {
            if (_mainCam == null) return;
            Vector3 mouseWorldPos = _mainCam.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0f;

            Vector3 lookDir = mouseWorldPos - transform.position;
            float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;

            transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);
        }

        if (transform.parent != null)
        {
            float parentDirection = Mathf.Sign(transform.parent.localScale.x);
            transform.localScale = new Vector3(parentDirection, 1f, 1f);
        }
    }

    public void SetLastDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude > 0.01f)
            _lastDirection = dir;
    }
}