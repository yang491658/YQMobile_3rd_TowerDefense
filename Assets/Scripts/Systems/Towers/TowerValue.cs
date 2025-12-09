using UnityEngine;

public enum TowerGrade
{
    [InspectorName("일반")] Normal,
    [InspectorName("희귀")] Rare,
    [InspectorName("서사")] Epic,
    [InspectorName("유일")] Unique,
    [InspectorName("전설")] Legend,
    [InspectorName("신화")] Mythic,
}

public enum TowerRole
{
    [InspectorName("딜러")] Dealer,
    [InspectorName("디버프")] Debuff,
    [InspectorName("버프")] Buff,
    [InspectorName("소환")] Summon,
}

public enum AttackTarget
{
    [InspectorName("없음")] None,
    [InspectorName("무작위")] Random,
    [InspectorName("앞쪽")] First,
    [InspectorName("뒤쪽")] Last,
    [InspectorName("근거리")] Near,
    [InspectorName("원거리")] Far,
    [InspectorName("약함")] Weak,
    [InspectorName("강함")] Strong,
}

public enum ValueType
{
    [InspectorName("데미지")] Damage,
    [InspectorName("퍼센트")] Percent,
    [InspectorName("범위")] Range,
    [InspectorName("지속시간")] Duration,
}

public enum RankApplyMode
{
    [InspectorName("미적용")] None,
    [InspectorName("더하기")] Add,
    [InspectorName("빼기")] Subtract,
    [InspectorName("곱하기")] Multiply,
    [InspectorName("나누기")] Divide,
}

[System.Serializable]
public struct SkillValue
{
    public ValueType Type;
    public float BaseValue;
    public RankApplyMode RankMode;
    public float RankBonus;
}