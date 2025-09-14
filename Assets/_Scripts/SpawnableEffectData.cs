using UnityEngine;

  

[CreateAssetMenu(fileName = "NewSpawnableEffect", menuName = "Game/Spawnable Effect Data")]
public class SpawnableEffectData : ScriptableObject
{
    [Header("Core Properties")]
    [Tooltip("O prefab que será instanciado no mapa (ex: a nuvem de fumaça, o muro).")]
    public GameObject effectPrefab;
    
    [Tooltip("Quantos turnos do jogador o efeito permanece ativo no tabuleiro. Use um número alto (ex: 99) para 'até ser destruído'.")]
    public int durationTurns;


    
    [Header("Behavior Properties")]
    [Tooltip("Este objeto bloqueia o movimento dos freelancers?")]
    public bool blocksMovement;

    [Tooltip("Este objeto bloqueia a linha de visão para ataques?")]
    public bool blocksLineOfSight;

    [Header("Combat Properties")]
    [Tooltip("Este objeto pode ser alvo de ataques e receber dano?")]
    public bool isDestructible;

    [Tooltip("A vida máxima do objeto, se for destrutível.")]
    public int maxHP;
}