// _Scripts/Editor/WeaponStatEditor.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class WeaponStatEditor : EditorWindow
{
    private WeaponStatDatabase statDatabase;
    private Vector2 scrollPosition;

    // Adiciona a opção "Weapon Stat Editor" ao menu "Tools" da Unity
    [MenuItem("Tools/Weapon Stat Editor")]
    public static void ShowWindow()
    {
        GetWindow<WeaponStatEditor>("Weapon Stats");
    }

    private void OnGUI()
    {
        GUILayout.Label("Banco de Dados de Status de Armas", EditorStyles.boldLabel);
        
        // Campo para o usuário arrastar o ScriptableObject do banco de dados
        statDatabase = (WeaponStatDatabase)EditorGUILayout.ObjectField("Stat Database", statDatabase, typeof(WeaponStatDatabase), false);

        if (statDatabase == null)
        {
            EditorGUILayout.HelpBox("Por favor, crie e/ou arraste um 'WeaponStatDatabase' asset para o campo acima.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space();

        // Botão para aplicar as mudanças a todos os freelancers
        if (GUILayout.Button("Aplicar para TODOS os Freelancers", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("Confirmar Alteração em Massa",
                "Você tem certeza que deseja sobrescrever os WeaponStats de TODOS os FreelancerData assets no projeto? Esta ação não pode ser desfeita.",
                "Sim, sobrescrever", "Cancelar"))
            {
                ApplyStatsToAllFreelancers();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // Área de scroll para editar os status de cada tipo de arma
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (var profile in statDatabase.weaponStats)
        {
            EditorGUILayout.LabelField(profile.weaponType.ToString(), EditorStyles.boldLabel);
            profile.damage = EditorGUILayout.IntField("Damage", profile.damage);
            profile.range = EditorGUILayout.IntField("Range", profile.range);
            profile.criticalDamage = EditorGUILayout.IntField("Critical Damage", profile.criticalDamage);
            profile.proximityBonus = EditorGUILayout.IntField("Proximity Bonus", profile.proximityBonus);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }

        EditorGUILayout.EndScrollView();

        // Salva as alterações feitas no ScriptableObject do banco de dados
        if (GUI.changed)
        {
            EditorUtility.SetDirty(statDatabase);
        }
    }

    private void ApplyStatsToAllFreelancers()
    {
        // 1. Encontra todos os assets do tipo FreelancerData no projeto
        string[] guids = AssetDatabase.FindAssets("t:FreelancerData");
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            FreelancerData freelancer = AssetDatabase.LoadAssetAtPath<FreelancerData>(path);

            if (freelancer != null)
            {
                // 2. Procura os status correspondentes ao tipo de arma do freelancer
                WeaponStatProfile stats = statDatabase.GetStatsFor(freelancer.weaponType);
                if (stats != null)
                {
                    // 3. Sobrescreve os valores
                    freelancer.weaponStats.damage = stats.damage;
                    freelancer.weaponStats.range = stats.range;
                    freelancer.weaponStats.criticalDamage = stats.criticalDamage;
                    freelancer.weaponStats.proximityBonus = stats.proximityBonus;

                    // 4. Gera automaticamente a string 'weaponInfo'
                    freelancer.weaponInfo = GenerateWeaponInfo(freelancer.weaponType, stats);

                    // 5. Marca o asset como modificado para que a Unity salve a alteração
                    EditorUtility.SetDirty(freelancer);
                    count++;
                }
            }
        }
        
        // Salva todas as alterações de assets
        AssetDatabase.SaveAssets();
        Debug.Log($"<color=green>[WeaponStatEditor]</color> {count} FreelancerData assets foram atualizados com sucesso!");
        EditorUtility.DisplayDialog("Sucesso", $"{count} FreelancerData assets foram atualizados.", "OK");
    }

    // Método auxiliar para gerar a string formatada
    private string GenerateWeaponInfo(WeaponType type, WeaponStatProfile stats)
    {
        string weaponName = System.Enum.GetName(typeof(WeaponType), type);
        // Você pode customizar os nomes completos aqui se desejar
        switch(type)
        {
            case WeaponType.AR: weaponName = "Assault Rifle"; break;
            case WeaponType.MR: weaponName = "Marksman Rifle"; break;
            case WeaponType.SR: weaponName = "Sniper Rifle"; break;
            case WeaponType.SG: weaponName = "Shotgun"; break;
            case WeaponType.SMG: weaponName = "Submachine Gun"; break;
            case WeaponType.LMG: weaponName = "Light Machine Gun"; break;
        }

        return $"{weaponName} 💥{stats.damage} 💠{stats.range} ⚠️{stats.criticalDamage} 💢{stats.proximityBonus}";
    }
}