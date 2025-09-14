// _Scripts/CombatManager.cs - VERSÃO FINAL com Regions Aprimoradas
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using static ServiceLocator;

public class CoroutineResult<T>
{
    public T result;
}

public class CombatManager : MonoBehaviour
{
    #region Fields & Properties
    [Header("Visual Settings")]
    public Color enemyHighlightColor = new Color(1, 0, 0, 0.5f);
    [Tooltip("Material a ser usado para destacar inimigos no modo de ataque.")]
    public Material enemyHighlightMaterial;

    [Header("Prefabs & References")]
    public GameObject diePrefab;
    public Transform dieCanvasTransform;

    private GameObject currentAttacker;
    private List<Tile> attackableTiles = new List<Tile>();
    private Dictionary<Renderer, Material[]> originalEnemyMaterials = new Dictionary<Renderer, Material[]>();
    private List<GameObject> temporaryHighlights = new List<GameObject>(); // <-- Adicione esta nova lista

    public static event Action<GameObject, int> OnDamageDealt;
    public static event Action<GameObject, GameObject> OnAttackStarted;
    public static event Action<GameObject, GameObject, int> OnAttackCompleted;
    public static event Action OnAttackCancelled;

    #endregion

    #region Lifecycle & Initialization
    void Start() => Initialize();

    private void Initialize()
    {
        if (dieCanvasTransform == null) FindDieCanvas();
        ValidateReferences();
    }

