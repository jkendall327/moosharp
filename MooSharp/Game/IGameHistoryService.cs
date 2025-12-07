namespace MooSharp.Game;

public interface IGameHistoryService
{
    IReadOnlyList<string> CommandHistory { get; }
    Task InitializeAsync();
    void AddCommand(string command);
    Task PersistAsync();
}