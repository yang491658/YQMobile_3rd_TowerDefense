public enum TowerGrade
{
    Normal,
    Rare,
    Hero,
    Legendary
}

public enum TowerRole
{
    Damage,
    Debuff,
    Buff,
    Summon,
    Economy,
    Control,
}

public enum AttackTarget
{
    None,
    Random,
    First,
    Last,
    Near,
    Far,
    Weak,
    Strong,
    NoDebuff,
}

public enum TriggerTime
{
    None,
    OnSpawn,
    OnAttack,
    OnHit,
    OnKill,
    OnMerge,
    OnRankUp,
    OnInterval,
    OnStack,
}

public enum HitType
{
    None,
    Single,
    Splash,
    Chain,
}

public enum DebuffType
{
    None,
    Poison,
    Burn,
    Slow,
    Freeze,
    Stun,
}

public enum BuffType
{
    None,
    AttackDamage,
    AttackSpeed,
    CritChance,
    CritDamage,
}
