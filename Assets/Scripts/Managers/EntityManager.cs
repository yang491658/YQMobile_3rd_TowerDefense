using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EntityManager : MonoBehaviour
{
    public static EntityManager Instance { private set; get; }

    [Header("Base")]
    [SerializeField] private GameObject towerBase;
    [SerializeField] private GameObject monsterBase;

    [Header("InGame")]
    [SerializeField] private Transform inGame;
    [SerializeField] private Transform towerTrans;
    [SerializeField] private Transform monsterTrans;
    [Space]
    [SerializeField] private List<Tower> towers = new List<Tower>();
    [SerializeField] private List<Monster> monsters = new List<Monster>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (towerBase == null)
            towerBase = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tower.prefab");
        if (monsterBase == null)
            monsterBase = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster.prefab");
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

    private void Start()
    {
        PoolManager.Instance?.Init(monsterBase);
    }

    #region 타워
    public Tower SpawnTower(int _id = 0, int _rank = 1, Vector3 _pos = default, bool _useGold = true)
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

        Tower tower = Instantiate(towerBase, _pos, Quaternion.identity, towerTrans)
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

    #region 몬스터
    public Monster SpawnMonster(Vector3 _pos = default)
    {
        Monster monster = SpawnPool<Monster>(monsterBase, _pos, monsterTrans);
        if (monster == null) return null;

        monster.SetMonster(GameManager.Instance.GetScore() / 50);
        monsters.Add(monster);

        return monster;
    }

    public void DespawnMonster(Monster _monster)
    {
        monsters.Remove(_monster);
        _monster.Despawn();
    }
    #endregion

    #region 풀링
    private T SpawnPool<T>(GameObject _prefab, Vector3 _pos, Transform _parent) where T : Component
        => PoolManager.Instance?.Rent(_prefab, _pos, _parent)?.GetComponent<T>();

    public void DespawnPool(Pooling _pooling)
        => PoolManager.Instance?.Release(_pooling.gameObject);
    #endregion

    public void DespawnAll()
    {
        for (int i = towers.Count - 1; i >= 0; i--)
            DespawnTower(towers[i]);
        for (int i = monsters.Count - 1; i >= 0; i--)
            DespawnMonster(monsters[i]);
    }

    #region SET
    public void ResetEntity()
    {
        towers.RemoveAll(_tower => _tower == null);
        monsters.RemoveAll(_monster => _monster == null);
    }

    public void SetEntity()
    {
        if (inGame == null) inGame = GameObject.Find("InGame")?.transform;
        if (towerTrans == null) towerTrans = GameObject.Find("InGame/Towers")?.transform;
        if (monsterTrans == null) monsterTrans = GameObject.Find("InGame/Monsters")?.transform;
    }
    #endregion

    #region GET
    #endregion
}
