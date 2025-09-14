using UnityEngine;

public class SpawnedEffect : MonoBehaviour
{
    public SpawnableEffectData data;
    public int turnsRemaining;
    public int currentHP;

    private PieceDisplay pieceDisplay;

    void Awake()
    {
        // Pega a referência ao seu próprio PieceDisplay para a barra de vida.
        pieceDisplay = GetComponentInChildren<PieceDisplay>();
    }

    void Start()
    {
        // Inicializa a vida e a UI quando o objeto é criado.
        if (data != null)
        {
            currentHP = data.maxHP;
            if (pieceDisplay != null && data.isDestructible)
            {
                pieceDisplay.SetName(data.name); // O nome do asset (ex: "Muro de Contenção")
                pieceDisplay.UpdateHP(currentHP, data.maxHP);
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (!data.isDestructible) return;

        currentHP -= damage;
        if (pieceDisplay != null)
        {
            pieceDisplay.ShowDamagePopup(damage);
            pieceDisplay.UpdateHP(currentHP, data.maxHP);
        }

        if (currentHP <= 0)
        {
            // O EffectManager cuidará da destruição no final do turno, 
            // mas podemos esconder o objeto imediatamente.
            gameObject.SetActive(false); 
            turnsRemaining = 0; // Marca para ser removido pelo EffectManager.
        }
    }
}