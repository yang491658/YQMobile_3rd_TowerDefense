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
    [SerializeField] private GameObject bulletBase;

    [Header("InGame")]
    [SerializeField] private Transform inGame;
    [SerializeField] private Transform towerTrans;
    [SerializeField] private Transform monsterTrans;
    [SerializeField] private Transform otherTrans;
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
        if (bulletBase == null)
            bulletBase = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bullet.prefab");
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
        PoolManager.Instance?.Init(bulletBase);
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

    public Bullet MakeBullet(Tower _tower, Monster _target)
    {
        Bullet bullet = SpawnPool<Bullet>(bulletBase, _tower.transform.position, otherTrans);
        if (bullet == null) return null;
        bullet.SetBullet(_tower, _target);
        return bullet;
    }
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
        if (otherTrans == null) otherTrans = GameObject.Find("InGame/Others")?.transform;
    }
    #endregion

    #region GET_공통
    private T GetRandom<T>(List<T> _list) where T : class
    {
        if (_list.Count == 0) return null;
        return _list[Random.Range(0, _list.Count)];
    }

    private T GetByIndex<T>(List<T> _list, int _index) where T : class
    {
        if (_list.Count == 0) return null;
        if (_index < 0 || _index >= _list.Count) return null;
        return _list[_index];
    }

    private T GetByDistance<T>(List<T> _list, Vector3 _pos, bool _near, int _distance) where T : Component
    {
        if (_list.Count == 0) return null;

        float maxDistSqr = _distance > 0 ? _distance * _distance : float.MaxValue;

        T result = null;
        bool found = false;
        float bestDistSqr = 0f;

        for (int i = 0; i < _list.Count; i++)
        {
            T entity = _list[i];
            if (entity == null) continue;

            float distSqr = (entity.transform.position - _pos).sqrMagnitude;

            if (distSqr > maxDistSqr) continue;

            if (!found || (_near ? distSqr < bestDistSqr : distSqr > bestDistSqr))
            {
                found = true;
                bestDistSqr = distSqr;
                result = entity;
            }
        }

        return found ? result : null;
    }

    private List<T> GetInRange<T>(List<T> _list, Vector3 _center, float _range, int _count = 0) where T : Component
    {
        List<T> result = new List<T>();
        int total = _list.Count;
        if (total == 0) return result;

        float r2 = _range * _range;

        if (_count <= 0)
        {
            for (int i = 0; i < total; i++)
            {
                T entity = _list[i];
                Vector3 diff = entity.transform.position - _center;
                if (diff.sqrMagnitude <= r2)
                    result.Add(entity);
            }
            return result;
        }

        List<int> indices = new List<int>();
        for (int i = 0; i < total; i++)
        {
            T entity = _list[i];
            Vector3 diff = entity.transform.position - _center;
            if (diff.sqrMagnitude <= r2)
                indices.Add(i);
        }

        int available = indices.Count;
        if (available == 0) return result;

        int pick = Mathf.Min(_count, available);
        for (int i = 0; i < pick; i++)
        {
            int r = Random.Range(0, indices.Count);
            int index = indices[r];
            result.Add(_list[index]);

            int last = indices.Count - 1;
            indices[r] = indices[last];
            indices.RemoveAt(last);
        }

        return result;
    }

    private T GetByStat<T>(List<T> _list, System.Func<T, float> _selector, bool _low, int _min = 0, bool _useMin = false) where T : class
    {
        if (_list.Count == 0) return null;

        bool hasBest = false;
        float bestValue = 0f;
        T bestEntity = null;

        for (int i = 0; i < _list.Count; i++)
        {
            T entity = _list[i];
            float value = _selector(entity);
            if (_useMin && value < _min) continue;

            if (!hasBest)
            {
                hasBest = true;
                bestValue = value;
                bestEntity = entity;
                continue;
            }

            if (_low)
            {
                if (value < bestValue)
                { bestValue = value; bestEntity = entity; }
            }
            else
            {
                if (value > bestValue)
                { bestValue = value; bestEntity = entity; }
            }
        }

        if (!hasBest) return null;

        return bestEntity;
    }
    #endregion

    #region GET_타워
    public List<Tower> GetTowers() => towers;
    public int GetTowerCount() => towers.Count;

    public Tower GetTowerRandom()
        => GetRandom(towers);

    public Tower GetTowerFirst()
        => GetByIndex(towers, 0);

    public Tower GetTowerLast()
        => GetByIndex(towers, towers.Count - 1);

    public Tower GetTowerNearest(Vector3 _pos, int _distance = 0)
        => GetByDistance(towers, _pos, true, _distance);

    public Tower GetTowerFarthest(Vector3 _pos, int _distance = 0)
        => GetByDistance(towers, _pos, false, _distance);

    public Tower GetTowerHighRank(int _minRank = 0)
        => GetByStat(towers, _tower => _tower.GetRank(), false, _minRank, true);

    public Tower GetTowerLowRank(int _minRank = 0)
        => GetByStat(towers, _tower => _tower.GetRank(), true, _minRank, true);

    public List<Tower> GetTowersInRange(Vector3 _center, float _range, int _count = 0)
        => GetInRange(towers, _center, _range, _count);
    #endregion

    #region GET_몬스터
    public List<Monster> GetMonsters() => monsters;
    public int GetMonsterCount() => monsters.Count;

    public Monster GetMonsterRandom()
        => GetRandom(monsters);

    public Monster GetMonsterByIndex(int _index)
        => GetByIndex(monsters, _index);

    public Monster GetMonsterFirst()
        => GetByIndex(monsters, 0);

    public Monster GetMonsterLast()
        => GetByIndex(monsters, monsters.Count - 1);

    public Monster GetMonsterNearest(Vector3 _pos, int _distance = 0)
        => GetByDistance(monsters, _pos, true, _distance);

    public Monster GetMonsterFarthest(Vector3 _pos, int _distance = 0)
        => GetByDistance(monsters, _pos, false, _distance);

    public Monster GetMonsterHighHealth()
        => GetByStat(monsters, _monster => _monster.GetHealth(), false, 0, false);

    public Monster GetMonsterLowHealth()
        => GetByStat(monsters, _monster => _monster.GetHealth(), true, 0, false);

    public List<Monster> GetMonstersInRange(Vector3 _center, float _range, int _count = 0)
        => GetInRange(monsters, _center, _range, _count);
    #endregion
}
