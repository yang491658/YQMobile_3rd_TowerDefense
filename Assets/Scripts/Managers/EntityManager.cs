using System.Collections;
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
    [SerializeField] private Transform mapOverlay;
    private Tilemap mapOverlayTilemap;
    private readonly List<Vector3Int> fieldCells = new();
    private Vector3Int entryCell;
    private Vector3Int exitCell;
    private Vector3Int? placeCell;
    private Coroutine mapRoutine;

    [Header("Map / Color")]
    [SerializeField] private Color entryColor = Color.green;
    [SerializeField] private Color pathColor = Color.yellow;
    [SerializeField] private Color towerColor = Color.blue;
    [SerializeField] private Color exitColor = Color.magenta;

    private static readonly Vector3Int[] moveDirs = { Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left };
    private readonly Dictionary<Vector3Int, Vector3Int> pathDic = new();
    private readonly Dictionary<Tower, Vector3Int> towerDic = new();

    public bool IsMoving { private set; get; } = false;

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
        SetEntity();

        PoolManager.Instance?.Init(monsterBase);
        PoolManager.Instance?.Init(bossBase);
        PoolManager.Instance?.Init(bulletBase);
        PoolManager.Instance?.Init(summonBase);
        PoolManager.Instance?.Init(viewBase);
        PoolManager.Instance?.Init(textBase);
    }

    private void Update()
    {
        if (TowerStore.Instance.IsPlacing)
            ShowPlaceField(true);
    }

    #region 필드
    private bool HasTower(Vector3Int _cell)
    {
        foreach (Vector3Int cell in towerDic.Values)
            if (cell == _cell) return true;

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

    private bool CanReachExit(Vector3Int _block)
    {
        if (!mapFieldTilemap.HasTile(entryCell)) return false;
        if (!mapFieldTilemap.HasTile(exitCell)) return false;

        HashSet<Vector3Int> towerCells = new();

        for (int i = 0; i < towers.Count; i++)
        {
            Tower tower = towers[i];
            if (tower == null) continue;

            if (towerDic.TryGetValue(tower, out Vector3Int c))
                towerCells.Add(c);
        }

        towerCells.Add(_block);

        if (towerCells.Contains(exitCell)) return false;

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

                if (!mapFieldTilemap.HasTile(next)) continue;
                if (visited.Contains(next)) continue;
                if (towerCells.Contains(next)) continue;

                visited.Add(next);
                queue.Enqueue(next);
            }
        }

        if (!visited.Contains(entryCell)) return false;

        for (int i = 0; i < monsters.Count; i++)
        {
            Monster monster = monsters[i];
            if (monster == null) continue;

            Vector3Int mCell = mapFieldTilemap.WorldToCell(monster.transform.position);
            if (!visited.Contains(mCell)) return false;
        }

        return true;
    }

    private bool CanPlaceTower(Vector3Int _cell)
    {
        if (!mapFieldTilemap.HasTile(_cell)) return false;
        if (_cell == entryCell || _cell == exitCell) return false;
        if (HasTower(_cell)) return false;
        if (HasMonster(_cell)) return false;

        return CanReachExit(_cell);
    }

    public bool CanMoveCell(Vector3Int _cell)
    {
        if (!mapFieldTilemap.HasTile(_cell)) return false;
        if (HasTower(_cell)) return false;

        return true;
    }

    private bool PickRandom(out Vector3Int _cell, bool _forTower = true)
    {
        _cell = default;

        int count = 0;

        for (int i = 0; i < fieldCells.Count; i++)
        {
            Vector3Int cell = fieldCells[i];
            bool canUse = _forTower ? CanPlaceTower(cell) : !HasTower(cell);
            if (!canUse) continue;

            count++;
            if (Random.Range(0, count) == 0)
                _cell = cell;
        }

        return count > 0;
    }

    public Vector3 SelectField(Vector3? _pos = null, bool _forTower = true)
    {
        if (fieldCells.Count == 0) return Vector3.positiveInfinity;

        float z = _pos.HasValue ? _pos.Value.z : 0f;
        Vector3Int cell;

        if (_pos.HasValue)
        {
            cell = mapFieldTilemap.WorldToCell(_pos.Value);

            bool canUse = _forTower ?
                CanPlaceTower(cell) :
                mapFieldTilemap.HasTile(cell) &&
                cell != entryCell &&
                cell != exitCell &&
                !HasTower(cell);

            if (!canUse)
                return Vector3.positiveInfinity;
        }
        else if (!PickRandom(out cell, _forTower))
            return Vector3.positiveInfinity;

        Vector3 result = mapFieldTilemap.GetCellCenterWorld(cell);
        result.z = z;
        return result;
    }

    public bool HasEmptyField() => PickRandom(out _, true);

    public void ShowPlaceField(bool _on, Vector3? _pos = null)
    {
        DrawBlock(_on);
        DrawPlace(_on, _pos);
    }

    private void DrawBlock(bool _on)
    {
        Color blockColor = _on ? Color.red : Color.clear;

        SetTileColor(mapOverlayTilemap, entryCell, blockColor);
        SetTileColor(mapOverlayTilemap, exitCell, blockColor);

        for (int i = 0; i < fieldCells.Count; i++)
        {
            Vector3Int cell = fieldCells[i];
            SetTileColor(mapOverlayTilemap, cell, _on && !CanPlaceTower(cell) ? Color.red : Color.clear);
        }
    }

    private void DrawPlace(bool _on, Vector3? _pos)
    {
        if (placeCell.HasValue)
        {
            Vector3Int prevCell = placeCell.Value;
            Color color = Color.white;

            if (prevCell == entryCell) color = entryColor;
            else if (prevCell == exitCell) color = exitColor;
            else if (HasTower(prevCell)) color = towerColor;
            else
            {
                Vector3Int pathCell = entryCell;

                for (int i = 0; i < fieldCells.Count + 2; i++)
                {
                    if (!pathDic.TryGetValue(pathCell, out Vector3Int nextCell)) break;
                    if (nextCell == exitCell) break;
                    if (nextCell == prevCell) { color = pathColor; break; }
                    pathCell = nextCell;
                }
            }

            SetTileColor(mapFieldTilemap, prevCell, color);
            placeCell = null;
        }

        if (!_on) return;

        Vector3 pos = _pos ?? HandleManager.Instance.ScreenToWorld(Input.mousePosition);
        Vector3Int cell = mapFieldTilemap.WorldToCell(pos);

        if (!CanPlaceTower(cell)) return;

        SetTileColor(mapFieldTilemap, cell, towerColor);
        placeCell = cell;
    }
    #endregion

    #region 맵
    public void ClearMap()
    {
        Pooling[] poolings = inGame.GetComponentsInChildren<Pooling>(true);

        for (int i = 0; i < poolings.Length; i++)
        {
            Pooling pooling = poolings[i];

            if (pooling is Bullet || pooling is Summon || pooling is ViewEffect)
                pooling.Despawn();
        }
    }

    public void MoveMap(Vector3 _target, float _time = 0f)
    {
        if (mapRoutine != null)
        {
            StopCoroutine(mapRoutine);
            mapRoutine = null;
            IsMoving = false;
        }

        ClearMap();

        if (_time <= 0f)
        {
            inGame.position = _target;
            return;
        }

        mapRoutine = StartCoroutine(MapCoroutine(_target, _time));
    }

    private IEnumerator MapCoroutine(Vector3 _target, float _time)
    {
        Vector3 start = inGame.position;
        float timer = _time;
        IsMoving = true;

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(timer / _time);
            inGame.position = Vector3.Lerp(start, _target, t);
            yield return null;
        }

        inGame.position = _target;
        mapRoutine = null;
        IsMoving = false;
    }
    #endregion

    #region 타워
    public Tower SpawnTower(int _id = 0, int _rank = 1, Vector3? _pos = null, bool _useGold = true)
    {
        TowerData data = null;

#if TEST_Manager
        if (!(TestManager.Instance?.Mode != TestMode.None && _id == 0))
#endif
        {
            int id = _id > 0 ? _id : DataManager.Instance.GetRandomTower().ID;
            data = DataManager.Instance?.SearchTower(id);
            if (data == null) return null;
        }

        Vector3 pos = SelectField(_pos);
        if (float.IsInfinity(pos.x)) return null;

        if (_useGold && !GameManager.Instance.UseGold(data)) return null;

        Tower tower = Instantiate(towerBase, pos, Quaternion.identity, towerTrans)
            .GetComponent<Tower>();

        tower.SetTower(data, _rank);
        tower.transform.localScale = map.localScale;
        towers.Add(tower);
        towerDic[tower] = GetCell(pos);

        SetPath();

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

    public void SellTower(Tower _tower)
    {
        GameManager.Instance?.GoldUp(GameManager.Instance.GetSellGold(_tower));
        UIManager.Instance?.UpdateStore(0);
        UIManager.Instance?.UpdateDrag(null);

        DespawnTower(_tower);
        SetPath();
    }

    public void DespawnTower(Tower _tower)
    {
        _tower.ClearSummon();

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

    public Monster SpawnMonster(Vector3? _pos = null)
    {
        Vector3 pos = _pos.HasValue
            ? SelectField(_pos, false)
            : mapFieldTilemap.GetCellCenterWorld(entryCell);

        if (float.IsInfinity(pos.x)) return null;

        Monster monster = SpawnPool<Monster>(monsterBase, pos, monsterTrans);
        if (monster == null) return null;

        monster.SetMonster(GameManager.Instance.GetScore() / 50);
        monster.SetMove(mapFieldTilemap.WorldToCell(pos));
        monster.transform.localScale = map.localScale;
        monsters.Add(monster);

        return monster;
    }

    public Boss SpawnBoss(int _order)
    {
        int id = DataManager.Instance.GetBossID(_order);
        if (id == 0) return null;

        BossData data = DataManager.Instance?.SearchBoss(id);
        if (data == null) return null;

        Vector3 pos = mapFieldTilemap.GetCellCenterWorld(entryCell);

        Boss boss = SpawnPool<Boss>(bossBase, pos, monsterTrans);
        if (boss == null) return null;

        boss.SetBoss(data);
        boss.SetMove(mapFieldTilemap.WorldToCell(pos));
        boss.transform.localScale = map.localScale;
        monsters.Add(boss);

        return boss;
    }

#if TEST_Manager
    public Boss SpawnBoss()
    {
        Vector3 pos = mapFieldTilemap.GetCellCenterWorld(entryCell);

        Boss boss = SpawnPool<Boss>(bossBase, pos, monsterTrans);
        if (boss == null) return null;

        boss.SetBoss(null);
        boss.SetMove(mapFieldTilemap.WorldToCell(pos));
        boss.transform.localScale = map.localScale;
        monsters.Add(boss);

        return boss;
    }
#endif

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

    public Summon MakeSummon(TowerSkill _skill, Tower _tower, Vector3 _pos, float _scale = 1f, float _speed = 0f)
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
        effect.transform.localScale = Vector3.Scale(effect.transform.localScale, map.localScale);
        return effect;
    }

    public ViewEffect MakeEffect(Tower _tower, Monster _target, float _duration = 0.7f)
    {
        if (_target == null || !_target.gameObject.activeInHierarchy) return null;

        Transform parent = _target.transform;

        ViewEffect effect = SpawnPool<ViewEffect>(viewBase, parent.position, parent);
        if (effect == null) return null;

        effect.SetEffect(_tower, 0.7f, _duration);
        effect.GetSR().sortingLayerID = _target.GetSR().sortingLayerID;
        effect.GetSR().sortingOrder = _target.GetSR().sortingOrder + 1;

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

        ClearMap();
        SetPath();
    }

    #region SET
    public void ResetEntity()
    {
        towers.Clear();
        monsters.Clear();
        pathDic.Clear();
        towerDic.Clear();

        if (mapRoutine != null)
        {
            StopCoroutine(mapRoutine);
            mapRoutine = null;
        }
        IsMoving = false;

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
        if (mapOverlay == null) mapOverlay = GameObject.Find("Overlay")?.transform;
        mapOverlayTilemap = mapOverlay.GetComponent<Tilemap>();

        SetMap();
        SetCell();
        SetPath();

        ShowPlaceField(false);
    }

    private void SetMap()
    {
        Rect r = UIManager.Instance.GetMapAreaRect(map.position.z);
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

        Vector3 worldCenter = mapFieldTilemap.transform.TransformPoint(localBounds.center);
        Vector2 areaCenter = r.center;
        Vector3 offset = new Vector3(areaCenter.x - worldCenter.x, areaCenter.y - worldCenter.y, 0f);
        map.position += offset;
    }

    private void SetCell()
    {
        fieldCells.Clear();

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

        fieldCells.Remove(entryCell);
        fieldCells.Remove(exitCell);

        SetTileColor(mapFieldTilemap, entryCell, entryColor);
        SetTileColor(mapFieldTilemap, exitCell, exitColor);
    }

    private void SetPath()
    {
        pathDic.Clear();

        if (!mapFieldTilemap.HasTile(entryCell)) return;
        if (!mapFieldTilemap.HasTile(exitCell)) return;

        HashSet<Vector3Int> towerCells = new();

        for (int i = 0; i < towers.Count; i++)
        {
            Tower tower = towers[i];
            if (tower == null) continue;

            if (towerDic.TryGetValue(tower, out Vector3Int c))
                towerCells.Add(c);
        }

        if (towerCells.Contains(exitCell)) return;

        Dictionary<Vector3Int, Vector3Int> nextExitDic = new();

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

                if (!mapFieldTilemap.HasTile(next)) continue;
                if (visited.Contains(next)) continue;
                if (towerCells.Contains(next)) continue;

                visited.Add(next);
                nextExitDic[next] = cell;
                queue.Enqueue(next);
            }
        }

        HashSet<Vector3Int> pathSet = new();
        pathSet.Add(entryCell);

        Vector3Int pathCell = entryCell;
        int limit = fieldCells.Count + 2;

        for (int i = 0; i < limit; i++)
        {
            if (!nextExitDic.TryGetValue(pathCell, out Vector3Int nextCell))
                break;

            pathSet.Add(nextCell);
            if (nextCell == exitCell) break;

            pathCell = nextCell;
        }

        Dictionary<Vector3Int, Vector3Int> toPathDic = new();

        Queue<Vector3Int> q2 = new();
        HashSet<Vector3Int> v2 = new();

        foreach (Vector3Int cell in pathSet)
        {
            q2.Enqueue(cell);
            v2.Add(cell);
        }

        while (q2.Count > 0)
        {
            Vector3Int cell = q2.Dequeue();

            for (int i = 0; i < moveDirs.Length; i++)
            {
                Vector3Int next = cell + moveDirs[i];

                if (!mapFieldTilemap.HasTile(next)) continue;
                if (v2.Contains(next)) continue;
                if (towerCells.Contains(next)) continue;

                v2.Add(next);
                toPathDic[next] = cell;
                q2.Enqueue(next);
            }
        }

        foreach (KeyValuePair<Vector3Int, Vector3Int> kv in nextExitDic)
        {
            if (pathSet.Contains(kv.Key))
                pathDic[kv.Key] = kv.Value;
        }

        foreach (KeyValuePair<Vector3Int, Vector3Int> kv in toPathDic)
        {
            if (!pathSet.Contains(kv.Key))
                pathDic[kv.Key] = kv.Value;
        }

        for (int i = 0; i < fieldCells.Count; i++)
            SetTileColor(mapFieldTilemap, fieldCells[i], Color.white);

        SetTileColor(mapFieldTilemap, entryCell, entryColor);
        SetTileColor(mapFieldTilemap, exitCell, exitColor);

        Vector3Int lineCell = entryCell;

        while (pathDic.TryGetValue(lineCell, out Vector3Int nextCell))
        {
            if (nextCell == exitCell) break;

            SetTileColor(mapFieldTilemap, nextCell, pathColor);
            lineCell = nextCell;
        }

        foreach (Vector3Int c in towerCells)
            SetTileColor(mapFieldTilemap, c, towerColor);

        for (int i = 0; i < monsters.Count; i++)
        {
            Monster monster = monsters[i];
            if (monster == null) continue;

            monster.SetMove(mapFieldTilemap.WorldToCell(monster.transform.position));
        }
    }

    private void SetTileColor(Tilemap _tilemap, Vector3Int _cell, Color _color)
    {
        TileFlags flags = _tilemap.GetTileFlags(_cell);
        _tilemap.SetTileFlags(_cell, flags & ~TileFlags.LockColor);
        _tilemap.SetColor(_cell, _color);
    }
    #endregion

    #region GET_기타
    public bool GetNextCell(Vector3Int _cell, out Vector3Int _next)
        => pathDic.TryGetValue(_cell, out _next);

    public Vector3Int GetCell(Vector3 _pos)
        => mapFieldTilemap.WorldToCell(_pos);

    public Vector3 GetCellPos(Vector3Int _cell)
        => mapFieldTilemap.GetCellCenterWorld(_cell);

    public Vector3? GetLShapePos()
    {
        if (fieldCells.Count == 0) return null;

        int minX = fieldCells[0].x; int maxX = fieldCells[0].x;
        int minY = fieldCells[0].y; int maxY = fieldCells[0].y;

        for (int i = 1; i < fieldCells.Count; i++)
        {
            Vector3Int cell = fieldCells[i];

            if (cell.x < minX) minX = cell.x;
            if (cell.x > maxX) maxX = cell.x;
            if (cell.y < minY) minY = cell.y;
            if (cell.y > maxY) maxY = cell.y;
        }

        int lineY = maxY - 1;
        int lineX = maxX - 1;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = maxY; y >= minY; y--)
            {
                Vector3Int cell = new(x, y, 0);

                bool horizontal = cell.y == lineY && cell.x >= minX && cell.x <= lineX;
                bool vertical = cell.x == lineX && cell.y >= minY && cell.y <= lineY;

                if (!horizontal && !vertical) continue;
                if (!CanPlaceTower(cell)) continue;

                return GetCellPos(cell);
            }
        }

        return null;
    }

    public Vector3? GetSnakePos()
    {
        if (fieldCells.Count == 0) return null;

        int minX = fieldCells[0].x; int maxX = fieldCells[0].x;
        int minY = fieldCells[0].y; int maxY = fieldCells[0].y;

        for (int i = 1; i < fieldCells.Count; i++)
        {
            Vector3Int cell = fieldCells[i];

            if (cell.x < minX) minX = cell.x;
            if (cell.x > maxX) maxX = cell.x;
            if (cell.y < minY) minY = cell.y;
            if (cell.y > maxY) maxY = cell.y;
        }

        int height = maxY - minY + 1;

        for (int row = 1; row < height; row += 2)
        {
            int y = maxY - row;
            bool rightOpen = (row / 2) % 2 == 0;

            if (rightOpen)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (x == maxX) continue;

                    Vector3Int cell = new(x, y, 0);
                    if (!CanPlaceTower(cell)) continue;

                    return GetCellPos(cell);
                }
            }
            else
            {
                for (int x = maxX; x >= minX; x--)
                {
                    if (x == minX) continue;

                    Vector3Int cell = new(x, y, 0);
                    if (!CanPlaceTower(cell)) continue;

                    return GetCellPos(cell);
                }
            }
        }

        return null;
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

    private T GetByDistance<T>(List<T> _list, Vector3 _pos, bool _near, int _distance = 0) where T : Component
    {
        if (_list.Count == 0) return null;

        Vector3Int centerCell = mapFieldTilemap.WorldToCell(_pos);
        int maxDistance = _distance > 0 ? _distance : int.MaxValue;

        T result = null;
        bool found = false;
        int bestDistance = 0;

        for (int i = 0; i < _list.Count; i++)
        {
            T entity = _list[i];
            if (entity == null) continue;

            Vector3Int entityCell = mapFieldTilemap.WorldToCell(entity.transform.position);
            int distance =
                Mathf.Abs(entityCell.x - centerCell.x) +
                Mathf.Abs(entityCell.y - centerCell.y);

            if (distance > maxDistance) continue;

            if (!found || (_near ? distance < bestDistance : distance > bestDistance))
            {
                found = true;
                bestDistance = distance;
                result = entity;
            }
        }

        return found ? result : null;
    }

    private List<T> GetInRange<T>(List<T> _list, Vector3 _center, int _range, int _count = 0, bool _square = false)
        where T : Component
    {
        List<T> targets = new();
        int total = _list.Count;
        if (total == 0) return targets;

        Vector3Int centerCell = mapFieldTilemap.WorldToCell(_center);

        for (int i = 0; i < total; i++)
        {
            T entity = _list[i];
            if (entity == null) continue;

            Vector3Int entityCell = mapFieldTilemap.WorldToCell(entity.transform.position);
            int dx = Mathf.Abs(entityCell.x - centerCell.x);
            int dy = Mathf.Abs(entityCell.y - centerCell.y);

            if (_square ? dx <= _range && dy <= _range : dx + dy <= _range)
                targets.Add(entity);
        }

        if (_count <= 0 || targets.Count <= _count)
            return targets;

        List<T> result = new(_count);
        for (int i = 0; i < _count; i++)
        {
            int last = targets.Count - 1;
            int index = Random.Range(0, targets.Count);

            result.Add(targets[index]);
            targets[index] = targets[last];
            targets.RemoveAt(last);
        }

        return result;
    }

    private T GetByStat<T>(List<T> _list, System.Func<T, float> _selector, bool _high) where T : class
    {
        if (_list.Count == 0) return null;

        bool hasBest = false;
        float bestValue = 0f;
        T bestEntity = null;

        for (int i = 0; i < _list.Count; i++)
        {
            T entity = _list[i];
            float value = _selector(entity);

            if (!hasBest || (_high ? value > bestValue : value < bestValue))
            {
                hasBest = true;
                bestValue = value;
                bestEntity = entity;
            }
        }

        return bestEntity;
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
            Tower t = towers[i];
            if (t == null) continue;

            if (t.GetID() == _id) count++;
        }
        return count;
    }

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

    public List<Tower> GetTowersInRange(Vector3 _center, int _range = 1, int _count = 0, bool _square = false)
        => GetInRange(towers, _center, _range, _count, _square);
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

    public Monster GetMonsterRandom(System.Predicate<Monster> _filter = null)
        => GetRandom(GetMonsters(_filter));

    public Monster GetMonsterByIndex(int _index)
        => GetByIndex(monsters, _index);

    public Monster GetMonsterFirst(System.Predicate<Monster> _filter = null)
        => GetByStat(GetMonsters(_filter), GetDistanceExit, false);

    public Monster GetMonsterLast(System.Predicate<Monster> _filter = null)
        => GetByStat(GetMonsters(_filter), GetDistanceExit, true);

    public Monster GetMonsterNearest(Vector3 _pos, int _distance = 0, System.Predicate<Monster> _filter = null)
        => GetByDistance(GetMonsters(_filter), _pos, true, _distance);

    public Monster GetMonsterFarthest(Vector3 _pos, int _distance = 0, System.Predicate<Monster> _filter = null)
        => GetByDistance(GetMonsters(_filter), _pos, false, _distance);

    public Monster GetMonsterHighHealth(System.Predicate<Monster> _filter = null)
        => GetByStat(GetMonsters(_filter), _monster => _monster.GetHealth(), true);

    public Monster GetMonsterLowHealth(System.Predicate<Monster> _filter = null)
        => GetByStat(GetMonsters(_filter), _monster => _monster.GetHealth(), false);

    public List<Monster> GetMonstersInRange(Vector3 _center, int _range, int _count = 0, bool _square = false)
        => GetInRange(monsters, _center, _range, _count, _square);

    private float GetDistanceExit(Monster _monster)
    {
        Vector3Int cell = mapFieldTilemap.WorldToCell(_monster.transform.position);
        if (cell == exitCell) return 0f;

        int limit = fieldCells.Count + 2;

        for (int dist = 1; dist <= limit; dist++)
        {
            if (!pathDic.TryGetValue(cell, out Vector3Int next))
                return int.MaxValue;

            if (next == exitCell) return dist;

            cell = next;
        }

        return int.MaxValue;
    }
    #endregion
}
