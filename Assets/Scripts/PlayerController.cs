using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3f;

    [Header("Animator Parameter Names")]
    [Tooltip("Int parameter: 0=Down  1=Up  2=Left  3=Right")]
    [SerializeField] private string animDirection = "Direction";
    [Tooltip("Float parameter: 0 = idle, 1 = moving")]
    [SerializeField] private string animSpeed = "Speed";

    private Rigidbody2D _rb;
    private Animator _anim;
    private Vector2 _input;
    private Vector2 _lastDir = Vector2.down;

    public bool MovementLocked { get; set; } = false;

    private const int DIR_DOWN = 0;
    private const int DIR_UP = 1;
    private const int DIR_LEFT = 2;
    private const int DIR_RIGHT = 3;
    private bool _isFacingForced = false;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();

        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
    }

    private void Start()
    {
        if (MindForestTrigger.IsReturningFromForest)
        {
            transform.position = MindForestTrigger.ReturnPosition;
            Debug.Log("Player position restored from Mind Forest trip.");
        }
    }

    private void Update()
    {
        if (MovementLocked)
        {
            _input = Vector2.zero;
            return;
        }

        _input.x = Input.GetAxisRaw("Horizontal");
        _input.y = Input.GetAxisRaw("Vertical");
        _input.Normalize();

        if (_input != Vector2.zero)
            _lastDir = _input;

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (MovementLocked)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        _rb.linearVelocity = _input * walkSpeed;
    }

    private void UpdateAnimator()
    {
        if (_anim == null) return;
        float speed = _isFacingForced ? 1f : _input.magnitude;
        _anim.SetFloat(animSpeed, speed);
        _anim.SetInteger(animDirection, GetDirectionInt(_lastDir));
    }

    private static int GetDirectionInt(Vector2 dir)
    {
        if (Mathf.Abs(dir.y) > Mathf.Abs(dir.x))
            return dir.y > 0 ? DIR_UP : DIR_DOWN;
        else
            return dir.x > 0 ? DIR_RIGHT : DIR_LEFT;
    }

    public Vector2 FacingDirection => _lastDir;

    public void ForceFacePosition(Vector3 targetPos)
    {
        Vector2 dir = (targetPos - transform.position).normalized;
        if (dir.sqrMagnitude > 0.01f)
        {
            _lastDir = dir;
            _isFacingForced = true;
            UpdateAnimator();
        }
    }

    public void ClearForcedFacing()
    {
        _isFacingForced = false;
        UpdateAnimator();
    }
}