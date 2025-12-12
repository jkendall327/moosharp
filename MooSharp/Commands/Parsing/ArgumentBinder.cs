using System;
using MooSharp.Actors.Players;
using MooSharp.Actors.Rooms;
using MooSharp.Actors;
using MooSharp.Commands.Searching;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Commands.Parsing;

public class ArgumentBinder(TargetResolver resolver, World.World world)
{
    public BindingResult<ExamineResolution> BindExamineTarget(ParsingContext ctx)
    {
        if (ctx.IsFinished)
        {
            return BindingResult<ExamineResolution>.Success(
                new ExamineResolution(ExamineResolutionKind.Room, string.Empty));
        }

        var targetText = ctx.GetRemainingText();

        var playerMatch = ctx.Room.PlayersInRoom
            .FirstOrDefault(p => p != ctx.Player &&
                                  p.Username.Equals(targetText, StringComparison.OrdinalIgnoreCase));

        if (playerMatch is not null)
        {
            return BindingResult<ExamineResolution>.Success(
                new ExamineResolution(ExamineResolutionKind.Player, targetText)
                {
                    PlayerTarget = playerMatch
                });
        }

        var objectSearch = resolver.FindNearbyObject(ctx.Player, ctx.Room, targetText);

        if (objectSearch.IsSelf)
        {
            return BindingResult<ExamineResolution>.Success(
                new ExamineResolution(ExamineResolutionKind.Self, targetText));
        }

        switch (objectSearch.Status)
        {
            case SearchStatus.Found:
                return BindingResult<ExamineResolution>.Success(
                    new ExamineResolution(ExamineResolutionKind.Object, targetText)
                    {
                        ObjectTarget = objectSearch.Match!
                    });

            case SearchStatus.Ambiguous:
                return BindingResult<ExamineResolution>.Success(
                    new ExamineResolution(ExamineResolutionKind.AmbiguousObject, targetText)
                    {
                        ObjectCandidates = objectSearch.Candidates
                    });

            case SearchStatus.IndexOutOfRange:
                return BindingResult<ExamineResolution>.Success(
                    new ExamineResolution(ExamineResolutionKind.ObjectIndexOutOfRange, targetText));

            case SearchStatus.NotFound:
                var exitSearch = resolver.FindExit(ctx.Room, targetText);

                switch (exitSearch.Status)
                {
                    case SearchStatus.Found:
                        return BindingResult<ExamineResolution>.Success(
                            new ExamineResolution(ExamineResolutionKind.Exit, targetText)
                            {
                                ExitTarget = exitSearch.Match!
                            });

                    case SearchStatus.Ambiguous:
                        return BindingResult<ExamineResolution>.Success(
                            new ExamineResolution(ExamineResolutionKind.AmbiguousExit, targetText)
                            {
                                ExitCandidates = exitSearch.Candidates
                            });

                    case SearchStatus.IndexOutOfRange:
                        return BindingResult<ExamineResolution>.Success(
                            new ExamineResolution(ExamineResolutionKind.ExitIndexOutOfRange, targetText));

                    case SearchStatus.NotFound:
                        return BindingResult<ExamineResolution>.Success(
                            new ExamineResolution(ExamineResolutionKind.ItemNotFound, targetText));
                }

                break;
        }

        return BindingResult<ExamineResolution>.Failure("Unable to parse examine target.");
    }

    // Binds a token to an object in the player's inventory
    public BindingResult<Object> BindInventoryItem(ParsingContext ctx)
    {
        if (ctx.IsFinished)
        {
            return BindingResult<Object>.Failure("You didn't specify an item.");
        }

        var token = ctx.Pop()!;
        var search = resolver.FindObjects(ctx.Player.Inventory, token);

        return search.Status switch
        {
            SearchStatus.Found => BindingResult<Object>.Success(search.Match!),
            SearchStatus.NotFound => BindingResult<Object>.Failure($"You aren't carrying a '{token}'."),
            SearchStatus.Ambiguous => BindingResult<Object>.Failure($"Which '{token}' do you mean?"),
            var _ => BindingResult<Object>.Failure("Invalid item.")
        };
    }

    /// <summary>
    /// Attempts to find a player currently connected to the game (Global search).
    /// Used for 'Whisper'.
    /// </summary>
    public BindingResult<Player> BindOnlinePlayer(ParsingContext ctx)
    {
        if (ctx.IsFinished)
        {
            return BindingResult<Player>.Failure("You didn't specify a player.");
        }

        var token = ctx.Pop()!;

        // Exact match or Case-insensitive match on Username
        var target = world
            .GetActivePlayers()
            .FirstOrDefault(p => p.Username.Equals(token, StringComparison.OrdinalIgnoreCase));

        if (target is null)
        {
            return BindingResult<Player>.Failure($"Player '{token}' is not online.");
        }

        return BindingResult<Player>.Success(target);
    }

    public BindingResult<string> BindChannelName(ParsingContext ctx)
    {
        if (ctx.IsFinished)
        {
            return BindingResult<string>.Failure("You didn't specify a channel.");
        }

        var token = ctx.Pop()!;

        if (!Features.Chats.ChatChannels.IsValid(token))
        {
            return BindingResult<string>.Failure($"Channel '{token}' does not exist.");
        }

        return BindingResult<string>.Success(Features.Chats.ChatChannels.Normalize(token));
    }

