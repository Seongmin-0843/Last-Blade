using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 타격감 연출 모음: 히트스톱, 피해 숫자, 처치 이펙트, 빙결 파티클.
/// 플레이 중 Instantiate를 하지 않도록 시작할 때 미리 만들어 풀에 넣어둔다.
/// </summary>
public class CombatEffects : MonoBehaviour
{
    public static CombatEffects Instance { get; private set; }

    [SerializeField] DamagePopup popupPrefab;
    [SerializeField] SpriteEffect deathEffectPrefab;
    [Tooltip("미리 만들어둘 개수. 모자라면 연출을 건너뛴다(게임 진행에는 영향 없음).")]
    [SerializeField] int poolSize = 64;
    [Tooltip("히트스톱 시간(초, 실제 시간 기준).")]
    [SerializeField] float hitStopDuration = 0.03f;

    [Header("빙결 파티클 (얼음 결정)")]
    [Tooltip("파티클 머티리얼. URP Particles/Unlit(Sprite 셰이더는 파티클에서 보라색으로 나온다). 텍스처 없이 그려서 사각형이 되고, 45° 돌려 마름모 결정처럼 보이게 한다.")]
    [SerializeField] Material frostMaterial;
    [SerializeField] Color frostColorA = new(0.85f, 0.97f, 1f);
    [SerializeField] Color frostColorB = new(0.4f, 0.75f, 1f);
    [Tooltip("한 번 얼릴 때 튀는 결정 개수.")]
    [SerializeField] int frostBurstCount = 12;
    [Tooltip("튀어 나가는 속도 최소·최대(units/s).")]
    [SerializeField] Vector2 frostSpeed = new(1.5f, 3f);
    [Tooltip("결정 크기 최소·최대(units). 0.0625 = 1픽셀.")]
    [SerializeField] Vector2 frostSize = new(0.15f, 0.3f);
    [Tooltip("결정이 사라지기까지(초).")]
    [SerializeField] float frostLifetime = 0.6f;
    [Tooltip("결정이 처음 나타나는 위치. 적 중심에서 이 거리만큼 떨어진 원 위(units). 적 몸통에 가려지지 않게.")]
    [SerializeField] float frostSpawnRadius = 0.3f;
    [SerializeField] int frostSortingOrder = 20;

    ObjectPool<DamagePopup> popups;
    ObjectPool<SpriteEffect> deathEffects;
    ParticleSystem frost;

    void Awake()
    {
        Instance = this;

        popups = new ObjectPool<DamagePopup>(() =>
        {
            DamagePopup popup = Instantiate(popupPrefab, transform);
            popup.gameObject.SetActive(false);
            popup.Finished = p => popups.Release(p);
            return popup;
        });

        deathEffects = new ObjectPool<SpriteEffect>(() =>
        {
            SpriteEffect effect = Instantiate(deathEffectPrefab, transform);
            effect.gameObject.SetActive(false);
            effect.Finished = e => deathEffects.Release(e);
            return effect;
        });

        Prewarm(popups, poolSize);
        Prewarm(deathEffects, poolSize);

        frost = CreateFrostParticles();
    }

    /// <summary>플레이 중 Instantiate가 일어나지 않도록 count개를 미리 만들어 풀에 넣는다.</summary>
    public static void Prewarm<T>(ObjectPool<T> pool, int count) where T : class
    {
        var items = new T[count];
        for (int i = 0; i < count; i++) items[i] = pool.Get();
        for (int i = 0; i < count; i++) pool.Release(items[i]);
    }

    // 파티클 하나를 여러 위치에서 Emit으로 재사용한다. 오브젝트가 하나라 풀링이 필요 없다.
    ParticleSystem CreateFrostParticles()
    {
        var go = new GameObject("FrostParticles");
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(frostLifetime * 0.6f, frostLifetime);
        main.startSize = new ParticleSystem.MinMaxCurve(frostSize.x, frostSize.y);
        main.startColor = new ParticleSystem.MinMaxGradient(frostColorA, frostColorB);
        main.startRotation = 45f * Mathf.Deg2Rad;
        main.maxParticles = 1000;

        var emission = ps.emission;
        emission.enabled = false;
        var shape = ps.shape;
        shape.enabled = false;

        // 날아가며 작아져서 사라진다.
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = frostMaterial;
        renderer.sortingOrder = frostSortingOrder;

        // emission이 꺼져 있어 저절로는 아무것도 안 나온다. 재생 중이어야 Emit한 결정이 움직인다.
        ps.Play();
        return ps;
    }

    public void PlayFrost(Vector2 position)
    {
        for (int i = 0; i < frostBurstCount; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            EmitFrostShard(position + dir * frostSpawnRadius, dir * Random.Range(frostSpeed.x, frostSpeed.y),
                Random.Range(frostSize.x, frostSize.y));
        }
    }

    /// <summary>얼음 결정 하나. 적 빙결과 칼(냉기 속성 부여)이 같은 파티클을 쓴다.</summary>
    public void EmitFrostShard(Vector2 position, Vector2 velocity, float size)
    {
        var emit = new ParticleSystem.EmitParams
        {
            position = position,
            velocity = velocity,
            startSize = size,
            applyShapeToPosition = false,
        };
        frost.Emit(emit, 1);
    }

    public void HitStop() => GameManager.Instance.HitStop(hitStopDuration);

    public void ShowDamage(Vector2 position, float damage)
    {
        if (popups.CountInactive > 0) popups.Get().Show(position, damage);
    }

    public void PlayDeath(Vector2 position)
    {
        if (deathEffects.CountInactive == 0) return;

        SpriteEffect effect = deathEffects.Get();
        effect.transform.position = position;
        effect.Play();
    }
}
