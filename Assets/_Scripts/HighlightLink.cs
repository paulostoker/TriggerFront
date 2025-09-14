// _Scripts/HighlightLink.cs

using UnityEngine;

/// <summary>
/// Este script simples serve como uma ponte de dados, ligando um 
/// GameObject de highlight visual (que pode ser clicado) ao seu 
/// Tile lógico correspondente no grid.
/// </summary>
public class HighlightLink : MonoBehaviour
{
    public Tile linkedTile;
}