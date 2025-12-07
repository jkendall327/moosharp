namespace MooSharp.Actors.Objects;

/// <summary>
/// Represents a single verb script attached to an object.
/// </summary>
public record VerbScript(
    string VerbName,
    string LuaCode,
    string? CreatorUsername = null,
    DateTime? CreatedAt = null)
{
    public static VerbScript Create(string verbName, string luaCode, string? creatorUsername = null)
    {
        return new(verbName, luaCode, creatorUsername, DateTime.UtcNow);
    }

    public static VerbScript CreateStub(string verbName, string? creatorUsername = null)
    {
        var stubCode = $"""
            -- Verb: {verbName}
            -- TODO: Add your Lua code here
            game.tell(actor.Name, "This verb is not yet implemented.")
            """;

        return Create(verbName, stubCode, creatorUsername);
    }
}