    /// <summary>
    /// Attempts to find an item in the Inventory OR the Room.
    /// Used for 'Examine', 'Read'.
    /// </summary>
    public BindingResult<Object> BindNearbyObject(ParsingContext ctx)
    {
        if (ctx.IsFinished)
        {
            return BindingResult<Object>.Failure("You didn't specify an item.");
        }

        var token = ctx.Pop()!;

        // 1. Check Inventory
        var invSearch = resolver.FindObjects(ctx.Player.Inventory, token);

        if (invSearch.Status == SearchStatus.Found)
        {
            return BindingResult<Object>.Success(invSearch.Match!);
        }

        // 2. Check Room
        var roomSearch = resolver.FindObjects(ctx.Room.Contents, token);

        return roomSearch.Status switch
        {
            SearchStatus.Found => BindingResult<Object>.Success(roomSearch.Match!),
            SearchStatus.NotFound => BindingResult<Object>.Failure($"You don't see a '{token}' here."),
            SearchStatus.Ambiguous => BindingResult<Object>.Failure($"Which '{token}' do you mean?"),
            SearchStatus.IndexOutOfRange => BindingResult<Object>.Failure($"You don't see that many '{token}'s."),
            _ => BindingResult<Object>.Failure("Invalid item.")
        };
    }

    public BindingResult<IOpenable> BindOpenable(ParsingContext ctx)
    {
        if (ctx.IsFinished)
        {
            return BindingResult<IOpenable>.Failure("You didn't specify a target.");
        }

        var token = ctx.Pop()!;

        var search = resolver.FindOpenable(ctx.Player, ctx.Room, token);

        return search.Status switch
        {
            SearchStatus.Found => BindingResult<IOpenable>.Success(search.Match!),
            SearchStatus.NotFound => BindingResult<IOpenable>.Failure($"You don't see a '{token}' here."),
            SearchStatus.Ambiguous => BindingResult<IOpenable>.Failure($"Which '{token}' do you mean?"),
            SearchStatus.IndexOutOfRange => BindingResult<IOpenable>.Failure($"You don't see that many '{token}'s."),
            _ => BindingResult<IOpenable>.Failure("Invalid target.")
        };
    }

    public BindingResult<Exit> BindExitInRoom(ParsingContext ctx)
    {
        if (ctx.IsFinished)
        {
            return BindingResult<Exit>.Failure("You didn't specify an exit.");
        }

        var token = ctx.Pop()!;

        var search = resolver.FindExit(ctx.Room, token);

        return search.Status switch
        {
            SearchStatus.Found => BindingResult<Exit>.Success(search.Match!),
            SearchStatus.NotFound => BindingResult<Exit>.Failure("Exit not found."),
            SearchStatus.Ambiguous => BindingResult<Exit>.Failure($"Which '{token}' do you mean?"),
            SearchStatus.IndexOutOfRange => BindingResult<Exit>.Failure($"You don't see that many '{token}'s."),
            _ => BindingResult<Exit>.Failure("Exit not found.")
        };
    }

    public BindingResult<ILockable> BindLockable(ParsingContext ctx)
    {
        if (ctx.IsFinished)
        {
            return BindingResult<ILockable>.Failure("You didn't specify a target.");
        }

        var token = ctx.Pop()!;

        var search = resolver.FindOpenable(ctx.Player, ctx.Room, token);

        if (search is { Status: SearchStatus.Found, Match: ILockable lockable })
        {
            return BindingResult<ILockable>.Success(lockable);
        }

        if (search.Status == SearchStatus.Found)
        {
            return BindingResult<ILockable>.Failure("You can't lock that.");
        }

        return search.Status switch
        {
            SearchStatus.NotFound => BindingResult<ILockable>.Failure($"You don't see a '{token}' here."),
            SearchStatus.Ambiguous => BindingResult<ILockable>.Failure($"Which '{token}' do you mean?"),
            SearchStatus.IndexOutOfRange => BindingResult<ILockable>.Failure($"You don't see that many '{token}'s."),
            _ => BindingResult<ILockable>.Failure("Invalid target.")
        };
    }

    // Binds a token to a player in the room
    public BindingResult<Player> BindPlayerInRoom(ParsingContext ctx)
    {
        if (ctx.IsFinished)
        {
            return BindingResult<Player>.Failure("You didn't specify a person.");
        }

        var token = ctx.Pop()!;

        // Simple linear search for now, could use TargetResolver logic here too
        var target =
            ctx.Room.PlayersInRoom.FirstOrDefault(p => p.Username.Equals(token, StringComparison.OrdinalIgnoreCase));

        return target is not null
            ? BindingResult<Player>.Success(target)
            : BindingResult<Player>.Failure($"You don't see '{token}' here.");
    }

    // Consumes a preposition (syntactic sugar)
    public bool ConsumePreposition(ParsingContext ctx, string preposition)
    {
        if (ctx
                .Peek()
                ?.Equals(preposition, StringComparison.OrdinalIgnoreCase) == true)
        {
            ctx.Pop();

            return true;
        }

        return false;
    }
}

public enum ExamineResolutionKind
{
    Room,
    Self,
    Player,
    Object,
    Exit,
    AmbiguousObject,
    AmbiguousExit,
    ObjectIndexOutOfRange,
    ExitIndexOutOfRange,
    ItemNotFound
}

public record ExamineResolution(
    ExamineResolutionKind Kind,
    string TargetText)
{
    public Player? PlayerTarget { get; init; }
    public Exit? ExitTarget { get; init; }
    public Object? ObjectTarget { get; init; }
    public IReadOnlyCollection<Object> ObjectCandidates { get; init; } = Array.Empty<Object>();
    public IReadOnlyCollection<Exit> ExitCandidates { get; init; } = Array.Empty<Exit>();
}