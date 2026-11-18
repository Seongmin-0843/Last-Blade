using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 이동 처리, 맵 경계 안으로 제한, 커서 위치에 따른 좌우 반전(X flip). (기획서 §3)
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("이동")]
    [Tooltip("units/s. 기획서 기본값 5.")]
    [SerializeField] float moveSpeed = 5f;

    [Header("참조")]
    [SerializeField] MapBounds bounds;
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] Animator animator;

    public Vector2 MoveInput { get; private set; }
    public bool MovementLocked { get; set; }
    public float SpeedMultiplier { get; set; } = 1f;

    static readonly int MovingParam = Animator.StringToHash("Moving");

    Camera cam;
    InputAction moveAction;
    Vector2 halfSize;

    void Awake()
    {
        cam = Camera.main;

        // 프로젝트 전역 Input Actions(Assets/Settings/InputSystem_Actions)를 쓴다.
        // Project Settings > Input System Package 에 이미 등록돼 있어 Inspector 참조가 필요 없다.
        moveAction = InputSystem.actions.FindAction("Player/Move", true);

        // 몸통 반지름만큼 경계 안쪽에서 멈추게 한다
        var col = GetComponent<Collider2D>();
        halfSize = col != null ? (Vector2)col.bounds.extents : Vector2.zero;
    }

    void OnEnable() => moveAction.Enable();
    void OnDisable() => moveAction.Disable();

    // 일부러 FixedUpdate가 아니라 Update에서 움직인다.
    // 플레이어는 중력도 없고 충돌에 밀리지도 않으며 경계는 MapBounds가 코드로 막는다.
    // 물리(50Hz) 시계에 태우면 카메라(렌더 프레임)와 주기가 달라서 플레이어만 떨려 보인다.
    // Rigidbody2D는 적 접촉 판정 때문에 남겨두되, 이동은 Transform으로 직접 한다(Interpolate는 None).
    void Update()
    {
        if (GameManager.Instance.InputBlocked) return;

        Vector2 input = moveAction.ReadValue<Vector2>();

        // 대각선 정규화 — 대각으로 가는 게 더 빠르면 안 된다
        if (input.sqrMagnitude > 1f) input.Normalize();
        MoveInput = input;

        if (!MovementLocked) Move(input * (moveSpeed * SpeedMultiplier * Time.deltaTime));

        animator.SetBool(MovingParam, input != Vector2.zero);

        // 바라보는 방향은 커서 기준. 이동 방향과는 무관.
        if (sprite == null || Mouse.current == null) return;

        Vector3 cursor = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        sprite.flipX = cursor.x < transform.position.x;
    }

    /// <summary>맵 경계 안에서 delta만큼 옮긴다.</summary>
    public void Move(Vector2 delta)
    {
        Vector2 clamped = bounds.Clamp((Vector2)transform.position + delta, halfSize);
        transform.position = new Vector3(clamped.x, clamped.y, transform.position.z);
    }
}
