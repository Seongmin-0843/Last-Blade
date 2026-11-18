using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 칼을 N번째 휘두를 때마다 클릭 방향으로 검기 발사. 획득 전에는 컴포넌트가 꺼져 있어서 획득한 순간부터 센다. (기획서 §6)
/// </summary>
public class SwordWaveSkill : MonoBehaviour
{
    [SerializeField] SwordWave wavePrefab;
    [Tooltip("몇 번째 스윙마다 발사할지.")]
    [SerializeField] int swingsPerFire = 5;
    [Tooltip("칼 피해 대비 비율.")]
    [SerializeField] float damageRatio = 0.5f;
    [Tooltip("사거리(units).")]
    [SerializeField] float range = 9f;
    [Tooltip("날아가는 속도(units/s).")]
    [SerializeField] float speed = 12f;
    [Tooltip("판정 반지름(units).")]
    [SerializeField] float hitRadius = 1.1f;
    [Tooltip("검기 사이의 각도(도).")]
    [SerializeField] float spreadAngle = 30f;
    [Tooltip("미리 만들어둘 개수. 모자라면 발사를 건너뛴다.")]
    [SerializeField] int poolSize = 12;
    [SerializeField] LayerMask enemyLayers;

    SwordAttack sword;
    ObjectPool<SwordWave> pool;
    ContactFilter2D enemyFilter;
    int waveCount;
    int swings;

    void Awake()
    {
        sword = GetComponent<SwordAttack>();

        enemyFilter = new ContactFilter2D { useTriggers = true };
        enemyFilter.SetLayerMask(enemyLayers);

        pool = new ObjectPool<SwordWave>(() =>
        {
            SwordWave wave = Instantiate(wavePrefab);
            wave.gameObject.SetActive(false);
            wave.Finished = w => pool.Release(w);
            return wave;
        });
        CombatEffects.Prewarm(pool, poolSize);
    }

    void OnEnable() => sword.Swung += OnSwung;
    void OnDisable() => sword.Swung -= OnSwung;

    public void Upgrade(int newWaveCount)
    {
        waveCount = newWaveCount;
        enabled = true;
    }

    void OnSwung(Vector2 origin, Vector2 aim)
    {
        if (++swings % swingsPerFire != 0) return;

        for (int i = 0; i < waveCount && pool.CountInactive > 0; i++)
        {
            float offset = (i - (waveCount - 1) * 0.5f) * spreadAngle;
            Vector2 direction = Quaternion.Euler(0f, 0f, offset) * aim;
            pool.Get().Fire(origin, direction, speed, range, sword.Damage * damageRatio, hitRadius, enemyFilter);
        }
    }
}
