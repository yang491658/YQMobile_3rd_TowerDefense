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
    private readonly Dictionary<int, Policy> policy = new Dictionary<int, Policy>();

    [Header("Pooling")]
    private readonly Dictionary<int, int> origin = new Dictionary<int, int>();
    private readonly Dictionary<int, Stack<GameObject>> pool = new Dictionary<int, Stack<GameObject>>();
    private readonly Dictionary<int, int> made = new Dictionary<int, int>();
    private readonly Dictionary<int, IPoolable[]> hook = new Dictionary<int, IPoolable[]>();
    private readonly List<GameObject> pending = new List<GameObject>();

    [Header("Parent")]
    private readonly Dictionary<int, Transform> parent = new Dictionary<int, Transform>();

#if UNITY_EDITOR
    private void OnValidate()
    {
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
        for (int i = pending.Count - 1; i >= 0; i--)
        {
            GameObject obj = pending[i];

            if (obj == null || obj.activeSelf)
            { pending.RemoveAt(i); continue; }

            int id = obj.GetInstanceID();
            obj.transform.SetParent(parent.TryGetValue(id, out var p) ? p : transform, false);
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

        int alive = 0;
        foreach (var o in stack)
            if (o != null) alive++;

        int need = _count - alive;
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
            Policy pLimit = null;
            policy.TryGetValue(key, out pLimit);

            if (pLimit != null && pLimit.limit > 0)
            {
                made.TryGetValue(key, out int activeCount);

                int waitCount = 0;
                foreach (var o in stack)
                    if (o != null) waitCount++;

                if (activeCount + waitCount >= pLimit.limit)
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
            int alive = 0;
            foreach (var o in stack) if (o != null) alive++;

            if (alive >= p.keep)
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
    private Policy GetPolicy(GameObject _prefab)
    {
        return default;
    }

    private Transform GetParent(GameObject _obj)
    {
        return transform;
    }

#if TEST_Manager
    private void UpdateStatistics()
    {
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

            int alive = 0;
            foreach (var o in kv.Value)
                if (o != null) alive++;

            p.wait = alive;
        }
    }
#endif
    #endregion
}
