using System.Collections.Generic;
using UnityEngine;

public class TowerChance : MonoBehaviour
{
    public static TowerChance Instance { private set; get; }

    [System.Serializable]
    private sealed class Weight
    {
        public TowerGrade grade;
        [SerializeField] private Vector2Int level;

        public int Min => level.x;
        public int Max => level.y;
        public int Add => DataManager.Instance.GetGradeStat(grade) * 10;

        public Weight(TowerGrade _grade, int _min, int _max)
        {
            grade = _grade;
            level.x = _min;
            level.y = _max;
        }

        public bool Contains(int _level)
        {
            if (Min <= 0 && Max <= 0) return false;
            if (Min > 0 && _level < Min) return false;
            if (Max > 0 && _level > Max) return false;

            return true;
        }
    }

    [Header("Role")]
    [SerializeField] private Weight normal = new(TowerGrade.Normal, 0, 0);
    [SerializeField] private Weight rare = new(TowerGrade.Rare, 2, 5);
    [SerializeField] private Weight epic = new(TowerGrade.Epic, 4, 9);
    [SerializeField] private Weight unique = new(TowerGrade.Unique, 8, 17);
    [SerializeField] private Weight legend = new(TowerGrade.Legend, 16, 0);
    private Weight[] weights;

    [Header("Chance")]
    [SerializeField] private List<int> currents = new();
    [SerializeField] private List<float> chances = new();
    private readonly SortedDictionary<TowerGrade, float> chanceDic = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Init();
    }

    private void Init()
    {
        weights = new Weight[] { normal, rare, epic, unique, legend };

        currents.Clear();
        chances.Clear();
        chanceDic.Clear();

        for (int i = 0; i < weights.Length; i++)
        {
            currents.Add(0);
            chances.Add(0);
            chanceDic[weights[i].grade] = 0;
        }
    }

    #region SET
    public void ResetChance()
    {
        for (int i = 0; i < weights.Length; i++)
        {
            if (weights[i].grade == normal.grade)
                currents[i] = 100;
            else
                currents[i] = 0;
        }

        for (int i = 1; i <= GameManager.Instance?.Level; i++)
            SetChance(i);
    }

    public void SetChance(int _level)
    {
        int gradeCount = weights.Length;
        for (int i = 0; i < gradeCount; i++)
        {
            Weight weight = weights[i];

            if (!weight.Contains(_level)) continue;

            currents[i] += weight.Add;
        }

        int totalWeight = 0;
        for (int i = 0; i < gradeCount; i++)
        {
            if (!IsValid(weights[i].grade)) continue;

            totalWeight += currents[i];
        }

        for (int i = 0; i < gradeCount; i++)
        {
            TowerGrade grade = weights[i].grade;

            chanceDic[grade] = totalWeight > 0 && IsValid(grade)
                ? (float)currents[i] / totalWeight * 100f
                : 0f;

            chances[i] = chanceDic[grade];
        }
    }
    #endregion

    #region GET
    public bool IsValid(TowerGrade _grade)
    {
        for (int i = 0; i < weights.Length; i++)
        {
            if (_grade > 0 && weights[i].grade != _grade) continue;
            if (currents[i] <= 0) return false;

            return DataManager.Instance?.GetTowerDatas(_grade).Length > 0;
        }

        return false;
    }

    public int GetBestLevel(TowerGrade _grade)
    {
        int bestLevel = 0;
        float bestChance = 0f;

        for (int i = 1; i <= GameManager.Instance?.MaxLevel; i++)
        {
            float chance = GetChance(_grade, i);
            if (chance <= bestChance) continue;

            bestChance = chance;
            bestLevel = i;
        }

        return bestLevel;
    }

    public TowerGrade GetGrade()
    {
        float roll = Random.Range(0f, 100f);
        float acc = 0f;

        for (int i = 0; i < weights.Length; i++)
        {
            acc += chances[i];
            if (roll < acc) return weights[i].grade;
        }

        return TowerGrade.Temp;
    }

    public float GetChance(TowerGrade _grade) => chanceDic.TryGetValue(_grade, out float chance) ? chance : 0f;
    public float GetChance(TowerGrade _grade, int _level)
    {
        int targetWeight = 0;
        int totalWeight = 0;

        for (int i = 0; i < weights.Length; i++)
        {
            Weight weight = weights[i];
            int current = weight.grade == normal.grade ? 100 : 0;

            for (int j = 1; j <= _level; j++)
                if (weight.Contains(j))
                    current += weight.Add;

            if (current <= 0) continue;
            if (!(DataManager.Instance?.GetTowerDatas(weight.grade).Length > 0)) continue;

            totalWeight += current;

            if (weight.grade == _grade)
                targetWeight = current;
        }

        return totalWeight > 0 ? (float)targetWeight / totalWeight * 100f : 0f;
    }

    public IReadOnlyDictionary<TowerGrade, float> GetChances() => chanceDic;
    #endregion
}
