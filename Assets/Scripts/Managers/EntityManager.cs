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

    [Header("Data Setting")]
    [SerializeField] private GameObject monsterBase;
    [SerializeField] private GameObject towerBase;
    [SerializeField] private GameObject bulletBase;

    [SerializeField] private TowerData[] towerDatas;
    private readonly Dictionary<int, TowerData> towerDic = new Dictionary<int, TowerData>();

    [Header("Entities Settings")]
    [SerializeField] private Transform inGame;
    [SerializeField] private Transform monsterTrans;
    [SerializeField] private List<Monster> monsters = new List<Monster>();
    [SerializeField] private Transform towerTrans;
    [SerializeField] private List<Tower> towers = new List<Tower>();

    [Header("Entities Settings")]
    [SerializeField] private Transform map;
    [SerializeField] private Transform mapTile;
    [SerializeField] private Transform mapRoad;
    [SerializeField] private Transform mapSell;

    [Header("Monster Settings")]
    [SerializeField][Min(0.1f)] private float delay = 5f;
    [SerializeField][Min(0.1f)] private float minDelay = 0.5f;
    private float delayBase;
    private Coroutine spawnRoutine;

    [SerializeField] private Transform[] path;
    [SerializeField] private Vector2 pathMargin = new Vector2(0.90f, 0.72f);
    [SerializeField] private int[] pathNum = { 1, 4, 2, 3, 4, 1, 3, 2, 1, 4 };

    [Header("Tower Settings")]
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
            if (data != null) tlist.Add(data.Clone());
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

        plist.Sort((a, b) => GetPathIndex(a.name).CompareTo(GetPathIndex(b.name)));
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
            delay = Mathf.Max(delay - dt / 100f, minDelay);

            if (timer > delay)
            {
                Monster monster = SpawnMonster(monsterPath[0].position + Vector3.left);

                monster.SetHealth(5 + GameManager.Instance.GetScore() / 100);
                monster.SetPath(monsterPath);

                timer = 0f;
                yield return new WaitForSeconds(0.01f);
            }
            yield return null;
        }
    }

    public void DespawnMonster(Monster _monster)
    {
        monsters.Remove(_monster);
        Destroy(_monster.gameObject);
    }
    #endregion

    #region 타워
    private Vector3 SelectTile(Vector3? _pos = null)
    {
        Tilemap tilemap = mapTile.GetComponent<Tilemap>();
        BoundsInt bounds = tilemap.cellBounds;

        HashSet<Vector3Int> tiles = new HashSet<Vector3Int>();
        for (int i = 0; i < towers.Count; i++)
        {
            Vector3Int towerCell = tilemap.WorldToCell(towers[i].transform.position);
            tiles.Add(towerCell);
        }

        if (!_pos.HasValue)
        {
            List<Vector3Int> cells = new List<Vector3Int>();
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    if (tilemap.HasTile(cell) && !tiles.Contains(cell))
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

        if (tiles.Contains(nearestCell))
            return default;

        return tilemap.GetCellCenterWorld(nearestCell);
    }

    public bool CanSpawn(Vector3? _pos = null, bool _useGold = true)
    {
        if (_useGold)
            if (GameManager.Instance?.GetGold() < needGold)
                return false;

        return SelectTile(_pos) != default;
    }

    public TowerData SearchTower(int _id) => towerDic.TryGetValue(_id, out var _data) ? _data : null;
    public Tower SpawnTower(int _id = 0, Vector3? _pos = null, bool _useGold = true)
    {
        TowerData data = (_id == 0)
            ? towerDatas[Random.Range(0, towerDatas.Length)]
            : SearchTower(_id);

        if (data == null) return null;

        if (!CanSpawn(_pos, _useGold))
            return null;

        Vector3 pos = SelectTile(_pos);

        Tower tower = Instantiate(towerBase, pos, Quaternion.identity, towerTrans)
            .GetComponent<Tower>();

        tower.SetData(data);
        tower.transform.localScale = map.transform.localScale;
        towers.Add(tower);

        GameManager.Instance?.GoldDown(_useGold ? needGold++ : 0);

        return tower;
    }

    public Tower MergeTower(Tower _select, Tower _target)
    {
        if (_select == _target
            || _select.GetID() != _target.GetID()
            || _select.GetRank() != _target.GetRank()
            || _select.IsMax() || _target.IsMax()) return null;

        int rank = _target.GetRank();
        Vector3 pos = _target.transform.position;

        DespawnTower(_select);
        DespawnTower(_target);

        Tower merge = SpawnTower(0, pos, false);
        merge.SetRank(rank + 1);

        return merge;
    }

    public void SellTower(Tower _tower)
    {
        GameManager.Instance?.GoldUp(_tower.GetRank());
        DespawnTower(_tower);
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

    public void ResetEntity()
    {
        monsters.RemoveAll(m => m == null);
        towers.RemoveAll(t => t == null);

        delay = delayBase;
        needGold = 0;
    }

    public void SetEntity()
    {
        delayBase = delay;

        if (inGame == null) inGame = GameObject.Find("InGame")?.transform;
        if (map == null) map = GameObject.Find("Map")?.transform;
        if (mapTile == null) mapTile = GameObject.Find("Tile")?.transform;
        if (mapRoad == null) mapRoad = GameObject.Find("Road")?.transform;
        if (mapSell == null) mapSell = GameObject.Find("Sell")?.transform;
        if (monsterTrans == null) monsterTrans = GameObject.Find("InGame/Monsters")?.transform;
        if (towerTrans == null) towerTrans = GameObject.Find("InGame/Towers")?.transform;

        SetMap(out float _halfX, out float _halfY);
        SetPath(_halfX, _halfY);
    }

    private void SetMap(out float _halfX, out float _halfY)
    {
        Rect r = AutoCamera.WorldRect;

        _halfX = r.xMax * pathMargin.x;
        _halfY = r.yMax * pathMargin.y;

        Tilemap tilemap = mapRoad.GetComponent<Tilemap>();
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

    #region GET
    public bool IsSell(Vector3 _pos)
    {
        Tilemap tilemap = mapSell.GetComponent<Tilemap>();
        if (tilemap == null) return false;

        Vector3Int cell = tilemap.WorldToCell(_pos);
        return tilemap.HasTile(cell);
    }

    public Monster GetMonster()
    {
        if (monsters.Count == 0) return null;
        return monsters[Random.Range(0, monsters.Count)];
    }
    public Monster GetMonster(int _index)
    {
        if (monsters.Count == 0) return null;
        return monsters[_index];
    }
    public Monster GetMonster(Vector3 _pos )
    {
        if (monsters.Count == 0) return null;

        Monster nearest = monsters[0];
        float minSqr = (nearest.transform.position - _pos).sqrMagnitude;

        for (int i = 1; i < monsters.Count; i++)
        {
            Monster monster = monsters[i];
            Vector3 delta = monster.transform.position - _pos;
            float sqr = delta.sqrMagnitude;
            if (sqr < minSqr)
            {
                minSqr = sqr;
                nearest = monster;
            }
        }

        return nearest;
    }

    public Tower GetTower()
    {
        if (towers.Count == 0) return null;

        return towers[Random.Range(0, towers.Count)];
    }
    public Tower GetTower(int _index)
    {
        if (towers.Count == 0) return null;

        return towers[_index];
    }
    public Tower GetTower(Vector3 _pos)
    {
        if (towers.Count == 0) return null;

        Tower nearest = towers[0];
        float minSqr = (nearest.transform.position - _pos).sqrMagnitude;

        for (int i = 1; i < towers.Count; i++)
        {
            Tower tower = towers[i];
            Vector3 delta = tower.transform.position - _pos;
            float sqr = delta.sqrMagnitude;
            if (sqr < minSqr)
            {
                minSqr = sqr;
                nearest = tower;
            }
        }

        return nearest;
    }
    public List<Tower> GetTowers() => towers;
    public GameObject GetBulletBase() => bulletBase;

    public int GetNeedGold() => needGold;
    #endregion
}
