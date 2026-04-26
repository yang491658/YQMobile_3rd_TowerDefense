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
    [SerializeField] private BossData[] bossDatas;
    private readonly Dictionary<int, BossData> bossDic = new();

    [Header("Tables")]
    [SerializeField] private TowerChance towerChance;
    [SerializeField] private TowerColor towerColor;
    [SerializeField] private TowerStat towerStat;

#if UNITY_EDITOR
    private void OnValidate()
    {
        towerDatas = CollectDatas<TowerData>("t:TowerData", new[] { "Assets/Datas/Towers" }, _data => _data.ID);
        bossDatas = CollectDatas<BossData>("t:BossData", new[] { "Assets/Datas/Monsters" }, _data => _data.ID);

        towerChance = LoadAsset<TowerChance>();
        towerColor = LoadAsset<TowerColor>();
        towerStat = LoadAsset<TowerStat>();

        EditorUtility.SetDirty(this);
    }

    private static TAsset[] CollectDatas<TAsset>
        (string _filter, string[] _folders, System.Func<TAsset, int> _order) where TAsset : ScriptableObject
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
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        return null;
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

    public BossData SearchBoss(int _id)
        => bossDic.TryGetValue(_id, out var _data) ? _data : null;

    public BossData SearchBossByOrder(int _order)
        => (_order > 0 && _order <= bossDatas.Length) ? bossDatas[_order - 1] : null;
    #endregion

    #region SET
    private void SetDictionary()
    {
        towerDic.Clear();
        foreach (var d in towerDatas)
            if (d != null)
                towerDic.TryAdd(d.ID, d);

        bossDic.Clear();
        foreach (var d in bossDatas)
            if (d != null)
                bossDic.TryAdd(d.ID, d);
    }
    #endregion

    #region GET
    public TowerData[] GetTowerDatas() => towerDatas;
    public TowerData[] GetTowerDatas(TowerGrade _grade)
    {
        List<TowerData> result = new(towerDatas.Length);

        for (int i = 0; i < towerDatas.Length; i++)
        {
            TowerData data = towerDatas[i];
            if (data != null && data.Grade == _grade)
                result.Add(data);
        }

        return result.ToArray();
    }
    public int GetTowerID(int _order)
        => (_order > 0 && _order <= towerDatas.Length) ? towerDatas[_order - 1].ID : 0;

    public TowerData GetRandomTower()
    {
        int level = GameManager.Instance.GetLevel();
        TowerGrade grade = GetRandomGrade(level);
        TowerData[] datas = GetTowerDatas(grade);
        return datas[Random.Range(0, datas.Length)];
    }

    public BossData[] GetBossDatas() => bossDatas;
    public int GetBossID(int _order)
        => (_order > 0 && _order <= bossDatas.Length) ? bossDatas[_order - 1].ID : 0;

    public IReadOnlyList<TowerChance.GradeChance> GetGradeChance(int _level) => towerChance.GetGradeChance(_level);
    public TowerGrade GetRandomGrade(int _level) => towerChance.GetGrade(_level);

    public Color GetGradeColor(TowerGrade _grade) => towerColor.GetColor(_grade);

    public TowerStat.Stat4 GetBaseStat(TowerRole _role, TowerGrade _grade) => towerStat.GetStat(_role, _grade);
    public int GetGradeStat(TowerGrade _grade) => towerStat.GetGradeStat(_grade);
    #endregion
}
