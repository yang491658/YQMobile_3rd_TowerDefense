using UnityEngine;

[CreateAssetMenu(fileName = "TowerColor", menuName = "Towers/Tables/TowerColor", order = 103)]
public class TowerColor : ScriptableObject
{
    [Header("Grades")]
    [SerializeField][InspectorName("일반")] private Color normal = Color.black;
    [SerializeField][InspectorName("희귀")] private Color rare = Color.magenta;
    [SerializeField][InspectorName("서사")] private Color epic = Color.blue;
    [SerializeField][InspectorName("유일")] private Color unique = Color.green;
    [SerializeField][InspectorName("전설")] private Color legend = Color.yellow;
    [SerializeField][InspectorName("신화")] private Color mythic = Color.red;

    #region GET
    public Color GetColor(TowerGrade _grade)
        => _grade switch
        {
            TowerGrade.Normal => normal,
            TowerGrade.Rare => rare,
            TowerGrade.Epic => epic,
            TowerGrade.Unique => unique,
            TowerGrade.Legend => legend,
            TowerGrade.Mythic => mythic,
            _ => Color.white,
        };
    #endregion 
}
