// _Scripts/DieController.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class DieController : MonoBehaviour
{
    #region Fields & Properties
    private bool isRolling = false;
    private Action onRollCompleteCallback;
    public int finalResult { get; private set; }

    [Header("Animation Settings")]
    [Tooltip("Duração da animação de queda e rotação simultânea")]
    public float fallAnimationDuration = 0.5f;
    [Tooltip("Duração da animação de parada e bounce")]
    public float stopAnimationDuration = 0.2f;
    [Tooltip("Altura inicial do dado (em pixels da tela)")]
    public float startHeight = 1920f;
    [Tooltip("Altura final do dado (0 = centro da tela)")]
    public float endHeight = 0f;

    [Header("X Movement Animation")]
    [Tooltip("Posição X inicial do dado")]
    public float startXPosition = 0f;
    [Tooltip("Posição X final do dado")]
    public float endXPosition = 0f;
    [Tooltip("Duração da animação no eixo X (0 = usa fallAnimationDuration)")]
    public float xMovementDuration = 0f;
    [Tooltip("Curva de animação para movimento no eixo X")]
    public AnimationCurve xMovementCurve;

    [Header("Rotation Settings")]
    [Tooltip("Velocidade mínima de rotação durante a queda")]
    public float minAngularVelocity = -360f;
    [Tooltip("Velocidade máxima de rotação durante a queda")]
    public float maxAngularVelocity = 360f;

    [Header("Bounce Effect")]
    [Tooltip("Controla bounce realista: valores negativos = impacto, positivos = bounce para cima")]
    public AnimationCurve bounceCurve;
    [Tooltip("Altura máxima do bounce em pixels/unidades")]
    public float bounceIntensity = 50f;
    [Tooltip("Profundidade do impacto inicial (quanto 'afunda' antes de quicar)")]
    public float impactDepth = 10f;
    [Tooltip("Aplica compressão baseada na proximidade do impacto")]
    public bool useCompressionEffect = true;
    [Tooltip("Intensidade da compressão (0.9 = 10% comprimido)")]
    public float compressionIntensity = 0.9f;

    [Header("Dice Values")]
    [Tooltip("Valor mínimo do dado (inclusivo)")]
    public int minDiceValue = 1;
    [Tooltip("Valor máximo do dado (exclusivo - Unity Random)")]
    public int maxDiceValue = 7;

    [Header("Bonus Animation")]
    [Tooltip("Duração da pausa antes da animação de bônus.")]
    public float bonusPauseDuration = 0.5f;
    [Tooltip("Duração da animação de giro para o resultado final.")]
    public float bonusSpinDuration = 0.2f;
    [Tooltip("Velocidade de rotação durante o giro mágico.")]
    public float bonusSpinSpeed = 1440f;

    private readonly Dictionary<int, Vector3> finalRotations = new Dictionary<int, Vector3>
    {
        { 1, new Vector3(-90, 0, 0) },
        { 2, new Vector3(0, 180, 0) },
        { 3, new Vector3(0, 90, 0) },
        { 4, new Vector3(0, -90, 0) },
        { 5, new Vector3(0, 0, 0) },
        { 6, new Vector3(90, 0, 0) }
    };
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        if (bounceCurve == null || bounceCurve.keys.Length == 0)
            CreateRealisticBounceCurve();
        if (xMovementCurve == null || xMovementCurve.keys.Length == 0)
            CreateEaseOutXCurve();
    }
    #endregion

    #region Curve Creation
    private void CreateRealisticBounceCurve()
    {
        bounceCurve = new AnimationCurve(
            new Keyframe(0, -0.2f),
            new Keyframe(0.2f, 1f),
            new Keyframe(0.5f, 0.3f),
            new Keyframe(0.8f, 0.1f),
            new Keyframe(1f, 0f)
        );
        for (int i = 0; i < bounceCurve.keys.Length; i++)
            bounceCurve.SmoothTangents(i, 0.3f);
    }

    private void CreateEaseOutXCurve()
    {
        xMovementCurve = new AnimationCurve(
            new Keyframe(0, 0),
            new Keyframe(1, 1)
        );
        Keyframe[] keys = xMovementCurve.keys;
        keys[0].outTangent = 2f;
        keys[1].inTangent = 0f;
        xMovementCurve.keys = keys;
    }
    #endregion

    #region Roll Animation System
    public void RollTheDie(int initialResult, int additiveResult, int finalMappedResult, Quaternion cameraRotation, Action onComplete)
    {
        if (!isRolling)
        {
            this.finalResult = finalMappedResult;
            onRollCompleteCallback = onComplete;
            StartCoroutine(RollCoroutine(initialResult, additiveResult, finalMappedResult, cameraRotation));
        }
    }

    private IEnumerator RollCoroutine(int initialResult, int additiveResult, int finalMappedResult, Quaternion cameraRotation)
    {
        isRolling = true;
        float elapsedTime = 0f;
        float actualStartHeight = startHeight > 0 ? startHeight : Screen.height;
        Vector3 startPosition = new Vector3(startXPosition, actualStartHeight, 0);
        Vector3 endPosition = new Vector3(endXPosition, endHeight, 0);
        float actualXDuration = xMovementDuration > 0 ? xMovementDuration : fallAnimationDuration;
        RectTransform rectTransform = GetComponent<RectTransform>();
        Vector3 randomAngularVelocity = new Vector3(
            UnityEngine.Random.Range(minAngularVelocity, maxAngularVelocity),
            UnityEngine.Random.Range(minAngularVelocity, maxAngularVelocity),
            UnityEngine.Random.Range(minAngularVelocity, maxAngularVelocity)
        );

        while (elapsedTime < fallAnimationDuration)
        {
            float fallFraction = elapsedTime / fallAnimationDuration;
            float xFraction = Mathf.Clamp01(elapsedTime / actualXDuration);
            float currentY = Mathf.Lerp(startPosition.y, endPosition.y, fallFraction);
            float xCurveValue = xMovementCurve.Evaluate(xFraction);
            float currentX = Mathf.Lerp(startXPosition, endXPosition, xCurveValue);
            rectTransform.localPosition = new Vector3(currentX, currentY, 0);
            transform.Rotate(randomAngularVelocity * Time.deltaTime, Space.World);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        rectTransform.localPosition = endPosition;

        StartCoroutine(RealisticBounceCoroutine(stopAnimationDuration));
        Quaternion startRotation = transform.rotation;
        Quaternion initialTargetRotation = cameraRotation * Quaternion.Euler(finalRotations[initialResult]);
        elapsedTime = 0f;
        while (elapsedTime < stopAnimationDuration)
        {
            transform.rotation = Quaternion.Slerp(startRotation, initialTargetRotation, elapsedTime / stopAnimationDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.rotation = initialTargetRotation;

        if (additiveResult != initialResult)
        {
            yield return new WaitForSeconds(bonusPauseDuration);
            ServiceLocator.Audio.PlayDieBuffSound();
            yield return StartCoroutine(MagicSpinCoroutine(cameraRotation, additiveResult));
        }

        if (finalMappedResult != additiveResult)
        {
            yield return new WaitForSeconds(bonusPauseDuration);
            ServiceLocator.Audio.PlayDieBuffSound();
            yield return StartCoroutine(MagicSpinCoroutine(cameraRotation, finalMappedResult));
        }

        isRolling = false;
        onRollCompleteCallback?.Invoke();
    }

    private IEnumerator MagicSpinCoroutine(Quaternion cameraRotation, int targetResult)
    {
        float elapsedTime = 0f;
        Vector3 spinAxis = Vector3.up + Vector3.right;
        while (elapsedTime < bonusSpinDuration)
        {
            transform.Rotate(spinAxis, bonusSpinSpeed * Time.deltaTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        Quaternion finalTargetRotation = cameraRotation * Quaternion.Euler(finalRotations[targetResult]);
        transform.rotation = finalTargetRotation;
    }

    private IEnumerator RealisticBounceCoroutine(float duration)
    {
        float elapsedTime = 0f;
        Vector3 originalScale = transform.localScale;
        Vector3 restPosition = transform.localPosition;
        while (elapsedTime < duration)
        {
            float progress = elapsedTime / duration;
            float curveValue = bounceCurve.Evaluate(progress);
            float bounceOffset;
            if (curveValue < 0)
                bounceOffset = curveValue * impactDepth;
            else
                bounceOffset = curveValue * bounceIntensity;
            Vector3 bouncePosition = restPosition + Vector3.up * bounceOffset;
            transform.localPosition = bouncePosition;
            if (useCompressionEffect)
            {
                float compressionFactor;
                if (curveValue < 0)
                    compressionFactor = Mathf.Lerp(1f, compressionIntensity, Mathf.Abs(curveValue) * 5f);
                else
                    compressionFactor = Mathf.Lerp(compressionIntensity, 1f, curveValue);
                Vector3 bounceScale = new Vector3(
                    originalScale.x,
                    originalScale.y * compressionFactor,
                    originalScale.z
                );
                transform.localScale = bounceScale;
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = restPosition;
        transform.localScale = originalScale;
    }
    #endregion
}