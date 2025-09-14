// _Scripts/FreelancerData.cs
using UnityEngine;
using System;
using System.Collections.Generic; // Adicionado para suportar List
using static ServiceLocator;

[Serializable]
public struct ActionCost
{
    public int action;
    public int utility;
    public int aura;
}

[Serializable]
public struct WeaponStats
{
    public int damage;
    public int range;
    public int criticalDamage;
    public int proximityBonus;
}

[CreateAssetMenu(fileName = "NewFreelancerData", menuName = "Game/Freelancer Card")]
public class FreelancerData : ScriptableObject
{
    [Header("Basic Info")]
    public new string name;
    public int HP;
    public string opClass;
    public string qualification;
    public int baseMovement;       
    public GameObject piecePrefab; 
    public RuntimeAnimatorController portrait;

    [Header("Weapon")]
    public string weaponName;
    public ActionCost weaponCost;
    public WeaponType weaponType;
    public WeaponStats weaponStats;
    public string weaponInfo;

    // --- CAMPOS ANTIGOS REMOVIDOS ---
    // techniqueName, techniqueCost, techniqueType, techniqueStats, techniqueInfo
    // ultimateName, ultimateCost, ultimateType, ultimateStats, ultimateInfo

    [Header("Techniques & Ultimate")]
    [Tooltip("Lista de habilidades especiais do Freelancer.")]
    public List<TechniqueData> techniques; // <-- SUBSTITUÍDO POR UMA LISTA

    [Tooltip("A habilidade Ultimate do Freelancer.")]
    public TechniqueData ultimate; // <-- SUBSTITUÍDO POR REFERÊNCIA

    [Header("Display")]
    public string footerInfo;
}