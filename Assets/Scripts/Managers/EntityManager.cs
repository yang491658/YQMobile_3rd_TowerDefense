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

    [Header("Map")]
    [SerializeField] private Transform map;
    [SerializeField] private Transform mapField;
    private Tilemap mapFieldTilemap;
    [SerializeField] private float mapMargin = 1f;
    private readonly List<Vector3Int> fieldCells = new List<Vector3Int>();
    private readonly HashSet<Vector3Int> fieldCellSet = new();
    private Vector3Int entryCell;
    private Vector3Int exitCell;
    [Space]
    [SerializeField] private Color entryColor = Color.green;
    [SerializeField] private Color exitColor = Color.magenta;
    [SerializeField] private Color pathColor = Color.yellow;

    private static readonly Vector3Int[] moveDirs = { Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left };
    private readonly Dictionary<Vector3Int, Vector3Int> pathDic = new();

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

    #region 필드
    private bool HasTower(Vector3Int _cell)
    {
        for (int i = 0; i < towers.Count; i++)
        {
            Tower tower = towers[i];
            if (tower == null) continue;

            if (mapFieldTilemap.WorldToCell(tower.transform.position) == _cell)
                return true;
        }
        return false;
    }

    private bool HasMonster(Vector3Int _cell)
    {
        for (int i = 0; i < monsters.Count; i++)
        {
            Monster monster = monsters[i];
            if (monster == null) continue;

            if (mapFieldTilemap.WorldToCell(monster.transform.position) == _cell)
                return true;
        }

        return false;
    }

    private bool CanMoveMonster(Vector3Int _cell)
        => _cell == entryCell || _cell == exitCell || fieldCellSet.Contains(_cell);

    private bool CanReachExit(Vector3Int _block)
    {
        if (!mapFieldTilemap.HasTile(entryCell) || !mapFieldTilemap.HasTile(exitCell))
            return false;

        HashSet<Vector3Int> towerCells = new();

        for (int i = 0; i < towers.Count; i++)
        {
            Tower tower = towers[i];
            if (tower == null) continue;

            towerCells.Add(mapFieldTilemap.WorldToCell(tower.transform.position));
        }

        towerCells.Add(_block);

        Queue<Vector3Int> queue = new();
        HashSet<Vector3Int> visited = new();

        queue.Enqueue(entryCell);
        visited.Add(entryCell);

        while (queue.Count > 0)
        {
            Vector3Int cell = queue.Dequeue();
            if (cell == exitCell) return true;

            for (int i = 0; i < moveDirs.Length; i++)
            {
                Vector3Int next = cell + moveDirs[i];

                if (!CanMoveMonster(next)) continue;
                if (visited.Contains(next)) continue;
                if (towerCells.Contains(next)) continue;

                visited.Add(next);
                queue.Enqueue(next);
            }
        }

        return false;
    }

    private bool CanPlaceTower(Vector3Int _cell)
    {
        if (!fieldCellSet.Contains(_cell)) return false;
        if (_cell == entryCell || _cell == exitCell) return false;
        if (HasTower(_cell)) return false;
        if (HasMonster(_cell)) return false;

        return CanReachExit(_cell);
    }

    private bool PickRandom(out Vector3Int _cell, bool _forTower = true)
    {
        _cell = default;

        bool found = false;
        int count = 0;

        for (int i = 0; i < fieldCells.Count; i++)
        {
            Vector3Int cell = fieldCells[i];
            bool canUse = _forTower ? CanPlaceTower(cell) : !HasTower(cell);
            if (!canUse) continue;

            count++;
            if (Random.Range(0, count) == 0)
            {
                _cell = cell;
                found = true;
            }
        }

        return found;
    }

    private bool PickNearest(Vector3 _pos, out Vector3Int _cell, bool _forTower = true)
    {
        _cell = default;

        bool found = false;
        float bestDistSqr = 0f;

        Vector2 p2 = new Vector2(_pos.x, _pos.y);

        for (int i = 0; i < fieldCells.Count; i++)
        {
            Vector3Int cell = fieldCells[i];
            bool canUse = _forTower ? CanPlaceTower(cell) : !HasTower(cell);
            if (!canUse) continue;

            Vector3 w = mapFieldTilemap.GetCellCenterWorld(cell);
            Vector2 w2 = new Vector2(w.x, w.y);
            float d = (w2 - p2).sqrMagnitude;

            if (!found || d < bestDistSqr)
            {
                found = true;
                bestDistSqr = d;
                _cell = cell;
            }
        }

        return found;
    }

    private Vector3 SelectField(Vector3? _pos = null, bool _forTower = true)
    {
        if (fieldCells.Count == 0) return Vector3.positiveInfinity;

        float z = _pos.HasValue ? _pos.Value.z : 0f;

        Vector3Int cell;

        if (!_pos.HasValue)
        {
            if (!PickRandom(out cell, _forTower)) return Vector3.positiveInfinity;

            Vector3 r = mapFieldTilemap.GetCellCenterWorld(cell);
            r.z = z;
            return r;
        }

        Vector3 p = _pos.Value;
        Vector3Int originCell = mapFieldTilemap.WorldToCell(p);

        bool canUseOrigin = _forTower ?
            CanPlaceTower(originCell) :
            mapFieldTilemap.HasTile(originCell) &&
            originCell != entryCell && originCell != exitCell &&
            !HasTower(originCell);

        if (canUseOrigin)
        {
            Vector3 r = mapFieldTilemap.GetCellCenterWorld(originCell);
            r.z = z;
            return r;
        }

        if (!PickNearest(p, out cell, _forTower)) return Vector3.positiveInfinity;

        Vector3 result = mapFieldTilemap.GetCellCenterWorld(cell);
        result.z = z;
        return result;
    }
    #endregion

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

        Vector3 pos = SelectField(_pos);
        if (float.IsInfinity(pos.x)) return null;

        Tower tower = Instantiate(towerBase, pos, Quaternion.identity, towerTrans)
            .GetComponent<Tower>();
        tower.SetData(data);
        tower.SetRank(_rank);
        towers.Add(tower);

        SetPath();

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

        towers.Remove(_select);
        towers.Remove(_target);

        Destroy(_select.gameObject);
        Destroy(_target.gameObject);

        return SpawnTower(id, rank + 1, pos, false);
    }

    public void DespawnTower(Tower _tower)
    {
        towers.Remove(_tower);
        Destroy(_tower.gameObject);

        SetPath();
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
    public Monster SpawnMonster(Vector3? _pos = null)
    {
        Vector3 pos = _pos.HasValue ?
            SelectField(_pos, false) :
            mapFieldTilemap.GetCellCenterWorld(entryCell);

        if (float.IsInfinity(pos.x)) return null;

        Monster monster = SpawnPool<Monster>(monsterBase, pos, monsterTrans);
        if (monster == null) return null;

        monster.SetMonster(GameManager.Instance.GetScore() / 50);
        monster.SetMove(mapFieldTilemap.WorldToCell(pos));
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

        if (map == null) map = GameObject.Find("Map")?.transform;
        if (mapField == null) mapField = GameObject.Find("Field")?.transform;
        mapFieldTilemap = mapField.GetComponent<Tilemap>();

        SetMap();
        SetCell();
        SetPath();
    }

    private void SetMap()
    {
        Rect r = AutoCamera.WorldRect;

        float side = r.width * (mapMargin / 100f);
        r = Rect.MinMaxRect(r.xMin + side, r.yMin, r.xMax - side, r.yMax);
        if (r.width <= 0f || r.height <= 0f) return;

        mapFieldTilemap.CompressBounds();

        Bounds localBounds = mapFieldTilemap.localBounds;
        float localW = localBounds.size.x;
        float localH = localBounds.size.y;
        if (localW <= 0f || localH <= 0f) return;

        Vector3 tileLossy = mapFieldTilemap.transform.lossyScale;
        Vector3 mapScale = map.localScale;

        float denomX = localW * Mathf.Abs(tileLossy.x / mapScale.x);
        float denomY = localH * Mathf.Abs(tileLossy.y / mapScale.y);
        if (denomX <= 0f || denomY <= 0f) return;

        float xScale = r.width / denomX;
        float yScale = r.height / denomY;
        float s = Mathf.Min(xScale, yScale);
        if (s <= 0f) return;

        map.localScale = new Vector3(s, s, mapScale.z);

        Vector3 pos = map.position;
        pos.x = r.center.x;
        pos.y = r.center.y;
        map.position = pos;
    }

    private void SetCell()
    {
        fieldCells.Clear();
        fieldCellSet.Clear();

        mapFieldTilemap.CompressBounds();
        BoundsInt bounds = mapFieldTilemap.cellBounds;

        bool hasTile = false;
        int minX = 0, maxX = 0, minY = 0, maxY = 0;

        for (int y = bounds.yMin; y < bounds.yMax; y++)
        {
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (!mapFieldTilemap.HasTile(cell)) continue;

                fieldCells.Add(cell);

                if (!hasTile)
                {
                    hasTile = true;
                    minX = maxX = x;
                    minY = maxY = y;
                    continue;
                }

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        if (!hasTile) return;

        entryCell = new Vector3Int(minX, maxY, 0);
        exitCell = new Vector3Int(maxX, minY, 0);

        for (int i = fieldCells.Count - 1; i >= 0; i--)
        {
            Vector3Int cell = fieldCells[i];
            if (cell == entryCell || cell == exitCell)
            {
                fieldCells.RemoveAt(i);
                continue;
            }

            fieldCellSet.Add(cell);
        }

        TileFlags entryFlags = mapFieldTilemap.GetTileFlags(entryCell);
        mapFieldTilemap.SetTileFlags(entryCell, entryFlags & ~TileFlags.LockColor);
        mapFieldTilemap.SetColor(entryCell, entryColor);

        TileFlags exitFlags = mapFieldTilemap.GetTileFlags(exitCell);
        mapFieldTilemap.SetTileFlags(exitCell, exitFlags & ~TileFlags.LockColor);
        mapFieldTilemap.SetColor(exitCell, exitColor);
    }

    private void SetPath()
    {
        pathDic.Clear();

        if (!mapFieldTilemap.HasTile(entryCell) || !mapFieldTilemap.HasTile(exitCell))
            return;

        HashSet<Vector3Int> towerCells = new();

        for (int i = 0; i < towers.Count; i++)
        {
            Tower tower = towers[i];
            if (tower == null) continue;

            towerCells.Add(mapFieldTilemap.WorldToCell(tower.transform.position));
        }

        Queue<Vector3Int> queue = new();
        HashSet<Vector3Int> visited = new();

        queue.Enqueue(exitCell);
        visited.Add(exitCell);

        while (queue.Count > 0)
        {
            Vector3Int cell = queue.Dequeue();

            for (int i = 0; i < moveDirs.Length; i++)
            {
                Vector3Int next = cell + moveDirs[i];

                if (!CanMoveMonster(next)) continue;
                if (visited.Contains(next)) continue;
                if (towerCells.Contains(next)) continue;

                visited.Add(next);
                pathDic[next] = cell;
                queue.Enqueue(next);
            }
        }

        for (int i = 0; i < fieldCells.Count; i++)
        {
            Vector3Int cell = fieldCells[i];
            TileFlags flags = mapFieldTilemap.GetTileFlags(cell);
            mapFieldTilemap.SetTileFlags(cell, flags & ~TileFlags.LockColor);
            mapFieldTilemap.SetColor(cell, Color.white);
        }

        Vector3Int pathCell = entryCell;

        while (pathDic.TryGetValue(pathCell, out Vector3Int nextCell))
        {
            if (nextCell == exitCell) break;

            TileFlags flags = mapFieldTilemap.GetTileFlags(nextCell);
            mapFieldTilemap.SetTileFlags(nextCell, flags & ~TileFlags.LockColor);
            mapFieldTilemap.SetColor(nextCell, pathColor);

            pathCell = nextCell;
        }
    }
    #endregion

    #region GET_기타
    public bool GetNextCell(Vector3Int _cell, out Vector3Int _next)
    {
        if (pathDic.TryGetValue(_cell, out _next))
            return true;

        _next = default;
        return false;
    }

    public Vector3 GetCellPos(Vector3Int _cell)
    => mapFieldTilemap.GetCellCenterWorld(_cell);
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
        List<T> result = new();
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

        List<int> indices = new();
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

            if (!hasBest || (_low ? value < bestValue : value > bestValue))
            {
                hasBest = true;
                bestValue = value;
                bestEntity = entity;
            }
        }

        return hasBest ? bestEntity : null;
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
