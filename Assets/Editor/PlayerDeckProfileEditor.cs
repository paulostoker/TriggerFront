using UnityEngine;
using UnityEditor;

// O atributo [CustomEditor] diz ao Unity para usar esta classe para desenhar
// o Inspector de qualquer objeto do tipo PlayerDeckProfile.
[CustomEditor(typeof(PlayerDeckProfile))]
public class PlayerDeckProfileEditor : Editor
{
    // O método OnInspectorGUI é chamado toda vez que o Inspector é desenhado.
    public override void OnInspectorGUI()
    {
        // 1. Desenha o Inspector padrão
        // Isso garante que todos os seus campos (listas, quantidades, etc.) continuem aparecendo normalmente.
        DrawDefaultInspector();

        // 2. Calcula o número total de cartas
        // Pega uma referência ao objeto PlayerDeckProfile que está sendo inspecionado.
        PlayerDeckProfile profile = (PlayerDeckProfile)target;

        int totalCards = 0;

        // Soma as quantidades de cartas de energia
        totalCards += profile.actionCardQuantity;
        totalCards += profile.utilityCardQuantity;
        totalCards += profile.auraCardQuantity;

        // Itera pela lista de skillCards e soma suas quantidades
        if (profile.skillCards != null)
        {
            foreach (var entry in profile.skillCards)
            {
                if (entry != null)
                {
                    totalCards += entry.quantity;
                }
            }
        }

        // Itera pela lista de strategyCards e soma suas quantidades
        if (profile.strategyCards != null)
        {
            foreach (var entry in profile.strategyCards)
            {
                if (entry != null)
                {
                    totalCards += entry.quantity;
                }
            }
        }
        
        // 3. Exibe o total no final do Inspector
        EditorGUILayout.Space(); // Adiciona um pequeno espaço para separação
        
        // Cria um rótulo (Label) com o texto do total, em negrito para dar destaque.
        EditorGUILayout.LabelField("Contagem Total", $"Total de Cartas no Baralho: {totalCards}", EditorStyles.boldLabel);
    }
}