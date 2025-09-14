// Assets/Editor/EffectModifierDrawer.cs
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(EffectModifier))]
public class EffectModifierDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), property.isExpanded, label);
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            
            var rect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);
            var logicProp = property.FindPropertyRelative("logic");
            
            EditorGUI.PropertyField(rect, logicProp);
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            ModifierLogic selectedLogic = (ModifierLogic)logicProp.enumValueIndex;

            switch (selectedLogic)
            {
                case ModifierLogic.Additive:
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative("type"));
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative("value"));
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative("consumedBy"));
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    break;

                case ModifierLogic.ResultMap:
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative("minRoll"));
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative("maxRoll"));
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative("newResult"));
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative("consumedBy"));
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    break;

                case ModifierLogic.SetDamage:
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative("consumedBy"));
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    break;

                case ModifierLogic.ApplyEffect:
                    EditorGUI.LabelField(rect, "Apply Effect Logic", EditorStyles.boldLabel);
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative("effectToApply"));
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative("target"));
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative("triggerOn"));
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    break;

                // --- NOVO CASE ADICIONADO ---
                case ModifierLogic.SearchDeck:
                    EditorGUI.LabelField(rect, "Search Deck Logic", EditorStyles.boldLabel);
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative("cardTypeToSearch"));
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative("value"));
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    break;
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        float totalHeight = EditorGUIUtility.singleLineHeight; 
        totalHeight += EditorGUIUtility.standardVerticalSpacing;

        var logicProp = property.FindPropertyRelative("logic");
        ModifierLogic selectedLogic = (ModifierLogic)logicProp.enumValueIndex;
        
        totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        switch (selectedLogic)
        {
            case ModifierLogic.Additive:
                totalHeight += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 3;
                break;
            case ModifierLogic.ResultMap:
                totalHeight += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 4;
                break;
            case ModifierLogic.SetDamage:
                totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                break;
            case ModifierLogic.ApplyEffect:
                totalHeight += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 4;
                break;
            // --- NOVO CASE ADICIONADO ---
            case ModifierLogic.SearchDeck:
                // 1 linha para o título, 1 para o tipo, 1 para o valor
                totalHeight += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 3;
                break;
        }

        return totalHeight;
    }
}