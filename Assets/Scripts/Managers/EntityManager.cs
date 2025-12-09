using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EntityManager : MonoBehaviour
{
    public static EntityManager Instance { private set; get; }

    [Header("Data")]
    [SerializeField] private GameObject monsterBase;
    [SerializeField] private GameObject towerBase;
    [SerializeField] private GameObject bulletBase;

    [SerializeField] private TowerData[] towerDatas;
    private readonly Dictionary<int, TowerData> towerDic = new Dictionary<int, TowerData>();

    [Header("InGame")]
    [SerializeField] private Transform inGame;
    [SerializeField] private Transform monsterTrans;
    [SerializeField] private List<Monster> monsters = new List<Monster>();
    [SerializeField] private Transform towerTrans;
    [SerializeField] private List<Tower> towers = new List<Tower>();

    [Header("Map")]
    [SerializeField] private Transform map;
    [SerializeField] private Transform mapSlot;
    [SerializeField] private Transform mapRoad;
    [SerializeField] private Transform mapSell;
    private Tilemap mapSlotTilemap;
    private Tilemap mapRoadTilemap;
    private Tilemap mapSellTilemap;

    [Header("Monster")]
    [SerializeField][Min(0.1f)] private float delay = 5f;
    [SerializeField][Min(0.1f)] private float minDelay = 0.5f;
    private float baseDelay;
    private Coroutine spawnRoutine;

    [SerializeField] private Transform[] path;
    [SerializeField] private Vector2 pathMargin = new Vector2(86f, 72f) / 100f;
    [SerializeField] private int[] pathNum = { 1, 4, 2, 3, 4, 1, 3, 2, 1, 4 };

    [Header("Tower")]
    [SerializeField] private int needGold = 0;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (monsterBase == null)
            monsterBase = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster.prefab");
        if (towerBase == null)
            towerBase = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tower.prefab");
        if (bulletBase == null)
            bulletBase = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bullet.prefab");

        string[] guids = AssetDatabase.FindAssets("t:TowerData", new[] { "Assets/Datas/Towers" });
        var tlist = new List<TowerData>(guids.Length);
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var data = AssetDatabase.LoadAssetAtPath<TowerData>(path);
            if (data != null) tlist.Add(data);
        }
        towerDatas = tlist.OrderBy(d => d.ID).ThenBy(d => d.Name).ToArray();

        List<Transform> plist = new List<Transform>();
        Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t.name.StartsWith("Path ("))
                plist.Add(t);
        }

        plist.Sort((_a, _b) => GetPathIndex(_a.name).CompareTo(GetPathIndex(_b.name)));
        path = plist.ToArray();
    }

    private int GetPathIndex(string _name)
    {
        int start = _name.IndexOf('(');
        int end = _name.IndexOf(')');
        if (start >= 0 && end > start)
        {
            string number = _name.Substring(start + 1, end - start - 1);
            if (int.TryParse(number, out int _index))
                return _index;
        }
        return 0;
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

        SetTowerDic();
        SetEntity();
    }

    #region 몬스터
    private Monster SpawnMonster(Vector3 _pos)
    {
        Monster monster = Instantiate(monsterBase, _pos, Quaternion.identity, monsterTrans)
            .GetComponent<Monster>();

        monsters.Add(monster);

        return monster;
    }

    public void ToggleSpawnMonster(bool _on)
    {
        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);

        if (_on)
            spawnRoutine = StartCoroutine(SpawnCoroutine());
        else
            spawnRoutine = null;
    }

    private IEnumerator SpawnCoroutine()
    {
        Transform[] monsterPath = new Transform[pathNum.Length];
        for (int i = 0; i < pathNum.Length; i++)
        {
            int index = pathNum[i] - 1;
            monsterPath[i] = path[index];
        }

        float timer = delay;
        while (true)
        {
            float dt = Time.deltaTime;
            timer += dt;
            delay = Mathf.Max(delay - dt / 50f, minDelay);

            if (timer > delay)
            {
                SpawnMonster(monsterPath[0].position + Vector3.left)
                    .SetPath(monsterPath);

                timer = 0f;
                yield return new WaitForSeconds(0.01f);
            }
            yield return null;
        }
    }

    public void RemoveMonster(Monster _monster) => monsters.Remove(_monster);

    public void DespawnMonster(Monster _monster)
    {
        monsters.Remove(_monster);
        Destroy(_monster.gameObject);
    }
    #endregion

    #region 슬롯
    private Vector3 SelectSlot(Vector3? _pos = null)
    {
        Tilemap tilemap = mapSlotTilemap;
        BoundsInt bounds = tilemap.cellBounds;

        HashSet<Vector3Int> slots = new HashSet<Vector3Int>();
        for (int i = 0; i < towers.Count; i++)
        {
            Vector3Int towerCell = tilemap.WorldToCell(towers[i].GetSlot());
            slots.Add(towerCell);
        }

        if (!_pos.HasValue)
        {
            List<Vector3Int> cells = new List<Vector3Int>();
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    if (tilemap.HasTile(cell) && !slots.Contains(cell))
                        cells.Add(cell);
                }
            }

            if (cells.Count == 0)
                return default;

            Vector3Int select = cells[Random.Range(0, cells.Count)];
            return tilemap.GetCellCenterWorld(select);
        }

        Vector3 pos = _pos.Value;
        bool found = false;
        Vector3Int nearestCell = default;
        float minSqr = 0f;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (!tilemap.HasTile(cell))
                    continue;

                Vector3 center = tilemap.GetCellCenterWorld(cell);
                float sqr = (center - pos).sqrMagnitude;

                if (!found || sqr < minSqr)
                {
                    found = true;
                    minSqr = sqr;
                    nearestCell = cell;
                }
            }
        }

        if (!found)
            return default;

        if (slots.Contains(nearestCell))
            return default;

        return tilemap.GetCellCenterWorld(nearestCell);
    }

    public bool CanSpawn(Vector3? _pos = null, bool _useGold = true)
        => EnoughGold(_useGold) && SelectSlot(_pos) != default;

    public bool EnoughGold(bool _useGold = true)
        => !(_useGold && GameManager.Instance?.GetGold() < needGold);
    #endregion

    #region 타워
    public TowerData SearchTower(int _id) => towerDic.TryGetValue(_id, out var _data) ? _data : null;
    public Tower SpawnTower(int _id = 0, int _rank = 1, Vector3? _pos = null, bool _useGold = true)
    {
        TowerData data = (_id == 0)
            ? towerDatas[Random.Range(0, towerDatas.Length)]
            : SearchTower(_id);

        if (data == null) return null;

        if (!CanSpawn(_pos, _useGold))
            return null;

        Vector3 pos = SelectSlot(_pos);

        Tower tower = Instantiate(towerBase, pos, Quaternion.identity, towerTrans)
            .GetComponent<Tower>();

        tower.SetData(data);
        tower.SetRank(_rank);
        tower.transform.localScale = map.transform.localScale;
        towers.Add(tower);

        GameManager.Instance?.GoldDown(_useGold ? needGold++ : 0);

        return tower;
    }

    public bool CanMerge(Tower _select, Tower _target)
        => _select != null && _target != null && _select != _target &&
        _select.GetID() == _target.GetID() &&
        _select.GetRank() == _target.GetRank() &&
        !_select.IsMax() && !_target.IsMax();

    public Tower MergeTower(Tower _select, Tower _target, int _id = 0)
    {
        if (!CanMerge(_select, _target)) return null;

        int rank = _target.GetRank();
        Vector3 pos = _target.transform.position;

        DespawnTower(_select);
        DespawnTower(_target);

        Tower merge = SpawnTower(_id, rank, pos, false);
        merge.RankUp();

        return merge;
    }

    public void DespawnTower(Tower _tower)
    {
        towers.Remove(_tower);
        Destroy(_tower.gameObject);
    }
    #endregion

    public void DespawnAll()
    {
        for (int i = monsters.Count - 1; i >= 0; i--)
            DespawnMonster(monsters[i]);
        for (int i = towers.Count - 1; i >= 0; i--)
            DespawnTower(towers[i]);
    }

    #region SET
    private void SetTowerDic()
    {
        towerDic.Clear();
        for (int i = 0; i < towerDatas.Length; i++)
        {
            var d = towerDatas[i];
            if (d != null && !towerDic.ContainsKey(d.ID))
                towerDic.Add(d.ID, d);
        }
    }

    public void SetDelay(float _delay) => delay = _delay;

    public void ResetEntity()
    {
        monsters.RemoveAll(_monster => _monster == null);
        towers.RemoveAll(_tower => _tower == null);

        delay = baseDelay;
        needGold = 0;
    }

    public void SetEntity()
    {
        baseDelay = delay;

        if (inGame == null) inGame = GameObject.Find("InGame")?.transform;
        if (monsterTrans == null) monsterTrans = GameObject.Find("InGame/Monsters")?.transform;
        if (towerTrans == null) towerTrans = GameObject.Find("InGame/Towers")?.transform;

        if (map == null) map = GameObject.Find("Map")?.transform;
        if (mapSlot == null) mapSlot = GameObject.Find("Slot")?.transform;
        if (mapRoad == null) mapRoad = GameObject.Find("Road")?.transform;
        if (mapSell == null) mapSell = GameObject.Find("Sell")?.transform;
        mapSlotTilemap = mapSlot.GetComponent<Tilemap>();
        mapRoadTilemap = mapRoad.GetComponent<Tilemap>();
        mapSellTilemap = mapSell.GetComponent<Tilemap>();

        SetMap(out float _halfX, out float _halfY);
        SetPath(_halfX, _halfY);
    }

    private void SetMap(out float _halfX, out float _halfY)
    {
        Rect r = AutoCamera.WorldRect;

        _halfX = r.xMax * pathMargin.x;
        _halfY = r.yMax * pathMargin.y;

        Tilemap tilemap = mapRoadTilemap;
        BoundsInt bounds = tilemap.cellBounds;
        Vector3Int cell = new Vector3Int(bounds.xMin + 1, bounds.yMin + 1, 0);
        Vector3 world = tilemap.GetCellCenterWorld(cell);

        float xScale = (_halfX * 2f) / Mathf.Abs(world.x * 2f);
        float yScale = (_halfY * 2f) / Mathf.Abs(world.y * 2f);

        Vector3 scale = new Vector3(xScale, yScale, (xScale + yScale) / 2f);
        if (scale.magnitude > 0f) map.localScale = scale;
    }

    private void SetPath(float _halfX, float _halfY)
    {
        path[0].position = new Vector3(-_halfX, -_halfY);
        path[1].position = new Vector3(_halfX, -_halfY);
        path[2].position = new Vector3(-_halfX, _halfY);
        path[3].position = new Vector3(_halfX, _halfY);
    }
    #endregion

    #region GET_기타
    public bool IsSell(Vector3 _pos)
    {
        Tilemap tilemap = mapSellTilemap;
        if (tilemap == null) return false;

        Vector3Int cell = tilemap.WorldToCell(_pos);
        bool isSell = tilemap.HasTile(cell);

        Color color = tilemap.color;
        color.a = 50f / 255f;
        if (isSell) color.a = 1f;

        tilemap.color = color;
        return isSell;
    }

    public GameObject GetBulletBase() => bulletBase;
    public List<Tower> GetTowers() => towers;
    public int GetNeedGold() => needGold;
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
        return _list[_index];
    }

    private T GetByDistance<T>(List<T> _list, Vector3 _pos, bool _near, int _distance, Tilemap _tilemap) where T : Component
    {
        if (_list.Count == 0) return null;

        Vector3Int centerCell = _tilemap.WorldToCell(_pos);
        int maxDist = _distance > 0 ? _distance : int.MaxValue;

        T result = null;
        bool found = false;
        int bestDist = 0;

        for (int i = 0; i < _list.Count; i++)
        {
            T entity = _list[i];
            Vector3Int cell = _tilemap.WorldToCell(entity.transform.position);
            Vector3Int delta = cell - centerCell;
            int dist = Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y));

            if (dist > maxDist) continue;

            if (!found || (_near ? dist < bestDist : dist > bestDist))
            {
                found = true;
                bestDist = dist;
                result = entity;
            }
        }

        if (!found) return null;
        return result;
    }

    private List<T> GetInRange<T>(List<T> _list, Vector3 _center, float _range) where T : Component
    {
        List<T> result = new List<T>();
        if (_list.Count == 0) return result;

        float r2 = _range * _range;

        for (int i = 0; i < _list.Count; i++)
        {
            T entity = _list[i];
            Vector3 diff = entity.transform.position - _center;
            if (diff.sqrMagnitude <= r2)
                result.Add(entity);
        }

        return result;
    }

    private T GetByStat<T>(List<T> _list, System.Func<T, float> _selector, bool _low, int _min = 0, bool _useMin = false) where T : class
    {
        if (_list.Count == 0) return null;

        List<T> candidates = new List<T>();
        bool hasFirst = false;
        float best = 0;

        for (int i = 0; i < _list.Count; i++)
        {
            T entity = _list[i];
            float value = _selector(entity);
            if (_useMin && value < _min) continue;

            if (!hasFirst)
            {
                hasFirst = true;
                best = value;
                candidates.Add(entity);
                continue;
            }

            if (_low)
            {
                if (value < best)
                {
                    best = value;
                    candidates.Clear();
                    candidates.Add(entity);
                }
                else if (value == best)
                {
                    candidates.Add(entity);
                }
            }
            else
            {
                if (value > best)
                {
                    best = value;
                    candidates.Clear();
                    candidates.Add(entity);
                }
                else if (value == best)
                {
                    candidates.Add(entity);
                }
            }
        }

        if (!hasFirst) return null;

        return candidates[Random.Range(0, candidates.Count)];
    }
    #endregion

    #region GET_몬스터
    public Monster GetMonsterRandom(bool _noDebuff = false)
        => GetRandom(GetMonsterList(_noDebuff));

    public Monster GetMonsterFirst(bool _noDebuff = false)
        => GetByIndex(GetMonsterList(_noDebuff), 0);

    public Monster GetMonsterLast(bool _noDebuff = false)
        => GetByIndex(GetMonsterList(_noDebuff), GetMonsterList(_noDebuff).Count - 1);

    public Monster GetMonsterNearest(Vector3 _pos, int _distance = 0, bool _noDebuff = false)
        => GetByDistance(GetMonsterList(_noDebuff), _pos, true, _distance, mapRoadTilemap);

    public Monster GetMonsterFarthest(Vector3 _pos, int _distance = 0, bool _noDebuff = false)
        => GetByDistance(GetMonsterList(_noDebuff), _pos, false, _distance, mapRoadTilemap);

    public Monster GetMonsterLowHealth(bool _noDebuff = false)
        => GetByStat(GetMonsterList(_noDebuff), _monster => _monster.GetHealth(), true, 0, false);

    public Monster GetMonsterHighHealth(bool _noDebuff = false)
        => GetByStat(GetMonsterList(_noDebuff), _monster => _monster.GetHealth(), false, 0, false);

    public List<Monster> GetMonstersInRange(Vector3 _center, float _range)
        => GetInRange(monsters, _center, _range);

    private List<Monster> GetMonsterList(bool _noDebuff)
    {
        if (!_noDebuff)
            return monsters;

        List<Monster> list = new List<Monster>();
        for (int i = 0; i < monsters.Count; i++)
        {
            Monster monster = monsters[i];
            if (!monster.HasDebuff())
                list.Add(monster);
        }
        if (list.Count == 0) return monsters;

        return list;
    }
    #endregion

    #region GET_타워
    public Tower GetTowerRandom()
        => GetRandom(towers);

    public Tower GetTowerFirst()
        => GetByIndex(towers, 0);

    public Tower GetTowerLast()
        => GetByIndex(towers, towers.Count - 1);

    public Tower GetTowerNearest(Vector3 _pos, int _distance = 0)
        => GetByDistance(towers, _pos, true, _distance, mapSlotTilemap);

    public Tower GetTowerFarthest(Vector3 _pos, int _distance = 0)
        => GetByDistance(towers, _pos, false, _distance, mapSlotTilemap);

    public Tower GetTowerLowRank(int _minRank = 0)
        => GetByStat(towers, _tower => _tower.GetRank(), true, _minRank, true);

    public Tower GetTowerHighRank(int _minRank = 0)
        => GetByStat(towers, _tower => _tower.GetRank(), false, _minRank, true);

    public List<Tower> GetTowersInRange(Vector3 _center, float _range)
        => GetInRange(towers, _center, _range);
    #endregion
}
