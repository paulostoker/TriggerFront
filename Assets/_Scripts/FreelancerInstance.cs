// _Scripts/FreelancerInstance.cs
using System.Collections.Generic;
using UnityEngine;

public class FreelancerInstance
{
    
    public FreelancerData BaseData { get; private set; }
    public int CurrentHP { get; set; }
    public bool IsAlive { get; set; }
    public bool IsPlayer1 { get; private set; }
    public bool HasMovedThisTurn { get; set; }
    public bool HasActedThisTurn { get; set; }
    public bool HasUsedSkillThisTurn { get; set; }
    public bool IsInEcoMode { get; set; } 
    public List<CardData> EquippedEnergies { get; private set; }
    public List<ActiveEffect> ActiveEffects { get; private set; }
    public GameObject PieceGameObject { get; set; }
    public GameObject CardGameObject { get; set; }
    public bool isBombCarrier { get; set; }
    public bool IsInOffAngleState { get; set; }
    public int ActionCharges { get; set; }
    public int? StoredDiceResult { get; set; }

    public FreelancerInstance(FreelancerData baseData, bool isPlayer1)
    {
        BaseData = baseData;
        IsPlayer1 = isPlayer1;
        CurrentHP = baseData.HP;
        HasMovedThisTurn = false;
        HasActedThisTurn = false;
        HasUsedSkillThisTurn = false;
        IsAlive = true;
        IsInEcoMode = true;
        EquippedEnergies = new List<CardData>();
        ActiveEffects = new List<ActiveEffect>();
        PieceGameObject = null;
        CardGameObject = null;
        isBombCarrier = false;
        IsInOffAngleState = false;
    }
}