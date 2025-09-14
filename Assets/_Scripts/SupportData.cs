// _Scripts/Data/SupportData.cs - CORRIGIDO para não ter campos duplicados
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewSupportCard", menuName = "Game/Support Card")]
public class SupportData : CardData

{

    [Header("Support Cards (Skill/Strategy)")]
    [TextArea(3, 10)]
    public string supportInfo;



    [Header("Card Logic")]


    [Tooltip("Lista de condições que a carta precisa para ser ativada.")]
    public List<EffectCondition> conditions;

    [Header("Card Effects")]

    [Tooltip("Lista de modificadores que a carta aplica se as condições forem satisfeitas.")]
    public List<EffectModifier> modifiers;
    [Tooltip("Duração do efeito em turnos. 1 = apenas na ação atual.")]
    public int duration = 1;
    [Tooltip("Se FALSE, este efeito não pode ser aplicado a um alvo que já o possua.")]
    public bool canStack = true;

    [Tooltip("Opcional: Se preenchido, este ícone substituirá a exibição padrão de status (ex: +2 ⚔️). Útil para efeitos únicos como One-Tap.")]
public string customStatusIcon;

    [Header("Complex Effects")]
    [Tooltip("Use apenas para efeitos complexos que não podem ser representados por modificadores simples.")]
    [SerializeReference]
    public List<EffectProperties> complexEffects;



}