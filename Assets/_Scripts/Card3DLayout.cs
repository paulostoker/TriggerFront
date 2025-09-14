// _Scripts/Card3DLayout.cs - Versão Definitiva, Completa e Verificada
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

public class Card3DLayout : MonoBehaviour
{
    [Header("Layout GameObjects")]
    public GameObject energyLayout;      // Para Action, Utility, Aura
    public GameObject freelancerLayout;    // Para Operadores
    public GameObject supportLayout;     // Para Skill, Strategy
    public GameObject backLayout;        // Verso da carta

    [Header("Energy Layout (Action/Utility/Aura)")]
    public TextMeshProUGUI energyType;     // Type
    public TextMeshProUGUI energySymbol;   // Symbol  
    public Image energyPortrait;           // Portrait

    [Header("Freelancer Layout")]
    public TextMeshProUGUI freelancerName;           // Name
    public TextMeshProUGUI freelancerHP;             // HP
    public TextMeshProUGUI freelancerWeapon;         // Weapon
    public TextMeshProUGUI freelancerWeaponCost;     // Weapon Cost
    public TextMeshProUGUI freelancerWeaponInfo;     // Weapon Info
    // --- VARIÁVEIS RENOMEADAS AQUI ---
    public TextMeshProUGUI freelancerTechnique;        // Ability -> Technique
    public TextMeshProUGUI freelancerTechniqueCost;    // Ability Cost -> Technique Cost
    public TextMeshProUGUI freelancerTechniqueInfo;    // Ability Info -> Technique Info
    // --- FIM DA MUDANÇA ---
    public TextMeshProUGUI freelancerUltimate;       // Ultimate
    public TextMeshProUGUI freelancerUltimateCost;   // Ultimate Cost
    public TextMeshProUGUI freelancerUltimateInfo;   // Ultimate Info
    public TextMeshProUGUI freelancerFooter;         // Footer
    public Image freelancerPortrait;                 // Portrait
    public TextMeshProUGUI freelancerEnergyText;
    public TextMeshProUGUI bombIndicatorText;

    [Header("Support Layout (Skill/Strategy)")]
    public TextMeshProUGUI supportType;     // Type
    public TextMeshProUGUI supportSymbol;   // Symbol
    public TextMeshProUGUI supportName;     // Name
    public TextMeshProUGUI supportInfo;     // Info
    public Image supportPortrait;           // Portrait

    [Header("Back Layout")]
    public Image backCardArt;
    public TextMeshProUGUI backTitle;

    void Awake()
    {
        // Garante que todos os layouts começam desativados
        HideAllLayouts();
    }

    public void SetupLayout(CardData cardData)
    {
        if (cardData == null) 
        {
            Debug.LogError("Card3DLayout: CardData is null!");
            return;
        }
        
        HideAllLayouts();

        if (cardData is SupportData)
        {
            SetupSupportLayout(cardData);
        }
        else if (cardData.cardType == CardType.Freelancer)
        {
            SetupFreelancerLayout(cardData);
        }
        else if (IsEnergyCard(cardData.cardType))
        {
            SetupEnergyLayout(cardData);
        }

        if (backLayout != null)
        {
            backLayout.SetActive(true);
            SetupBackLayout(cardData);
        }
    }

    private void SetupEnergyLayout(CardData cardData)
    {
        if (energyLayout == null) return;
        energyLayout.SetActive(true);

        if (energyType != null) energyType.text = cardData.cardType.ToString();
        if (energySymbol != null) energySymbol.text = GetSymbolForEnergyType(cardData.cardType);
        
        if (energyPortrait != null)
        {
            if (cardData.portrait != null)
            {
                energyPortrait.sprite = cardData.portrait;
                energyPortrait.gameObject.SetActive(true);
            }
            else
            {
                energyPortrait.gameObject.SetActive(false);
            }
        }
    }

    private void SetupFreelancerLayout(CardData cardData)
    {
        if (freelancerLayout == null) return;
        freelancerLayout.SetActive(true);

        if (freelancerName != null) freelancerName.text = cardData.cardName;
        if (freelancerHP != null) freelancerHP.text = cardData.freelancerHP.ToString();
        if (freelancerWeapon != null) freelancerWeapon.text = cardData.weaponName;
        if (freelancerWeaponCost != null) freelancerWeaponCost.text = cardData.weaponCost;
        if (freelancerWeaponInfo != null) freelancerWeaponInfo.text = cardData.weaponInfo;

        // --- CORREÇÃO APLICADA AQUI ---
        // Agora lê dos campos 'technique...' do CardData para os campos renomeados.
        if (freelancerTechnique != null) freelancerTechnique.text = cardData.techniqueName;
        if (freelancerTechniqueCost != null) freelancerTechniqueCost.text = cardData.techniqueCost;
        if (freelancerTechniqueInfo != null) freelancerTechniqueInfo.text = cardData.techniqueInfo;
        // --- FIM DA CORREÇÃO ---

        if (freelancerUltimate != null) freelancerUltimate.text = cardData.ultimateName;
        if (freelancerUltimateCost != null) freelancerUltimateCost.text = cardData.ultimateCost;
        if (freelancerUltimateInfo != null) freelancerUltimateInfo.text = cardData.ultimateInfo;
        if (freelancerFooter != null) freelancerFooter.text = cardData.footer;

        if (freelancerPortrait != null && cardData.portrait != null)
        {
            freelancerPortrait.sprite = cardData.portrait;
            freelancerPortrait.gameObject.SetActive(true);
        }
    }

