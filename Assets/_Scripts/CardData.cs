// _Scripts/CardData.cs - Versão Limpa sem valores padrão
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCard", menuName = "Game/Card")]
public class CardData : ScriptableObject
{
    [Header("Basic Info")]
    public string cardName;
    public CardType cardType;
    
    [Header("Visual")]
    public Sprite portrait;
    public string symbol;
    
    [Header("Energy Cards (Action/Utility/Aura)")]
    [Tooltip("Para cartas de energia - deixe vazio para outras")]
    public string energyDescription;
    
    [Header("Freelancer Cards")]
    [Tooltip("Para cartas de operador - deixe vazio para outras")]
    public int freelancerHP;
    public string weaponName;
    public string weaponCost;
    public int weaponDamage;
    public string weaponInfo;
    
    // --- CAMPOS ANTIGOS DE 'ABILITY' RENOMEADOS PARA 'TECHNIQUE' ---
    public string techniqueName;
    public string techniqueCost;
    public string techniqueInfo;
    // --- FIM DA ALTERAÇÃO ---

    public string ultimateName;
    public string ultimateCost;
    public string ultimateInfo;
    public string footer;
    
    // Método utilitário para verificar categoria
    public bool IsEnergyCard()
    {
        return cardType == CardType.Action || cardType == CardType.Utility || cardType == CardType.Aura;
    }
    
    public bool IsSupportCard()
    {
        return cardType == CardType.Skill || cardType == CardType.Strategy;
    }
    
    public bool IsFreelancerCard()
    {
        return cardType == CardType.Freelancer;
    }
    
    // Método para obter símbolo padrão se não definido
    public string GetDisplaySymbol()
    {
        if (!string.IsNullOrEmpty(symbol)) return symbol;
        
        return cardType switch
        {
            CardType.Action => "A",
            CardType.Utility => "U",
            CardType.Aura => "R",
            CardType.Skill => "S",
            CardType.Strategy => "T",
            CardType.Freelancer => "O",
            _ => "?"
        };
    }
}