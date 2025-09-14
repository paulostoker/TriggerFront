using UnityEngine;
using System.Collections.Generic;

public enum TechniqueType { Support, Attack, Utility }
public enum TargetingType { Self, Enemy, Ally, Tile, AreaOfEffect, NoTarget }
public enum TechniqueAnimationType { Place, Throw } // <-- ENUM ADICIONADO

[CreateAssetMenu(fileName = "NewTechnique", menuName = "Game/Technique Data")]
public class TechniqueData : ScriptableObject
{
    [Header("Basic Info")]
    public string techniqueName;
    [TextArea(3, 5)]
    public string description;
    public TechniqueType type;

    [Header("Gameplay Cost & Rules")]
    public ActionCost cost;
    public TargetingType targeting;
    public int range = 0;
    public int aoeRadius = 0;
    public bool requiresLineOfSight = true;
    public TechniqueAnimationType animationType = TechniqueAnimationType.Throw; // <-- CAMPO ADICIONADO

    [Header("Logic & Effects")]
    [Tooltip("Condições que devem ser atendidas para ativar esta técnica.")]
    public List<EffectCondition> conditions;

    [Tooltip("Os efeitos que esta técnica aplica nos alvos.")]
    public List<EffectModifier> modifiers;
    
    [Header("Spawnable Effect")]
    [Tooltip("Opcional: O efeito que esta técnica deixa no tabuleiro (ex: fumaça, fogo).")]
    public SpawnableEffectData effectToSpawn;
}