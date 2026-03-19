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

    [Header("InGame")]
    [SerializeField] private Transform inGame;
    [SerializeField] private Transform towerTrans;
    [Space]
    [SerializeField] private List<Tower> towers = new List<Tower>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (towerBase == null)
            towerBase = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tower.prefab");
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

    #region SET
    public void ResetEntity()
    {
        towers.RemoveAll(_tower => _tower == null);
    }

    public void SetEntity()
    {
        if (inGame == null) inGame = GameObject.Find("InGame")?.transform;
        if (towerTrans == null) towerTrans = GameObject.Find("InGame/Towers")?.transform;
    }
    #endregion

    #region GET
    #endregion
}
