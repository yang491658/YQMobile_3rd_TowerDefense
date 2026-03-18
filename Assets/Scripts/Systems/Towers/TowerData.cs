using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "TowerData", menuName = "Towers/Data", order = 101)]
public class TowerData : ScriptableObject
{
    [Header("Base")]
    public Color Color = default;
    public Sprite Image;
    public int ID;
    public string Name;

    [Header("Type")]
    public TowerGrade Grade = TowerGrade.Temp;
    public TowerRole Role = TowerRole.None;

    [Header("Stat")]
    public AttackTarget Target = AttackTarget.First;

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoImage();
        AutoName();
        AutoValue();

        EditorUtility.SetDirty(this);
    }

    private void AutoImage()
    {
    }

    private void AutoName()
    {
    }

    private void AutoValue()
    {
    }
#endif

    public TowerData Clone()
    {
        TowerData clone = CreateInstance<TowerData>();

        clone.ID = this.ID;
        clone.Name = this.Name;
        clone.Image = this.Image;
        clone.Color = this.Color;

        clone.Grade = this.Grade;
        clone.Role = this.Role;

        clone.Target = this.Target;

        return clone;
    }
}
