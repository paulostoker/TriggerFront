// _Scripts/ObstacleProperties.cs - Versão com FindObjectOfType corrigido
using UnityEngine;

public class ObstacleProperties : MonoBehaviour
{
    [Header("Obstacle Data")]
    public ObstacleType obstacleType = ObstacleType.None;
    public int prefabIndex = 0;
    public int rotation = 0;
    
    [Header("Position Data")]
    public float obstacleYPosition = 0f;
    
    [Header("Behavior")]
    [SerializeField] private bool blocksMovement = true;
    [SerializeField] private bool blocksLineOfSight = true;
    [SerializeField] private bool isDestructible = false;
    [SerializeField] private int maxHealth = 100;
    
    private int currentHealth;
    
    void Start()
    {
        currentHealth = maxHealth;
        obstacleYPosition = transform.position.y;
        ConfigureByType();
    }
    
    void ConfigureByType()
    {
        switch (obstacleType)
        {
            case ObstacleType.Wall:
                blocksMovement = true;
                blocksLineOfSight = true;
                isDestructible = false;
                break;
                
            case ObstacleType.Box:
                blocksMovement = true;
                blocksLineOfSight = false;
                isDestructible = true;
                maxHealth = 50;
                break;
                
            case ObstacleType.Door:
                blocksMovement = false;
                blocksLineOfSight = true;
                isDestructible = true;
                maxHealth = 75;
                break;
        }
    }
    
    public void SetObstacleData(ObstacleType type, int prefabIdx, int rot)
    {
        obstacleType = type;
        prefabIndex = prefabIdx;
        rotation = rot;
        obstacleYPosition = transform.position.y;
        transform.rotation = Quaternion.Euler(0, rotation, 0);
        ConfigureByType();
    }
    
    public void SetYPosition(float yPos)
    {
        obstacleYPosition = yPos;
        Vector3 pos = transform.position;
        pos.y = yPos;
        transform.position = pos;
    }
    
    public float GetYPosition()
    {
        return transform.position.y;
    }
    
    public bool BlocksMovement()
    {
        return blocksMovement;
    }
    
    public bool BlocksLineOfSight()
    {
        return blocksLineOfSight;
    }
    
    public bool IsDestructible()
    {
        return isDestructible;
    }
    
    public void TakeDamage(int damage)
    {
        if (!isDestructible) return;
        
        currentHealth -= damage;
        
        if (currentHealth <= 0)
        {
            DestroyObstacle();
        }
        else
        {
            StartCoroutine(DamageFlash());
        }
    }
    
    void DestroyObstacle()
    {
        Vector2Int gridPos = GetGridPosition();
        
        // --- CORREÇÃO DO AVISO ---
        GridManager gridManager = FindFirstObjectByType<GridManager>();
        
        if (gridManager != null && gridManager.obstacles.ContainsKey(gridPos))
        {
            gridManager.obstacles.Remove(gridPos);
        }
        
        Destroy(gameObject);
    }
    
    System.Collections.IEnumerator DamageFlash()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Color originalColor = renderer.material.color;
            renderer.material.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            renderer.material.color = originalColor;
        }
    }
    
    public Vector2Int GetGridPosition()
    {
        Vector3 worldPos = transform.position;
        return new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.z));
    }
    
    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }
    
    #region DEBUG
    
    [ContextMenu("Debug - Obstacle Info")]
    void DebugObstacleInfo()
    {
        Debug.Log($"=== OBSTACLE DEBUG ===");
        Debug.Log($"GameObject: {gameObject.name}");
        Debug.Log($"Type: {obstacleType}");
        Debug.Log($"Position: {transform.position}");
        Debug.Log($"Y Position: {obstacleYPosition:F3}");
        Debug.Log($"Grid Position: {GetGridPosition()}");
        Debug.Log($"Blocks Movement: {blocksMovement}");
        Debug.Log($"Blocks Line of Sight: {blocksLineOfSight}");
        Debug.Log($"Destructible: {isDestructible}");
        Debug.Log($"Health: {currentHealth}/{maxHealth}");
    }
    
    void OnDrawGizmosSelected()
    {
        if (blocksMovement)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.9f);
        }
        
        if (blocksLineOfSight)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
        }
        
        #if UNITY_EDITOR
        string info = $"{obstacleType}\nY: {transform.position.y:F3}";
        if (isDestructible)
        {
            info += $"\nHP: {currentHealth}/{maxHealth}";
        }
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1f, info);
        #endif
    }
    
    #endregion
}