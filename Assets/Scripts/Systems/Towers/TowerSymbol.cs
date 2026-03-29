using System;
using UnityEngine;

[CreateAssetMenu(fileName = "TowerSymbol", menuName = "Towers/Tables/TowerSymbol", order = 104)]
public class TowerSymbol : ScriptableObject
{
    [Header("Role")]
    [SerializeField] private Sprite dealer;
    [SerializeField] private Sprite debuff;
    [SerializeField] private Sprite buff;
    [SerializeField] private Sprite summon;

#if UNITY_EDITOR
    private void OnValidate()
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Symbols");

        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite s = sprites[i];
            string name = s.name;

            if (string.Equals(name, TowerRole.Dealer.ToString(), StringComparison.OrdinalIgnoreCase)) dealer = s;
            else if (string.Equals(name, TowerRole.Debuff.ToString(), StringComparison.OrdinalIgnoreCase)) debuff = s;
            else if (string.Equals(name, TowerRole.Buff.ToString(), StringComparison.OrdinalIgnoreCase)) buff = s;
            else if (string.Equals(name, TowerRole.Summon.ToString(), StringComparison.OrdinalIgnoreCase)) summon = s;
        }
    }
#endif

    #region GET
    public Sprite GetSymbol(TowerRole _role)
        => _role switch
        {
            TowerRole.Dealer => dealer,
            TowerRole.Debuff => debuff,
            TowerRole.Buff => buff,
            TowerRole.Summon => summon,
            _ => null,
        };
    #endregion
}