    private void FindDieCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var canvas in canvases)
        {
            if (canvas.name.ToLower().Contains("die") || canvas.name.ToLower().Contains("ui"))
            {
                dieCanvasTransform = canvas.transform;
                break;
            }
        }
        if (dieCanvasTransform == null && canvases.Length > 0)
            dieCanvasTransform = canvases[0].transform;
    }

    private void ValidateReferences()
    {
        if (ServiceLocator.MainCamera == null) Debug.LogError("CombatManager: Main Camera not found via ServiceLocator!");
        if (ServiceLocator.Grid == null) Debug.LogError("CombatManager: GridManager not found via ServiceLocator!");
        if (ServiceLocator.UI == null) Debug.LogError("CombatManager: UIManager not found via ServiceLocator!");
        if (ServiceLocator.Pieces == null) Debug.LogError("CombatManager: PieceManager not found via ServiceLocator!");
        if (diePrefab == null) Debug.LogWarning("CombatManager: diePrefab not assigned!");
        if (dieCanvasTransform == null) Debug.LogWarning("CombatManager: dieCanvasTransform not found!");
        if (enemyHighlightMaterial == null) Debug.LogError("CombatManager: 'Enemy Highlight Material' não foi atribuído no Inspector!");
    }
    #endregion

    #region Attack Mode Management
    public List<Tile> StartAttackMode(GameObject attacker, Func<GameObject, bool> isValidTarget)
    {
        if (attacker == null || ServiceLocator.Freelancers == null) return new List<Tile>();

        currentAttacker = attacker;
        FreelancerData opData = ServiceLocator.Freelancers.GetFreelancerData(attacker);
        if (opData == null) return new List<Tile>();

        int finalRange = GetAttackRange(attacker);
        Tile attackerTile = ServiceLocator.Grid.GetTileUnderPiece(attacker);
        if (attackerTile == null) return new List<Tile>();

        List<Tile> potentialTiles = ServiceLocator.Grid.FindTilesInRange(attackerTile, finalRange);
        attackableTiles.Clear();
        ClearEnemyHighlights();
        int penetrationPower = ServiceLocator.Effects.GetStatModifier(attacker, ModifierType.AllowWallbang);

        foreach (var tile in potentialTiles)
        {
            if (HasLineOfSight(attackerTile, tile, penetrationPower))
            {
                if (tile == attackerTile) continue;

                GameObject objectOnTile = ServiceLocator.Grid.GetObjectAtTile(tile.GetGridPosition());

                if (objectOnTile == null)
                {
                    attackableTiles.Add(tile);
                    tile.ShowAttackHighlight();
                    continue;
                }

                SpawnedEffect spawnedEffect = objectOnTile.GetComponent<SpawnedEffect>();
                bool isDestructibleObject = spawnedEffect != null && spawnedEffect.data.isDestructible;
                bool isEnemy = isValidTarget(objectOnTile);

                if (isEnemy || isDestructibleObject)
                {
                    attackableTiles.Add(tile);
                    tile.ShowAttackHighlight();
                    HighlightEnemy(objectOnTile);
                }
            }
        }
        return new List<Tile>(attackableTiles);
    }



    public void ExecuteAttack(GameObject attacker, GameObject target, Action<bool> onComplete = null)
    {
        if (attacker == null || target == null)
        {
            onComplete?.Invoke(false);
            return;
        }
        StartCoroutine(AttackSequenceCoroutine(attacker, target, onComplete));
    }
    public bool IsValidAttackTarget(Tile targetTile) => attackableTiles.Contains(targetTile);
    public void CancelAttackMode()
    {
        ClearAttackHighlights();
        ClearEnemyHighlights();
        attackableTiles.Clear();
        currentAttacker = null;
        OnAttackCancelled?.Invoke();
    }
    private IEnumerator PlaySoundAfterDelay(WeaponType weaponType, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (ServiceLocator.Audio != null)
        {
            ServiceLocator.Audio.PlayWeaponSound(weaponType);
        }
    }
    #endregion

    #region Die 

    public IEnumerator RollDie(GameObject roller, GameObject target, int baseBonus, Action<int> onComplete)
    {
        var rollerInstance = ServiceLocator.Freelancers.GetFreelancerInstance(roller);
        if (rollerInstance == null)
        {
            onComplete?.Invoke(UnityEngine.Random.Range(1, 7));
            yield break;
        }

        int initialRoll = UnityEngine.Random.Range(1, 7);

        int attackDiceBonus = ServiceLocator.Effects.GetStatModifier(roller, ModifierType.AttackDice);
        if (rollerInstance.IsInOffAngleState) { attackDiceBonus += 2; }

        int defenseDiceBonus = 0;
        if (target != null)
        {
            var targetInstance = ServiceLocator.Freelancers.GetFreelancerInstance(target);
            if (targetInstance != null)
            {
                defenseDiceBonus = ServiceLocator.Effects.GetStatModifier(target, ModifierType.DefenseDice);
                if (targetInstance.IsInOffAngleState) { defenseDiceBonus -= 3; }
            }
        }

        int totalBonus = baseBonus + attackDiceBonus - defenseDiceBonus;
        int additiveResult = Mathf.Clamp(initialRoll + totalBonus, 1, 6);

        int mappedResult = ServiceLocator.Effects.ApplyResultMapModifiers(roller, additiveResult);
        int finalResult = Mathf.Clamp(mappedResult, 1, 6);

        GameObject dieObject = Instantiate(diePrefab, dieCanvasTransform);
        DieController dieController = dieObject.GetComponent<DieController>();
        if (dieController != null)
        {
            bool animationComplete = false;
            dieController.RollTheDie(initialRoll, additiveResult, finalResult, ServiceLocator.MainCamera.transform.rotation, () => { animationComplete = true; });
            yield return new WaitUntil(() => animationComplete);

            // --- NOVA PAUSA ADICIONADA AQUI ---
            // Adiciona uma pausa para que o jogador possa ver o resultado final no dado.
            yield return new WaitForSeconds(GameConfig.Instance.dieResultDisplayDuration);

            Destroy(dieObject);
        }

        onComplete?.Invoke(finalResult);
    }

    #endregion

    #region Core Attack Logic (Coroutine)
    private IEnumerator AttackSequenceCoroutine(GameObject attacker, GameObject target, Action<bool> onCompleteTurnAction)
    {
        bool isSprayTransferAttempt = ServiceLocator.Effects.FindAndConsumeEffect(attacker, ModifierType.SprayTransfer);
        
        if (isSprayTransferAttempt)
        {
            var realTargetResult = new CoroutineResult<GameObject>();
            var sprayTransferExecuted = new CoroutineResult<bool>();
            
            yield return StartCoroutine(AttemptSprayTransfer(attacker, target, realTargetResult, sprayTransferExecuted));
            
            if (sprayTransferExecuted.result)
            {
                ServiceLocator.Turn.NotifyActionCompleted(isMove: false);
                onCompleteTurnAction?.Invoke(true);
                yield break;
            }
            
            target = realTargetResult.result;
            if (target == null) 
            {
                CancelAttackMode();
                ServiceLocator.Turn.NotifyActionCompleted(isMove: false);
                onCompleteTurnAction?.Invoke(true);
                yield break;
            }
        }

        OnAttackStarted?.Invoke(attacker, target);
        if (onCompleteTurnAction != null)
        {
            CancelAttackMode();
        }

        var attackerInstance = ServiceLocator.Freelancers.GetFreelancerInstance(attacker);
        if (attackerInstance == null)
        {
            onCompleteTurnAction?.Invoke(false);
            yield break;
        }
        
        // --- INÍCIO DA CORREÇÃO ---
        // A verificação de fumaça foi movida para DENTRO da corrotina,
        // mas a lógica foi mantida em um único lugar para clareza.
        GameObject obstructingObject = GetFirstPieceHitByRaycast(attacker, target);
        if (obstructingObject != null && obstructingObject != target)
        {
            SpawnedEffect smoke = obstructingObject.GetComponent<SpawnedEffect>();
            // A verificação agora usa a propriedade booleana 'blocksLineOfSight'
            if (smoke != null && smoke.data.blocksLineOfSight)
            {
                Debug.Log($"<color=orange>[CombatManager]</color> Ataque de {attacker.name} bloqueado pela fumaça em direção a {target.name}.");
                
                // Precisamos garantir que o alvo tenha um PieceDisplay para mostrar o "miss"
                PieceDisplay targetDisplay = target.GetComponentInChildren<PieceDisplay>();
                if (targetDisplay != null)
                {
                    targetDisplay.ShowMissPopup();
                }

                if (ServiceLocator.Audio != null) ServiceLocator.Audio.PlayMissSound();
                
                ServiceLocator.Effects.CleanUpActionEffects(attacker, ActionType.Attack);
                OnAttackCompleted?.Invoke(attacker, target, 0);
                if (onCompleteTurnAction != null)
                {
                    ServiceLocator.Turn.NotifyActionCompleted(isMove: false);
                }
                onCompleteTurnAction?.Invoke(true);
                yield break; 
            }
        }
        // --- FIM DA CORREÇÃO ---

        bool canCollateral = ServiceLocator.Effects.GetStatModifier(attacker, ModifierType.CanCollateralDamage) > 0;
        List<GameObject> allTargets = new List<GameObject>();
        if (canCollateral)
        {
            Tile startTile = ServiceLocator.Grid.GetTileUnderPiece(attacker);
            Tile endTile = ServiceLocator.Grid.GetTileUnderPiece(target);
            if (startTile != null && endTile != null)
            {
                allTargets = ServiceLocator.Grid.GetPiecesInLineOfFire(startTile, endTile);
            }
        }
        allTargets.Remove(attacker);
        if (allTargets.Count > 2)
        {
            allTargets = allTargets.GetRange(0, 2);
        }
        if (allTargets.Count == 0)
        {
            GameObject actualTarget = FindActualTarget(attacker, target);
            if (actualTarget != null) allTargets.Add(actualTarget);
            else allTargets.Add(target);
        }
        
        int finalDamage = 0;
        int finalDiceResultForSound = 0;
        bool useOverrideDamage = false;

        if (ServiceLocator.Effects.TryGetDamageOverride(attacker, out int overrideDamageValue))
        {
            finalDamage = overrideDamageValue;
            useOverrideDamage = true;
            finalDiceResultForSound = 4;
            Debug.Log($"<color=cyan>[CombatManager]</color> Usando dano fixo (override): {finalDamage}");
        }
        else if (attackerInstance.StoredDiceResult.HasValue)
        {
            finalDiceResultForSound = attackerInstance.StoredDiceResult.Value;
            attackerInstance.StoredDiceResult = null;
            Debug.Log($"<color=cyan>[CombatManager]</color> Usando resultado de dado pré-rolado: {finalDiceResultForSound}");
        }
        else
        {
            CoroutineResult<int> dieRollResult = new CoroutineResult<int>();
            yield return StartCoroutine(RollDie(attacker, allTargets.Count > 0 ? allTargets[0] : null, 0, (result) => { dieRollResult.result = result; }));
            finalDiceResultForSound = dieRollResult.result;
        }
        
        WeaponType currentWeaponType = ServiceLocator.Freelancers.IsInEcoMode(attacker) ? WeaponType.Pistol : ServiceLocator.Freelancers.GetFreelancerData(attacker).weaponType;
        StartCoroutine(PlaySoundAfterDelay(currentWeaponType, GameConfig.Instance.weaponSoundFireDelay));

        yield return new WaitForSeconds(GameConfig.Instance.postDiceRollDelay);
        
        for (int i = 0; i < allTargets.Count; i++)
        {
            GameObject currentTarget = allTargets[i];
            
            if (!useOverrideDamage)
            {
                finalDamage = CalculateDamage(attacker, currentTarget, finalDiceResultForSound);
            }
            
            finalDamage = ApplyObstacleModifiers(attacker, currentTarget, finalDamage);
            
            SpawnedEffect destructibleObject = currentTarget.GetComponent<SpawnedEffect>();
            if (destructibleObject != null)
            {
                if (destructibleObject.data.isDestructible)
                {
                    destructibleObject.TakeDamage(finalDamage);
                    if (ServiceLocator.Audio != null) ServiceLocator.Audio.PlayHitSound();
                }
                continue;
            }

            if (canCollateral && i > 0)
            {
                finalDamage -= 20;
                finalDamage = Mathf.Max(0, finalDamage);
            }

            bool isTargetAnAlly = ServiceLocator.Pieces.IsPieceOnTeam(currentTarget, attackerInstance.IsPlayer1);
            if (isTargetAnAlly)
            {
                if (GameConfig.Instance.enableFriendlyFire) { finalDamage /= 2; }
                else { finalDamage = 0; }
            }

            int damageReduction = ServiceLocator.Effects.GetStatModifier(currentTarget, ModifierType.DamageReduction);
            if (damageReduction > 0)
            {
                finalDamage -= damageReduction;
                finalDamage = Mathf.Max(0, finalDamage);
            }

            if (finalDamage > 0 && ServiceLocator.Freelancers.IsAlive(currentTarget))
            {
                ServiceLocator.Freelancers.TakeDamage(currentTarget, finalDamage);
                OnDamageDealt?.Invoke(currentTarget, finalDamage);
                ServiceLocator.Effects.CleanUpActionEffects(currentTarget, ActionType.TakeDamage); 

                if (ServiceLocator.Audio != null)
                {
                    if (finalDiceResultForSound >= 6) ServiceLocator.Audio.PlayHSSound();
                    else ServiceLocator.Audio.PlayHitSound();
                }
            }
            else
            {
                currentTarget.GetComponentInChildren<PieceDisplay>()?.ShowMissPopup();
                if (ServiceLocator.Audio != null) ServiceLocator.Audio.PlayMissSound();
            }

            if (allTargets.Count > 1 && i < allTargets.Count - 1) yield return new WaitForSeconds(0.3f);
        }
        
        foreach(var currentTarget in allTargets)
        {
            yield return StartCoroutine(CheckAndTriggerCounterAttack(attacker, currentTarget));
        }

        ServiceLocator.Effects.ProcessTriggeredEffects(attacker, ActionType.Attack);
        ServiceLocator.Effects.CleanUpActionEffects(attacker, ActionType.Attack);
        
        OnAttackCompleted?.Invoke(attacker, target, 0);
        
        if (onCompleteTurnAction != null)
        {
            ServiceLocator.Turn.NotifyActionCompleted(isMove: false);
        }
        onCompleteTurnAction?.Invoke(true);
    }
    private IEnumerator CheckAndTriggerCounterAttack(GameObject originalAttacker, GameObject originalTarget)
    {
        if (!ServiceLocator.Freelancers.IsAlive(originalTarget) || !ServiceLocator.Freelancers.IsAlive(originalAttacker))
        {
            yield break;
        }

        if (ServiceLocator.Effects.FindAndConsumeCounterAttackEffect(originalTarget))
        {
            Debug.Log($"<color=orange>[Counter-Attack]</color> {originalTarget.name} vai tentar contra-atacar {originalAttacker.name}!");

            yield return new WaitForSeconds(0.75f);

            if (IsInAttackRange(originalTarget, originalAttacker))
            {
                yield return StartCoroutine(AttackSequenceCoroutine(originalTarget, originalAttacker, null));
            }
            else
            {
                Debug.Log($"<color=orange>[Counter-Attack]</color> Falhou: {originalAttacker.name} está fora de alcance ou sem linha de visão.");
            }
        }
    }

    #endregion

    #region Spray Transfer
    private GameObject FindValidSprayTransferTarget(GameObject attacker, GameObject primaryTarget)
    {
        var attackerInstance = ServiceLocator.Freelancers.GetFreelancerInstance(attacker);
        if (attackerInstance == null) return null;

        Tile attackerTile = ServiceLocator.Grid.GetTileUnderPiece(attacker);
        Tile primaryTargetTile = ServiceLocator.Grid.GetTileUnderPiece(primaryTarget);
        if (attackerTile == null || primaryTargetTile == null) return null;

        var allEnemies = ServiceLocator.Pieces.GetPlayerPieces(!attackerInstance.IsPlayer1);
        int attackRange = GetAttackRange(attacker);
        
        List<GameObject> validSecondaryTargets = new List<GameObject>();

        foreach (var potentialTarget in allEnemies)
        {
            if (potentialTarget == primaryTarget || !ServiceLocator.Freelancers.IsAlive(potentialTarget))
            {
                continue;
            }

            Tile targetTile = ServiceLocator.Grid.GetTileUnderPiece(potentialTarget);
            if (targetTile == null) continue;

            int distanceInTiles = ServiceLocator.Grid.GetManhattanDistance(attackerTile, targetTile);

            if (distanceInTiles > attackRange)
            {
                continue;
            }
            
            GameObject firstHitPiece = GetFirstPieceHitByRaycast(attacker, potentialTarget);

            if (firstHitPiece != null && firstHitPiece == potentialTarget)
            {
                validSecondaryTargets.Add(potentialTarget);
            }
        }
        
        if (validSecondaryTargets.Count == 0)
        {
            return null;
        }

        return validSecondaryTargets.OrderBy(target => 
            ServiceLocator.Grid.GetManhattanDistance(primaryTargetTile, ServiceLocator.Grid.GetTileUnderPiece(target))
        ).FirstOrDefault();
    }

     private IEnumerator ExecuteSprayTransferAttack(GameObject attacker, GameObject primaryTarget, GameObject secondaryTarget)
    {
        CancelAttackMode();
        Debug.Log($"<color=cyan>[CombatManager]</color> Executando Spray Transfer em '{primaryTarget.name}' e '{secondaryTarget.name}'.");
        
        var attackerInstance = ServiceLocator.Freelancers.GetFreelancerInstance(attacker);
        FreelancerData attackerData = attackerInstance.BaseData;
        int damagePerTarget = (attackerData != null) ? (attackerData.weaponStats.damage / 2) : 0;

        float pacingDelay = GameConfig.Instance.attackDelay / 2f;

        // Ataque ao Alvo Primário
        if (ServiceLocator.Audio != null) ServiceLocator.Audio.PlayWeaponSound(attackerData.weaponType);
        yield return new WaitForSeconds(pacingDelay);
        
        if (damagePerTarget > 0 && ServiceLocator.Freelancers.IsAlive(primaryTarget))
        {
            ServiceLocator.Freelancers.TakeDamage(primaryTarget, damagePerTarget);
            if (ServiceLocator.Audio != null) ServiceLocator.Audio.PlayHitSound();
        }
        
        yield return new WaitForSeconds(0.15f);

        // Ataque ao Alvo Secundário
        if (ServiceLocator.Audio != null) ServiceLocator.Audio.PlayWeaponSound(attackerData.weaponType);
        yield return new WaitForSeconds(pacingDelay);
        
        if (damagePerTarget > 0 && ServiceLocator.Freelancers.IsAlive(secondaryTarget))
        {
            ServiceLocator.Freelancers.TakeDamage(secondaryTarget, damagePerTarget);
            if (ServiceLocator.Audio != null) ServiceLocator.Audio.PlayHitSound();
        }

        // A finalização agora é de responsabilidade da corrotina principal
        OnAttackCompleted?.Invoke(attacker, primaryTarget, damagePerTarget * 2);
    }
     private IEnumerator AttemptSprayTransfer(GameObject attacker, GameObject intendedTarget, CoroutineResult<GameObject> realTargetResult, CoroutineResult<bool> sprayTransferExecuted)
    {
        // 1. Validar o Alvo Primário Real
        GameObject primaryTarget = GetFirstPieceHitByRaycast(attacker, intendedTarget);
        realTargetResult.result = primaryTarget; // Guarda o alvo real para ser usado depois

        if (primaryTarget == null)
        {
            sprayTransferExecuted.result = false;
            yield break;
        }

        var attackerInstance = ServiceLocator.Freelancers.GetFreelancerInstance(attacker);
        bool isPrimaryTargetAnAlly = ServiceLocator.Pieces.IsPieceOnTeam(primaryTarget, attackerInstance.IsPlayer1);
        
        if (isPrimaryTargetAnAlly)
        {
            sprayTransferExecuted.result = false;
            yield break;
        }

        // 2. Buscar e Validar o Alvo Secundário
        GameObject secondaryTarget = FindValidSprayTransferTarget(attacker, primaryTarget);
        
        // 3. Decidir o Resultado
        if (secondaryTarget != null)
        {
            // SUCESSO: Executa o ataque especial e sinaliza que a ação foi concluída.
            yield return StartCoroutine(ExecuteSprayTransferAttack(attacker, primaryTarget, secondaryTarget));
            sprayTransferExecuted.result = true;
        }
        else
        {
            // FALHA: Sinaliza que um ataque normal deve prosseguir.
            sprayTransferExecuted.result = false;
        }
    }
      private GameObject GetFirstPieceHitByRaycast(GameObject attacker, GameObject intendedTarget)
    {
        if (attacker == null || intendedTarget == null) return null;

        Vector3 startPos = attacker.transform.position;
        Vector3 targetPos = intendedTarget.transform.position;

        Vector3 direction = (targetPos - startPos).normalized;
        float distance = Vector3.Distance(startPos, targetPos);

        // --- LÓGICA REVERTIDA ---
        // O raycast volta a usar apenas as layers de Obstáculo e Peça.
        LayerMask combinedMask = GameConfig.Instance.losObstacleLayerMask | 
                                 GameConfig.Instance.pieceLayerMask;
        // --- FIM DA LÓGICA REVERTIDA ---
        
        RaycastHit[] hits = Physics.RaycastAll(startPos, direction, distance, combinedMask, QueryTriggerInteraction.Collide)
            .OrderBy(h => h.distance).ToArray();

        if (hits.Length == 0)
        {
            return intendedTarget;
        }

        foreach (var hit in hits)
        {
            PieceProperties piece = hit.collider.GetComponentInParent<PieceProperties>();
            SpawnedEffect spawnedEffect = hit.collider.GetComponentInParent<SpawnedEffect>();

            if (piece != null)
            {
                return piece.gameObject;
            }

            if (spawnedEffect != null)
            {
                return spawnedEffect.gameObject;
            }

            ObstacleProperties obstacle = hit.collider.GetComponentInParent<ObstacleProperties>();
            if (obstacle != null && obstacle.BlocksLineOfSight())
            {
                return null;
            }
        }
        
        return intendedTarget;
    }
    #endregion

    #region Damage & Path Calculation
    public int CalculateDamage(GameObject attacker, GameObject target, int finalDiceResult)
    {
        int baseDamage;

        if (ServiceLocator.Freelancers.IsInEcoMode(attacker))
        {
            baseDamage = CalculateEcoDamage(finalDiceResult);
        }
        else
        {
            baseDamage = CalculateWeaponDamage(attacker, target, finalDiceResult);
        }

        if (baseDamage > 0)
        {
            int bonusDamage = ServiceLocator.Effects.GetStatModifier(attacker, ModifierType.Damage);
            int penetrationPower = ServiceLocator.Effects.GetStatModifier(attacker, ModifierType.AllowWallbang);
            if (penetrationPower > 0)
            {
                Tile attackerTile = ServiceLocator.Grid.GetTileUnderPiece(attacker);
                Tile targetTile = ServiceLocator.Grid.GetTileUnderPiece(target);

                if (attackerTile != null && targetTile != null)
                {
                    int obstacleCount = ServiceLocator.Grid.CountBlockingObstaclesInPath(attackerTile.GetGridPosition(), targetTile.GetGridPosition());

                    if (obstacleCount > 0)
                    {
                        int wallbangPenalty = ServiceLocator.Effects.GetStatModifier(attacker, ModifierType.WallbangDamage);
                        bonusDamage += wallbangPenalty;
                    }
                }
            }
            return baseDamage + bonusDamage;
        }

        return 0;
    }


    private int CalculateEcoDamage(int finalDiceResult)
    {
        return GameConfig.Instance.ecoDamageTable.GetDamage(finalDiceResult);
    }

    private int CalculateWeaponDamage(GameObject attacker, GameObject target, int finalDiceResult)
    {
        if (ServiceLocator.Freelancers == null) return 0;
        FreelancerData attackerData = ServiceLocator.Freelancers.GetFreelancerData(attacker);
        if (attackerData == null) return 0;

        bool hasProximityBonus = IsAdjacent(ServiceLocator.Grid.GetTileUnderPiece(attacker), ServiceLocator.Grid.GetTileUnderPiece(target));
        int proximityDamage = hasProximityBonus ? attackerData.weaponStats.proximityBonus : 0;
        int heightModifier = CalculateHeightAdvantage(attacker, target);

        return finalDiceResult switch
        {
            1 => 0,
            2 => (attackerData.weaponStats.damage + proximityDamage + heightModifier) / 2,
            6 => attackerData.weaponStats.damage + proximityDamage + heightModifier + attackerData.weaponStats.criticalDamage,
            _ => attackerData.weaponStats.damage + proximityDamage + heightModifier
        };
    }

    private GameObject FindActualTarget(GameObject attacker, GameObject intendedTarget)
    {
        Tile attackerTile = ServiceLocator.Grid.GetTileUnderPiece(attacker);
        Tile targetTile = ServiceLocator.Grid.GetTileUnderPiece(intendedTarget);

        if (attackerTile == null || targetTile == null)
        {
            return intendedTarget;
        }

        Vector3 startPos = attackerTile.transform.position + new Vector3(0, GameConfig.Instance.losRayHeight, 0);
        Vector3 endPos = targetTile.transform.position + new Vector3(0, GameConfig.Instance.losRayHeight, 0);
        Vector3 direction = (endPos - startPos).normalized;
        float distance = Vector3.Distance(startPos, endPos);

        RaycastHit[] hits = Physics.RaycastAll(startPos, direction, distance)
                                   .OrderBy(h => h.distance)
                                   .ToArray();

        foreach (RaycastHit hit in hits)
        {
            PieceProperties piece = hit.collider.GetComponentInParent<PieceProperties>();
            if (piece != null)
            {
                return piece.gameObject;
            }
        }

        return intendedTarget;
    }

    private int ApplyObstacleModifiers(GameObject attacker, GameObject target, int baseDamage)
    {
        Tile attackerTile = ServiceLocator.Grid.GetTileUnderPiece(attacker);
        Tile targetTile = ServiceLocator.Grid.GetTileUnderPiece(target);
        if (attackerTile == null || targetTile == null) return baseDamage;
        if (HasBoxInPath(attackerTile, targetTile))
        {
            float modifier = GameConfig.Instance.boxDamageModifier;
            return Mathf.RoundToInt(baseDamage * modifier);
        }
        return baseDamage;
    }

    private bool HasBoxInPath(Tile start, Tile end)
    {
        Vector3 startWorldPos = ServiceLocator.Grid.GridToWorld(new Vector2Int(start.x, start.z), GameConfig.Instance.losRayHeight);
        Vector3 endWorldPos = ServiceLocator.Grid.GridToWorld(new Vector2Int(end.x, end.z), GameConfig.Instance.losRayHeight);
        Vector3 direction = (endWorldPos - startWorldPos).normalized;
        float distance = Vector3.Distance(startWorldPos, endWorldPos);
        RaycastHit[] hits = Physics.RaycastAll(startWorldPos, direction, distance, GameConfig.Instance.losObstacleLayerMask);
        foreach (RaycastHit hit in hits)
        {
            ObstacleProperties obstacle = hit.collider.GetComponentInParent<ObstacleProperties>();
            if (obstacle != null && obstacle.obstacleType == ObstacleType.Box && !obstacle.BlocksLineOfSight())
            {
                return true;
            }
        }
        return false;
    }

    private int CalculateHeightAdvantage(GameObject attacker, GameObject target)
    {

        return 0;
    }
    #endregion

    #region Line of Sight & Adjacency
    public bool HasLineOfSight(Tile start, Tile end, int penetrationPower = 0)
    {
        if (start == null || end == null) return false;

        Vector3 startCenter = ServiceLocator.Grid.GridToWorld(start.GetGridPosition(), GameConfig.Instance.losRayHeight);
        Vector3 endCenter = ServiceLocator.Grid.GridToWorld(end.GetGridPosition(), GameConfig.Instance.losRayHeight);
        
        LayerMask obstacleMask = GameConfig.Instance.losObstacleLayerMask;
        LayerMask pieceMask = GameConfig.Instance.pieceLayerMask;
        LayerMask combinedMask = obstacleMask | pieceMask;

        bool IsRayBlocked(Vector3 startPoint, Vector3 endPoint, Color rayColor)
        {
            Vector3 direction = (endPoint - startPoint).normalized;
            float distance = Vector3.Distance(startPoint, endPoint);

            // Desenha o raio na janela Scene para depuração
            Debug.DrawRay(startPoint, direction * distance, rayColor, 5.0f);

            RaycastHit[] hits = Physics.RaycastAll(startPoint, direction, distance, combinedMask, QueryTriggerInteraction.Collide);

            int blockingObstacles = 0;
            foreach (var hit in hits)
            {
                ObstacleProperties obstacle = hit.collider.GetComponentInParent<ObstacleProperties>();
                if (obstacle != null && obstacle.BlocksLineOfSight())
                {
                    blockingObstacles++;
                }

                SpawnedEffect smoke = hit.collider.GetComponentInParent<SpawnedEffect>();
                if (smoke != null && smoke.data.blocksLineOfSight)
                {
                    blockingObstacles++;
                }
            }
            return blockingObstacles > penetrationPower;
        }

        // Verifica o raio central (amarelo)
        if (!IsRayBlocked(startCenter, endCenter, Color.yellow)) return true;

        Vector3 centralDirection = (endCenter - startCenter).normalized;
        if (centralDirection.sqrMagnitude < 0.01f) return false;

        Vector3 perpendicular = new Vector3(-centralDirection.z, 0, centralDirection.x) * GameConfig.Instance.raycastOffset;
        
        // Verifica o raio da esquerda (ciano)
        if (!IsRayBlocked(startCenter, endCenter + perpendicular, Color.cyan)) return true;

        // Verifica o raio da direita (ciano)
        if (!IsRayBlocked(startCenter, endCenter - perpendicular, Color.cyan)) return true;

        return false;
    }

    public bool IsAdjacent(Tile tile1, Tile tile2)
    {
        return ServiceLocator.Grid.AreAdjacent(tile1, tile2);
    }
    #endregion

    #region Visuals & Highlighting
    private void HighlightEnemy(GameObject enemy)
    {
        var pieceRenderer = enemy.GetComponentInChildren<Renderer>();
        if (pieceRenderer == null || enemyHighlightMaterial == null) return;

        if (!originalEnemyMaterials.ContainsKey(pieceRenderer))
        {
            originalEnemyMaterials[pieceRenderer] = pieceRenderer.materials;
        }

        var highlightMat = new Material(enemyHighlightMaterial);

        highlightMat.color = enemyHighlightColor;

        pieceRenderer.material = highlightMat;
    }

    private void ClearEnemyHighlights()
    {
        foreach (var entry in originalEnemyMaterials)
        {
            if (entry.Key != null) // entry.Key é o Renderer
            {
                entry.Key.materials = entry.Value; // entry.Value são os materiais originais
            }
        }
        originalEnemyMaterials.Clear();

        // 2. Destrói todos os highlights fantasmas temporários criados para os Boxes
        foreach (var highlight in temporaryHighlights)
        {
            if (highlight != null)
            {
                Destroy(highlight);
            }
        }
        temporaryHighlights.Clear();
    }
    private void ClearAttackHighlights()
    {
        foreach (var tile in attackableTiles)
        {
            if (tile != null) tile.HideAllHighlights();
        }
    }
    #endregion

    #region Public Getters & State Checks
    public GameObject GetCurrentAttacker() => currentAttacker;
    public bool IsInAttackMode() => currentAttacker != null;
    public List<Tile> GetAttackableTiles() => new List<Tile>(attackableTiles);

    public void ForceStopAttack()
    {
        StopAllCoroutines();
        CancelAttackMode();
    }

    public bool HasEnergyToAttack(GameObject piece)
    {
        if (ServiceLocator.Freelancers == null) return false;
        if (ServiceLocator.Freelancers.IsInEcoMode(piece)) return true;
        FreelancerData data = ServiceLocator.Freelancers.GetFreelancerData(piece);
        if (data == null) return true;
        return ServiceLocator.Freelancers.GetEnergyCount(piece, CardType.Action) >= data.weaponCost.action;
    }

    public int GetAttackRange(GameObject piece)
    {
        if (ServiceLocator.Freelancers == null) return GameConfig.Instance.pistolRange;

        int baseRange;
        if (ServiceLocator.Freelancers.IsInEcoMode(piece))
        {
            baseRange = GameConfig.Instance.pistolRange;
        }
        else
        {
            FreelancerData data = ServiceLocator.Freelancers.GetFreelancerData(piece);
            baseRange = data?.weaponStats.range ?? GameConfig.Instance.pistolRange;
        }

        int rangeBonus = ServiceLocator.Effects.GetStatModifier(piece, ModifierType.WeaponRange);

        // --- LÓGICA DE BÔNUS "OFF-ANGLE" ---
        FreelancerInstance pieceInstance = ServiceLocator.Freelancers.GetFreelancerInstance(piece);
        if (pieceInstance != null && pieceInstance.IsInOffAngleState)
        {
            rangeBonus += 2; // Adiciona +2 de alcance se estiver em Off-Angle
        }
        // --- FIM DA LÓGICA ---

        return baseRange + rangeBonus;
    }

    public bool IsInAttackRange(GameObject attacker, GameObject target)
    {
        Tile attackerTile = ServiceLocator.Grid.GetTileUnderPiece(attacker);
        Tile targetTile = ServiceLocator.Grid.GetTileUnderPiece(target);
        if (attackerTile == null || targetTile == null) return false;

        int range = GetAttackRange(attacker);
        int distance = ServiceLocator.Grid.GetManhattanDistance(attackerTile, targetTile);
        if (distance > range) return false;

        int penetrationPower = ServiceLocator.Effects.GetStatModifier(attacker, ModifierType.AllowWallbang);
        return HasLineOfSight(attackerTile, targetTile, penetrationPower);
    }
    #endregion

    #region Unity Cleanup
    void OnDestroy()
    {
        StopAllCoroutines();
        CancelAttackMode();
        OnDamageDealt = null;
        OnAttackStarted = null;
        OnAttackCompleted = null;
        OnAttackCancelled = null;
    }
    public static void ResetStaticData()
    {
        OnDamageDealt = null;
        OnAttackStarted = null;
        OnAttackCompleted = null;
        OnAttackCancelled = null;
    }

    #endregion
}






