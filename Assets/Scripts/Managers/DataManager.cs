using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { private set; get; }

    [Header("Data")]
    [SerializeField] private TowerData[] towerDatas;
    private readonly Dictionary<int, TowerData> towerDic = new();

    [Header("Tables")]
    [SerializeField] private TowerChance towerChance;
    [SerializeField] private TowerConfig towerConfig;

#if UNITY_EDITOR
    private void OnValidate()
    {
        towerDatas = CollectDatas<TowerData>("t:TowerData", new[] { "Assets/Datas/Towers" }, _data => _data.ID);
        towerChance = LoadAsset<TowerChance>();
        towerConfig = LoadAsset<TowerConfig>();

        EditorUtility.SetDirty(this);
    }

    private static TAsset[] CollectDatas<TAsset>(string _filter, string[] _folders, System.Func<TAsset, int> _order) where TAsset : ScriptableObject
    {
        string[] guids = AssetDatabase.FindAssets(_filter, _folders);
        var list = new List<TAsset>(guids.Length);

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            TAsset data = AssetDatabase.LoadAssetAtPath<TAsset>(path);
            if (data != null)
                list.Add(data);
        }

        list.Sort((_a, _b) => _order(_a).CompareTo(_order(_b)));

        return list.ToArray();
    }

    private static T LoadAsset<T>() where T : ScriptableObject
    {
        string typeName = typeof(T).Name;
        string[] guids = AssetDatabase.FindAssets($"t:{typeName}", new[] { "Assets/Datas" });
        if (guids.Length == 0) return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetDictionary();
    }

    #region 검색
    public TowerData SearchTower(int _id)
        => towerDic.TryGetValue(_id, out var _data) ? _data : null;
    #endregion

    #region 상점 시스템
    public bool IsUnlocked(TowerGrade _grade)
    {
        if (_grade == 0) return true;

        IReadOnlyList<TowerChance.GradeChance> chances = GetGradeChance(GameManager.Instance.Level);

        for (int i = 0; i < chances.Count; i++)
        {
            TowerChance.GradeChance chance = chances[i];
            if (chance.grade == _grade)
                return chance.weight > 0;
        }

        return false;
    }

    public int GetBestLevel(TowerGrade _grade)
    {
        if (_grade == 0) return 0;

        int result = 0;
        float bestChance = 0f;
        int maxLevel = GameManager.Instance.MaxLevel;

        for (int level = 1; level <= maxLevel; level++)
        {
            IReadOnlyList<TowerChance.GradeChance> chances = GetGradeChance(level);

            int total = 0; int weight = 0;

            for (int i = 0; i < chances.Count; i++)
            {
                TowerChance.GradeChance chance = chances[i];

                if (chance.weight > 0) total += chance.weight;
                if (chance.grade == _grade) weight = chance.weight;
            }

            float value = total > 0 ? (float)weight / total : 0f;

            if (value > bestChance)
            { bestChance = value; result = level; }
        }

        return result;
    }
    #endregion

    #region SET
    private void SetDictionary()
    {
        towerDic.Clear();
        foreach (TowerData data in towerDatas)
            if (data != null)
                towerDic.TryAdd(data.ID, data);
    }
    #endregion

    #region GET
    public TowerData[] GetTowerDatas() => towerDatas;
    public TowerData[] GetTowerDatas(TowerGrade _grade)
    {
        if (_grade == 0) return towerDatas;

        List<TowerData> result = new(towerDatas.Length);

        for (int i = 0; i < towerDatas.Length; i++)
        {
            TowerData data = towerDatas[i];
            if (data != null && data.HasGrade(_grade))
                result.Add(data);
        }

        return result.ToArray();
    }

    public int GetTowerID(int _order) => (_order > 0 && _order <= towerDatas.Length) ? towerDatas[_order - 1].ID : 0;

    public TowerData GetRandomTower(out TowerGrade _grade)
    {
        int level = GameManager.Instance.Level;
        _grade = GetRandomGrade(level);

        TowerData result = null;
        int count = 0;

        for (int i = 0; i < towerDatas.Length; i++)
        {
            TowerData data = towerDatas[i];
            if (data == null || !data.HasGrade(_grade)) continue;

            if (Random.Range(0, ++count) == 0)
                result = data;
        }

        return result;
    }

    public IReadOnlyList<TowerChance.GradeChance> GetGradeChance(int _level) => towerChance.GetGradeChance(_level);
    public TowerGrade GetRandomGrade(int _level) => towerChance.GetGrade(_level);

    public Color GetTowerColor(TowerGrade _grade) => towerConfig.GetColor(_grade);

    public Stat4 GetTowerStat(TowerRole _role, TowerGrade _grade, int _rank = 1) => towerConfig.GetStat(_role, _grade, _rank);
    public int GetGradeStat(TowerGrade _grade) => towerConfig.GetGradeStat(_grade);

    public DamageData GetTowerDamage(DamageType _type) => towerConfig.GetDamage(_type);
    #endregion
}
