using System;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 젬 풀, 경험치 계산, 레벨업 이벤트. 필요 경험치 = baseExp + expPerLevel × 현재 레벨. (기획서 §6)
/// </summary>
public class ExperienceManager : MonoBehaviour
{
    [SerializeField] int baseExp = 5;
    [SerializeField] int expPerLevel = 4;
    [SerializeField] int maxLevel = 10;
    [Tooltip("젬 흡수 반경(units).")]
    [SerializeField] float absorbRadius = 1.5f;

    [SerializeField] ExpGem gemPrefab;
    [Tooltip("미리 만들어둘 젬 개수. 바닥에 이만큼 쌓이면 새 젬은 나오지 않는다.")]
    [SerializeField] int poolSize = 300;
    [SerializeField] Transform player;

    public event Action LeveledUp;

    public int Level { get; private set; }
    public int Exp { get; private set; }
    public int RequiredExp => baseExp + expPerLevel * Level;
    public bool IsMaxLevel => Level >= maxLevel;

    ObjectPool<ExpGem> gems;

    void Awake()
    {
        gems = new ObjectPool<ExpGem>(() =>
        {
            ExpGem gem = Instantiate(gemPrefab, transform);
            gem.gameObject.SetActive(false);
            gem.Init(this, player, absorbRadius);
            return gem;
        }, collectionCheck: false);
        CombatEffects.Prewarm(gems, poolSize);
    }

    public void DropGem(Vector2 position, int value)
    {
        if (gems.CountInactive > 0) gems.Get().Drop(position, value);
    }

    public void Collect(ExpGem gem, int value)
    {
        gems.Release(gem);

        if (IsMaxLevel) return;

        Exp += value;
        while (!IsMaxLevel && Exp >= RequiredExp)
        {
            Exp -= RequiredExp;
            Level++;
            LeveledUp?.Invoke();
        }
    }
}
