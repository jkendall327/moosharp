using MooSharp.Actors.Players;

namespace MooSharp.Commands.Presentation;

public record GameMessage(Player Player, IGameEvent Event, MessageAudience Audience = MessageAudience.Actor);