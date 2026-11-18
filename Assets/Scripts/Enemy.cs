using System;
using UnityEngine;

/// <summary>
/// 플레이어 추적, 접촉 피해, 피격(넉백·흰색 점멸), 사망. (기획서 §5)
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("스탯")]
    [SerializeField] float maxHealth = 10f;
    [Tooltip("units/s.")]
    [SerializeField] float moveSpeed = 1.5f;
    [SerializeField] float contactDamage = 5f;
    [Tooltip("드롭하는 젬의 경험치.")]
    [SerializeField] int expValue = 1;

    [Header("접촉")]
    [Tooltip("플레이어 중심까지 이 거리 안이면 접촉 피해를 준다(units).")]
    [SerializeField] float contactDistance = 0.7f;
    [Tooltip("플레이어 중심까지 이 거리에서 멈춘다(units). 한 점에 겹치지 않게 contactDistance보다 살짝 작게 둔다.")]
    [SerializeField] float stopDistance = 0.6f;

    [Header("피격")]
    [Tooltip("넉백으로 밀려나는 시간(초). 이 동안은 추적하지 않는다.")]
    [SerializeField] float knockbackDuration = 0.1f;
    [Tooltip("흰색 점멸 시간(초).")]
    [SerializeField] float flashDuration = 0.1f;
    [SerializeField] Material flashMaterial;

    [Header("빙결")]
    [Tooltip("얼어 있는 동안 스프라이트에 곱하는 색.")]
    [SerializeField] Color frozenColor = new(0.55f, 0.8f, 1f);

    [Header("참조")]
    [SerializeField] SpriteRenderer sprite;

    PlayerHealth player;
    Transform target;
    MapBounds bounds;
    Action<Enemy> died;

    public int ExpValue => expValue;

    Material normalMaterial;
    Color normalColor;
    Animator animator;
    Vector2 halfSize;

    float health;
    float flashLeft;
    float knockbackLeft;
    float frozenLeft;
    Vector2 knockbackVelocity;

    void Awake()
    {
        normalMaterial = sprite.sharedMaterial;
        normalColor = sprite.color;
        animator = GetComponent<Animator>();
        halfSize = GetComponent<Collider2D>().bounds.extents;
    }

    /// <summary>프리팹은 씬 오브젝트를 참조할 수 없어서 스포너가 만들 때 한 번 넘겨준다.</summary>
    public void Init(PlayerHealth player, MapBounds bounds, Action<Enemy> died)
    {
        this.player = player;
        target = player.transform;
        this.bounds = bounds;
        this.died = died;
    }

    // 풀에서 다시 꺼낼 때를 위해 Awake가 아니라 여기서 초기화한다.
    void OnEnable()
    {
        health = maxHealth;
        flashLeft = 0f;
        knockbackLeft = 0f;
        sprite.sharedMaterial = normalMaterial;
        frozenLeft = 0f;
        sprite.color = normalColor;
        if (animator != null) animator.speed = 1f;
    }

    // 플레이어와 같은 이유로 Update에서 Transform을 직접 움직인다. (PlayerController 참고)
    void Update()
    {
        float dt = Time.deltaTime;
        Vector2 pos = transform.position;
        Vector2 toPlayer = (Vector2)target.position - pos;
        float distance = toPlayer.magnitude;

        // 접촉은 물리 이벤트 대신 거리로 본다. 적 300마리분의 접촉 쌍을 물리 엔진에 맡기지 않으려고.
        if (dt > 0f && frozenLeft <= 0f && distance <= contactDistance) player.TakeDamage(contactDamage);

        Vector2 next = pos;

        if (knockbackLeft > 0f)
        {
            float step = Mathf.Min(dt, knockbackLeft);
            next = pos + knockbackVelocity * step;
            knockbackLeft -= step;
        }
        else if (frozenLeft <= 0f && distance > stopDistance)
        {
            next = pos + toPlayer / distance * Mathf.Min(moveSpeed * dt, distance - stopDistance);
        }

        Vector2 clamped = bounds.Clamp(next, halfSize);
        transform.position = new Vector3(clamped.x, clamped.y, transform.position.z);

        if (frozenLeft > 0f)
        {
            frozenLeft -= dt;
            if (frozenLeft <= 0f) Unfreeze();
        }
        else
        {
            sprite.flipX = toPlayer.x < 0f;
        }

        if (flashLeft > 0f)
        {
            flashLeft -= dt;
            if (flashLeft <= 0f) sprite.sharedMaterial = normalMaterial;
        }
    }

    public void TakeDamage(float damage, Vector2 knockback)
    {
        health -= damage;
        CombatEffects.Instance.ShowDamage(transform.position, damage);

        if (health <= 0f)
        {
            CombatEffects.Instance.PlayDeath(transform.position);
            gameObject.SetActive(false);
            died?.Invoke(this);
            return;
        }

        knockbackLeft = knockbackDuration;
        knockbackVelocity = knockback / knockbackDuration;

        flashLeft = flashDuration;
        sprite.sharedMaterial = flashMaterial;
    }

    /// <summary>duration초 동안 멈추고 접촉 피해를 주지 않는다. 넉백은 그대로 받는다. 이미 얼어 있으면 시간을 갱신한다.</summary>
    public void Freeze(float duration)
    {
        if (!gameObject.activeSelf) return;

        frozenLeft = Mathf.Max(frozenLeft, duration);
        sprite.color = frozenColor;
        if (animator != null) animator.speed = 0f;
        CombatEffects.Instance.PlayFrost(transform.position);
    }

    void Unfreeze()
    {
        frozenLeft = 0f;
        sprite.color = normalColor;
        if (animator != null) animator.speed = 1f;
    }
}
