using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬·능력치의 현재 단계를 들고 있다가, 단계가 오르면 해당 컴포넌트에 수치를 넘긴다.
/// </summary>
public class PlayerSkills : MonoBehaviour
{
    readonly Dictionary<AbilityData, int> levels = new();

    PlayerController controller;
    PlayerHealth health;
    SwordAttack sword;
    DashSkill dash;
    SwordWaveSkill wave;
    SpinSlashSkill spin;
    FrostSkill frost;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        health = GetComponent<PlayerHealth>();
        sword = GetComponent<SwordAttack>();
        dash = GetComponent<DashSkill>();
        wave = GetComponent<SwordWaveSkill>();
        spin = GetComponent<SpinSlashSkill>();
        frost = GetComponent<FrostSkill>();
    }

    public int GetLevel(AbilityData ability) => levels.GetValueOrDefault(ability);

    public void LevelUp(AbilityData ability)
    {
        int level = GetLevel(ability) + 1;
        levels[ability] = level;

        float value = ability.values[level - 1];
        float previous = level > 1 ? ability.values[level - 2] : 0f;

        switch (ability.type)
        {
            case AbilityType.Dash: dash.Upgrade(value); break;
            case AbilityType.SwordWave: wave.Upgrade(Mathf.RoundToInt(value)); break;
            case AbilityType.SpinSlash: spin.Upgrade(value); break;
            case AbilityType.MoveSpeed: controller.SpeedMultiplier = 1f + value; break;
            case AbilityType.MaxHealth: health.AddMaxHealth(value - previous); break;
            case AbilityType.AttackSpeed: sword.DelayMultiplier = 1f - value; break;
            case AbilityType.AttackPower: sword.DamageMultiplier = 1f + value; break;
            case AbilityType.Frost: frost.Upgrade(value); break;
        }
    }
}
