namespace MooSharp.Features.Chats;

public static class ChatChannels
{
    public const string Global = "Global";
    public const string Newbie = "Newbie";
    public const string Trade = "Trade";

    private static readonly HashSet<string> Channels = new(StringComparer.OrdinalIgnoreCase)
    {
        Global,
        Newbie,
        Trade
    };

    public static bool IsValid(string channel) => Channels.Contains(channel);

    public static string Normalize(string channel)
    {
        var match = Channels.FirstOrDefault(c => c.Equals(channel, StringComparison.OrdinalIgnoreCase));

        return match ?? channel;
    }
}
