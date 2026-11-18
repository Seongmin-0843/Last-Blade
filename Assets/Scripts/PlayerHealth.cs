using UnityEngine;

/// <summary>
/// 체력, 피격, 피격 후 무적과 깜빡임, 사망. (기획서 §3)
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] float maxHealth = 100f;
    [Tooltip("피격 후 무적 시간(초).")]
    [SerializeField] float invincibleDuration = 0.5f;
    [Tooltip("무적 중 깜빡이는 주기의 절반(초).")]
    [SerializeField] float blinkInterval = 0.08f;
    [SerializeField] SpriteRenderer sprite;

    public float MaxHealth => maxHealth;
    public float Health { get; private set; }

    float invincibleLeft;

    void Awake() => Health = maxHealth;

    /// <summary>최대 체력을 올리고 올린 만큼 회복한다.</summary>
    public void AddMaxHealth(float amount)
    {
        maxHealth += amount;
        Health += amount;
    }

    void Update()
    {
        if (invincibleLeft <= 0f) return;

        invincibleLeft -= Time.deltaTime;
        sprite.enabled = invincibleLeft <= 0f || Mathf.Repeat(invincibleLeft, blinkInterval * 2f) < blinkInterval;
    }

    public void TakeDamage(float damage)
    {
        if (invincibleLeft > 0f || Health <= 0f) return;

        Health = Mathf.Max(0f, Health - damage);

        if (Health > 0f)
        {
            invincibleLeft = invincibleDuration;
            return;
        }

        sprite.enabled = true;
        GameManager.Instance.GameOver();
    }
}
