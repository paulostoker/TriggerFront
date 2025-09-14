using UnityEngine;
using UnityEditor;
using TMPro;
using System.Collections.Generic;

// Este script adiciona uma opção no menu do Unity para corrigir o mapeamento Unicode de um Sprite Asset.
public class EmojiUnicodeAssigner
{
    // O nome do item de menu que aparecerá no Unity.
    private const string MENU_NAME = "Tools/Assign Emoji Unicodes";

    // --- NOME DO ARQUIVO ATUALIZADO AQUI ---
    // O nome exato do seu arquivo TMP_SpriteAsset.
    private const string TARGET_ASSET_NAME = "SupportPortraits";

    // O código que roda quando você clica no item de menu.
    [MenuItem(MENU_NAME)]
    private static void AssignUnicodes()
    {
        // Encontra o asset pelo nome, em vez de pela seleção manual.
        string[] guids = AssetDatabase.FindAssets($"{TARGET_ASSET_NAME} t:TMP_SpriteAsset");

        if (guids.Length == 0)
        {
            Debug.LogError($"Não foi possível encontrar o TMP_SpriteAsset chamado '{TARGET_ASSET_NAME}'. Verifique se o nome do seu arquivo está exatamente assim e se ele é um Sprite Asset do TextMeshPro.");
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        TMP_SpriteAsset spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(path);

        if (spriteAsset == null)
        {
            Debug.LogError($"Falha ao carregar o TMP_SpriteAsset do caminho: {path}");
            return;
        }

        // A lista dos seus emojis, na ordem, convertida para seus códigos decimais.
        uint[] unicodeValues = new uint[]
        {
            129495, // 🧗‍♂️
            128694, // 🚶‍♂️
            128007, // 🐇
            127919, // 🎯
            128166, // 💦
            128591, // 🙏
            128208, // 📐
            9875,   // ⚓
            129521, // 🧱
            9201,   // ⏱️
            129508, // 🧤
            129309, // 🤝
            127841, // 🍡
            128682, // 🚪
            127787, // 🌫️
            128049, // 🐱‍👤
            129656, // 🩸
            128065, // 👁️‍🗨️
            129681, // 🪑
            129520, // 🧰
            10160,  // ➰
            11093,  // ⭕
            128081, // 👑
            127890, // 🎒
            128717, // 🛍️
            9757,   // ☝️
            127744, // 🌀
            127950, // 🏎️
            127907, // 🎣
            129337, // 🤹‍♂️
            129470, // 🦾
            128184, // 💸
            128176, // 💰
            129297, // 🤑
            129522, // 🧲
            10024,  // ✨
            129517, // 🧭
            128260, // 🔄
            128148, // 💔
            127939, // 🏃‍♂️
            128205, // 📍
            128683, // 🚫
            127838, // 🍞
            128737, // 🛡️
            127793, // 🌱
            128190, // 💾
            129504, // 🧠
            128123, // 👻
            128722, // 🛒
            129510, // 🧦
            128177, // 💱
            9851,   // ♻️
            128483, // 🗣️
            128108, // 👬
            128221, // 📝
            9940,   // ⛔
            9208,   // ⏸️
            128270, // 🔎
            129374, // 🥞
            8987,   // ⌛
            128163, // 💣
        };

        // Limpa a tabela de caracteres existente (que está com os valores errados).
        spriteAsset.spriteCharacterTable.Clear();
        
        // Pega a lista de glifos (as imagens fatiadas).
        List<TMP_SpriteGlyph> glyphs = spriteAsset.spriteGlyphTable;

        Debug.Log($"Iniciando mapeamento para '{spriteAsset.name}'. Total de glifos: {glyphs.Count}. Total de Unicodes: {unicodeValues.Length}.");

        // Recria a tabela de caracteres, um por um, com os valores corretos.
        for (int i = 0; i < unicodeValues.Length; i++)
        {
            if (i >= glyphs.Count)
            {
                Debug.LogWarning($"A lista de Unicodes é maior que a lista de glifos. Parando no índice {i}.");
                break;
            }

            TMP_SpriteCharacter character = new TMP_SpriteCharacter(unicodeValues[i], glyphs[i]);
            spriteAsset.spriteCharacterTable.Add(character);
        }

        // Marca o asset como "sujo" para forçar o Unity a salvar as alterações.
        EditorUtility.SetDirty(spriteAsset);
        AssetDatabase.SaveAssets();

        Debug.Log("Mapeamento de Unicodes de Emoji concluído com sucesso!");
    }
}