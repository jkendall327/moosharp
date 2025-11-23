using System.ComponentModel.DataAnnotations;

namespace MooSharp;

public class AppOptions
{
    [Required]
    public required string WorldDataFilepath { get; init; }

    [Required]
    public required string PlayerDataFilepath { get; init; }
}