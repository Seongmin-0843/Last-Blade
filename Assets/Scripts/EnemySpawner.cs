using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 카메라 밖 스폰 포인트에서 적을 종류별 독립 타이머로 꺼낸다. 종류마다 최대 수만큼 미리 만들어둔다. (기획서 §5)
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemyType
    {
        public Enemy prefab;
        [Tooltip("스폰 간격(초).")]
        public float spawnInterval = 0.5f;
        [Tooltip("생존 시간이 이 값(초) 이상이면 스폰을 시작한다. 한번 등장하면 계속 나온다.")]
        public float startTime;

        [System.NonSerialized] public ObjectPool<Enemy> pool;
        [System.NonSerialized] public float timer;
    }

    [SerializeField] EnemyType[] enemyTypes;
    [Tooltip("모든 종류를 합쳐 동시에 존재할 수 있는 최대 수.")]
    [SerializeField] int maxEnemies = 300;
    [Tooltip("카메라 화면에서 이만큼 바깥까지도 '화면 안'으로 친다(units). 적이 화면 가장자리에서 튀어나오지 않게.")]
    [SerializeField] float screenMargin = 1f;
    [SerializeField] Transform[] spawnPoints;

    [Header("참조")]
    [SerializeField] PlayerHealth player;
    [SerializeField] MapBounds bounds;
    [SerializeField] ExperienceManager experience;

    readonly List<Transform> candidates = new();

    Camera cam;
    int activeCount;

    void Awake()
    {
        cam = Camera.main;

        foreach (EnemyType type in enemyTypes)
        {
            EnemyType t = type;
            t.pool = new ObjectPool<Enemy>(() =>
            {
                Enemy enemy = Instantiate(t.prefab, transform);
                enemy.gameObject.SetActive(false);
                enemy.Init(player, bounds, e => OnEnemyDied(t, e));
                return enemy;
            }, collectionCheck: false);

            // ponytail: 한 종류가 최대 수를 다 채울 수 있어서 종류마다 maxEnemies개씩 만든다(3종 = 900개, 대부분 꺼진 채).
            // 시작이 느려지면 종류별 비율로 줄일 것.
            CombatEffects.Prewarm(t.pool, maxEnemies);
        }
    }

    void OnEnemyDied(EnemyType type, Enemy enemy)
    {
        type.pool.Release(enemy);
        activeCount--;
        GameManager.Instance.AddKill();
        experience.DropGem(enemy.transform.position, enemy.ExpValue);
    }

    void Update()
    {
        float elapsed = GameManager.Instance.SurvivalTime;

        foreach (EnemyType type in enemyTypes)
        {
            if (elapsed < type.startTime) continue;

            type.timer -= Time.deltaTime;
            while (type.timer <= 0f)
            {
                type.timer += type.spawnInterval;
                Spawn(type);
            }
        }
    }

    void Spawn(EnemyType type)
    {
        if (activeCount >= maxEnemies) return;

        float halfH = cam.orthographicSize + screenMargin;
        float halfW = cam.orthographicSize * cam.aspect + screenMargin;
        Vector2 camPos = cam.transform.position;

        candidates.Clear();
        foreach (Transform point in spawnPoints)
        {
            Vector2 offset = (Vector2)point.position - camPos;
            if (Mathf.Abs(offset.x) > halfW || Mathf.Abs(offset.y) > halfH) candidates.Add(point);
        }

        if (candidates.Count == 0) return;

        Enemy enemy = type.pool.Get();
        activeCount++;
        enemy.transform.position = candidates[Random.Range(0, candidates.Count)].position;
        enemy.gameObject.SetActive(true);
    }

    // 스폰 포인트는 빈 오브젝트라 씬 뷰에서 안 보여서 표시해준다.
    void OnDrawGizmos()
    {
        if (spawnPoints == null) return;

        Gizmos.color = Color.red;
        foreach (Transform point in spawnPoints)
            if (point != null) Gizmos.DrawWireSphere(point.position, 0.5f);
    }
}
