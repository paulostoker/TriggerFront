// Assets/Editor/SupportPortraitAssigner.cs - Atualizado com o caminho correto da pasta
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SupportPortraitAssigner
{
    // --- CAMINHO ATUALIZADO AQUI ---
    // Define os caminhos onde o script deve procurar pelos arquivos.
    private const string PortraitsFolderPath = "Assets/_Textures/CardPortraits/Support";
    private const string SupportCardsFolderPath = "Assets/_Cards";

    [MenuItem("Tools/Assign Support Card Portraits")]
    public static void AssignPortraits()
    {
        // Passo 1: Encontrar todos os Sprites na pasta de portraits e colocá-los em um
        // dicionário para busca rápida. A chave do dicionário será o nome do sprite.
        Dictionary<string, Sprite> portraitMap = new Dictionary<string, Sprite>();
        string[] portraitGuids = AssetDatabase.FindAssets("t:Sprite", new[] { PortraitsFolderPath });

        foreach (string guid in portraitGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null && !portraitMap.ContainsKey(sprite.name))
            {
                portraitMap.Add(sprite.name, sprite);
            }
        }

        Debug.Log($"<color=cyan>[Portrait Assigner]</color> Found {portraitMap.Count} unique sprites in '{PortraitsFolderPath}'.");

        // Passo 2: Encontrar todas as cartas de Suporte (Skills e Strategies).
        string[] cardGuids = AssetDatabase.FindAssets("t:SupportData", new[] { SupportCardsFolderPath });
        
        int linkedCount = 0;
        int notFoundCount = 0;

        // Passo 3: Iterar por cada carta de Suporte encontrada.
        foreach (string guid in cardGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SupportData card = AssetDatabase.LoadAssetAtPath<SupportData>(path);

            if (card != null)
            {
                // Tenta encontrar um sprite no nosso dicionário que tenha o mesmo nome da carta.
                if (portraitMap.TryGetValue(card.name, out Sprite matchingSprite))
                {
                    // Se encontrou, atribui o sprite ao campo 'portrait' da carta.
                    card.portrait = matchingSprite;
                    linkedCount++;
                    
                    // Marca o asset como "sujo" para que o Unity saiba que precisa ser salvo.
                    EditorUtility.SetDirty(card);
                }
                else
                {
                    // Se não encontrou, avisa no console.
                    Debug.LogWarning($"[Portrait Assigner] Portrait for card '{card.name}' not found. Make sure a sprite with the exact same name exists in the portraits folder.", card);
                    notFoundCount++;
                }
            }
        }

        // Passo 4: Salva todas as alterações feitas nos assets.
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"<color=lime><b>Process Complete!</b></color> {linkedCount} portraits linked successfully. {notFoundCount} portraits could not be found.");
    }
}