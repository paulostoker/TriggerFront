// _Scripts/MapData.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewMapData", menuName = "Game/Map Data")]
public class MapData : ScriptableObject
{
    public List<string> layout;
    public List<Vector2Int> player1SpawnPoints;
    public List<Vector2Int> player2SpawnPoints;
}