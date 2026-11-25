using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 조준·공격 입력(PC: 커서 + 클릭 / 모바일: 오른쪽 스틱을 떼면), 공격 딜레이, 부채꼴 판정, 스윙 이벤트. (기획서 §4)
/// </summary>
public class SwordAttack : MonoBehaviour
{
    [Header("칼")]
    [SerializeField] float damage = 10f;
    [Tooltip("부채꼴 반지름(units).")]
    [SerializeField] float radius = 1.8f;
    [Tooltip("부채꼴 전체 각도(도). 칼도 이 각도만큼 돈다.")]
    [SerializeField] float arcAngle = 180f;
    [Tooltip("넉백 거리(units).")]
    [SerializeField] float knockback = 0.3f;
    [Tooltip("공격 딜레이(초).")]
    [SerializeField] float attackDelay = 1f;
    [Tooltip("휘두르는 시간(초). 칼을 휘두르는 동안 타격 판정도 켜져 있다.")]
    [SerializeField] float hitWindow = 0.3f;
    [SerializeField] LayerMask enemyLayers;

    [Header("조준")]
    [Tooltip("오른쪽 스틱을 이만큼(0~1) 넘게 당겨야 조준으로 친다. 이 아래로 돌아오면 손을 뗀 것으로 보고 휘두른다.")]
    [SerializeField] float aimThreshold = 0.3f;

    [Header("연출")]
    [SerializeField] Transform swordPivot;
    [Tooltip("SwordPivot 자식 칼 궤적. 휘두르는 동안만 켠다. 길이·폭·색은 TrailRenderer에서 조정.")]
    [SerializeField] TrailRenderer trail;

    /// <summary>휘두를 때마다 (휘두른 위치, 조준 방향)으로 불린다. 검기·회전 베기가 듣는다.</summary>
    public event Action<Vector2, Vector2> Swung;

    /// <summary>기본 공격에 맞은 적마다 불린다(피해를 준 뒤). 냉기 속성 부여가 듣는다.</summary>
    public event Action<Enemy> Hit;

    public Vector2 Aim => aim;
    public TrailRenderer Trail => trail;
    public bool IsSwinging => Time.time < hitWindowEnd;
    public float Damage => damage * DamageMultiplier;
    public float DamageMultiplier { get; set; } = 1f;
    public float DelayMultiplier { get; set; } = 1f;

    readonly List<Collider2D> overlaps = new();
    readonly HashSet<Enemy> hitThisSwing = new();

    Camera cam;
    InputAction attackAction;
    InputAction lookAction;
    ContactFilter2D enemyFilter;
    Vector2 aim = Vector2.right;
    Vector2 swingOrigin;
    Vector2 swingAim;
    float aimAngle;
    float nextAttackTime;
    float hitWindowEnd;
    bool aiming;

    void Awake()
    {
        cam = Camera.main;
        attackAction = InputSystem.actions.FindAction("Player/Attack", true);
        // 오른쪽 가상 스틱(OnScreenStick)이 <Gamepad>/rightStick 으로 값을 넣는다.
        lookAction = InputSystem.actions.FindAction("Player/Look", true);

        enemyFilter = new ContactFilter2D { useTriggers = true };
        enemyFilter.SetLayerMask(enemyLayers);
    }

    void OnEnable()
    {
        attackAction.Enable();
        lookAction.Enable();
    }

    void OnDisable()
    {
        attackAction.Disable();
        lookAction.Disable();
    }

    void Update()
    {
        if (GameManager.Instance.InputBlocked)
        {
            aiming = false; // 레벨업 창이 뜰 때 잡고 있던 스틱이 창을 닫은 뒤 휘두르기로 이어지지 않게
            return;
        }

        if (GameManager.TouchControls) UpdateStickAttack();
        else UpdateMouseAttack();

        if (Time.time < hitWindowEnd)
        {
            ApplyHits();

            float t = 1f - (hitWindowEnd - Time.time) / hitWindow;
            swordPivot.localRotation = Quaternion.Euler(0f, 0f, aimAngle + Mathf.Lerp(arcAngle, -arcAngle, t) * 0.5f);
        }
        else if (Time.time < hitWindowEnd + trail.time)
        {
            // 잔상이 사라질 때까지 칼을 끝 자세에 둔다. 바로 돌아가면 TrailRenderer가
            // 마지막 점과 칼의 현재 위치를 이어서, 돌아가는 동작에도 잔상이 그려진다.
            trail.emitting = false;
            swordPivot.localRotation = Quaternion.Euler(0f, 0f, aimAngle - arcAngle * 0.5f);
        }
        else
        {
            // 평소에는 칼끝이 공격 방향(조준)을 가리킨다.
            aimAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            swordPivot.localRotation = Quaternion.Euler(0f, 0f, aimAngle);
        }
    }

