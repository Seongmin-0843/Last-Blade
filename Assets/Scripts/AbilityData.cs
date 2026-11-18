using UnityEngine;

public enum AbilityType
{
    Dash,
    SwordWave,
    SpinSlash,
    MoveSpeed,
    MaxHealth,
    AttackSpeed,
    AttackPower,
    Frost,
}

/// <summary>
/// 스킬·능력치 하나의 이름, 설명, 단계별 수치. 레벨업 카드와 효과 적용에 쓴다. (기획서 §6)
/// </summary>
[CreateAssetMenu(menuName = "Last Blade/Ability")]
public class AbilityData : ScriptableObject
{
    public AbilityType type;
    public string displayName;

    [Tooltip("이 단계로 올릴 때 카드에 보여줄 한 줄 설명. 0번이 1단계.")]
    public string[] descriptions;

    [Tooltip("단계별 누적 수치. 0번이 1단계. 대쉬=쿨타임(초), 검기=개수, 회전 베기=반지름, 냉기=빙결 확률(0~1), 능력치=누적 증가량(이동·공격 속도·공격력은 비율, 체력은 수치).")]
    public float[] values;

    public int MaxLevel => values.Length;
}
