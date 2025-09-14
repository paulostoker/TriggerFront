// _Scripts/PlayerDeckProfile.cs - Novo arquivo dedicado para o ScriptableObject
using UnityEngine;
using System.Collections.Generic;

// A classe DeckEntry pode viver aqui junto com o perfil para manter tudo organizado.
/// COMEÇO DO TRECHO A SER REMOVIDO
// A classe DeckEntry não precisa mudar.
[System.Serializable]
public class DeckEntry
{
    public CardData card;
    [Min(1)]
    public int quantity = 1;
}

// PlayerDeckProfile agora é um ScriptableObject, o que o torna um asset independente.
[CreateAssetMenu(fileName = "NewDeckProfile", menuName = "Game/Player Deck Profile")]
public class PlayerDeckProfile : ScriptableObject
{
    [Header("Energy Cards (by Quantity)")]
    public CardData actionCardData;
    [Min(0)]
    public int actionCardQuantity;
    
    public CardData utilityCardData;
    [Min(0)]
    public int utilityCardQuantity;

    public CardData auraCardData;
    [Min(0)]
    public int auraCardQuantity;

    [Header("Support Cards")]
    public List<DeckEntry> skillCards;
    public List<DeckEntry> strategyCards;
}
/// FIM DO TRECHO A SER REMOVIDO