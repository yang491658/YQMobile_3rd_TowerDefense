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
    [InspectorName("경제")] Economy,
    [InspectorName("소환")] Summon,
    [InspectorName("빌드")] Build,
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
    [InspectorName("데미지")] Damage = 101,
    [InspectorName("개수")] Count = 102,
    [InspectorName("수치")] Amount = 103,

    [InspectorName("계수")] Factor = 201,
    [InspectorName("비율")] Percent = 202,
    [InspectorName("확률")] Chance = 203,

    [InspectorName("범위")] Range = 301,
    [InspectorName("속도")] Speed = 302,

    [InspectorName("지속")] Duration = 401,
    [InspectorName("간격")] Interval = 402,
    [InspectorName("쿨다운")] Cooldown = 403,

    [InspectorName("중첩")] Stack = 501,
    [InspectorName("제한")] Limit = 502,

    [InspectorName("생산")] Production = 601,
    [InspectorName("변동")] Delta = 602,
}

public enum RankType
{
    [InspectorName("미적용")] None,
    [InspectorName("더하기")] Add,
    [InspectorName("빼기")] Subtract,
    [InspectorName("곱하기")] Multiply,
    [InspectorName("나누기")] Divide,
}