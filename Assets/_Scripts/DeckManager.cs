// _Scripts/DeckManager.cs - Versão Limpa
using UnityEngine;
using System.Collections.Generic;



public class DeckManager : MonoBehaviour
{
    // As referências diretas aos perfis foram removidas.
    // O DeckManager agora é um "serviço" que aguarda ordens.

    private List<CardData> player1DrawPile = new List<CardData>();
    private List<CardData> player2DrawPile = new List<CardData>();
    private List<CardData> player1DiscardPile = new List<CardData>();
    private List<CardData> player2DiscardPile = new List<CardData>();

    // O Awake agora fica vazio. A construção é iniciada pelo GameManager.
    void Awake()
    {
    }

    // BuildDecks agora é um método público que recebe os perfis a serem usados.
    public void BuildDecks(PlayerDeckProfile p1Profile, PlayerDeckProfile p2Profile)
    {
        player1DrawPile.Clear();
        player1DiscardPile.Clear(); // <-- ADICIONE ESTA LINHA
        BuildPileFromProfile(player1DrawPile, p1Profile);
        ShuffleDeck(true);
        Debug.Log($"<color=teal>[DeckManager]</color> Player 1 deck built with {player1DrawPile.Count} cards.");

        player2DrawPile.Clear();
        player2DiscardPile.Clear(); // <-- ADICIONE ESTA LINHA
        BuildPileFromProfile(player2DrawPile, p2Profile);
        ShuffleDeck(false);
        Debug.Log($"<color=teal>[DeckManager]</color> Player 2 deck built with {player2DrawPile.Count} cards.");
    }
     public List<CardData> GetDeckForSearch(bool isPlayer1)
    {
        List<CardData> deck = isPlayer1 ? player1DrawPile : player2DrawPile;
        // Retornamos uma cópia para não modificar o baralho original acidentalmente
        return new List<CardData>(deck);
    }

     

    private void BuildPileFromProfile(List<CardData> drawPile, PlayerDeckProfile profile)
    {
        if (profile == null)
        {
            Debug.LogError("[DeckManager] Tentativa de construir baralho com um PlayerDeckProfile NULO. O baralho ficará vazio.");

#if !UNITY_EDITOR
            AutoBuildMonitor.Log("ERRO CRÍTICO em BuildPileFromProfile: O perfil recebido é NULO.");
#endif

            return;
        }

#if !UNITY_EDITOR
        AutoBuildMonitor.Log($"Construindo baralho a partir do perfil '{profile.name}'. Cartas de Skill no perfil: {profile.skillCards.Count}");
#endif

        // ... (o resto do método continua igual)
        if (profile.actionCardData != null && profile.actionCardQuantity > 0)
        {
            for (int i = 0; i < profile.actionCardQuantity; i++) drawPile.Add(profile.actionCardData);
        }
        if (profile.utilityCardData != null && profile.utilityCardQuantity > 0)
        {
            for (int i = 0; i < profile.utilityCardQuantity; i++) drawPile.Add(profile.utilityCardData);
        }
        if (profile.auraCardData != null && profile.auraCardQuantity > 0)
        {
            for (int i = 0; i < profile.auraCardQuantity; i++) drawPile.Add(profile.auraCardData);
        }
        if (profile.skillCards != null)
        {
            foreach (DeckEntry entry in profile.skillCards)
            {
                if (entry.card == null) continue;
                for (int i = 0; i < entry.quantity; i++) drawPile.Add(entry.card);
            }
        }
        if (profile.strategyCards != null)
        {
            foreach (DeckEntry entry in profile.strategyCards)
            {
                if (entry.card == null) continue;
                for (int i = 0; i < entry.quantity; i++) drawPile.Add(entry.card);
            }
        }
    }
     public void RemoveFromDrawPile(CardData card, bool isPlayer1)
    {
        List<CardData> drawPile = isPlayer1 ? player1DrawPile : player2DrawPile;
        if (drawPile.Contains(card))
        {
            drawPile.Remove(card);
        }
    }
    public void ClearDrawPile(bool isPlayer1)
    {
        List<CardData> drawPile = isPlayer1 ? player1DrawPile : player2DrawPile;
        drawPile.Clear();

        if (GameConfig.Instance.enableCardLogs)
        {
            Debug.Log($"<color=teal>[DeckManager]</color> O baralho de compra do {(isPlayer1 ? "Player 1" : "Player 2")} foi esvaziado temporariamente para a busca.");
        }
    }

    public void ShuffleDeck(bool isPlayer1)
    {
        List<CardData> deckToShuffle = isPlayer1 ? player1DrawPile : player2DrawPile;
        System.Random rng = new System.Random();
        int n = deckToShuffle.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            CardData value = deckToShuffle[k];
            deckToShuffle[k] = deckToShuffle[n];
            deckToShuffle[n] = value;
        }
    }

    public CardData DrawCard(bool isPlayer1)
    {
        List<CardData> drawPile = isPlayer1 ? player1DrawPile : player2DrawPile;
        if (drawPile.Count == 0)
        {
            Debug.LogWarning($"[DeckManager] O baralho de compra de {(isPlayer1 ? "Player 1" : "Player 2")} está vazio!");
            return null;
        }

        CardData drawnCard = drawPile[drawPile.Count - 1];
        drawPile.RemoveAt(drawPile.Count - 1);
        return drawnCard;
    }
    public void AddToDiscard(CardData card, bool isPlayer1)
    {
        if (card == null) return;

        List<CardData> discardPile = isPlayer1 ? player1DiscardPile : player2DiscardPile;
        discardPile.Add(card);

        // Log para depuração
        if (GameConfig.Instance.enableCardLogs)
        {
            Debug.Log($"<color=teal>[DeckManager]</color> Carta '{card.cardName}' adicionada ao descarte do {(isPlayer1 ? "Player 1" : "Player 2")}. Total: {discardPile.Count}");
        }
    }
}