// _Scripts/Effects/EffectProperties.cs - Versão Corrigida
using System;
using UnityEngine;

[Serializable]
public abstract class EffectProperties
{
    public string description;

    public virtual bool CheckCondition(GameObject source, GameObject target)
    {
        return true;
    }

    public virtual void ApplyModifier(GameObject target)
    {
        // A lógica será implementada nas classes filhas
    }
}