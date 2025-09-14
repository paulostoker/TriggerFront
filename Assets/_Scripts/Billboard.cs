// _Scripts/Billboard.cs
using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform mainCameraTransform;

    void Start()
    {
        // Encontra a câmera principal no início do jogo e guarda sua referência
        // para não precisar procurar a cada frame, o que é mais eficiente.
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    // Usamos LateUpdate para garantir que a rotação da câmera já foi processada
    // neste frame. Isso evita tremores ou atrasos na rotação da UI.
    void LateUpdate()
    {
        if (mainCameraTransform != null)
        {
            // Força a rotação deste objeto a ser exatamente a mesma da câmera.
            transform.rotation = mainCameraTransform.rotation;
        }
    }
}