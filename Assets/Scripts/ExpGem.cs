using UnityEngine;

/// <summary>
/// 바닥에 떨어진 경험치 젬. 흡수 반경에 들어오면 플레이어에게 날아가 경험치가 된다.
/// </summary>
public class ExpGem : MonoBehaviour
{
    [Tooltip("플레이어에게 날아가는 속도(units/s).")]
    [SerializeField] float flySpeed = 10f;
    [Tooltip("이 거리까지 오면 흡수된다(units).")]
    [SerializeField] float pickupDistance = 0.2f;

    ExperienceManager manager;
    Transform player;
    float absorbRadius;
    int value;
    bool flying;

    public void Init(ExperienceManager manager, Transform player, float absorbRadius)
    {
        this.manager = manager;
        this.player = player;
        this.absorbRadius = absorbRadius;
    }

    public void Drop(Vector2 position, int value)
    {
        this.value = value;
        flying = false;
        transform.position = position;
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (Time.deltaTime <= 0f) return;

        Vector2 position = transform.position;
        float distance = Vector2.Distance(position, player.position);

        if (distance <= absorbRadius) flying = true;
        if (!flying) return;

        if (distance <= pickupDistance)
        {
            gameObject.SetActive(false);
            manager.Collect(this, value);
            return;
        }

        transform.position = Vector2.MoveTowards(position, player.position, flySpeed * Time.deltaTime);
    }
}
