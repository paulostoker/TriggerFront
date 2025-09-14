// _Scripts/TechniqueManager.cs
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class TechniqueManager : MonoBehaviour
{
    [Header("Highlight Materials")]
    public Material supportHighlightMaterial;

     [Header("Prefabs")]
    public GameObject grenadeProjectilePrefab;
    public bool IsInTechniqueMode { get; private set; }

    private GameObject currentUser;
    private TechniqueData currentTechnique;
    private List<Tile> validTargetTiles = new List<Tile>();

    public void StartTechniqueMode(GameObject user, TechniqueData technique)
    {
        if (user == null || technique == null) return;

        CancelTechniqueMode(); // Garante que qualquer modo anterior seja limpo

        currentUser = user;
        currentTechnique = technique;
        IsInTechniqueMode = true;

        Debug.Log($"Iniciando modo de técnica para '{technique.techniqueName}' usada por '{user.name}'. Targeting: {technique.targeting}");

        switch (technique.targeting)
        {
            case TargetingType.Self:
            case TargetingType.NoTarget:
                ExecuteTechnique(user, technique, user); // O alvo é o próprio usuário
                break;

            case TargetingType.Tile:
            case TargetingType.Enemy:
            case TargetingType.Ally:
            case TargetingType.AreaOfEffect:
                CalculateAndShowValidTargets();
                break;
        }
    }

     private void CalculateAndShowValidTargets()
    {
        Tile startTile = ServiceLocator.Grid.GetTileUnderPiece(currentUser);
        if (startTile == null)
        {
            CancelTechniqueMode();
            return;
        }

        List<Tile> tilesInRange = ServiceLocator.Grid.FindTilesInRange(startTile, currentTechnique.range);

        foreach (var tile in tilesInRange)
        {
            if (currentTechnique.requiresLineOfSight && !ServiceLocator.Grid.HasLineOfSightBetweenTiles(startTile.GetGridPosition(), tile.GetGridPosition()))
            {
                continue;
            }
            
            // --- INÍCIO DA CORREÇÃO DE ALVO ---
            // Adicionamos uma verificação para garantir que o tile não esteja ocupado
            // para técnicas que miram no chão.
            bool isTileOccupied = ServiceLocator.Pieces.GetPieceAtTile(tile) != null;
            // --- FIM DA CORREÇÃO DE ALVO ---

            bool isValid = false;
            switch (currentTechnique.targeting)
            {
                case TargetingType.Tile:
                case TargetingType.AreaOfEffect:
                    // Um tile só é válido se for andável E NÃO ESTIVER OCUPADO
                    if (tile.IsWalkable() && !isTileOccupied)
                    {
                        isValid = true;
                    }
                    break;
                case TargetingType.Enemy:
                    if (isTileOccupied && !ServiceLocator.Pieces.IsPieceOnTeam(ServiceLocator.Pieces.GetPieceAtTile(tile), ServiceLocator.Game.IsPlayer1Turn()))
                    {
                        isValid = true;
                    }
                    break;
                case TargetingType.Ally:
                    if (isTileOccupied && ServiceLocator.Pieces.IsPieceOnTeam(ServiceLocator.Pieces.GetPieceAtTile(tile), ServiceLocator.Game.IsPlayer1Turn()))
                    {
                        isValid = true;
                    }
                    break;
            }

            if (isValid)
            {
                validTargetTiles.Add(tile);
                if (currentTechnique.type == TechniqueType.Attack)
                {
                    tile.ShowAttackHighlight();
                }
                else
                {
                    tile.ShowSupportHighlight();
                }
            }
        }
    }

    public void HandleTargetSelection(Tile targetTile, GameObject targetPiece)
    {
        if (!IsInTechniqueMode || targetTile == null)
        {
            CancelTechniqueMode();
            return;
        }

        // Verifica se o tile clicado está na lista de alvos válidos
        if (validTargetTiles.Contains(targetTile))
        {
            // Determina o alvo final com base no tipo de mira
            object finalTarget = currentTechnique.targeting switch
            {
                TargetingType.Enemy or TargetingType.Ally => targetPiece,
                _ => targetTile,
            };
            
            if (finalTarget != null)
            {
                ExecuteTechnique(currentUser, currentTechnique, finalTarget);
            }
            else
            {
                // Caso especial: clicou num tile válido de inimigo/aliado, mas a peça não foi detectada
                Debug.LogWarning("Alvo válido clicado, mas a peça no tile era nula. Cancelando.");
                CancelTechniqueMode();
            }
        }
        else
        {
            Debug.Log("Clique em tile inválido. Cancelando modo de técnica.");
            CancelTechniqueMode();
        }
    }


    public void ExecuteTechnique(GameObject user, TechniqueData technique, object target)
    {
        // Agora este método inicia a corrotina de sequência da técnica
        StartCoroutine(TechniqueSequenceCoroutine(user, technique, target));
    }
    
    private IEnumerator TechniqueSequenceCoroutine(GameObject user, TechniqueData technique, object target)
    {
        Debug.Log($"SEQUÊNCIA INICIADA para '{technique.techniqueName}' usada por '{user.name}' no alvo '{target}'.");

        var actionState = ServiceLocator.Game.CurrentState as ActionState;
        if (actionState != null) actionState.IsProcessingAction = true;

        ServiceLocator.Freelancers.ConsumeEnergyForTechnique(user, technique.cost);

        if (technique.effectToSpawn != null && target is Tile targetTile)
        {
            GameObject pivot = GameObject.Find("Pivot");
            Transform parentTransform = pivot != null ? pivot.transform : null;
            Vector3 endPos_Effect = ServiceLocator.Pieces.GetRaycastPositionOnTile(targetTile);

            // --- LÓGICA DE ANIMAÇÃO CONDICIONAL ---
            if (technique.animationType == TechniqueAnimationType.Throw)
            {
                // Se for arremesso, executa a animação da granada
                Vector3 startPos = user.transform.position + Vector3.up * 1.5f;
                Vector3 endPos_Projectile = endPos_Effect - (Vector3.up * GameConfig.Instance.pieceHeightOffset);

                GameObject projectile = null;
                if (grenadeProjectilePrefab != null)
                {
                    projectile = Instantiate(grenadeProjectilePrefab, startPos, Quaternion.identity, parentTransform);
                    float travelDuration = 1.0f;
                    float elapsedTime = 0f;
                    Vector3 randomRotationAxis = UnityEngine.Random.onUnitSphere;

                    while (elapsedTime < travelDuration)
                    {
                        float progress = elapsedTime / travelDuration;
                        Vector3 currentPos = Vector3.Lerp(startPos, endPos_Projectile, progress);
                        currentPos.y += 2.6f * Mathf.Sin(progress * Mathf.PI);
                        
                        projectile.transform.position = currentPos;
                        projectile.transform.Rotate(randomRotationAxis, 720 * Time.deltaTime);

                        elapsedTime += Time.deltaTime;
                        yield return null;
                    }
                    projectile.transform.position = endPos_Projectile;
                    // O projétil agora será destruído junto com o efeito, então não o destruímos aqui.
                }
            }
            // Se o tipo for "Place", ele simplesmente pulará a animação de arremesso.

            yield return new WaitForSeconds(0.2f);

            GameObject effectObject = Instantiate(technique.effectToSpawn.effectPrefab, endPos_Effect, Quaternion.identity, parentTransform);
            SpawnedEffect spawnedEffect = effectObject.GetComponent<SpawnedEffect>();
            if (spawnedEffect != null)
            {
                spawnedEffect.data = technique.effectToSpawn;
                spawnedEffect.turnsRemaining = technique.effectToSpawn.durationTurns;
                ServiceLocator.Effects.RegisterSpawnedEffect(spawnedEffect);
            }
        }
        else
        {
            yield return new WaitForSeconds(1f);
        }

        if (actionState != null)
        {
            actionState.SetTechniqueUsed();
            actionState.IsProcessingAction = false;
        }
        
        CancelTechniqueMode();
    }

    public void CancelTechniqueMode()
    {
        foreach (var tile in validTargetTiles)
        {
            tile.HideAllHighlights();
        }
        validTargetTiles.Clear();

        currentUser = null;
        currentTechnique = null;
        IsInTechniqueMode = false;
    }
}