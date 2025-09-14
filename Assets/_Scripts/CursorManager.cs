using System.Collections;
using UnityEngine;

public class CursorManager : MonoBehaviour
{
    // === CURSOR ANIMADO ===
    [Header("Animated Cursor")]
    // Arraste todos os frames da animação aqui
    public Texture2D[] animatedCursorFrames;
    // Velocidade da animação em frames por segundo (FPS)
    public float frameRate = 10f;
    // O ponto de clique para a animação
    public Vector2 animatedHotspot = Vector2.zero;

    // === OUTROS CURSORES (Estáticos) ===
    [Header("Static Cursor (Optional)")]
    public Texture2D interactionCursor;
    public Vector2 interactionHotspot = Vector2.zero;

    // Singleton para facilitar o acesso
    public static CursorManager Instance;

    private Coroutine cursorAnimationCoroutine;

    private void Awake()
    {
        // Configura o Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Inicia com o cursor animado por padrão
        StartCursorAnimation();
    }

    // Inicia a Corrotina que anima o cursor
    public void StartCursorAnimation()
    {
        // Se já houver uma animação rodando, pare-a primeiro
        if (cursorAnimationCoroutine != null)
        {
            StopCoroutine(cursorAnimationCoroutine);
        }
        cursorAnimationCoroutine = StartCoroutine(AnimateCursor());
    }

    // Para a animação e define um cursor estático (ex: de interação)
    public void SetStaticCursor(Texture2D cursor, Vector2 hotspot)
    {
        // Se a animação estiver rodando, pare-a
        if (cursorAnimationCoroutine != null)
        {
            StopCoroutine(cursorAnimationCoroutine);
            cursorAnimationCoroutine = null;
        }
        Cursor.SetCursor(cursor, hotspot, CursorMode.Auto);
    }

    // A rotina que efetivamente faz a animação quadro a quadro
    private IEnumerator AnimateCursor()
    {
        int currentFrame = 0;
        // Loop infinito enquanto a corrotina estiver ativa
        while (true)
        {
            // Define o cursor para o frame atual da animação
            Cursor.SetCursor(animatedCursorFrames[currentFrame], animatedHotspot, CursorMode.Auto);

            // Avança para o próximo frame
            currentFrame++;
            
            // Se chegar ao final da lista de frames, volta para o início
            if (currentFrame >= animatedCursorFrames.Length)
            {
                currentFrame = 0;
            }

            // Espera o tempo certo para exibir o próximo frame, baseado no frameRate
            yield return new WaitForSeconds(1f / frameRate);
        }
    }
}