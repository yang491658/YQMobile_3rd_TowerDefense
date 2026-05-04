using System.Collections.Generic;
using UnityEngine;

public class TowerBuff : MonoBehaviour
{
    public enum BuffType { Stat, Attack, Resource, Defense, Skill }
    public enum SubType { Damage, Speed, Chance, Critical }
    public enum ApplyType { Add, Refresh, Replace }

    [System.Serializable]
    private sealed class Buff
    {
        [SerializeField] private Tower tower;
        [SerializeField] private int skillID;
        [Space]
        [SerializeField] private BuffType type;
        [SerializeField] private SubType sub;
        [Space]
        [SerializeField] private int value;
        [SerializeField] private float duration;
        [SerializeField] private float timer;

        public BuffType Type => type;
        public SubType Sub => sub;
        public int Value => value;
        public bool IsActive => duration == 0f || timer > 0f;

        public Buff(Tower _tower, TowerSkill _skill, BuffType _type, SubType _sub, int _value, float _duration)
        {
            tower = _tower;
            skillID = _skill.ID;
            type = _type;
            sub = _sub;
            value = _value;
            duration = _duration;
            timer = _duration;
        }

        public bool IsSame(Tower _tower, TowerSkill _skill = null, BuffType? _type = null, SubType? _sub = null)
        {
            if (tower != _tower) return false;
            if (_skill != null && skillID != _skill.ID) return false;
            if (_type != null && type != _type.Value) return false;
            if (_sub != null && sub != _sub.Value) return false;

            return true;
        }

        public void Refresh(int _value, float _duration)
        {
            value = _value;
            duration = _duration;
            timer = _duration;
        }

        public bool Update(float _deltaTime)
        {
            if (duration == 0f) return true;

            timer -= _deltaTime;
            return timer > 0f;
        }
    }

    [Header("Buff")]
    [SerializeField] private List<Buff> buffs = new();

    private void Update()
    {
        UpdateStat(Time.deltaTime);
    }

    public void Clear()
    {
        buffs.Clear();
    }

    #region 스탯형 버프
    public void ApplyStat(Tower _tower, TowerSkill _skill, SubType _sub, int _value, float _duration, ApplyType _applyType)
    {
        switch (_applyType)
        {
            case ApplyType.Add:
                buffs.Add(new Buff(_tower, _skill, BuffType.Stat, _sub, _value, _duration));
                return;

            case ApplyType.Refresh:
                for (int i = 0; i < buffs.Count; i++)
                {
                    Buff buff = buffs[i];
                    if (!buff.IsSame(_tower, _skill, BuffType.Stat, _sub)) continue;

                    buff.Refresh(_value, _duration);
                    return;
                }

                buffs.Add(new Buff(_tower, _skill, BuffType.Stat, _sub, _value, _duration));
                return;

            case ApplyType.Replace:
                RemoveStat(_tower, _skill, _sub);
                buffs.Add(new Buff(_tower, _skill, BuffType.Stat, _sub, _value, _duration));
                return;
        }
    }

    private void UpdateStat(float _deltaTime)
    {
        if (buffs.Count == 0) return;

        for (int i = buffs.Count - 1; i >= 0; i--)
        {
            if (buffs[i].Type != BuffType.Stat) continue;
            if (buffs[i].Update(_deltaTime)) continue;

            buffs.RemoveAt(i);
        }
    }

    public int CalcStat(SubType _sub, int _value)
    {
        int bonus = 0;
        for (int i = 0; i < buffs.Count; i++)
        {
            Buff buff = buffs[i];
            if (buff.Type != BuffType.Stat) continue;
            if (buff.Sub != _sub || !buff.IsActive) continue;

            bonus += buff.Value;
        }

        switch (_sub)
        {
            case SubType.Damage:
            case SubType.Speed:
                _value = Mathf.RoundToInt(_value * (100f + bonus) / 100f);
                break;

            case SubType.Chance:
            case SubType.Critical:
                _value += bonus;
                break;
        }

        return _value;
    }

    public void RemoveStat(Tower _tower, TowerSkill _skill, SubType? _sub = null)
    {
        for (int i = buffs.Count - 1; i >= 0; i--)
        {
            Buff buff = buffs[i];
            if (!buff.IsSame(_tower, _skill, BuffType.Stat, _sub)) continue;

            buffs.RemoveAt(i);
        }
    }
    #endregion
}
