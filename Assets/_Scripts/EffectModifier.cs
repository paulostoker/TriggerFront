// _Scripts/Effects/EffectModifier.cs

using System;
using UnityEngine;

[Serializable]
public class EffectModifier
{
    [Tooltip("Ordem de aplicação do efeito (menor para o maior). Padrão: 1 para bônus, 10 para remapeamento.")]
    public int priority = 1;
    public ModifierLogic logic = ModifierLogic.Additive;

    // --- Campos para a lógica 'Additive' e 'ResultMap' ---
    public ModifierType type;
    public int value;
    public ActionType consumedBy = ActionType.Any;
    public int minRoll = 1;
    public int maxRoll = 3;
    public int newResult = 1;

    // --- Campos para a nova lógica 'ApplyEffect' (sem o Header) ---
    [Tooltip("A carta de efeito a ser aplicada no alvo.")]
    public SupportData effectToApply;

    [Tooltip("Define em quem o efeito será aplicado.")]
    public EffectTarget target;

    [Tooltip("Define QUANDO o efeito deve ser aplicado (o gatilho).")]
    public ActionType triggerOn;
    [Tooltip("O tipo de carta a ser buscado no baralho.")]
    public CardType cardTypeToSearch; 
}