// _Scripts/PieceDisplay.cs - Versão Final Corrigida
using UnityEngine;
using TMPro;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class PieceDisplay : MonoBehaviour
{
    [Header("Core Info")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;
    private Vector3 damageTextInitialPosition;
    
    /// COMEÇO DA LINHA ADICIONADA
    [Tooltip("O RectTransform do objeto pai que contém o Vertical Layout Group para forçar a atualização.")]
    public RectTransform layoutGroupParent;
    /// FIM DA LINHA ADICIONADA

    [Header("Energy Counters")]
    public TextMeshProUGUI energyText;
    public TextMeshProUGUI statusEffectText;

    [Header("Damage Display")]
    public TextMeshProUGUI damageText;
    [Tooltip("Deslocamento vertical (altura) inicial para o popup de dano.")]
    public float damagePopupYOffset = 0.5f; // Valor padrão, pode ser ajustado no Inspector
        public Color missTextColor = Color.gray;
    public float damageDisplayDuration = 2f;
    public float damageAnimationHeight = 1f;
    public AnimationCurve damageAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("HP Bar (Alternative to Text)")]
    public GameObject hpBarContainer;
    public UnityEngine.UI.Image hpBarBackground;
    public UnityEngine.UI.Image hpBarFill;
    public bool useHPBar = true;

    private const string ACTION_EMOJI = "⚡";
    private const string UTILITY_EMOJI = "🧰";
    private const string AURA_EMOJI = "🌀";

    void Awake()
    {
        // Salva a posição local correta do DamageText (definida no prefab) antes que o jogo comece.
        if (damageText != null)
        {
            damageTextInitialPosition = damageText.transform.localPosition;
        }
    }
    public void SetName(string freelancerName)
    {
        if (nameText != null)
        {
            nameText.text = freelancerName;
        }
    }

    public void UpdateHP(int currentHP, int maxHP)
    {
        if (hpText != null)
        {
            hpText.text = $"{currentHP}/{maxHP}";
            hpText.gameObject.SetActive(true);
        }
        
        if (hpBarFill != null)
        {
            float hpPercentage = maxHP > 0 ? (float)currentHP / maxHP : 0f;
            hpBarFill.fillAmount = hpPercentage;
            
            if (hpPercentage > 0.6f)
                hpBarFill.color = Color.green;
            else if (hpPercentage > 0.3f)
                hpBarFill.color = Color.yellow;
            else
                hpBarFill.color = Color.red;
        }
        
        if (hpBarContainer != null)
            hpBarContainer.SetActive(hpBarFill != null);
    }

    public void UpdateEnergyDisplay(int actionCount, int utilityCount, int auraCount)
    {
        if (energyText != null)
        {
            StringBuilder energyBuilder = new StringBuilder();
            energyBuilder.Append(GenerateEmojiString(ACTION_EMOJI, actionCount));
            energyBuilder.Append(GenerateEmojiString(UTILITY_EMOJI, utilityCount));
            energyBuilder.Append(GenerateEmojiString(AURA_EMOJI, auraCount));

            bool hasContent = energyBuilder.Length > 0;
            energyText.gameObject.SetActive(hasContent);
            if (hasContent)
            {
                energyText.text = energyBuilder.ToString();
            }

            if (layoutGroupParent != null) LayoutRebuilder.MarkLayoutForRebuild(layoutGroupParent);
        }
    }
    
    public void UpdateStatusEffects(FreelancerInstance freelancerInstance)
{
    if (statusEffectText == null) return;
    
    // Condição de saída antecipada se não houver nada para mostrar.
    if (freelancerInstance == null || (freelancerInstance.ActiveEffects.Count == 0 && !freelancerInstance.IsInOffAngleState))
    {
        statusEffectText.gameObject.SetActive(false);
        if (layoutGroupParent != null) LayoutRebuilder.MarkLayoutForRebuild(layoutGroupParent);
        return;
    }

    var aggregatedModifiers = new Dictionary<ModifierType, int>();
    var customIconsToShow = new HashSet<string>();
    var passiveIconsToShow = new HashSet<string>();

    List<ModifierType> offAngleModifierTypes = new List<ModifierType> { 
        ModifierType.HasOffAngleCardActive, 
        ModifierType.AllowWallbang, 
        ModifierType.WallbangDamage 
    };

    // 1. Itera por TODOS os efeitos ativos para agregar bônus e ícones.
    foreach (var activeEffect in freelancerInstance.ActiveEffects)
    {
        if (activeEffect.Card is SupportData supportCard)
        {
            // Verifica se as condições passivas da carta estão ativas.
            bool allPassiveConditionsMet = true;
            EffectCondition firstPassiveCondition = null;

            foreach (var condition in supportCard.conditions)
            {
                if (condition.isPassiveCondition)
                {
                    if (firstPassiveCondition == null) firstPassiveCondition = condition;
                    if (!condition.Check(freelancerInstance.PieceGameObject, null))
                    {
                        allPassiveConditionsMet = false;
                        break; 
                    }
                }
            }
            
            if (allPassiveConditionsMet)
            {
                // Se um ícone customizado existe, ele tem prioridade.
                if (!string.IsNullOrEmpty(supportCard.customStatusIcon))
                {
                    customIconsToShow.Add(supportCard.customStatusIcon);
                }
                else // Senão, soma os modificadores aditivos.
                {
                    foreach (var modifier in supportCard.modifiers.Where(m => m.logic == ModifierLogic.Additive))
                    {
                        if (!aggregatedModifiers.ContainsKey(modifier.type))
                        {
                            aggregatedModifiers[modifier.type] = 0;
                        }
                        aggregatedModifiers[modifier.type] += modifier.value;
                    }
                }
            }
            else if (firstPassiveCondition != null && !string.IsNullOrEmpty(firstPassiveCondition.passiveIcon))
            {
                // Se as condições não foram atendidas, mostra o ícone passivo da condição.
                passiveIconsToShow.Add(firstPassiveCondition.passiveIcon);
            }
        }
    }

    // 2. Lógica ADITIVA específica para o estado Off-Angle.
    if (freelancerInstance.IsInOffAngleState)
    {
        // Se o estado está ATIVO, SOMA os bônus de combate aos que já foram agregados.
        if (!aggregatedModifiers.ContainsKey(ModifierType.AttackDice)) aggregatedModifiers[ModifierType.AttackDice] = 0;
        aggregatedModifiers[ModifierType.AttackDice] += 2;

        if (!aggregatedModifiers.ContainsKey(ModifierType.WeaponRange)) aggregatedModifiers[ModifierType.WeaponRange] = 0;
        aggregatedModifiers[ModifierType.WeaponRange] += 2;
        
        if (!aggregatedModifiers.ContainsKey(ModifierType.DefenseDice)) aggregatedModifiers[ModifierType.DefenseDice] = 0;
        aggregatedModifiers[ModifierType.DefenseDice] -= 3;
    }
    else
    {
        // Se o estado está INATIVO, verifica se a carta está ativa para mostrar o ícone passivo dela.
        var offAngleEffect = freelancerInstance.ActiveEffects.FirstOrDefault(eff => eff.Card is SupportData sd && sd.modifiers.Any(m => m.type == ModifierType.HasOffAngleCardActive));
        if (offAngleEffect != null)
        {
            var offAngleCardData = offAngleEffect.Card as SupportData;
            // Lê o ícone do campo customStatusIcon da carta, como deveria ser.
            if (offAngleCardData != null && !string.IsNullOrEmpty(offAngleCardData.customStatusIcon))
            {
                passiveIconsToShow.Add(offAngleCardData.customStatusIcon);
            }
        }
    }
    
    // 3. Constrói a string final a partir dos dados agregados.
    StringBuilder statusBuilder = new StringBuilder();
    
    foreach (var icon in customIconsToShow)
    {
        if (statusBuilder.Length > 0) statusBuilder.Append("  ");
        statusBuilder.Append(icon);
    }
    foreach (var icon in passiveIconsToShow)
    {
        if (statusBuilder.Length > 0) statusBuilder.Append("  ");
        statusBuilder.Append(icon);
    }
    foreach (var pair in aggregatedModifiers)
    {
        if (pair.Value == 0 || offAngleModifierTypes.Contains(pair.Key)) continue; // Ignora o marcador HasOffAngleCardActive
        string icon = pair.Key switch {
            ModifierType.AttackDice => "⚔️",
            ModifierType.DefenseDice => "🛡️",
            ModifierType.Movement => "💠",
            ModifierType.Damage => "💥",
            ModifierType.WeaponRange => "🎯",
             _ => ""
        };
        if (!string.IsNullOrEmpty(icon))
        {
            if (statusBuilder.Length > 0) statusBuilder.Append(" ");
            statusBuilder.AppendFormat("{0}{1:+#;-#;0}{2}", (pair.Value > 0 ? "<color=green>" : "<color=red>"), pair.Value, icon + "</color>");
        }
    }
    
    bool hasContent = statusBuilder.Length > 0;
    statusEffectText.gameObject.SetActive(hasContent);
    if (hasContent)
    {
        statusEffectText.text = statusBuilder.ToString();
    }

    if (layoutGroupParent != null) LayoutRebuilder.MarkLayoutForRebuild(layoutGroupParent);
}

    private string GenerateEmojiString(string emoji, int count)
    {
        if (count <= 0)
        {
            return "";
        }

        StringBuilder sb = new StringBuilder(emoji.Length * count);
        for (int i = 0; i < count; i++)
        {
            sb.Append(emoji);
        }
        return sb.ToString();
    }
    
    public void ShowDamagePopup(int damageAmount)
    {
        if (damageText != null)
        {
            StopAllCoroutines();
            StartCoroutine(DamagePopupCoroutine(damageAmount));
        }
    }
    
    public void ShowMissPopup()
    {
        if (damageText != null)
        {
            // Interrompe qualquer outra animação de dano que esteja acontecendo
            StopAllCoroutines();
            StartCoroutine(MissPopupCoroutine());
        }
    }

    private IEnumerator MissPopupCoroutine()
    {
        damageText.text = "MISS";
        damageText.color = missTextColor;
        damageText.gameObject.SetActive(true);
        
        Vector3 startPos = new Vector3(0, damagePopupYOffset, 0);
        damageText.transform.localPosition = startPos;
        
        Vector3 endPos = startPos + Vector3.up * damageAnimationHeight;
        
        Vector3 startScale = Vector3.one * 0.5f;
        Vector3 maxScale = Vector3.one * 1f;
        Vector3 endScale = Vector3.one * 0.9f;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < damageDisplayDuration)
        {
            float progress = elapsedTime / damageDisplayDuration;
            float curveValue = damageAnimationCurve.Evaluate(progress);
            
            damageText.transform.localPosition = Vector3.Lerp(startPos, endPos, curveValue);
            
            if (progress < 0.2f)
            {
                float scaleProgress = progress / 0.2f;
                damageText.transform.localScale = Vector3.Lerp(startScale, maxScale, scaleProgress);
            }
            else
            {
                float scaleProgress = (progress - 0.2f) / 0.8f;
                damageText.transform.localScale = Vector3.Lerp(maxScale, endScale, scaleProgress);
            }
            
            if (progress > 0.7f)
            {
                float alphaProgress = (progress - 0.7f) / 0.3f;
                Color currentColor = damageText.color;
                currentColor.a = Mathf.Lerp(1f, 0f, alphaProgress);
                damageText.color = currentColor;
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        damageText.gameObject.SetActive(false);
        
        // Reseta o estado visual para a próxima vez
        damageText.transform.localPosition = startPos;
        damageText.transform.localScale = Vector3.one;
        Color resetColor = damageText.color;
        resetColor.a = 1f;
        damageText.color = resetColor;
    }

   private IEnumerator DamagePopupCoroutine(int damageAmount)
    {
        damageText.text = $"-{damageAmount}";
        damageText.color = Color.red;

        /// COMEÇO DAS NOVAS ALTERAÇÕES
        // Define a posição inicial usando o offset que criamos, em vez da posição do prefab.
        Vector3 startPos = new Vector3(0, damagePopupYOffset, 0);
        damageText.transform.localPosition = startPos;
        /// FIM DAS NOVAS ALTERAÇÕES

        damageText.gameObject.SetActive(true);

        Vector3 endPos = startPos + Vector3.up * damageAnimationHeight;

        Vector3 startScale = Vector3.one * 0.5f;
        Vector3 maxScale = Vector3.one * 1.5f;
        Vector3 endScale = Vector3.one;

        float elapsedTime = 0f;

        while (elapsedTime < damageDisplayDuration)
        {
            float progress = elapsedTime / damageDisplayDuration;
            float curveValue = damageAnimationCurve.Evaluate(progress);

            damageText.transform.localPosition = Vector3.Lerp(startPos, endPos, curveValue);

            if (progress < 0.2f)
            {
                float scaleProgress = progress / 0.2f;
                damageText.transform.localScale = Vector3.Lerp(startScale, maxScale, scaleProgress);
            }
            else
            {
                float scaleProgress = (progress - 0.2f) / 0.8f;
                damageText.transform.localScale = Vector3.Lerp(maxScale, endScale, scaleProgress);
            }

            if (progress > 0.7f)
            {
                float alphaProgress = (progress - 0.7f) / 0.3f;
                Color currentColor = damageText.color;
                currentColor.a = Mathf.Lerp(1f, 0f, alphaProgress);
                damageText.color = currentColor;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        damageText.gameObject.SetActive(false);

        // Reseta o estado visual para a próxima vez
        damageText.transform.localPosition = startPos;
        damageText.transform.localScale = Vector3.one;
        Color resetColor = damageText.color;
        resetColor.a = 1f;
        damageText.color = resetColor;
    }
}