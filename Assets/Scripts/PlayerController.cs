using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float sprintSpeed = 5.5f;

    [Header("Animation Parameter Names")]
    [SerializeField] private string animMoveX = "MoveX";
    [SerializeField] private string animMoveY = "MoveY";
    [SerializeField] private string animSpeed = "Speed";

    private Rigidbody2D _rb;
    private Animator _anim;
    private Vector2 _input;
    private Vector2 _lastDir = Vector2.down;

    public bool MovementLocked {get; set;} = false;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();

        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
    }

    void Update()
    {
        if(MovementLocked)
        {
            _input = Vector2.zero;
            return;
        }

        _input.x = Input.GetAxisRaw("Horizontal");
        _input.y = Input.GetAxisRaw("Vertical");
        _input.Normalize();

        if(_input != Vector2.zero)
        {
            _lastDir = _input;
        }

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if(MovementLocked)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        bool isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float speed = isSprinting ? sprintSpeed : walkSpeed;
        _rb.linearVelocity = _input * speed;
    }

    private void UpdateAnimator()
    {
        _anim.SetFloat(animMoveX, _lastDir.x);
        _anim.SetFloat(animMoveY, _lastDir.y);
        _anim.SetFloat(animSpeed, _input.magnitude);
    }

    public Vector2 FacingDirection => _lastDir;
}
