// Assets/Editor/SupportCardExporter.cs - Versão Final com Lógica Completa
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class SupportCardExporter
{
    private const string BaseSearchPath = "Assets/_Cards";

    [MenuItem("Tools/Export Support Cards to JSON (Separate Files)")]
    public static void ExportSupportCards()
    {
        ExportCardsByType(CardType.Skill);
        ExportCardsByType(CardType.Strategy);
    }

    private static void ExportCardsByType(CardType cardType)
    {
        string fileTypeName = cardType.ToString();
        string path = EditorUtility.SaveFilePanel($"Save {fileTypeName} Cards as JSON", "", $"exported_{fileTypeName.ToLower()}.json", "json");

        if (string.IsNullOrEmpty(path)) return;

        string[] guids = AssetDatabase.FindAssets("t:SupportData", new[] { BaseSearchPath });
        
        List<SupportData> filteredCards = guids
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Select(assetPath => AssetDatabase.LoadAssetAtPath<SupportData>(assetPath))
            .Where(card => card != null && card.cardType == cardType)
            .ToList();

        if (filteredCards.Count == 0)
        {
            EditorUtility.DisplayDialog("Export Warning", $"No '{fileTypeName}' cards were found.", "OK");
            return;
        }
        
        List<SupportCardSchema> cardSchemas = new List<SupportCardSchema>();
        foreach (var cardData in filteredCards)
        {
            /// COMEÇO DAS NOVAS ALTERAÇÕES
            // Mapeia os modificadores e condições para suas versões Schema
            List<EffectModifierSchema> modifierSchemas = cardData.modifiers.Select(m => new EffectModifierSchema
            {
                logic = m.logic.ToString(),
                type = m.type.ToString(),
                value = m.value,
                consumedBy = m.consumedBy.ToString(),
                minRoll = m.minRoll,
                maxRoll = m.maxRoll,
                newResult = m.newResult
            }).ToList();

            List<EffectConditionSchema> conditionSchemas = cardData.conditions.Select(c => new EffectConditionSchema
            {
                type = c.type.ToString(),
                value = c.value,
                passiveIcon = c.passiveIcon,
                requiredState = c.requiredState,
                isPassiveCondition = c.isPassiveCondition
            }).ToList();

            SupportCardSchema schema = new SupportCardSchema
            {
                type = cardData.cardType.ToString(),
                symbol = cardData.symbol,
                name = cardData.cardName,
                info = cardData.supportInfo,
                
                // Adiciona os novos dados de lógica
                duration = cardData.duration,
                customStatusIcon = cardData.customStatusIcon,
                conditions = conditionSchemas,
                modifiers = modifierSchemas
            };
            cardSchemas.Add(schema);
            /// FIM DAS NOVAS ALTERAÇÕES
        }
        
        SupportCardCollection collection = new SupportCardCollection { cards = cardSchemas.ToArray() };
        string jsonString = JsonUtility.ToJson(collection, true);
        string finalJson = ExtractJsonArray(jsonString);

        File.WriteAllText(path, finalJson);
        
        EditorUtility.DisplayDialog("Export Complete", $"{cardSchemas.Count} '{fileTypeName}' cards (with full logic) were exported to:\n{path}", "OK");
        EditorUtility.RevealInFinder(path);
    }
    
    private static string ExtractJsonArray(string jsonWrapper)
    {
        int startIndex = jsonWrapper.IndexOf('[');
        int endIndex = jsonWrapper.LastIndexOf(']');
        if (startIndex == -1 || endIndex == -1) return "[]";
        return jsonWrapper.Substring(startIndex, endIndex - startIndex + 1);
    }
}