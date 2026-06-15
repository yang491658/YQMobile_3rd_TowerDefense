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
    [SerializeField] private GameObject bossBase;
    [SerializeField] private GameObject bulletBase;
    [SerializeField] private GameObject summonBase;
    [SerializeField] private GameObject viewBase;
    [SerializeField] private GameObject textBase;

    [Header("InGame")]
    [SerializeField] private Transform inGame;
    [SerializeField] private Transform towerTrans;
    [SerializeField] private Transform monsterTrans;
    [SerializeField] private Transform otherTrans;
    [SerializeField] private RectTransform effectTrans;
    [Space]
    [SerializeField] private List<Tower> towers = new();
    [SerializeField] private List<Monster> monsters = new();

    [Header("Map")]
    [SerializeField] private Transform map;
    [SerializeField] private Transform mapField;
    private Tilemap mapFieldTilemap;
    [SerializeField] private Transform mapRoad;
    private Tilemap mapRoadTilemap;
    private readonly List<Vector3Int> fields = new();
    private readonly Dictionary<Tower, Vector3Int> towerDic = new();

    [Header("Monster / Path")]
    [SerializeField] private Transform[] path;
    [SerializeField] private int[] pathNum = { 0, 1, 2, 7, 8, 4, 3, 6, 7, 2, 1, 5, 6, 3, 2, 7, 8, 9 };
    private Transform[] monsterPath;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (towerBase == null)
            towerBase = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tower.prefab");
        if (monsterBase == null)
            monsterBase = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster.prefab");
        if (bossBase == null)
            bossBase = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Boss.prefab");
        if (bulletBase == null)
            bulletBase = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bullet.prefab");
        if (summonBase == null)
            summonBase = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Summon.prefab");
        if (viewBase == null)
            viewBase = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ViewEffect.prefab");
        if (textBase == null)
            textBase = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TextEffect.prefab");
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
    }

    private void Start()
    {
        PoolManager.Instance?.Init(monsterBase);
        PoolManager.Instance?.Init(bossBase);
        PoolManager.Instance?.Init(bulletBase);
        PoolManager.Instance?.Init(summonBase);
        PoolManager.Instance?.Init(viewBase);
        PoolManager.Instance?.Init(textBase);
    }

    #region 필드
    private bool PickRandom(out Vector3Int _cell)
    {
        _cell = default;
        int count = 0;

        for (int i = 0; i < fields.Count; i++)
        {
            Vector3Int cell = fields[i];
            if (towerDic.ContainsValue(cell)) continue;

            count++;
            if (Random.Range(0, count) == 0)
                _cell = cell;
        }

        return count > 0;
    }

    public Vector3 SelectField(Vector3? _pos = null)
    {
        if (_pos.HasValue)
        {
            Vector3Int cell = GetCell(_pos.Value);
            if (fields.Contains(cell) && !towerDic.ContainsValue(cell))
                return GetCellPos(cell);

            return Vector3.positiveInfinity;
        }

        return PickRandom(out Vector3Int randomCell)
            ? GetCellPos(randomCell)
            : Vector3.positiveInfinity;
    }

    public bool HasEmptyField() => PickRandom(out _);
    #endregion

    #region 타워
    public Tower SpawnTower(int _id, TowerGrade _grade, int _rank = 1, Vector3? _pos = null, bool _useGold = true)
    {
        TowerData data = null;

        if (_id > 0)
        {
            data = DataManager.Instance?.SearchTower(_id);
            if (data == null) return null;

            if (_grade == 0) _grade = data.RandomGrade;
            if (_id != 999 && !data.HasGrade(_grade)) return null;
        }
        else if (_grade > 0)
        {
            TowerData[] datas = DataManager.Instance?.GetTowerDatas(_grade);
            data = datas[Random.Range(0, datas.Length)];
        }
        else data = DataManager.Instance?.GetRandomTower(out _grade);

        if (data == null) return null;

        Vector3 pos = SelectField(_pos);
        if (float.IsInfinity(pos.x)) return null;

        if (_useGold && !GameManager.Instance.UseGold()) return null;

        Tower tower = Instantiate(towerBase, pos, Quaternion.identity, towerTrans)
            .GetComponent<Tower>();

        tower.SetTower(data, _grade, _rank);
        tower.transform.localScale = map.localScale;
        towers.Add(tower);
        towerDic[tower] = GetCell(pos);

        return tower;
    }

    public bool CanMerge(Tower _select, Tower _target)
        => _select != null && _target != null && _select != _target &&
        _select.ID == _target.ID &&
        _select.Grade == _target.Grade &&
        _select.Rank == _target.Rank &&
        !_select.IsMax && !_target.IsMax;

    public Tower MergeTower(Tower _select, Tower _target)
    {
        int id = _target.ID;
        TowerGrade grade = _target.Grade;
        int rank = _target.Rank;
        Vector3 pos = _target.transform.position;

        _select.Despawn();
        _target.Despawn();

        return SpawnTower(id, grade, rank + 1, pos, false);
    }

    public void SellTower(Tower _tower)
    {
        GameManager.Instance?.GoldUp(GameManager.Instance.GetSellGold(_tower));
        UIManager.Instance?.UpdateStore(false);
        UIManager.Instance?.UpdateDrag(null);

        _tower.Despawn();
    }

    public void DespawnTower(Tower _tower)
    {
        towers.Remove(_tower);
        towerDic.Remove(_tower);
        Destroy(_tower.gameObject);
    }
    #endregion

    #region 몬스터
    public void ToggleSpawn(bool _on)
    {
        if (_on)
        {
            if (!MonsterWave.Instance.IsRunning)
                MonsterWave.Instance?.StartWave();
            else
                MonsterWave.Instance?.PauseWave(false);
        }
        else MonsterWave.Instance?.PauseWave(true);
    }

    public Monster SpawnMonster()
    {
        Vector3 pos = monsterPath[0].position;

        if (float.IsInfinity(pos.x)) return null;

        Monster monster = SpawnPool<Monster>(monsterBase, pos, monsterTrans);
        if (monster == null) return null;

        monster.SetPath(monsterPath);
        monster.transform.localScale = Vector3.Scale(monster.transform.localScale, map.localScale);
        monsters.Add(monster);

        return monster;
    }

    public Boss SpawnBoss()
    {
        Vector3 pos = monsterPath[0].position;

        Boss boss = SpawnPool<Boss>(bossBase, pos, monsterTrans);
        if (boss == null) return null;

        boss.SetPath(monsterPath);
        boss.transform.localScale = Vector3.Scale(boss.transform.localScale, map.localScale);
        monsters.Add(boss);

        return boss;
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

    public Summon MakeSummon(TowerSkill _skill, Tower _tower, Vector3 _pos, float _scale, float _speed)
    {
        Summon summon = SpawnPool<Summon>(summonBase, _pos, otherTrans);
        if (summon == null) return null;

        summon.SetSummon(_skill, _tower, _scale, _speed);
        return summon;
    }

    public ViewEffect MakeEffect(Tower _tower, Vector3 _pos, float _scale, float _duration = 0.7f)
    {
        ViewEffect effect = SpawnPool<ViewEffect>(viewBase, _pos, otherTrans);
        if (effect == null) return null;

        effect.SetEffect(_tower, _scale, _duration);
        return effect;
    }

    public ViewEffect MakeEffect(Tower _tower, Monster _target, float _duration = 0.7f)
    {
        if (_target == null || !_target.gameObject.activeInHierarchy) return null;

        Transform parent = _target.transform;

        ViewEffect effect = SpawnPool<ViewEffect>(viewBase, parent.position, parent);
        if (effect == null) return null;

        effect.SetEffect(_tower, 0.7f, _duration);
        effect.SR.sortingLayerID = _target.SR.sortingLayerID;
        effect.SR.sortingOrder = _target.SR.sortingOrder + 1;

        return effect;
    }

    public TextEffect MakeText(Vector3 _pos = default)
    {
        TextEffect effect = SpawnPool<TextEffect>(textBase, _pos, effectTrans);
        if (effect == null) return null;

        effect.SetPosition(WorldToCanvas(_pos));
        return effect;
    }

    private Vector2 WorldToCanvas(Vector3 _worldPos)
    {
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, _worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(effectTrans, screenPos, null, out Vector2 uiPos);
        return uiPos;
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
        towers.Clear();
        monsters.Clear();
        towerDic.Clear();

        TowerStore.Instance?.ResetStore();
        MonsterWave.Instance?.StopWave();

        SetEntity();
        ToggleSpawn(true);
    }

    public void SetEntity()
    {
        if (inGame == null) inGame = GameObject.Find("InGame")?.transform;
        if (towerTrans == null) towerTrans = GameObject.Find("InGame/Towers")?.transform;
        if (monsterTrans == null) monsterTrans = GameObject.Find("InGame/Monsters")?.transform;
        if (otherTrans == null) otherTrans = GameObject.Find("InGame/Others")?.transform;
        if (effectTrans == null) effectTrans = GameObject.Find("InGame/Effects")?.GetComponent<RectTransform>();

        if (map == null) map = GameObject.Find("Map")?.transform;
        if (mapField == null) mapField = GameObject.Find("Field")?.transform;
        mapFieldTilemap = mapField.GetComponent<Tilemap>();
        if (mapRoad == null) mapRoad = GameObject.Find("Road")?.transform;
        mapRoadTilemap = mapRoad.GetComponent<Tilemap>();

        Transform pathTrans = map.Find("Paths");
        path = new Transform[pathTrans.childCount];

        for (int i = 0; i < path.Length; i++)
            path[i] = pathTrans.GetChild(i);

        System.Array.Sort(path, (_a, _b) => string.Compare(_a.name, _b.name, System.StringComparison.Ordinal));

        SetMap();
        SetPath();
    }

    private void SetMap()
    {
        fields.Clear();

        mapFieldTilemap.CompressBounds();
        BoundsInt fieldBounds = mapFieldTilemap.cellBounds;

        for (int y = fieldBounds.yMin; y < fieldBounds.yMax; y++)
        {
            for (int x = fieldBounds.xMin; x < fieldBounds.xMax; x++)
            {
                Vector3Int cell = new(x, y, 0);
                if (mapFieldTilemap.HasTile(cell))
                    fields.Add(cell);
            }
        }

        Rect r = UIManager.Instance.GetMapAreaRect(map.position.z);
        if (r.width <= 0f || r.height <= 0f) return;

        mapRoadTilemap.CompressBounds();

        Tilemap tilemap = mapRoadTilemap;
        Bounds localBounds = tilemap.localBounds;
        float localH = localBounds.size.y;
        if (localH <= 0f) return;

        Vector3 tileLossy = tilemap.transform.lossyScale;
        Vector3 mapScale = map.localScale;

        float denomY = localH * Mathf.Abs(tileLossy.y / mapScale.y);
        if (denomY <= 0f) return;

        float yScale = r.height / denomY;
        map.localScale = new Vector3(mapScale.x, yScale, mapScale.z);

        Vector3 worldCenter = tilemap.transform.TransformPoint(localBounds.center);
        Vector2 areaCenter = r.center;
        map.position += new Vector3(0f, areaCenter.y - worldCenter.y, 0f);

        BoundsInt bounds = tilemap.cellBounds;
        Vector3Int cell0 = new(bounds.xMin, bounds.yMin, 0);
        Vector3Int cell1 = new(bounds.xMin + 1, bounds.yMin, 0);
        Vector3 pathCenter = (tilemap.GetCellCenterWorld(cell0) + tilemap.GetCellCenterWorld(cell1)) * 0.5f;

        float xScale = map.localScale.x * (r.xMin - map.position.x) / (pathCenter.x - map.position.x);
        map.localScale = new Vector3(xScale, yScale, mapScale.z);
    }

    private void SetPath()
    {
        Tilemap tilemap = mapRoadTilemap;
        BoundsInt bounds = tilemap.cellBounds;

        int minX = bounds.xMin;
        int maxX = bounds.xMax - 1;
        int minY = bounds.yMin;
        int maxY = bounds.yMax - 1;

        Vector3Int cell0 = new Vector3Int(minX, minY, 0);
        Vector3Int cell1 = new Vector3Int(minX + 1, minY, 0);
        Vector3Int cell2 = new Vector3Int(minX + 2, minY, 0);
        Vector3Int cell3 = new Vector3Int(maxX - 2, minY, 0);
        Vector3Int cell4 = new Vector3Int(maxX - 1, minY, 0);
        Vector3Int cell5 = new Vector3Int(minX + 1, maxY, 0);
        Vector3Int cell6 = new Vector3Int(minX + 2, maxY, 0);
        Vector3Int cell7 = new Vector3Int(maxX - 2, maxY, 0);
        Vector3Int cell8 = new Vector3Int(maxX - 1, maxY, 0);
        Vector3Int cell9 = new Vector3Int(maxX, maxY, 0);

        path[0].position = tilemap.GetCellCenterWorld(cell0);
        path[1].position = tilemap.GetCellCenterWorld(cell1);
        path[2].position = tilemap.GetCellCenterWorld(cell2);
        path[3].position = tilemap.GetCellCenterWorld(cell3);
        path[4].position = tilemap.GetCellCenterWorld(cell4);
        path[5].position = tilemap.GetCellCenterWorld(cell5);
        path[6].position = tilemap.GetCellCenterWorld(cell6);
        path[7].position = tilemap.GetCellCenterWorld(cell7);
        path[8].position = tilemap.GetCellCenterWorld(cell8);
        path[9].position = tilemap.GetCellCenterWorld(cell9);

        monsterPath = new Transform[pathNum.Length];
        for (int i = 0; i < pathNum.Length; i++)
            monsterPath[i] = path[pathNum[i]];
    }
    #endregion

    #region GET_기타
    public Vector3Int GetCell(Vector3 _pos)
        => mapFieldTilemap.WorldToCell(_pos);

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

    private T GetByDistance<T>(List<T> _list, Vector3 _pos, bool _near) where T : Component
    {
        if (_list.Count == 0) return null;

        T result = null;
        float bestDistance = _near ? float.PositiveInfinity : float.NegativeInfinity;

        for (int i = 0; i < _list.Count; i++)
        {
            T entity = _list[i];
            if (entity == null) continue;

            float distance = (entity.transform.position - _pos).sqrMagnitude;

            if (_near && distance >= bestDistance) continue;
            if (!_near && distance <= bestDistance) continue;

            bestDistance = distance;
            result = entity;
        }

        return result;
    }

    private T GetByStat<T>(List<T> _list, System.Func<T, float> _selector, bool _high) where T : class
    {
        if (_list.Count == 0) return null;

        T bestEntity = _list[0];
        float bestValue = _selector(bestEntity);

        for (int i = 1; i < _list.Count; i++)
        {
            T entity = _list[i];
            float value = _selector(entity);

            if (_high ? value > bestValue : value < bestValue)
            {
                bestValue = value;
                bestEntity = entity;
            }
        }

        return bestEntity;
    }

    private List<T> GetInRange<T>(List<T> _list, Vector3 _center, float _range, int _count = 0) where T : Component
    {
        List<T> targets = new(_list.Count);
        int total = _list.Count;
        if (total == 0) return targets;

        float range = _range * _range;

        for (int i = 0; i < total; i++)
        {
            T entity = _list[i];
            if (entity == null) continue;

            if (_range <= 0f || (entity.transform.position - _center).sqrMagnitude <= range)
                targets.Add(entity);
        }

        targets.Sort((_a, _b) =>
        {
            float distanceA = (_a.transform.position - _center).sqrMagnitude;
            float distanceB = (_b.transform.position - _center).sqrMagnitude;
            return distanceA.CompareTo(distanceB);
        });

        if (_count > 0 && targets.Count > _count)
            targets.RemoveRange(_count, targets.Count - _count);

        return targets;
    }
    #endregion

    #region GET_타워
    public List<Tower> GetTowers() => towers;
    public int GetTowerCount(int _id = 0)
    {
        if (_id == 0) return towers.Count;

        int count = 0;
        for (int i = 0; i < towers.Count; i++)
        {
            Tower tower = towers[i];
            if (tower == null) continue;

            if (tower.ID == _id) count++;
        }
        return count;
    }

    public Tower GetTowerRandom() => GetRandom(towers);

    public Tower GetTowerFirst() => GetByIndex(towers, 0);

    public Tower GetTowerLast() => GetByIndex(towers, towers.Count - 1);

    public Tower GetTowerNearest(Vector3 _pos) => GetByDistance(towers, _pos, true);

    public Tower GetTowerFarthest(Vector3 _pos) => GetByDistance(towers, _pos, false);

    public List<Tower> GetTowersInRange(Vector3 _center, float _range, int _count = 0) => GetInRange(towers, _center, _range, _count);
    #endregion

    #region GET_몬스터
    public List<Monster> GetMonsters() => monsters;
    public List<Monster> GetMonsters(System.Predicate<Monster> _filter)
    {
        List<Monster> targets = new(monsters.Count);
        List<Monster> preferred = null;

        for (int i = 0; i < monsters.Count; i++)
        {
            Monster monster = monsters[i];
            if (monster.IsExclude()) continue;

            targets.Add(monster);

            if (_filter != null && _filter(monster))
            {
                preferred ??= new List<Monster>(monsters.Count);
                preferred.Add(monster);
            }
        }

        return preferred ?? targets;
    }
    public int GetMonsterCount() => monsters.Count;

    public Monster GetMonsterRandom(System.Predicate<Monster> _filter = null) => GetRandom(GetMonsters(_filter));

    public Monster GetMonsterByIndex(int _index) => GetByIndex(monsters, _index);

    public Monster GetMonsterFirst(System.Predicate<Monster> _filter = null)
        => GetByStat(GetMonsters(_filter), _monster => _monster.PathProgress, false);

    public Monster GetMonsterLast(System.Predicate<Monster> _filter = null)
        => GetByStat(GetMonsters(_filter), _monster => _monster.PathProgress, true);

    public Monster GetMonsterNearest(Vector3 _pos, System.Predicate<Monster> _filter = null)
        => GetByDistance(GetMonsters(_filter), _pos, true);

    public Monster GetMonsterFarthest(Vector3 _pos, System.Predicate<Monster> _filter = null)
        => GetByDistance(GetMonsters(_filter), _pos, false);

    public Monster GetMonsterHighHealth(System.Predicate<Monster> _filter = null)
        => GetByStat(GetMonsters(_filter), _monster => _monster.Health, true);

    public Monster GetMonsterLowHealth(System.Predicate<Monster> _filter = null)
        => GetByStat(GetMonsters(_filter), _monster => _monster.Health, false);

    public List<Monster> GetMonstersInRange(Vector3 _center, float _range, int _count = 0)
        => GetInRange(monsters, _center, _range, _count);
    #endregion
}
