// _Scripts/GameConfig.cs - Versão Otimizada com SphereCast e Delay de UI
using UnityEngine;



[CreateAssetMenu(fileName = "GameConfig", menuName = "Game/Game Configuration")]
public class GameConfig : ScriptableObject
{
    [Header("Grid Settings")]
    public int gridWidth = 20;
    public int gridHeight = 20;

    [Header("Game Rules")]
    public int totalFreelancers = 5;
    public int maxHandSize = 7;
    public int maxTotalTurns = 40;

    [Tooltip("Se marcado, freelancers podem acertar e causar dano em aliados que estiverem na linha de tiro.")]
    public bool enableFriendlyFire = false;
    
    [Header("Card Draw Settings")]
    [Tooltip("Se TRUE, o número de cartas compradas no início da Preparação será igual ao número de freelancers vivos no time.")]
    public bool drawCardsBasedOnAliveFreelancers = true;

    [Tooltip("Se 'drawCardsBasedOnAliveFreelancers' for FALSE, este número fixo de cartas será usado.")]
    public int cardsDrawnPerPreparation = 3;

        [Header("Advanced Card Draw Settings")]
    [Tooltip("Se TRUE, usa o sistema de mão inicial + compra por turno. Esta opção tem prioridade sobre as outras.")]
    public bool useInitialHandDraw = false;
    [Tooltip("Número de cartas compradas APENAS no primeiro turno de preparação de cada jogador.")]
    public int initialHandSize = 5;
    [Tooltip("Número fixo de cartas compradas em TODOS os turnos de preparação subsequentes.")]
    public int cardsPerTurnAfterFirst = 2;

    [Header("Height Constraints")]
    public float minTileHeight = 0f;
    public float maxTileHeight = 3.0f;
    public float heightSnapIncrement = 0.1f;

    [Header("Raycast Settings")]
    [Tooltip("Layer mask para identificar as peças dos freelancers.")]
    public LayerMask pieceLayerMask;
    public float raycastStartHeight = 100f;
    public float raycastMaxDistance = 200f;
    public LayerMask tileLayerMask = -1;
    public float pieceHeightOffset = 1.0f;



    [Header("Combat Settings")]
    public int pistolRange = 4;
    public float attackDelay = 1.5f;
    
    [Header("Obstacle Settings")]
    [Range(0f, 1f)]
    public float boxDamageModifier = 0.75f;

    [Header("Movement Settings")]
    public float moveSpeed = 7.5f;
    public float dodgeDistance = 0.5f;
    public float dodgeDuration = 0.2f;
    public float returnDuration = 0.2f;

    [Header("Camera Settings")]
    public float defaultCameraRadius = 14f;
    public float actionCameraRadius = 8f;
     [Tooltip("Altura padrão da câmera (Vertical Axis) para a visão de preparação.")]
    public float defaultCameraVertical = 30f;
    [Tooltip("Inclinação padrão da câmera (Tilt Axis) para a visão de preparação.")]
    public float defaultCameraTilt = 45f;
    public float cameraAnimationDuration = 0.5f;
    [Tooltip("Curva de animação para os movimentos da câmera (zoom, rotação, etc.).")]
    public AnimationCurve cameraAnimationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public Vector3 initialCameraPosition = new Vector3(9.5f, 15f, 9.5f);

    [Header("Turn Settings")]
    public Vector3 turnIndicatorOffset = new Vector3(0, 1.5f, 0);

    [Header("Highlight Settings")]
    [Range(0f, 1f)]
    public float highlightTransparency = 0.5f;
    public Color movementHighlightColor = Color.green;
    public Color attackHighlightColor = Color.red;
    public float highlightYOffset = 0.01f;

    [Header("Eco Mode Damage")]
    public EcoDamageTable ecoDamageTable = new EcoDamageTable();

    [System.Serializable]
    public class EcoDamageTable
    {
        public int damage1 = 0;
        public int damage2 = 20;
        public int damage3 = 30;
        public int damage4 = 40;
        public int damage5 = 50;
        public int damage6 = 60;

        public int GetDamage(int diceResult)
        {
            return diceResult switch
            {
                1 => damage1,
                2 => damage2,
                3 => damage3,
                4 => damage4,
                5 => damage5,
                6 => damage6,
                _ => 0
            };
        }
    }

    [Header("Line of Sight Settings (Multi-Raycast)")]
    [Tooltip("Define a 'largura' do personagem para a verificação de visão. É a distância do centro para cada um dos Raycasts laterais.")]
    [Range(0.001f, 0.1f)]
    public float raycastOffset = 0.1f;

    [Tooltip("Altura dos Raycasts acima do chão (simula altura dos freelancers)")]
    [Range(0.05f, 1f)]
    public float losRayHeight = 1.0f;

    [Tooltip("Layer mask de TODOS os obstáculos (Wall, Box, Door)")]
    public LayerMask losObstacleLayerMask = 1 << 6;

   [Header("Intro Animation Settings")]
[Tooltip("Duração da animação de introdução do tabuleiro (em segundos).")]
public float introAnimationDuration = 5.0f;

[Tooltip("Graus de rotação no eixo Y para a animação de introdução.")]
public float introAnimationYRotation = 720f;

    [Tooltip("Curva de animação para o scale e rotação, permitindo efeitos de ease-in/ease-out.")]
    public AnimationCurve introAnimationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

