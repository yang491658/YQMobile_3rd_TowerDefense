using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "BossData", menuName = "Monster/Boss/Data", order = 201)]
public class BossData : ScriptableObject
{
    [Header("Base")]
    public int ID;
    public string Name;
    public Sprite Image;

    [Header("Stat")]
    [Min(0f)] public float MoveSpeed = 1f;
    [Min(0)] public int TotalHealth;

    [Header("Reward")]
    [Min(0)] public int Exp;
    [Min(0)] public int Gold;

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoName();
        AutoImage();
        AutoValue();

        EditorUtility.SetDirty(this);
    }

    private void AutoName()
    {
        if (Image != null)
        {
        }
        else { Name = "Temp"; }
    }

    private void AutoImage()
    {
    }

    private void AutoValue()
    {
        if (Name == "Temp")
        {
            TotalHealth = ID * 10000;
            Exp = ID * 100;
            Gold = ID * 100;
        }
    }
#endif
}
