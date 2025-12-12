using System;

namespace MooSharp.Actors.Players;

public enum PlayerActivityStatus
{
    Active,
    Idle,
    Away
}

public readonly record struct PlayerActivityState(PlayerActivityStatus Status, TimeSpan IdleTime);

public static class PlayerActivityHelper
{
    public static PlayerActivityState GetActivityState(Player player, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(player);

        var idleTime = utcNow - player.LastActionAt;
        var clampedIdleTime = idleTime < TimeSpan.Zero ? TimeSpan.Zero : idleTime;

        if (clampedIdleTime > TimeSpan.FromMinutes(30))
        {
            return new PlayerActivityState(PlayerActivityStatus.Away, clampedIdleTime);
        }

        if (clampedIdleTime > TimeSpan.FromMinutes(10))
        {
            return new PlayerActivityState(PlayerActivityStatus.Idle, clampedIdleTime);
        }

        return new PlayerActivityState(PlayerActivityStatus.Active, clampedIdleTime);
    }

    public static string FormatStatusLabel(PlayerActivityState activityState)
    {
        return activityState.Status switch
        {
            PlayerActivityStatus.Away => $"Away for {FormatMinutes(activityState.IdleTime)}",
            PlayerActivityStatus.Idle => $"Idle for {FormatMinutes(activityState.IdleTime)}",
            _ => "Active"
        };
    }

    public static string FormatInlineStatus(PlayerActivityState activityState)
    {
        return activityState.Status switch
        {
            PlayerActivityStatus.Away => $"Away ({FormatMinutes(activityState.IdleTime)})",
            PlayerActivityStatus.Idle => $"Idle ({FormatMinutes(activityState.IdleTime)})",
            _ => "Active"
        };
    }

    private static string FormatMinutes(TimeSpan idleTime)
    {
        var minutes = Math.Max(0, (int)Math.Floor(idleTime.TotalMinutes));
        return $"{minutes}m";
    }
}
