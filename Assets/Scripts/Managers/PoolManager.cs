using System.Collections.Generic;
using UnityEngine;

public interface IPoolable
{
    void OnSpawnPool();
    void OnDespawnPool();
    void ResetPool();
}

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { private set; get; }

    [System.Serializable]
    private class Policy
    {
        [Min(0)] public int prewarm;
        [Min(0)] public int limit;
        [Min(0)] public int keep;

#if TEST_Manager
        [Space]
        [Min(0)] public int active;
        [Min(0)] public int wait;
        [Min(0)] public int peak;
#endif

        public Policy(int _prewarm, int _limit, int _keep)
        {
            prewarm = _prewarm;
            limit = _limit;
            keep = _keep;

#if TEST_Manager
            active = 0;
            wait = 0;
            peak = 0;
#endif
        }
    }

    [Header("Policy")]
    [SerializeField] private Policy monsterPolicy = new(0, 0, 0);
    [SerializeField] private Policy bulletPolicy = new(0, 0, 0);
    [SerializeField] private Policy summonPolicy = new(0, 0, 0);
    [SerializeField] private Policy effectPolicy = new(0, 0, 0);
    private readonly Dictionary<int, Policy> policy = new();

    [Header("Pooling")]
    private readonly Dictionary<int, int> origin = new();
    private readonly Dictionary<int, Stack<GameObject>> pool = new();
    private readonly Dictionary<int, int> made = new();
    private readonly Dictionary<int, IPoolable[]> hook = new();
    private readonly List<GameObject> pending = new();

    [Header("Parent")]
    [SerializeField] private Transform monsterTrans;
    [SerializeField] private Transform bulletTrans;
    [SerializeField] private Transform summonTrans;
    [SerializeField] private Transform effectTrans;
    private readonly Dictionary<int, Transform> parent = new();

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (monsterTrans == null)
            monsterTrans = transform.Find("Monsters");
        if (bulletTrans == null)
            bulletTrans = transform.Find("Bullets");
        if (summonTrans == null)
            summonTrans = transform.Find("Summons");
        if (effectTrans == null)
            effectTrans = transform.Find("Effects");
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

#if TEST_Manager
    private void Update()
    {
        UpdateStatistics();
    }
