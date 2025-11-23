using System.Text.Json;
using Microsoft.Extensions.Options;
using MooSharp;

namespace MooSharp.Persistence;

public interface IPlayerStore
{
    Task SaveNewPlayer(Player player, Room currentLocation, string password);
    Task SavePlayer(Player player, Room currentLocation);
    Task<PlayerDto?> LoadPlayer(LoginCommand command);
}

public class JsonPlayerStore : IPlayerStore
{
    private readonly SemaphoreSlim _sync = new(1, 1);
    private readonly string _filePath;
    private readonly List<PlayerDto> _players;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new RoomIdJsonConverter() }
    };

    public JsonPlayerStore(IOptions<AppOptions> options)
    {
        _filePath = options.Value.PlayerDataFilepath
            ?? throw new InvalidOperationException("PlayerDataFilepath is not set.");

        _players = LoadPlayersFromDisk();
    }

    public async Task SaveNewPlayer(Player player, Room currentLocation, string password)
    {
        var dto = new PlayerDto
        {
            Username = player.Username,
            Password = password,
            CurrentLocation = currentLocation.Id
        };

        await UpsertPlayerAsync(dto);
    }

    public async Task SavePlayer(Player player, Room currentLocation)
    {
        await _sync.WaitAsync();

        try
        {
            var existingPlayer = _players.SingleOrDefault(s => s.Username == player.Username);

            if (existingPlayer is null)
            {
                return;
            }

            existingPlayer.CurrentLocation = currentLocation.Id;

            await PersistAsync();
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<PlayerDto?> LoadPlayer(LoginCommand command)
    {
        await _sync.WaitAsync();

        try
        {
            var player = _players.SingleOrDefault(s => s.Username == command.Username && s.Password == command.Password);

            return player;
        }
        finally
        {
            _sync.Release();
        }
    }

    private List<PlayerDto> LoadPlayersFromDisk()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        var raw = File.ReadAllText(_filePath);

        var players = JsonSerializer.Deserialize<List<PlayerDto>>(raw, _serializerOptions);

        return players ?? [];
    }

    private async Task UpsertPlayerAsync(PlayerDto player)
    {
        await _sync.WaitAsync();

        try
        {
            var existingPlayer = _players.SingleOrDefault(s => s.Username == player.Username);

            if (existingPlayer is null)
            {
                _players.Add(player);
            }
            else
            {
                existingPlayer.Password = player.Password;
                existingPlayer.CurrentLocation = player.CurrentLocation;
            }

            await PersistAsync();
        }
        finally
        {
            _sync.Release();
        }
    }

    private async Task PersistAsync()
    {
        var directory = Path.GetDirectoryName(_filePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(_filePath, JsonSerializer.Serialize(_players, _serializerOptions));
    }
}