    private void SetupSupportLayout(CardData data)
    {
        if (supportLayout == null) return;
        supportLayout.SetActive(true);

        if (data is SupportData supportData)
        {
            if (supportType != null) supportType.text = supportData.cardType.ToString();
            if (supportSymbol != null) supportSymbol.text = supportData.symbol;
            if (supportName != null) supportName.text = supportData.name;
            if (supportInfo != null) supportInfo.text = supportData.supportInfo;
            
            if (supportPortrait != null)
            {
                if (supportData.portrait != null)
                {
                    supportPortrait.sprite = supportData.portrait;
                    supportPortrait.gameObject.SetActive(true);
                }
                else
                {
                    supportPortrait.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            Debug.LogError($"Tentativa de configurar layout de suporte com um tipo de carta incorreto: {data.cardType}");
        }
    }

    private void SetupBackLayout(CardData cardData)
    {
        if (backLayout == null) return;

        if (backTitle != null) backTitle.text = "TRIGGER FRONT";
    }

    private void HideAllLayouts()
    {
        if (energyLayout != null) energyLayout.SetActive(false);
        if (freelancerLayout != null) freelancerLayout.SetActive(false);
        if (supportLayout != null) supportLayout.SetActive(false);
        if (backLayout != null) backLayout.SetActive(false);
    }

    private bool IsEnergyCard(CardType cardType)
    {
        return cardType == CardType.Action || cardType == CardType.Utility || cardType == CardType.Aura;
    }

    private bool IsSupportCard(CardType cardType)
    {
        return cardType == CardType.Skill || cardType == CardType.Strategy;
    }

    private string GetSymbolForEnergyType(CardType cardType)
    {
        return cardType switch
        {
            CardType.Action => "⚡",
            CardType.Utility => "🧰",
            CardType.Aura => "🌀", 
            _ => "?"
        };
    }

    private string GetSymbolForSupportType(CardType cardType)
    {
        return cardType switch
        {
            CardType.Skill => "✨", 
            CardType.Strategy => "📣", 
            _ => "?"
        };
    }
    
    public void UpdateFreelancerEnergyDisplay(int actionCount, int utilityCount, int auraCount)
    {
        if (freelancerEnergyText == null)
        {
            Debug.LogError($"<color=red>[Card3DLayout]</color> FALHA CRÍTICA no objeto {gameObject.name}: A referência 'freelancerEnergyText' está NULA no Inspector! Verifique o prefab.");
            return;
        }

        StringBuilder energyBuilder = new StringBuilder();
        energyBuilder.Append(GenerateEmojiString("⚡", actionCount));
        energyBuilder.Append(GenerateEmojiString("🧰", utilityCount));
        energyBuilder.Append(GenerateEmojiString("🌀", auraCount));
        freelancerEnergyText.text = energyBuilder.ToString();
    }
    
    private string GenerateEmojiString(string emoji, int count)
    {
        if (count <= 0) return "";
        StringBuilder sb = new StringBuilder(emoji.Length * count);
        for (int i = 0; i < count; i++)
        {
            sb.Append(emoji);
        }
        return sb.ToString();
    }
    
    public void FlipCard(bool showFront)
    {
        if (energyLayout != null && energyLayout.activeSelf) energyLayout.SetActive(showFront);
        if (freelancerLayout != null && freelancerLayout.activeSelf) freelancerLayout.SetActive(showFront);
        if (supportLayout != null && supportLayout.activeSelf) supportLayout.SetActive(showFront);

        if (backLayout != null) backLayout.SetActive(!showFront);
    }

    public void ForceLayout(string layoutName)
    {
        HideAllLayouts();
        
        switch (layoutName.ToLower())
        {
            case "energy":
                if (energyLayout != null) energyLayout.SetActive(true);
                break;
            case "freelancer":
                if (freelancerLayout != null) freelancerLayout.SetActive(true);
                break;
            case "support":
                if (supportLayout != null) supportLayout.SetActive(true);
                break;
            case "back":
                if (backLayout != null) backLayout.SetActive(true);
                break;
        }
    }
}