using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooSharp.Commands;
using MooSharp.Commands.Commands;
using MooSharp.Infrastructure;
using MooSharp.Messaging;

namespace MooSharp.Actors;

public class PlayerMessageProvider(
    World.World world,
    IOptionsMonitor<AppOptions> appOptions,
    ILogger<PlayerMessageProvider> logger)
{
    private string MessageOfTheDay => (appOptions.CurrentValue.Motd ?? string.Empty).Trim();

    public Task<List<GameMessage>> GetMessagesForLogin(Player player, CancellationToken ct = default)
    {
        var description = BuildCurrentRoomDescription(player);

        var messages = new List<GameMessage>
        {
            new(player, new SystemMessageEvent($"Welcome, {player.Username}."))
        };

        AddMotdMessage(messages, player);

        messages.Add(new(player, new RoomDescriptionEvent(description.ToString())));

        return Task.FromResult(messages);
    }

    private void AddMotdMessage(List<GameMessage> messages, Player player)
    {
        var motd = MessageOfTheDay;

        if (string.IsNullOrWhiteSpace(motd))
        {
            return;
        }

        messages.Add(new(player, new SystemMessageEvent(motd)));
    }

    private StringBuilder BuildCurrentRoomDescription(Player player)
    {
        var sb = new StringBuilder();

        var room = world.GetPlayerLocation(player);

        if (room is null)
        {
            logger.LogWarning("Player {Player} has no known location when building description", player.Username);

            sb.AppendLine("You are nowhere.");

            return sb;
        }

        sb.Append(room.DescribeFor(player));

        return sb;
    }
}