// Assets/Editor/SupportCardImporter.cs - Versão Final com Lógica Completa
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;

public class SupportCardImporter
{
    private const string PortraitsFolderPath = "Assets/_Art/CardPortraits/Support";
    private const string BaseSavePath = "Assets/_Cards";

    [MenuItem("Tools/Import Support Cards From JSON")]
    public static void ImportSupportCards()
    {
        string path = EditorUtility.OpenFilePanel("Select Support Card JSON File", "", "json");
        if (string.IsNullOrEmpty(path)) return;

        string jsonString = File.ReadAllText(path);
        string wrappedJson = "{ \"cards\": " + jsonString + "}";
        SupportCardCollection collection = JsonUtility.FromJson<SupportCardCollection>(wrappedJson);

        int importedCount = 0;
        int updatedCount = 0;

        foreach (var cardSchema in collection.cards)
        {
            CardType cardType = ParseEnum<CardType>(cardSchema.type);
            string subfolder = (cardType == CardType.Skill) ? "Skills" : "Strategies";
            string finalSavePath = Path.Combine(BaseSavePath, subfolder);

            if (!Directory.Exists(finalSavePath))
            {
                Directory.CreateDirectory(finalSavePath);
            }

            string assetPath = $"{finalSavePath}/{cardSchema.name}.asset";
            SupportData cardData = AssetDatabase.LoadAssetAtPath<SupportData>(assetPath);

            // Se a carta não existe, cria uma nova.
            if (cardData == null)
            {
                cardData = ScriptableObject.CreateInstance<SupportData>();
                AssetDatabase.CreateAsset(cardData, assetPath);
                importedCount++;
            }
            else
            {
                updatedCount++;
            }

            // Preenche/Atualiza todos os dados da carta.
            cardData.cardName = cardSchema.name;
            cardData.cardType = cardType;
            cardData.symbol = cardSchema.symbol;
            cardData.supportInfo = cardSchema.info;
            cardData.duration = cardSchema.duration;
            cardData.customStatusIcon = cardSchema.customStatusIcon;

            // Limpa as listas antigas para garantir que não haja dados duplicados.
            cardData.conditions.Clear();
            cardData.modifiers.Clear();

            // Converte e adiciona as novas condições.
            if (cardSchema.conditions != null)
            {
                foreach (var conSchema in cardSchema.conditions)
                {
                    cardData.conditions.Add(new EffectCondition
                    {
                        type = ParseEnum<ConditionType>(conSchema.type),
                        value = conSchema.value,
                        passiveIcon = conSchema.passiveIcon,
                        requiredState = conSchema.requiredState,
                        isPassiveCondition = conSchema.isPassiveCondition
                    });
                }
            }

            // Converte e adiciona os novos modificadores.
            if (cardSchema.modifiers != null)
            {
                foreach (var modSchema in cardSchema.modifiers)
                {
                    cardData.modifiers.Add(new EffectModifier
                    {
                        logic = ParseEnum<ModifierLogic>(modSchema.logic),
                        type = ParseEnum<ModifierType>(modSchema.type),
                        value = modSchema.value,
                        consumedBy = ParseFlagsEnum<ActionType>(modSchema.consumedBy),
                        minRoll = modSchema.minRoll,
                        maxRoll = modSchema.maxRoll,
                        newResult = modSchema.newResult
                    });
                }
            }
            
            // Lógica do portrait continua a mesma.
            string portraitPath = $"{PortraitsFolderPath}/{cardData.cardName}.png";
            cardData.portrait = AssetDatabase.LoadAssetAtPath<Sprite>(portraitPath);
            if (cardData.portrait == null)
            {
                portraitPath = $"{PortraitsFolderPath}/{cardData.cardName}.jpg";
                cardData.portrait = AssetDatabase.LoadAssetAtPath<Sprite>(portraitPath);
            }
            
            // Marca o asset como "sujo" para que o Unity salve as alterações.
            EditorUtility.SetDirty(cardData);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Import Complete", 
            $"{importedCount} new cards were created.\n{updatedCount} existing cards were updated.", "OK");
    }

    // Método auxiliar genérico para converter uma string para qualquer tipo de enum.
    private static T ParseEnum<T>(string value) where T : struct, Enum
    {
        if (Enum.TryParse<T>(value, true, out T result))
        {
            return result;
        }
        Debug.LogWarning($"Falha ao converter '{value}' para o enum do tipo '{typeof(T).Name}'. Usando valor padrão.");
        return default;
    }

    // Método auxiliar para converter uma string para um enum com [Flags].
    private static T ParseFlagsEnum<T>(string value) where T : struct, Enum
    {
        try
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }
        catch (Exception)
        {
            Debug.LogWarning($"Falha ao converter a flag '{value}' para o enum do tipo '{typeof(T).Name}'. Usando valor padrão.");
            return default;
        }
    }
}