using UnityEngine;

public enum TowerGrade
{
    [InspectorName("일반")] Normal = 1,
    [InspectorName("희귀")] Rare = 2,
    [InspectorName("서사")] Epic = 3,
    [InspectorName("유일")] Unique = 4,
    [InspectorName("전설")] Legend = 5,
    [InspectorName("신화")] Mythic = 6,

    [InspectorName("임시")] Temp = 9,
}

public enum TowerRole
{
    [InspectorName("딜러")] Dealer = 1,
    [InspectorName("디버프")] Debuff = 2,
    [InspectorName("버프")] Buff = 3,
    [InspectorName("소환")] Summon = 4,
}

public enum AttackTarget
{
    [InspectorName("앞쪽")] First,
    [InspectorName("뒤쪽")] Last,
    [InspectorName("근거리")] Near,
    [InspectorName("원거리")] Far,
    [InspectorName("강함")] Strong,
    [InspectorName("약함")] Weak,
    [InspectorName("무작위")] Random,
    [InspectorName("없음")] None,
}

[System.Serializable]
public struct SkillValue
{
    public ValueType valueType;
    [Min(0f)] public float baseValue;
    public RankType rankType;
    [Min(0f)] public float rankBonus;

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
    [InspectorName("개수/횟수")] Count = 102,

    [InspectorName("계수")] Factor = 201,
    [InspectorName("확률")] Chance = 202,

    [InspectorName("범위")] Range = 301,
    [InspectorName("크기")] Scale = 302,
    [InspectorName("속도")] Speed = 303,

    [InspectorName("지속")] Duration = 401,
    [InspectorName("쿨다운")] Cooldown = 402,

    [InspectorName("최대")] Max = 501,
    [InspectorName("최소")] Min = 502,
}

public enum RankType
{
    [InspectorName("미적용")] None,
    [InspectorName("더하기")] Add,
    [InspectorName("빼기")] Subtract,
    [InspectorName("곱하기")] Multiply,
    [InspectorName("나누기")] Divide,
}
