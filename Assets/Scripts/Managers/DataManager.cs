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
    [SerializeField] private TowerColor towerColor;
    [SerializeField] private TowerSymbol towerSymbol;
    [SerializeField] private TowerStat towerStat;

#if UNITY_EDITOR
    private void OnValidate()
    {
        towerDatas = CollectDatas<TowerData>("t:TowerData", new[] { "Assets/Datas/Towers" });

        towerChance = LoadTableAsset<TowerChance>();
        towerColor = LoadTableAsset<TowerColor>();
        towerSymbol = LoadTableAsset<TowerSymbol>();
        towerStat = LoadTableAsset<TowerStat>();

        EditorUtility.SetDirty(this);
    }

    private static TAsset[] CollectDatas<TAsset>(string _filter, string[] _folders) where TAsset : ScriptableObject
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

        return list.ToArray();
    }

    private T LoadTableAsset<T>() where T : ScriptableObject
    {
        string typeName = typeof(T).Name;
        string[] guids = AssetDatabase.FindAssets($"t:{typeName}", new[] { "Assets/Datas/Tables" });
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
        => towerDic.TryGetValue(_id, out var _data) ? _data : towerDatas[Random.Range(0, towerDatas.Length)];
    #endregion

    #region SET
    private void SetDictionary()
    {
        towerDic.Clear();
        foreach (var d in towerDatas)
            if (d != null)
                towerDic.TryAdd(d.ID, d);
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

    public IReadOnlyList<TowerChance.GradeChance> GetGradeChance(int _level) => towerChance.GetGradeChance(_level);
    public TowerGrade GetRandomGrade(int _level) => towerChance.GetGrade(_level);

    public Color GetGradeColor(TowerGrade _grade) => towerColor.GetColor(_grade);
    public Sprite GetRoleSymbol(TowerRole _role) => towerSymbol.GetSymbol(_role);

    public TowerStat.Stat4 GetBaseStat(TowerRole _role, TowerGrade _grade) => towerStat.GetStat(_role, _grade);
    public int GetGradeStat(TowerGrade _grade) => towerStat.GetGradeStat(_grade);
    #endregion
}
