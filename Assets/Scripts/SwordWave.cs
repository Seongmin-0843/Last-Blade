using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 직선으로 날아가며 닿는 적을 관통해 한 번씩 때리는 검기.
/// </summary>
public class SwordWave : MonoBehaviour
{
    public Action<SwordWave> Finished;

    readonly List<Collider2D> overlaps = new();
    readonly HashSet<Enemy> hit = new();

    Vector2 velocity;
    float timeLeft;
    float damage;
    float hitRadius;
    ContactFilter2D enemyFilter;

    public void Fire(Vector2 origin, Vector2 direction, float speed, float range, float damage, float hitRadius, ContactFilter2D enemyFilter)
    {
        velocity = direction * speed;
        timeLeft = range / speed;
        this.damage = damage;
        this.hitRadius = hitRadius;
        this.enemyFilter = enemyFilter;
        hit.Clear();

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.SetPositionAndRotation(origin, Quaternion.Euler(0f, 0f, angle));
        gameObject.SetActive(true);
    }

    void Update()
    {
        float step = Mathf.Min(Time.deltaTime, timeLeft);
        Vector2 position = (Vector2)transform.position + velocity * step;
        transform.position = position;
        timeLeft -= step;

        int count = Physics2D.OverlapCircle(position, hitRadius, enemyFilter, overlaps);
        for (int i = 0; i < count; i++)
        {
            if (overlaps[i].TryGetComponent(out Enemy enemy) && hit.Add(enemy))
                enemy.TakeDamage(damage, Vector2.zero);
        }

        if (timeLeft > 0f) return;

        gameObject.SetActive(false);
        Finished?.Invoke(this);
    }
}
