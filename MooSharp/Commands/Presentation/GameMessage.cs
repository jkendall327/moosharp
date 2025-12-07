using MooSharp.Actors;

namespace MooSharp.Messaging;

public record GameMessage(Player Player, IGameEvent Event, MessageAudience Audience = MessageAudience.Actor);