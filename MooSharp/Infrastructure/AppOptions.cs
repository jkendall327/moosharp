using System.ComponentModel.DataAnnotations;

namespace MooSharp;

public class AppOptions
{
    public bool EnableAgents { get; init; }
    
    [Required]
    public required string WorldDataFilepath { get; init; }
}