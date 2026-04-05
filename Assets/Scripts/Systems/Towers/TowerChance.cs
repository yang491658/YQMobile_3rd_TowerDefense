using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TowerChance", menuName = "Tower/Table/Chance", order = 102)]
public class TowerChance : ScriptableObject
{
    [System.Serializable]
    public class GradeChance
    {
        public TowerGrade grade;
        public int weight;
    }

    [System.Serializable]
    public class LevelChanceRow
    {
        public int level;
        public List<GradeChance> gradeChances = new();
    }

    [SerializeField] private List<LevelChanceRow> levels = new();
    private readonly Dictionary<int, LevelChanceRow> levelDic = new();

#if UNITY_EDITOR
    [SerializeField] private TextAsset csv;

    private void OnValidate()
    {
        if (csv != null)
            ApplyCSV(csv);
    }

    private void ApplyCSV(TextAsset _csv)
    {
        string[] lines = _csv.text.Split('\n');
        string[] header = lines[0].Trim().Split(',');

        int columnCount = header.Length;
        TowerGrade[] columnGrades = new TowerGrade[columnCount];

        for (int i = 1; i < columnCount; i++)
        {
            string name = header[i].Trim();
            if (name.Length == 0) continue;

            TowerGrade grade = (TowerGrade)System.Enum.Parse(typeof(TowerGrade), name, true);
            columnGrades[i] = grade;
        }

        int maxLevel = 0;
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.Length == 0) continue;

            string[] parts = line.Split(',');
            int level = int.Parse(parts[0]);
            if (level > maxLevel) maxLevel = level;
        }

        BuildLevel(maxLevel);

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.Length == 0) continue;

            string[] parts = line.Split(',');
            int level = int.Parse(parts[0]);

            LevelChanceRow row = levelDic[level];

            for (int col = 1; col < columnCount; col++)
            {
                if (col >= parts.Length) break;

                string v = parts[col].Trim();
                if (v.Length == 0) continue;

                TowerGrade grade = columnGrades[col];
                if (grade == 0) continue;

                int weight = int.Parse(v);

                for (int g = 0; g < row.gradeChances.Count; g++)
                {
                    if (row.gradeChances[g].grade == grade)
                    {
                        row.gradeChances[g].weight = weight;
                        break;
                    }
                }
            }
        }
    }

    private void BuildLevel(int _max)
    {
        TowerGrade[] grades = (TowerGrade[])System.Enum.GetValues(typeof(TowerGrade));
        List<LevelChanceRow> newLevels = new List<LevelChanceRow>(_max);

        for (int level = 1; level <= _max; level++)
        {
            LevelChanceRow row = null;

            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i].level == level)
                { row = levels[i]; break; }
            }

            if (row == null)
            {
                row = new LevelChanceRow();
                row.level = level;
            }

            for (int i = row.gradeChances.Count - 1; i >= 0; i--)
            {
                GradeChance gc = row.gradeChances[i];
                if (gc.grade == TowerGrade.Temp)
                    row.gradeChances.RemoveAt(i);
            }

            for (int i = 0; i < grades.Length; i++)
            {
                TowerGrade grade = grades[i];
                if (grade == TowerGrade.Temp) continue;

                bool found = false;
                for (int j = 0; j < row.gradeChances.Count; j++)
                {
                    if (row.gradeChances[j].grade == grade)
                    { found = true; break; }
                }

                if (!found)
                    row.gradeChances.Add(new GradeChance { grade = grade, weight = 0 });
            }

            newLevels.Add(row);
        }

        newLevels.Sort((_a, _b) => _a.level.CompareTo(_b.level));
        levels = newLevels;

        SetDictionary();
    }
#endif

    private void OnEnable()
    {
        SetDictionary();
    }

    #region SET
    private void SetDictionary()
    {
        levelDic.Clear();
        for (int i = 0; i < levels.Count; i++)
        {
            LevelChanceRow row = levels[i];
            levelDic[row.level] = row;
        }
    }
    #endregion

    #region GET
    public TowerGrade GetGrade(int _level)
    {
        List<GradeChance> gradeChances = levelDic[_level].gradeChances;

        int totalWeight = 0;
        for (int i = 0; i < gradeChances.Count; i++)
        {
            int w = gradeChances[i].weight;
            if (w > 0) totalWeight += w;
        }

        if (totalWeight <= 0)
            return TowerGrade.Temp;

        int roll = Random.Range(0, totalWeight);
        int acc = 0;

        for (int i = 0; i < gradeChances.Count; i++)
        {
            GradeChance gc = gradeChances[i];
            if (gc.weight <= 0) continue;

            acc += gc.weight;
            if (roll < acc) return gc.grade;
        }

        return TowerGrade.Temp;
    }

    public IReadOnlyList<GradeChance> GetGradeChance(int _level) => levelDic[_level].gradeChances;
    #endregion
}
