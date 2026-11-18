using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레벨업 때 3단계 미만인 항목 중 서로 다른 2개를 뽑아 보여주고, 고른 것을 적용한다. (기획서 §6)
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    [SerializeField] AbilityData[] abilities;
    [SerializeField] ExperienceManager experience;
    [SerializeField] PlayerSkills skills;
    [SerializeField] LevelUpUI ui;

    readonly List<AbilityData> candidates = new();

    int pendingChoices;

    void OnEnable() => experience.LeveledUp += OnLeveledUp;
    void OnDisable() => experience.LeveledUp -= OnLeveledUp;

    void OnLeveledUp()
    {
        pendingChoices++;
        if (pendingChoices == 1) ShowChoice();
    }

    void ShowChoice()
    {
        candidates.Clear();
        foreach (AbilityData ability in abilities)
            if (skills.GetLevel(ability) < ability.MaxLevel) candidates.Add(ability);

        // 앞에서 2칸만 섞으면 된다
        for (int i = 0; i < 2; i++)
        {
            int j = Random.Range(i, candidates.Count);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        GameManager.Instance.SetLevelUpOpen(true);
        ui.Show(candidates[0], skills.GetLevel(candidates[0]), candidates[1], skills.GetLevel(candidates[1]), OnChosen);
    }

    void OnChosen(AbilityData ability)
    {
        skills.LevelUp(ability);
        pendingChoices--;

        if (pendingChoices > 0) ShowChoice();
        else GameManager.Instance.SetLevelUpOpen(false);
    }
}
