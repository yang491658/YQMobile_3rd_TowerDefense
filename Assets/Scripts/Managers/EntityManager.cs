using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class EntityManager : MonoBehaviour
{
    public static EntityManager Instance { private set; get; }

    [Header("Base")]
    [SerializeField] private GameObject towerBase;

    [Header("InGame")]
    [SerializeField] private Transform inGame;
    [SerializeField] private Transform towerTrans;
    [Space]
    [SerializeField] private List<Tower> towers = new List<Tower>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (towerBase == null)
            towerBase = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tower.prefab");
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

        SetEntity();
    }

    #region 타워
    public Tower SpawnTower(int _id = 0, int _rank = 1, Vector3? _pos = null, bool _useGold = true)
    {
        int id = _id;
        if (_id == 0)
        {
            TowerGrade grade = DataManager.Instance.GetRandomGrade(GameManager.Instance.GetLevel());
            TowerData[] datas = DataManager.Instance?.GetTowerDatas(grade);

            if (datas.Length > 0)
                id = datas[Random.Range(0, datas.Length)].ID;
        }

        TowerData data = DataManager.Instance?.SearchTower(id);
        if (data == null) return null;

        if (_useGold && !GameManager.Instance.EnoughGold()) return null;

        Tower tower = Instantiate(towerBase, _pos.Value, Quaternion.identity, towerTrans)
            .GetComponent<Tower>();
        tower.SetData(data);
        tower.SetRank(_rank);
        towers.Add(tower);

        GameManager.Instance?.UseGold(_useGold);

        return tower;
    }

    public bool CanMerge(Tower _select, Tower _target)
        => _select != null && _target != null && _select != _target &&
        _select.GetID() == _target.GetID() &&
        _select.GetRank() == _target.GetRank() &&
        !_select.IsMax && !_target.IsMax;

    public Tower MergeTower(Tower _select, Tower _target)
    {
        int id = _target.GetID();
        int rank = _target.GetRank();
        Vector3 pos = _target.transform.position;

        DespawnTower(_select);
        DespawnTower(_target);

        return SpawnTower(id, rank + 1, pos, false);
    }

    public void DespawnTower(Tower _tower)
    {
        towers.Remove(_tower);
        Destroy(_tower.gameObject);
    }

    public void SellTower(Tower _tower)
    {
        int gain = DataManager.Instance.GetGradeStat(_tower.GetData().Grade);
        int rank = _tower.GetRank();

        GameManager.Instance?.ExpUp(gain * rank);
        GameManager.Instance?.GoldUp(GameManager.Instance.GetNeedGold() * rank / 2);

        DespawnTower(_tower);
    }
    #endregion

    #region SET
    public void ResetEntity()
    {
        towers.RemoveAll(_tower => _tower == null);
    }

    public void SetEntity()
    {
        if (inGame == null) inGame = GameObject.Find("InGame")?.transform;
        if (towerTrans == null) towerTrans = GameObject.Find("InGame/Towers")?.transform;
    }
    #endregion

    #region GET
    #endregion
}
