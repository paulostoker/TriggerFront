// _Scripts/Effects/ActiveEffect.cs
using UnityEngine;
public class ActiveEffect
{
    public CardData Card { get; }
    public GameObject Source { get; }
    public int TurnsRemaining { get; set; }

public ActiveEffect(CardData card, GameObject source)
    {
        Card = card;
        Source = source;
        TurnsRemaining = (card is SupportData sc) ? sc.duration : 1; 
    }
    
    
}