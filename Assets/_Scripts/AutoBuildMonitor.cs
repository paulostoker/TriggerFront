// _Scripts/AutoBuildMonitor.cs
using UnityEngine;
using System.IO;

// Este script só funcionará em uma build final, ele se autodestruirá no Editor.
public class AutoBuildMonitor : MonoBehaviour
{
    private static string logFilePath;

    void Awake()
    {
        // Só executa em builds, não no editor.
        #if UNITY_EDITOR
        Destroy(this);
        return;
        #endif

        // Garante que o monitor não seja destruído ao carregar novas cenas.
        DontDestroyOnLoad(gameObject);
        
        // Define o caminho do arquivo de log
        logFilePath = Path.Combine(Application.persistentDataPath, "build_log.txt");
        
        // Limpa o log antigo e registra o início
        File.WriteAllText(logFilePath, $"====== LOG DE BUILD INICIADO EM {System.DateTime.Now} ======\n\n");

        // Registra exceções não tratadas no log
        Application.logMessageReceived += HandleLog;
        
        Log("AutoBuildMonitor ATIVADO. O log será salvo em: " + logFilePath);
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Se for um erro ou exceção, registra no arquivo com detalhes.
        if (type == LogType.Error || type == LogType.Exception)
        {
            File.AppendAllText(logFilePath, $"[{type}] {logString}\nStackTrace:\n{stackTrace}\n\n");
        }
    }

    // Método estático para que outros scripts possam escrever no nosso log.
    public static void Log(string message)
    {
        // Só funciona em builds.
        #if !UNITY_EDITOR
        if (string.IsNullOrEmpty(logFilePath)) return;
        try
        {
            File.AppendAllText(logFilePath, $"[{Time.time:F2}s] {message}\n");
        }
        catch (System.Exception)
        {
            // Ignora erros se não conseguir escrever no arquivo.
        }
        #endif
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }
}