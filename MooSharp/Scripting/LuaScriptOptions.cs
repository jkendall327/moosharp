using System.ComponentModel.DataAnnotations;

namespace MooSharp.Scripting;

public class LuaScriptOptions
{
    public const string SectionName = "LuaScripting";

    [Range(1000, 1_000_000)]
    public int InstructionLimit { get; set; } = 10_000;

    [Range(100, 30_000)]
    public int TimeoutMilliseconds { get; set; } = 2000;

    [Range(1, 1000)]
    public int MaxPropertiesPerObject { get; set; } = 100;

    [Range(1, 200)]
    public int MaxVerbsPerObject { get; set; } = 50;
}
