// _Scripts/IGameState.cs
public interface IGameState
{
    void Enter();    // Ações ao entrar no estado
    void Execute();  // Ações a cada frame (no Update)
    void Exit();     // Ações ao sair do estado
}