// _Scripts/CameraManager.cs
using UnityEngine;
using System.Collections;
using Unity.Cinemachine;
using System;

public class CameraManager : MonoBehaviour
{
    #region Fields & Properties
    [Header("Camera References")]
    public Transform cameraTarget;
    public CinemachineOrbitalFollow orbitalFollow;
    public CinemachinePanTilt panTilt;
    
    private CinemachineInputAxisController cameraInputController;
    private Vector3 initialCameraTargetPosition;
    private Coroutine activeCameraMovementCoroutine;
    
    public static event Action OnCameraAnimationComplete;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        Initialize();
    }

    void OnDestroy()
    {
        StopAllAnimations();
        OnCameraAnimationComplete = null;
    }
    #endregion

    #region Initialization
    private void Initialize()
    {
        if (cameraTarget == null || orbitalFollow == null || panTilt == null)
        {
            Debug.LogError("CameraManager: Referências da câmera não foram atribuídas!");
            return;
        }
        cameraInputController = FindFirstObjectByType<CinemachineInputAxisController>();
        if (cameraInputController == null)
            Debug.LogWarning("CameraManager: CinemachineInputAxisController não encontrado. A trava da câmera pode não funcionar.");
        
        Vector3 correctInitialPosition = new Vector3(9.5f, 9f, 9.5f);
        cameraTarget.position = correctInitialPosition;
        initialCameraTargetPosition = correctInitialPosition;
        Debug.Log($"<color=blue>[CameraManager]</color> Initialized with position {correctInitialPosition}");
    }
    #endregion

    #region Camera Control
    public void ResetForPreparation(bool isPlayer1Turn)
    {
        Vector3 safeInitialPosition = new Vector3(9.5f, 9f, 9.5f);
        float duration = GameConfig.Instance.cameraAnimationDuration;

        MoveTargetTo(safeInitialPosition, duration);
        AnimateRadius(GameConfig.Instance.defaultCameraRadius, duration);
        
        float targetYRotation = isPlayer1Turn ? 0f : 180f;
        AnimateRotation(targetYRotation, duration);

        StartCoroutine(AnimateVerticalTiltCoroutine(
            GameConfig.Instance.defaultCameraVertical,
            GameConfig.Instance.defaultCameraTilt,
            duration
        ));

        Debug.Log($"<color=blue>[CameraManager]</color> Reset for preparation - Player {(isPlayer1Turn ? "1" : "2")}");
    }
    
    public void SetupForAction()
    {
        AnimateRadius(GameConfig.Instance.actionCameraRadius, GameConfig.Instance.cameraAnimationDuration);
        Debug.Log("<color=blue>[CameraManager]</color> Setup for action phase");
    }
    
    public void FocusOnPiece(GameObject piece)
    {
        if (piece == null)
        {
            Debug.LogWarning("CameraManager: Tentativa de focar em peça nula!");
            return;
        }
        Vector3 targetPos = new Vector3(
            piece.transform.position.x, 
            cameraTarget.position.y, 
            piece.transform.position.z
        );
        MoveTargetTo(targetPos, GameConfig.Instance.cameraAnimationDuration);
        Debug.Log($"<color=blue>[CameraManager]</color> Focusing on piece: {piece.name}");
    }
    
    public void SetupForEndGame()
    {
        MoveTargetTo(initialCameraTargetPosition, 2f);
        AnimateRadius(GameConfig.Instance.defaultCameraRadius, 1f);
        if (cameraInputController != null)
        {
            cameraInputController.enabled = false;
            Debug.Log("<color=yellow>[CameraManager]</color> Input da Câmera DESATIVADO para a cena final.");
        }
    }
    #endregion

    #region Animation Methods
    public void MoveTargetTo(Vector3 targetPosition, float duration = -1f)
    {
        if (duration < 0) duration = GameConfig.Instance.cameraAnimationDuration;
        if (activeCameraMovementCoroutine != null)
            StopCoroutine(activeCameraMovementCoroutine);
        activeCameraMovementCoroutine = StartCoroutine(MoveTargetCoroutine(targetPosition, duration));
    }
    
    public void AnimateRadius(float targetRadius, float duration = -1f)
    {
        if (duration < 0) duration = GameConfig.Instance.cameraAnimationDuration;
        StartCoroutine(AnimateRadiusCoroutine(targetRadius, duration));
    }
    
    public void AnimateRotation(float targetValue, float duration = -1f)
    {
        if (duration < 0) duration = GameConfig.Instance.cameraAnimationDuration;
        StartCoroutine(AnimateRotationCoroutine(targetValue, duration));
    }
    
    public void StopAllAnimations()
    {
        StopAllCoroutines();
        activeCameraMovementCoroutine = null;
        Debug.Log("<color=blue>[CameraManager]</color> All animations stopped");
    }
    #endregion

    #region Animation Coroutines
    private IEnumerator MoveTargetCoroutine(Vector3 targetPosition, float duration)
    {
        if (cameraTarget == null) yield break;
        float elapsedTime = 0f;
        Vector3 startingPosition = cameraTarget.position;
        while (elapsedTime < duration)
        {
            float progress = elapsedTime / duration;
            float curveValue = GameConfig.Instance.cameraAnimationCurve.Evaluate(progress);
            cameraTarget.position = Vector3.Lerp(startingPosition, targetPosition, curveValue);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        cameraTarget.position = targetPosition;
        activeCameraMovementCoroutine = null;
        OnCameraAnimationComplete?.Invoke();
    }
    
    private IEnumerator AnimateRadiusCoroutine(float targetRadius, float duration)
    {
        if (orbitalFollow == null) yield break;
        float elapsedTime = 0f;
        float startingRadius = orbitalFollow.Radius;
        while (elapsedTime < duration)
        {
            float progress = elapsedTime / duration;
            float curveValue = GameConfig.Instance.cameraAnimationCurve.Evaluate(progress);
            orbitalFollow.Radius = Mathf.Lerp(startingRadius, targetRadius, curveValue);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        orbitalFollow.Radius = targetRadius;
        OnCameraAnimationComplete?.Invoke();
    }

    private IEnumerator AnimateRotationCoroutine(float targetValue, float duration)
    {
        if (orbitalFollow == null || panTilt == null) yield break;
        float elapsedTime = 0f;
        float startHorizontal = orbitalFollow.HorizontalAxis.Value;
        float startPan = panTilt.PanAxis.Value;
        while (elapsedTime < duration)
        {
            float progress = elapsedTime / duration;
            float curveValue = GameConfig.Instance.cameraAnimationCurve.Evaluate(progress);
            float currentValue = Mathf.LerpAngle(startHorizontal, targetValue, curveValue);
            
            orbitalFollow.HorizontalAxis.Value = currentValue;
            panTilt.PanAxis.Value = currentValue;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        orbitalFollow.HorizontalAxis.Value = targetValue;
        panTilt.PanAxis.Value = targetValue;
        OnCameraAnimationComplete?.Invoke();
    }

    private IEnumerator AnimateVerticalTiltCoroutine(float targetVertical, float targetTilt, float duration)
    {
        if (orbitalFollow == null || panTilt == null) yield break;

        float elapsedTime = 0f;
        float startVertical = orbitalFollow.VerticalAxis.Value;
        float startTilt = panTilt.TiltAxis.Value;

        while (elapsedTime < duration)
        {
            float progress = elapsedTime / duration;
            float curveValue = GameConfig.Instance.cameraAnimationCurve.Evaluate(progress);
            orbitalFollow.VerticalAxis.Value = Mathf.Lerp(startVertical, targetVertical, curveValue);
            panTilt.TiltAxis.Value = Mathf.Lerp(startTilt, targetTilt, curveValue);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        orbitalFollow.VerticalAxis.Value = targetVertical;
        panTilt.TiltAxis.Value = targetTilt;
    }
     public void TrackPiece(GameObject piece)
    {
        if (piece == null || cameraTarget == null) return;

        // Cria a nova posição para o alvo da câmera baseada na peça
        Vector3 newPos = new Vector3(
            piece.transform.position.x,
            cameraTarget.position.y, // Mantém a altura Y atual da câmera para evitar saltos
            piece.transform.position.z
        );

        // Define a posição instantaneamente, sem Lerp ou animação
        cameraTarget.position = newPos;
    }
    #endregion

    #region Utility Methods
    public Vector3 GetCurrentTargetPosition()
    {
        return cameraTarget != null ? cameraTarget.position : Vector3.zero;
    }
    
    public float GetCurrentRadius()
    {
        return orbitalFollow != null ? orbitalFollow.Radius : 0f;
    }
    
    public bool IsAnimating()
    {
        return activeCameraMovementCoroutine != null;
    }
    
    public void SetInitialTargetPosition(Vector3 position)
    {
        initialCameraTargetPosition = position;
    }
    #endregion
}