    // 모바일: 스틱을 당기는 동안 칼끝이 그 방향을 가리키고, 손을 떼는 순간 휘두른다.
    // 놓을 때마다 1회(클릭당 1회와 같은 규칙). 딜레이 중에 놓으면 무시한다.
    void UpdateStickAttack()
    {
        Vector2 look = lookAction.ReadValue<Vector2>();
        bool holding = look.sqrMagnitude >= aimThreshold * aimThreshold;
        if (holding) aim = look.normalized;

        bool released = aiming && !holding;
        aiming = holding;
        if (released && Time.time >= nextAttackTime) Swing();
    }

    // PC: 칼끝이 커서를 따라가고 클릭하면 휘두른다. UI(레벨업 카드 등) 위 클릭은 공격이 아니다.
    void UpdateMouseAttack()
    {
        if (Mouse.current == null) return;

        Vector2 toCursor = (Vector2)cam.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - (Vector2)transform.position;
        if (toCursor.sqrMagnitude > Mathf.Epsilon) aim = toCursor.normalized;

        if (attackAction.WasPressedThisFrame() && Time.time >= nextAttackTime && !IsPointerOverUI())
            Swing();
    }

    static bool IsPointerOverUI() =>
        EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

    void Swing()
    {
        // 판정 방향은 휘두른 순간에 고정한다. 휘두르는 도중 조준을 바꿔도 범위는 돌아가지 않는다.
        swingAim = aim;
        aimAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;

        swingOrigin = transform.position;
        nextAttackTime = Time.time + attackDelay * DelayMultiplier;
        hitWindowEnd = Time.time + hitWindow;
        hitThisSwing.Clear();

        trail.Clear();
        trail.emitting = true;

        Swung?.Invoke(swingOrigin, aim);
    }

    // 콜라이더를 켜고 끄는 대신 판정 시간 동안 매 프레임 범위를 조회한다.
    // 트리거 이벤트는 물리 스텝(50Hz)에 묶이고, 반지름·각도를 바꿀 때 콜라이더 모양도 다시 만들어야 해서.
    void ApplyHits()
    {
        // 범위는 휘두른 순간의 위치에 고정한다. 휘두르는 도중 플레이어가 움직여도 범위는 따라가지 않는다.
        Vector2 origin = swingOrigin;
        int hitsBefore = hitThisSwing.Count;
        int count = Physics2D.OverlapCircle(origin, radius, enemyFilter, overlaps);

        for (int i = 0; i < count; i++)
        {
            if (!overlaps[i].TryGetComponent(out Enemy enemy)) continue;

            Vector2 toEnemy = (Vector2)enemy.transform.position - origin;

            // 중심이 부채꼴 밖이어도 몸통이 걸치면 맞는다. 눈에 닿아 보이는데 안 맞으면 안 되니까.
            float distance = toEnemy.magnitude;
            float bodyRadius = overlaps[i].bounds.extents.x;
            float bodyAngle = distance > bodyRadius ? Mathf.Asin(bodyRadius / distance) * Mathf.Rad2Deg : 180f;
            if (Vector2.Angle(swingAim, toEnemy) - bodyAngle > arcAngle * 0.5f) continue;
            if (!hitThisSwing.Add(enemy)) continue;

            Vector2 pushDir = toEnemy.sqrMagnitude > Mathf.Epsilon ? toEnemy.normalized : swingAim;
            enemy.TakeDamage(Damage, pushDir * knockback);
            Hit?.Invoke(enemy);
        }

        // 스윙당 첫 타격에 한 번만 멈춘다. 맞을 때마다 멈추면 적이 몰릴수록 화면이 끊겨 보인다.
        if (hitsBefore == 0 && hitThisSwing.Count > 0) CombatEffects.Instance.HitStop();
    }
}
