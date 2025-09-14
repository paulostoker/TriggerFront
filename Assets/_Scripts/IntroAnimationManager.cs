using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class IntroAnimationManager : MonoBehaviour
{
    [Header("Animation Targets")]
    public CanvasGroup mainUICanvasGroup;
    public CanvasGroup cardsCanvasGroup;

    [Header("Cinemachine Targets")]
    [Tooltip("Arraste aqui o componente 'Orbital Follow' da sua Câmera Virtual.")]
    public CinemachineOrbitalFollow orbitalFollow;
    [Tooltip("Arraste aqui o componente 'Pan & Tilt' da sua Câmera Virtual.")]
    public CinemachinePanTilt panTilt;

    [Header("Animation Settings")]
    public float initialPivotScale = 0.1f;
    public float finalPivotScale = 1.0f;

    [System.NonSerialized]
    public Transform pivotTransform;

    private bool isAnimationComplete = false;
    private bool isAnimationInProgress = false;
    
    private InputManager clickInputManager;
    private CinemachineInputAxisController cameraInputController;

    private float animationDuration => GameConfig.Instance.introAnimationDuration;
    private float rotationAmount => GameConfig.Instance.introAnimationYRotation;
    private AnimationCurve animationCurve => GameConfig.Instance.introAnimationCurve;

    void Awake()
    {
        clickInputManager = FindFirstObjectByType<InputManager>();
        cameraInputController = FindFirstObjectByType<CinemachineInputAxisController>();

        if (cameraInputController == null)
        {
            Debug.LogWarning("IntroAnimationManager: CinemachineInputAxisController não encontrado. A câmera poderá ser movida durante a intro.");
        }
        
        if (mainUICanvasGroup == null)
        {
            GameObject canvas2D = GameObject.Find("Canvas2D");
            if (canvas2D != null) mainUICanvasGroup = GetOrAddCanvasGroup(canvas2D);
        }

        if (cardsCanvasGroup == null)
        {
            GameObject canvasCards = GameObject.Find("CanvasCards");
            if (canvasCards != null) cardsCanvasGroup = GetOrAddCanvasGroup(canvasCards);
        }
    }

    public void StartIntroAnimation()
    {
        if (isAnimationInProgress || isAnimationComplete) return;

        if (ServiceLocator.Audio != null)
        {
            ServiceLocator.Audio.PlayBackgroundMusic();
        }

        StartCoroutine(IntroAnimationSequence());
    }

    private IEnumerator IntroAnimationSequence()
    {
        isAnimationInProgress = true;
        SetAllInputs(false);

        SetupInitialState();
        yield return new WaitForEndOfFrame();
        StartCoroutine(AnimatePivot());
        yield return new WaitForSeconds(animationDuration);
        FinalizeAnimation();

        isAnimationInProgress = false;
        isAnimationComplete = true;
        
        SetAllInputs(true);
    }

    private void SetAllInputs(bool enabled)
    {
        if (clickInputManager != null)
        {
            clickInputManager.SetInputEnabled(enabled);
        }
        
        if (cameraInputController != null)
        {
            cameraInputController.enabled = enabled;
            
            if(enabled) Debug.Log("<color=green>CinemachineInputAxisController ENABLED</color>");
            else Debug.Log("<color=yellow>CinemachineInputAxisController DISABLED</color>");
        }
    }

    private void SetupInitialState()
    {
        if (pivotTransform != null)
        {
            pivotTransform.gameObject.SetActive(false);
        }

        if (mainUICanvasGroup != null)
        {
            mainUICanvasGroup.gameObject.SetActive(false);
            SetCanvasGroupVisibility(mainUICanvasGroup, false);
        }
        if (cardsCanvasGroup != null)
        {
            cardsCanvasGroup.gameObject.SetActive(false);
            SetCanvasGroupVisibility(cardsCanvasGroup, false);
        }

        // Define os valores iniciais para os eixos do Cinemachine
        if (orbitalFollow != null)
        {
            orbitalFollow.VerticalAxis.Value = 65f;
        }
        if (panTilt != null)
        {
            panTilt.TiltAxis.Value = 70f;
        }
    }

    private IEnumerator AnimatePivot()
    {
        if (pivotTransform == null) yield break;

        pivotTransform.gameObject.SetActive(true);
        pivotTransform.localScale = Vector3.one * initialPivotScale;
        pivotTransform.rotation = Quaternion.identity;

        // Variáveis para a animação do Pivô
        Vector3 initialScale = Vector3.one * initialPivotScale;
        Vector3 finalScale = Vector3.one * finalPivotScale;
        float initialRotationY = -180f;
        float finalRotationY = rotationAmount;

        // Variáveis para a animação da Câmera
        float startVertical = 65f;
        float endVertical = 16.5f;
        float startTilt = 70f;
        float endTilt = 25f;
        
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            float normalizedTime = elapsedTime / animationDuration;
            float curveValue = animationCurve.Evaluate(normalizedTime);

            // Animação do Pivô (existente)
            pivotTransform.localScale = Vector3.Lerp(initialScale, finalScale, curveValue);
            float currentRotationY = Mathf.Lerp(initialRotationY, finalRotationY, curveValue);
            pivotTransform.rotation = Quaternion.Euler(0, currentRotationY, 0);

            // Lógica de Animação da Câmera
            if (orbitalFollow != null)
            {
                orbitalFollow.VerticalAxis.Value = Mathf.Lerp(startVertical, endVertical, curveValue);
            }
            if (panTilt != null)
            {
                panTilt.TiltAxis.Value = Mathf.Lerp(startTilt, endTilt, curveValue);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Garante que os valores finais sejam aplicados ao final da animação
        pivotTransform.localScale = finalScale;
        pivotTransform.rotation = Quaternion.Euler(0, finalRotationY, 0);
        if (orbitalFollow != null) orbitalFollow.VerticalAxis.Value = endVertical;
        if (panTilt != null) panTilt.TiltAxis.Value = endTilt;
    }
    

    private void FinalizeAnimation()
    {
        if (pivotTransform != null)
        {
            pivotTransform.gameObject.SetActive(true);
            pivotTransform.localScale = Vector3.one * finalPivotScale;
            pivotTransform.rotation = Quaternion.Euler(0, rotationAmount, 0);
        }

        if (mainUICanvasGroup != null)
        {
            mainUICanvasGroup.gameObject.SetActive(true);
            SetCanvasGroupVisibility(mainUICanvasGroup, true);
        }
        if (cardsCanvasGroup != null)
        {
            cardsCanvasGroup.gameObject.SetActive(true);
            SetCanvasGroupVisibility(cardsCanvasGroup, true);
        }

        NotifyIntroComplete();
    }

    private void NotifyIntroComplete()
    {
        TurnDisplay turnDisplay = FindFirstObjectByType<TurnDisplay>();
        if (turnDisplay != null)
        {
            turnDisplay.ForceShowAfterIntro();
        }
    }
    
    private CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        if (target == null) return null;
        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = target.AddComponent<CanvasGroup>();
        }
        return canvasGroup;
    }

    private void SetCanvasGroupVisibility(CanvasGroup canvasGroup, bool visible)
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    public bool IsAnimationComplete => isAnimationComplete;
    public bool IsAnimationInProgress => isAnimationInProgress;

    public void RegisterPivot(Transform pivot)
    {
        pivotTransform = pivot;
    }
    
    public void ResetAnimation()
    {
        StopAllCoroutines();
        isAnimationInProgress = false;
        isAnimationComplete = false;
        SetupInitialState();
    }
}