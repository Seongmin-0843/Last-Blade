using UnityEngine;

/// <summary>
/// 냉기 속성 부여: 기본 공격에 맞은 적을 확률로 얼린다. 획득하면 칼 잔상이 푸르게 바뀌고, 휘두를 때 얼음 결정이 흩날린다.
/// 획득 전에는 컴포넌트가 꺼져 있다.
/// </summary>
public class FrostSkill : MonoBehaviour
{
    [Tooltip("얼어붙는 시간(초).")]
    [SerializeField] float freezeDuration = 3f;
    [Tooltip("획득 후 칼 잔상 색. 투명도는 원래 잔상 값을 그대로 쓴다.")]
    [SerializeField] Color trailColor = new(0.45f, 0.8f, 1f);

    [Header("칼 결정")]
    [Tooltip("휘두르는 동안 칼에서 흩날리는 얼음 결정 수(초당).")]
    [SerializeField] float swingShardRate = 60f;
    [Tooltip("칼 결정이 흩어지는 속도 최소·최대(units/s).")]
    [SerializeField] Vector2 swingShardSpeed = new(0.3f, 1f);
    [Tooltip("칼 결정 크기 최소·최대(units). 적이 얼 때 튀는 결정보다 작게.")]
    [SerializeField] Vector2 swingShardSize = new(0.08f, 0.18f);

    SwordAttack sword;
    float chance;
    float shardsOwed;

    void Awake() => sword = GetComponent<SwordAttack>();

    void OnEnable()
    {
        sword.Hit += OnHit;

        TrailRenderer trail = sword.Trail;
        trail.startColor = new Color(trailColor.r, trailColor.g, trailColor.b, trail.startColor.a);
        trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, trail.endColor.a);
    }

    void OnDisable() => sword.Hit -= OnHit;

    public void Upgrade(float newChance)
    {
        chance = newChance;
        enabled = true;
    }

    // 휘두르는 동안 잔상이 쓸고 가는 칼날 위 아무 곳에서 결정을 흩날린다.
    void Update()
    {
        if (!sword.IsSwinging || Time.deltaTime <= 0f)
        {
            shardsOwed = 0f;
            return;
        }

        TrailRenderer trail = sword.Trail;
        Transform blade = trail.transform;
        float halfWidth = trail.widthMultiplier * 0.5f;

        for (shardsOwed += swingShardRate * Time.deltaTime; shardsOwed >= 1f; shardsOwed--)
        {
            Vector2 position = blade.position + blade.right * Random.Range(-halfWidth, halfWidth);
            Vector2 velocity = Random.insideUnitCircle.normalized * Random.Range(swingShardSpeed.x, swingShardSpeed.y);
            CombatEffects.Instance.EmitFrostShard(position, velocity, Random.Range(swingShardSize.x, swingShardSize.y));
        }
    }

    void OnHit(Enemy enemy)
    {
        if (Random.value < chance) enemy.Freeze(freezeDuration);
    }
}