#endif

    private void LateUpdate()
    {
        if (pending.Count == 0) return;

        for (int i = pending.Count - 1; i >= 0; i--)
        {
            GameObject obj = pending[i];

            if (obj != null && !obj.activeSelf)
            {
                int id = obj.GetInstanceID();
                obj.transform.SetParent(parent.TryGetValue(id, out var p) ? p : transform, false);
            }

            pending.RemoveAt(i);
        }
    }

    #region 정책
    public void Init(GameObject _prefab)
    {
        Policy p = GetPolicy(_prefab);

        Register(_prefab, p);
        Prewarm(_prefab, p.prewarm);
    }

    private void Register(GameObject _prefab, Policy _policy)
    {
        int key = _prefab.GetInstanceID();

        policy[key] = _policy;
        made[key] = 0;
    }

    private void Prewarm(GameObject _prefab, int _count)
    {
        int key = _prefab.GetInstanceID();

        if (!pool.TryGetValue(key, out var stack))
        {
            stack = new Stack<GameObject>();
            pool.Add(key, stack);
        }

        int need = _count - GetAlive(stack);
        if (need <= 0) return;

        for (int i = 0; i < need; i++)
        {
            GameObject obj = Create(_prefab, key);
            CallDespawn(obj.GetInstanceID());

            stack.Push(obj);
            pending.Add(obj);
        }
    }
    #endregion

    #region 풀링
    private GameObject Create(GameObject _prefab, int _key)
    {
        GameObject obj = Instantiate(_prefab);
        obj.SetActive(false);

        int id = obj.GetInstanceID();

        origin[id] = _key;
        hook[id] = obj.GetComponentsInChildren<IPoolable>(true);
        parent[id] = GetParent(obj);

        return obj;
    }

    public GameObject Rent(GameObject _prefab, Vector3 _pos, Transform _parent = null)
    {
        int key = _prefab.GetInstanceID();

        if (!pool.TryGetValue(key, out var stack))
        {
            stack = new Stack<GameObject>();
            pool.Add(key, stack);
        }

        GameObject obj = null;
        while (stack.Count > 0 && obj == null)
            obj = stack.Pop();

        if (obj == null)
        {
            if (policy.TryGetValue(key, out var pLimit) && pLimit.limit > 0)
            {
                made.TryGetValue(key, out int activeCount);

                if (activeCount + GetAlive(stack) >= pLimit.limit)
                    return null;
            }

            obj = Create(_prefab, key);
        }

        int id = obj.GetInstanceID();

        Transform t = obj.transform;
        Transform p0 = _parent != null
            ? _parent
            : (parent.TryGetValue(id, out var p1) ? p1 : transform);

        t.SetParent(p0, false);
        t.SetPositionAndRotation(_pos, Quaternion.identity);

        made.TryGetValue(key, out int count);
        made[key] = ++count;

        CallSpawn(id);
        obj.SetActive(true);

        return obj;
    }

    public void Release(GameObject _obj)
    {
        int id = _obj.GetInstanceID();

        if (!origin.TryGetValue(id, out int key))
        { Destroy(_obj); return; }

        if (!pool.TryGetValue(key, out var stack))
        {
            stack = new Stack<GameObject>();
            pool.Add(key, stack);
        }

        if (made.TryGetValue(key, out int count) && count > 0)
            made[key] = count - 1;

        CallDespawn(id);

        _obj.SetActive(false);

        if (policy.TryGetValue(key, out var p) && p.keep > 0)
        {
            if (GetAlive(stack) >= p.keep)
            {
                origin.Remove(id);
                hook.Remove(id);
                parent.Remove(id);

                Destroy(_obj);
                return;
            }
        }

        stack.Push(_obj);
        pending.Add(_obj);
    }
    #endregion

    #region 호출
    private void CallSpawn(int _id)
    {
        if (!hook.TryGetValue(_id, out var list)) return;

        for (int i = 0; i < list.Length; i++)
            list[i].OnSpawnPool();
    }

    private void CallDespawn(int _id)
    {
        if (!hook.TryGetValue(_id, out var list)) return;

        for (int i = 0; i < list.Length; i++)
            list[i].OnDespawnPool();
    }
    #endregion

    #region 유틸
    private int GetAlive(Stack<GameObject> _stack)
    {
        int alive = 0;

        foreach (GameObject obj in _stack)
            if (obj != null) alive++;

        return alive;
    }

    private Policy GetPolicy(GameObject _prefab)
    {
        if (_prefab.TryGetComponent(out Monster _)) return monsterPolicy;
        if (_prefab.TryGetComponent(out Bullet _)) return bulletPolicy;
        if (_prefab.TryGetComponent(out Summon _)) return summonPolicy;
        if (_prefab.TryGetComponent(out ViewEffect _)) return effectPolicy;
        if (_prefab.TryGetComponent(out TextEffect _)) return effectPolicy;

        return default;
    }

    private Transform GetParent(GameObject _obj)
    {
        if (_obj.TryGetComponent(out Monster _)) return monsterTrans;
        if (_obj.TryGetComponent(out Bullet _)) return bulletTrans;
        if (_obj.TryGetComponent(out Summon _)) return summonTrans;
        if (_obj.TryGetComponent(out ViewEffect _)) return effectTrans;
        if (_obj.TryGetComponent(out TextEffect _)) return effectTrans;

        return transform;
    }

#if TEST_Manager
    private void UpdateStatistics()
    {
        if (monsterPolicy != null) { monsterPolicy.active = 0; monsterPolicy.wait = 0; }
        if (bulletPolicy != null) { bulletPolicy.active = 0; bulletPolicy.wait = 0; }
        if (summonPolicy != null) { summonPolicy.active = 0; summonPolicy.wait = 0; }
        if (effectPolicy != null) { effectPolicy.active = 0; effectPolicy.wait = 0; }

        foreach (var p in policy.Values)
        {
            if (p == null) continue;
            p.active = 0;
            p.wait = 0;
        }

        foreach (var kv in made)
        {
            if (!policy.TryGetValue(kv.Key, out var p) || p == null)
                continue;

            p.active = Mathf.Max(kv.Value, 0);
            if (p.active > p.peak)
                p.peak = p.active;
        }

        foreach (var kv in pool)
        {
            if (!policy.TryGetValue(kv.Key, out var p) || p == null)
                continue;

            p.wait = GetAlive(kv.Value);
        }

        monsterPolicy.peak = Mathf.Max(monsterPolicy.active, monsterPolicy.peak);
        bulletPolicy.peak = Mathf.Max(bulletPolicy.active, bulletPolicy.peak);
        summonPolicy.peak = Mathf.Max(summonPolicy.active, summonPolicy.peak);
        effectPolicy.peak = Mathf.Max(effectPolicy.active, effectPolicy.peak);
    }
#endif
    #endregion

#if TEST_Manager
    #region 프로퍼티
    public int OtherCount => bulletPolicy.active + summonPolicy.active + effectPolicy.active;
    #endregion
#endif
}
