using TMPro;
using UnityEngine;

// Este atributo nos permite adicionar o componente pelo menu, como o TextMeshPro original.
[AddComponentMenu("UI/TextMeshPro - Text (UI) Emoji Fix")]
public class TextMeshProEmojiFix : TextMeshProUGUI
{
    // O caractere invisível "Variation Selector-16" (U+FE0F) que causa o problema do quadradinho.
    private const string VARIATION_SELECTOR = "\uFE0F";

    // Esta é a "propriedade" que armazena o texto. Vamos sobrescrevê-la.
    public override string text
    {
        get
        {
            // A leitura do texto funciona normalmente.
            return base.text;
        }
        set
        {
            // "set" é chamado quando você faz: meuTexto.text = "novo valor";
            // Antes de passar o texto para o componente base, nós o limpamos.
            // O value?.Replace(...) garante que, se o texto for nulo, não dará erro,
            // e remove todas as ocorrências do caractere invisível.
            base.text = value?.Replace(VARIATION_SELECTOR, "");
        }
    }
}