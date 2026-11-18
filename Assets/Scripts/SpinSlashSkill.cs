using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 칼을 N번째 휘두를 때마다 주변 360° 베기. 획득 전에는 컴포넌트가 꺼져 있어서 획득한 순간부터 센다. (기획서 §6)
/// </summary>
public class SpinSlashSkill : MonoBehaviour
{
    [Tooltip("몇 번째 스윙마다 발동할지.")]
    [SerializeField] int swingsPerSpin = 3;
    [Tooltip("칼 피해 대비 비율.")]
    [SerializeField] float damageRatio = 0.8f;
    [Tooltip("넉백 거리(units).")]
    [SerializeField] float knockback = 0.3f;
    [Tooltip("타격 판정과 연출이 이어지는 시간(초).")]
    [SerializeField] float duration = 0.3f;
    [SerializeField] LayerMask enemyLayers;

    [Header("연출")]
    [Tooltip("슬래시 두 장을 등지게 놓은 오브젝트. 크기 1일 때 반지름 1 unit이 되게 배치돼 있다.")]
    [SerializeField] Transform visual;
    [SerializeField] SpriteEffect[] slashes;

    readonly List<Collider2D> overlaps = new();
    readonly HashSet<Enemy> hit = new();

    SwordAttack sword;
    ContactFilter2D enemyFilter;
    float radius;
    int swings;
    Vector2 origin;
    float timeLeft;

    void Awake()
    {
        sword = GetComponent<SwordAttack>();

        enemyFilter = new ContactFilter2D { useTriggers = true };
        enemyFilter.SetLayerMask(enemyLayers);
    }

    void OnEnable() => sword.Swung += OnSwung;
    void OnDisable() => sword.Swung -= OnSwung;

    public void Upgrade(float newRadius)
    {
        radius = newRadius;
        enabled = true;
    }

    void OnSwung(Vector2 swingOrigin, Vector2 aim)
    {
        if (++swings % swingsPerSpin != 0) return;

        origin = swingOrigin;
        timeLeft = duration;
        hit.Clear();

        visual.position = origin;
        visual.localScale = new Vector3(radius, radius, 1f);
        foreach (SpriteEffect slash in slashes) slash.Play(duration);
    }

    void Update()
    {
        if (timeLeft <= 0f || Time.deltaTime <= 0f) return;

        timeLeft -= Time.deltaTime;

        int count = Physics2D.OverlapCircle(origin, radius, enemyFilter, overlaps);
        for (int i = 0; i < count; i++)
        {
            if (!overlaps[i].TryGetComponent(out Enemy enemy) || !hit.Add(enemy)) continue;

            Vector2 toEnemy = (Vector2)enemy.transform.position - origin;
            enemy.TakeDamage(sword.Damage * damageRatio, toEnemy.normalized * knockback);
        }
    }
}
