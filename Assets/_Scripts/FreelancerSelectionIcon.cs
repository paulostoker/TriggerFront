// _Scripts/UI/FreelancerSelectionIcon.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FreelancerSelectionIcon : MonoBehaviour
{
    [Header("UI References")]
    public Image portraitImage;
    public Image highlightImage;
    public Button iconButton;

    private FreelancerData associatedFreelancer;
    private CharacterSelectManager manager;

    /// <summary>
    /// Configura o ícone com os dados do freelancer e a referência ao manager.
    /// </summary>
    public void Setup(FreelancerData data, CharacterSelectManager selectManager)
    {
        associatedFreelancer = data;
        manager = selectManager;

        // Configura o retrato animado
        if (portraitImage != null && data.portrait != null)
        {
            Animator animator = portraitImage.GetComponent<Animator>();
            if (animator == null)
            {
                animator = portraitImage.gameObject.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = data.portrait;
        }

        // Garante que o highlight comece desligado
        SetHighlight(false);

        // Adiciona o listener para o clique
        iconButton.onClick.AddListener(OnIconClicked);
    }

    /// <summary>
    /// Ativa ou desativa o anel/imagem de destaque.
    /// </summary>
    public void SetHighlight(bool isHighlighted)
    {
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(isHighlighted);
        }
    }

    /// <summary>
    /// Ativa ou desativa a interatividade do botão e muda a cor.
    /// </summary>
    public void SetSelectable(bool isSelectable)
    {
        iconButton.interactable = isSelectable;
        portraitImage.color = isSelectable ? Color.white : Color.gray;
    }

    /// <summary>
    /// Avisa o manager principal que este ícone foi clicado.
    /// </summary>
    private void OnIconClicked()
    {
        manager.OnFreelancerIconClicked(associatedFreelancer);
    }
}