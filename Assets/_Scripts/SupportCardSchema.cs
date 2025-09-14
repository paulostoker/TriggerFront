// _Scripts/Data/SupportCardSchema.cs - Versão Aprimorada com Lógica Completa
using System;
using System.Collections.Generic;

[Serializable]
public class EffectModifierSchema
{
    public string logic;
    public string type;
    public int value;
    public string consumedBy;
    public int minRoll;
    public int maxRoll;
    public int newResult;
}

[Serializable]
public class EffectConditionSchema
{
    public string type;
    public int value;
    public string passiveIcon;
    public bool requiredState;
    public bool isPassiveCondition;
}

[Serializable]
public class SupportCardSchema
{
    // Campos básicos (como antes)
    public string type;
    public string symbol;
    public string name;
    public string info;

    // --- NOVOS CAMPOS PARA A LÓGICA ---
    public int duration;
    public string customStatusIcon;
    public List<EffectConditionSchema> conditions;
    public List<EffectModifierSchema> modifiers;
}

[Serializable]
public class SupportCardCollection
{
    public SupportCardSchema[] cards;
}