using UnityEngine;

public enum GameMode { Offline, OnlineHost, OnlineClient }

public class GameInitializer : MonoBehaviour
{
    public static GameMode Mode = GameMode.Offline;
    public static int SelectedMapId = 0;

    public static void SetOffline() { Mode = GameMode.Offline; }
    public static void SetOnlineHost() { Mode = GameMode.OnlineHost; }
    public static void SetOnlineClient() { Mode = GameMode.OnlineClient; }
    public static void SetMap(int mapId) { SelectedMapId = mapId; }
}
