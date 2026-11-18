using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 카메라 밖 스폰 포인트에서 적을 주기적으로 꺼낸다. 시작할 때 최대 수만큼 미리 만들어둔다. (기획서 §5)
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] Enemy enemyPrefab;
    [Tooltip("스폰 간격(초).")]
    [SerializeField] float spawnInterval = 0.5f;
    [Tooltip("동시에 존재할 수 있는 최대 수.")]
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
    ObjectPool<Enemy> pool;
    float spawnTimer;

    void Awake()
    {
        cam = Camera.main;

        pool = new ObjectPool<Enemy>(() =>
        {
            Enemy enemy = Instantiate(enemyPrefab, transform);
            enemy.gameObject.SetActive(false);
            enemy.Init(player, bounds, OnEnemyDied);
            return enemy;
        }, collectionCheck: false);

        CombatEffects.Prewarm(pool, maxEnemies);
    }

    void OnEnemyDied(Enemy enemy)
    {
        pool.Release(enemy);
        GameManager.Instance.AddKill();
        experience.DropGem(enemy.transform.position, enemy.ExpValue);
    }

    void Update()
    {
        spawnTimer -= Time.deltaTime;

        while (spawnTimer <= 0f)
        {
            spawnTimer += spawnInterval;
            Spawn();
        }
    }

    void Spawn()
    {
        if (pool.CountActive >= maxEnemies) return;

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

        Enemy enemy = pool.Get();
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
