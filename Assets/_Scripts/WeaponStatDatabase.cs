// _Scripts/Data/WeaponStatDatabase.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Esta classe armazena os status para um único tipo de arma.
[System.Serializable]
public class WeaponStatProfile
{
    public WeaponType weaponType;
    public int damage;
    public int range;
    public int criticalDamage;
    public int proximityBonus;
}

// Este ScriptableObject será o nosso banco de dados central.
[CreateAssetMenu(fileName = "WeaponStatDatabase", menuName = "Game/Weapon Stat Database")]
public class WeaponStatDatabase : ScriptableObject
{
    public List<WeaponStatProfile> weaponStats;

    // Método auxiliar para encontrar facilmente os status de um tipo de arma
    public WeaponStatProfile GetStatsFor(WeaponType type)
    {
        return weaponStats.FirstOrDefault(s => s.weaponType == type);
    }
}