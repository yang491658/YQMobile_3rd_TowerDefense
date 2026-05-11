using System.Collections.Generic;
using UnityEngine;

public class TowerControl : MonoBehaviour
{
    public static TowerControl Instance { private set; get; }

    [Header("Buff")]
    [SerializeField] private List<int> buffs = new();
    private int index;
    [SerializeField] private float interval = 1.5f;
    private float timer;

#if UNITY_EDITOR
    private void OnValidate()
    {
        buffs.Clear();

        TowerData[] towerDatas = FindAnyObjectByType<DataManager>().GetTowerDatas();

        for (int i = 0; i < towerDatas.Length; i++)
        {
            TowerData data = towerDatas[i];
            if (data.Role != TowerRole.Buff) continue;

            buffs.Add(data.ID);
        }
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
    }

    private void Update()
    {
        UpdateBuff();
    }

    #region 버프 이펙트
    private void UpdateBuff()
    {
        if (buffs.Count == 0) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        if (index >= buffs.Count) index = 0;

        ShowBuff(buffs[index]);

        index = (index + 1) % buffs.Count;
        timer = interval;
    }

    private void ShowBuff(int _id)
    {
        List<Tower> towers = EntityManager.Instance?.GetTowers();

        for (int i = 0; i < towers.Count; i++)
        {
            Tower buffTower = towers[i];
            if (buffTower.ID != _id) continue;

            for (int j = 0; j < towers.Count; j++)
            {
                Tower target = towers[j];
                if (!target.Buff.HasBuff(_id)) continue;

                EntityManager.Instance?.MakeEffect(buffTower, target.transform.position, 1f);
            }

            return;
        }
    }
    #endregion
}
