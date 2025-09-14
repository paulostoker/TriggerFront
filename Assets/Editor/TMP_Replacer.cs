// Assets/Editor/TMP_Replacer.cs - Translated to English
using UnityEngine;
using UnityEditor;
using TMPro;
using System.Collections.Generic;

public class TMP_Replacer
{
    [MenuItem("Tools/Replace TMP with EmojiFix/Selection")]
    private static void ReplaceInSelection()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "No Object Selected", 
                "Please select one or more GameObjects in the Hierarchy to update.", 
                "OK");
            return;
        }

        int count = 0;
        foreach (GameObject go in selectedObjects)
        {
            List<TextMeshProUGUI> componentsToReplace = new List<TextMeshProUGUI>();
            go.GetComponentsInChildren<TextMeshProUGUI>(true, componentsToReplace);

            foreach (TextMeshProUGUI oldComponent in componentsToReplace)
            {
                // Pula se já for do tipo correto.
                if (oldComponent is TextMeshProEmojiFix) continue;

                GameObject targetGameObject = oldComponent.gameObject;
                
                // Salva todas as propriedades importantes em variáveis.
                string text = oldComponent.text;
                TMP_FontAsset font = oldComponent.font;
                Material fontMaterial = oldComponent.fontSharedMaterial;
                float fontSize = oldComponent.fontSize;
                FontStyles fontStyle = oldComponent.fontStyle;
                Color color = oldComponent.color;
                bool raycastTarget = oldComponent.raycastTarget;
                bool richText = oldComponent.richText;
                TextAlignmentOptions alignment = oldComponent.alignment;
                TextWrappingModes wrappingMode = oldComponent.textWrappingMode;

                Undo.RecordObject(targetGameObject, "Replace TMP with EmojiFix");

                // 1. Primeiro, destrói o componente antigo.
                Undo.DestroyObjectImmediate(oldComponent);

                // 2. Adiciona o novo componente.
                var newComponent = targetGameObject.AddComponent<TextMeshProEmojiFix>();

                // 3. Aplica todas as propriedades salvas ao novo componente.
                newComponent.text = text;
                newComponent.font = font;
                newComponent.fontSharedMaterial = fontMaterial;
                newComponent.fontSize = fontSize;
                newComponent.fontStyle = fontStyle;
                newComponent.color = color;
                newComponent.raycastTarget = raycastTarget;
                newComponent.richText = richText;
                newComponent.alignment = alignment;
                newComponent.textWrappingMode = wrappingMode;
                
                count++;
            }
        }
        
        Debug.Log($"Replacement complete! {count} TextMeshProUGUI components were updated to TextMeshProEmojiFix.");
    }

    [MenuItem("Tools/Replace TMP with EmojiFix/All Prefabs")]
    private static void ReplaceInAllPrefabs()
    {
        if (!EditorUtility.DisplayDialog("Warning!", 
            "This will modify ALL prefabs in your project. It is highly recommended to make a backup before proceeding.\n\nDo you wish to continue?", 
            "Yes, modify all prefabs", "Cancel"))
        {
            return;
        }

        string[] allPrefabGuids = AssetDatabase.FindAssets("t:Prefab");
        int count = 0;

        foreach (string guid in allPrefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            List<TextMeshProUGUI> componentsToReplace = new List<TextMeshProUGUI>();
            prefab.GetComponentsInChildren<TextMeshProUGUI>(true, componentsToReplace);
            
            bool modified = false;
            foreach (TextMeshProUGUI oldComponent in componentsToReplace)
            {
                // Pula se já for do tipo correto.
                if (oldComponent is TextMeshProEmojiFix) continue;

                GameObject targetGameObject = oldComponent.gameObject;
                
                // Salva propriedades.
                string text = oldComponent.text;
                TMP_FontAsset font = oldComponent.font;
                Material fontMaterial = oldComponent.fontSharedMaterial;
                float fontSize = oldComponent.fontSize;
                FontStyles fontStyle = oldComponent.fontStyle;
                Color color = oldComponent.color;
                bool raycastTarget = oldComponent.raycastTarget;
                bool richText = oldComponent.richText;
                TextAlignmentOptions alignment = oldComponent.alignment;
                TextWrappingModes wrappingMode = oldComponent.textWrappingMode;

                // Destrói o antigo (importante para prefabs usar DestroyImmediate com 'true').
                Object.DestroyImmediate(oldComponent, true);

                // Adiciona o novo e restaura as propriedades.
                var newComponent = targetGameObject.AddComponent<TextMeshProEmojiFix>();
                newComponent.text = text;
                newComponent.font = font;
                newComponent.fontSharedMaterial = fontMaterial;
                newComponent.fontSize = fontSize;
                newComponent.fontStyle = fontStyle;
                newComponent.color = color;
                newComponent.raycastTarget = raycastTarget;
                newComponent.richText = richText;
                newComponent.alignment = alignment;
                newComponent.textWrappingMode = wrappingMode;
                
                count++;
                modified = true;
            }

            if (modified)
            {
                // Marca o prefab como "sujo" para garantir que a modificação seja salva.
                EditorUtility.SetDirty(prefab);
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Replacement complete! {count} components in prefabs were updated to TextMeshProEmojiFix.");
    }
}