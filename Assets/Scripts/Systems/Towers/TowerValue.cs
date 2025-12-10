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
}

public enum AttackTarget
{
    [InspectorName("없음")] None,
    [InspectorName("무작위")] Random,
    [InspectorName("앞쪽")] First,
    [InspectorName("뒤쪽")] Last,
    [InspectorName("근거리")] Near,
    [InspectorName("원거리")] Far,
    [InspectorName("강함")] Strong,
    [InspectorName("약함")] Weak,
}

[System.Serializable]
public struct SkillValue
{
    public ValueType valueType;
    public float baseValue;
    public RankType rankType;
    public float rankBonus;

    public SkillValue(ValueType _vt, float _bv, RankType _rt, float _rb = 0f)
    {
        valueType = _vt;
        baseValue = Mathf.Max(_bv, 0f);
        rankType = _rt;

        if (_rt == RankType.None)
            rankBonus = 0f;
        else if (_rt == RankType.Multiply || _rt == RankType.Divide)
            rankBonus = 1f;
        else
            rankBonus = _rb;
    }
}

public enum ValueType
{
    [InspectorName("데미지")] Damage,
    [InspectorName("횟수")] Count,
    [InspectorName("지속시간")] Duration,
    [InspectorName("범위")] Range,
    [InspectorName("퍼센트")] Percent,
    [InspectorName("확률")] Chance,
}

public enum RankType
{
    [InspectorName("미적용")] None,
    [InspectorName("더하기")] Add,
    [InspectorName("빼기")] Subtract,
    [InspectorName("곱하기")] Multiply,
    [InspectorName("나누기")] Divide,
}