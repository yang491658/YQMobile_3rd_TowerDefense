using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EntityManager : MonoBehaviour
{
    public static EntityManager Instance { private set; get; }

    [Header("Data Setting")]
    [SerializeField] private GameObject monsterBase;

    [Header("Entities Settings")]
    [SerializeField] private Transform inGame;
    [SerializeField] private Transform map;
    [SerializeField] private Transform towerTrans;
    [SerializeField] private List<Tower> towers = new List<Tower>();
    [SerializeField] private Transform monsterTrans;
    [SerializeField] private List<Monster> monsters = new List<Monster>();

    [Header("Monster Settings")]
    [SerializeField][Min(0.1f)] private float delay = 3f;
    [SerializeField][Min(0.1f)] private float minDelay = 0.1f;
    private float delayBase;
    private Coroutine spawnRoutine;

    [SerializeField][Min(0.1f)] private float speed = 3f;
    [SerializeField] private Transform[] path;
    [SerializeField] private Vector2 pathMargin = new Vector2(0.88f, 0.68f);
    [SerializeField] private int[] pathNum = { 1, 4, 2, 3, 4, 1, 3, 2, 1, 4 };

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (monsterBase == null)
            monsterBase = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster.prefab");

        List<Transform> list = new List<Transform>();
        Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t.name.StartsWith("Path ("))
                list.Add(t);
        }

        list.Sort((a, b) => GetPathIndex(a.name).CompareTo(GetPathIndex(b.name)));
        path = list.ToArray();
    }

    private int GetPathIndex(string _name)
    {
        int start = _name.IndexOf('(');
        int end = _name.IndexOf(')');
        if (start >= 0 && end > start)
        {
            string number = _name.Substring(start + 1, end - start - 1);
            int index;
            if (int.TryParse(number, out index))
                return index;
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

        SetEntity();
    }

    #region 타워
    #endregion

    #region 몬스터
    public Monster Spawn(Vector3 _pos)
    {
        Monster e = Instantiate(monsterBase, _pos, Quaternion.identity, monsterTrans)
            .GetComponent<Monster>();

        monsters.Add(e);

        return e;
    }

    public void ToggleSpawn(bool _on)
    {
        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);

        if (_on)
            spawnRoutine = StartCoroutine(SpawnCoroutine());
        else
            spawnRoutine = null;
    }

    public IEnumerator SpawnCoroutine()
    {
        Transform[] monsterPath = new Transform[pathNum.Length];
        for (int i = 0; i < pathNum.Length; i++)
        {
            int index = pathNum[i] - 1;
            monsterPath[i] = path[index];
        }

        Rect r = AutoCamera.WorldRect;
        float timer = delay;
        while (true)
        {
            float dt = Time.deltaTime;
            timer += dt;
            delay = Mathf.Max(delay - dt / 100f, minDelay);

            if (timer > delay)
            {
                Monster monster = Spawn(monsterPath[0].position + Vector3.left);

                monster.SetHealth(10 + (GameManager.Instance.GetScore() / 100) * 5);
                monster.SetSpeed(speed);
                monster.SetPath(monsterPath);

                timer = 0f;
                yield return new WaitForSeconds(0.01f);
            }
            yield return null;
        }
    }

    public void Despawn(Monster _enemy)
    {
        if (_enemy == null) return;

        monsters.Remove(_enemy);
        Destroy(_enemy.gameObject);
    }

    public void DespawnAll()
    {
        for (int i = monsters.Count - 1; i >= 0; i--)
            Despawn(monsters[i]);
    }
    #endregion

    #region SET
    public void SetEntity()
    {
        delayBase = delay;

        if (inGame == null) inGame = GameObject.Find("InGame")?.transform;
        if (map == null) map = GameObject.Find("Map")?.transform;
        if (towerTrans == null) towerTrans = GameObject.Find("InGame/Towers")?.transform;
        if (monsterTrans == null) monsterTrans = GameObject.Find("InGame/Monsters")?.transform;

        Rect r = AutoCamera.WorldRect;

        float left = r.xMin * pathMargin.x ;
        float right = r.xMax * pathMargin.x;
        float bottom = r.yMin * pathMargin.y;
        float top = r.yMax * pathMargin.y ;

        path[0].position = new Vector3(left, bottom);
        path[1].position = new Vector3(right, bottom);
        path[2].position = new Vector3(left, top);
        path[3].position = new Vector3(right, top);

        map.localScale = new Vector3(right - left, bottom - top);
    }
    public void ResetDelay() => delay = delayBase;
    #endregion

    #region GET
    #endregion
}