       [Header("Die Delay Settings")]

    [Tooltip("Duração (em segundos) que o resultado do dado fica visível antes da ação continuar.")]
    public float dieResultDisplayDuration = 1.2f;
    [Tooltip("Atraso (em segundos) entre o dado sumir e o tiro ser disparado.")]
    public float postDiceRollDelay = 0.1f;
    
        [Tooltip("Atraso (em segundos) entre o início da animação de tiro e o som do disparo.")]
    public float weaponSoundFireDelay = 0.2f; // <-- ADICIONAR ESTA LINHA






    [Header("Debug Settings")]
    
    [Header("Debug Settings")]
    [Tooltip("DEBUG: Se marcado, permite equipar múltiplas energias no mesmo freelancer por turno de preparação.")]
    public bool allowMultipleEquipsPerTurn = false;

    /// COMEÇO DO CÓDIGO A SER ADICIONADO
    [Tooltip("DEBUG: Se marcado, permite usar múltiplas skills no mesmo turno de um freelancer.")]
    public bool allowMultipleSkillsPerTurn = false;
    /// FIM DO CÓDIGO A SER ADICIONADO
    public bool enableCombatLogs = true;
    public bool enableMovementLogs = true;
    public bool enablePieceLogs = true;
    public bool enableCardLogs = true;
    public bool enableTurnLogs = true;
    public bool enableGridLogs = false;

    private static GameConfig instance;
   public static GameConfig Instance
    {
        get
        {
            if (instance == null)
            {
                /// COMEÇO DO TRECHO A SER MODIFICADO
                #if !UNITY_EDITOR
                AutoBuildMonitor.Log("GameConfig.Instance está sendo acessado pela primeira vez. Carregando de 'Resources'...");
                #endif
                instance = Resources.Load<GameConfig>("GameConfig");
                if (instance == null)
                {
                    #if !UNITY_EDITOR
                    AutoBuildMonitor.Log("FALHA CRÍTICA AO CARREGAR GAMECONFIG!");
                    #endif
                    Debug.LogError("FALHA CRÍTICA: 'GameConfig.asset' não foi encontrado!");
                    instance = CreateInstance<GameConfig>();
                }
                /// FIM DO TRECHO A SER MODIFICADO
            }
            return instance;
        }
    }
    
    // Valida altura
    public bool IsValidHeight(float height)
    {
        return height >= minTileHeight && height <= maxTileHeight;
    }
    
    // Aplica snap na altura
    public float SnapHeight(float height)
    {
        if (heightSnapIncrement <= 0f) return height;
        return Mathf.Round(height / heightSnapIncrement) * heightSnapIncrement;
    }
    
    // Grid utilities
    public Vector3 GetGridCenter()
    {
        return new Vector3(gridWidth / 2f - 0.5f, 0, gridHeight / 2f - 0.5f);
    }
    
    public Vector3 GetCameraPositionForGrid()
    {
        Vector3 center = GetGridCenter();
        return new Vector3(center.x, initialCameraPosition.y, center.z);
    }
    
    public bool IsValidGridPosition(int x, int z)
    {
        return x >= 0 && x < gridWidth && z >= 0 && z < gridHeight;
    }
    
    public bool IsValidGridPosition(Vector2Int pos)
    {
        return IsValidGridPosition(pos.x, pos.y);
    }
    
    // Highlight colors com transparência
    public Color GetMovementHighlightColor()
    {
        Color color = movementHighlightColor;
        color.a = highlightTransparency;
        return color;
    }
    
    public Color GetAttackHighlightColor()
    {
        Color color = attackHighlightColor;
        color.a = highlightTransparency;
        return color;
    }

    void OnValidate()
    {
        gridWidth = Mathf.Max(10, gridWidth);
        gridHeight = Mathf.Max(10, gridHeight);
        totalFreelancers = Mathf.Max(1, totalFreelancers);
        maxHandSize = Mathf.Max(1, maxHandSize);
        cardsDrawnPerPreparation = Mathf.Max(0, cardsDrawnPerPreparation);
        maxTotalTurns = Mathf.Max(1, maxTotalTurns);

        minTileHeight = Mathf.Max(0f, minTileHeight);
        maxTileHeight = Mathf.Max(minTileHeight + 0.1f, maxTileHeight);

        pistolRange = Mathf.Max(1, pistolRange);
        attackDelay = Mathf.Max(0f, attackDelay);
        boxDamageModifier = Mathf.Clamp01(boxDamageModifier);

        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        dodgeDistance = Mathf.Max(0f, dodgeDistance);
        dodgeDuration = Mathf.Max(0.1f, dodgeDuration);
        returnDuration = Mathf.Max(0.1f, returnDuration);

        defaultCameraRadius = Mathf.Max(1f, defaultCameraRadius);
        actionCameraRadius = Mathf.Max(1f, actionCameraRadius);
        cameraAnimationDuration = Mathf.Max(0.1f, cameraAnimationDuration);

        highlightTransparency = Mathf.Clamp01(highlightTransparency);
        highlightYOffset = Mathf.Max(0.001f, highlightYOffset);

         introAnimationDuration = Mathf.Max(0.1f, introAnimationDuration);
        introAnimationYRotation = Mathf.Max(0f, introAnimationYRotation);
        
        
    }
}