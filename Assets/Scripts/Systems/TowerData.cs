using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "Tower", menuName = "TowerData", order = 1)]
public class TowerData : ScriptableObject
{
    [Header("Default")]
    public int ID;
    public string Name;
    public Sprite Image;
    public Color Color = Color.black;

    [Header("Battle")]
    public int AttackDamage = 1;
    public float AttackSpeed = 3;

#if UNITY_EDITOR
    private void OnValidate()
    {
        var sprites = Resources.LoadAll<Sprite>("Images/Towers");
        var used = new HashSet<string>();
        var excluded = new HashSet<string> { "OutLine", "Symbol" };
        foreach (var g in AssetDatabase.FindAssets("t:TowerData"))
        {
            var d = AssetDatabase.LoadAssetAtPath<TowerData>(AssetDatabase.GUIDToAssetPath(g));
            if (d != null && d != this && d.Image != null && !excluded.Contains(d.Image.name))
                used.Add(d.Image.name);
        }

        Sprite pick = null;
        if (Image == null || used.Contains(Image.name) || excluded.Contains(Image.name))
        {
            foreach (var s in sprites)
            {
                if (used.Contains(s.name)) continue;
                if (excluded.Contains(s.name)) continue;

                pick = s;
                break;
            }

            Image = pick;
        }

        if (Image != null)
        {
            var m = Regex.Match(Image.name, @"^(?<num>\d+)\.");
            ID = m.Success ? int.Parse(m.Groups["num"].Value) : ID;

            string rawName = Image.name;
            Name = Regex.Replace(rawName, @"^\d+\.", "");
        }
        else
        {
            ID = 0;
            Name = null;
        }

        EditorUtility.SetDirty(this);
    }
#endif

    public TowerData Clone()
    {
        TowerData clone = CreateInstance<TowerData>();

        clone.name = this.Name;

        clone.ID = this.ID;
        clone.Name = this.Name;
        clone.Image = this.Image;
        clone.Color = this.Color;

        clone.AttackDamage = this.AttackDamage;
        clone.AttackSpeed = this.AttackSpeed;

        return clone;
    }
}
